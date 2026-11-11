using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ProjetoClubes.IntegrationTests.Infra;

namespace ProjetoClubes.IntegrationTests;

/// <summary>Critérios de aceitação 10 e 11 e regras do módulo de dependentes (seção 8).</summary>
[Collection(ColecaoApi.Nome)]
public class DependentesTests(ApiFixture api)
{
    private readonly Cenario _c = new(api);

    private async Task<(Sessao Presidente, Guid TitularId, string Matricula)> CenarioAsync(string categoria = "Sócio associado")
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var p = await _c.LoginAsync(clube.EmailPresidente);
        var a = await _c.CriarAssociadoAsync(p.AccessToken, categoria);
        return (p, a.GetProperty("id").GetGuid(), a.GetProperty("matricula").GetString()!);
    }

    private async Task AtualizarConfigAsync(string token, Action<Dictionary<string, object?>> alterar)
    {
        var cliente = api.Cliente(token);
        var atual = await cliente.GetFromJsonAsync<Dictionary<string, object?>>("/api/v1/dependentes/configuracoes", Cenario.Json);
        alterar(atual!);
        var r = await cliente.PutAsJsonAsync("/api/v1/dependentes/configuracoes", atual);
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Inclusao_numera_carteirinha_e_aplica_permissoes_iniciais()
    {
        var (p, titular, matricula) = await CenarioAsync();
        var d1 = await _c.CriarDependenteAsync(p.AccessToken, titular, "Conjuge", 40);
        var d2 = await _c.CriarDependenteAsync(p.AccessToken, titular, "Filho", 9);
        Assert.Equal($"{matricula}-01", d1.GetProperty("numeroIdentificacao").GetString());
        Assert.Equal($"{matricula}-02", d2.GetProperty("numeroIdentificacao").GetString());
        Assert.True(d2.GetProperty("menorDeIdade").GetBoolean());
        Assert.False(d2.GetProperty("permissoes").GetProperty("solicitacaoReservas").GetBoolean());
        Assert.True(d1.GetProperty("permissoes").GetProperty("solicitacaoReservas").GetBoolean());

        var detalhe = await api.Cliente(p.AccessToken).GetFromJsonAsync<JsonElement>($"/api/v1/associados/{titular}", Cenario.Json);
        Assert.Equal(2, detalhe.GetProperty("dependentes").GetArrayLength());
    }

    [Fact]
    public async Task Regras_do_clube_limite_idade_parentesco_conjuge_e_categoria()
    {
        var (p, titular, _) = await CenarioAsync();
        await AtualizarConfigAsync(p.AccessToken, c =>
        {
            c["limitePadraoDependentes"] = 3;
            c["idadeMaximaFilhos"] = 21;
            c["parentescosPermitidos"] = new[] { "Conjuge", "Filho", "Pai", "Mae" };
        });

        async Task Esperar(string codigo, string parentesco, int idade)
        {
            var r = await _c.PostDependenteAsync(p.AccessToken, titular, parentesco, idade);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);
            Assert.Equal(codigo, await Cenario.CodigoProblemaAsync(r));
        }

        await Esperar("dependente.idade_excedida", "Filho", 22);
        await Esperar("dependente.parentesco_nao_permitido", "Irmao", 15);
        await _c.CriarDependenteAsync(p.AccessToken, titular, "Conjuge", 40);
        await Esperar("dependente.limite_conjuge", "Conjuge", 38);
        await _c.CriarDependenteAsync(p.AccessToken, titular, "Filho", 5);
        await _c.CriarDependenteAsync(p.AccessToken, titular, "Mae", 70);
        await Esperar("dependente.limite_atingido", "Filho", 3);

        var (p2, simpatizante, _) = await CenarioAsync("Simpatizante");
        var r2 = await _c.PostDependenteAsync(p2.AccessToken, simpatizante);
        Assert.Equal("dependente.categoria_sem_dependentes", await Cenario.CodigoProblemaAsync(r2));
    }

    [Fact]
    public async Task Mesma_pessoa_em_dois_titulares_so_com_excecao_autorizada()
    {
        var (p, titularA, _) = await CenarioAsync();
        var titularB = (await _c.CriarAssociadoAsync(p.AccessToken)).GetProperty("id").GetGuid();
        var cpf = Cenario.NovoCpf();

        await _c.CriarDependenteAsync(p.AccessToken, titularA, "Filho", 10, cpf);
        var semExcecao = await _c.PostDependenteAsync(p.AccessToken, titularB, "Filho", 10, cpf);
        Assert.Equal(HttpStatusCode.Conflict, semExcecao.StatusCode);
        Assert.Equal("dependente.outro_titular", await Cenario.CodigoProblemaAsync(semExcecao));

        var comExcecao = await _c.PostDependenteAsync(p.AccessToken, titularB, "Filho", 10, cpf,
            justificativa: "Guarda compartilhada — pais separados, ambos associados.");
        Assert.Equal(HttpStatusCode.Created, comExcecao.StatusCode);
        var d = await comExcecao.Content.ReadFromJsonAsync<JsonElement>(Cenario.Json);
        Assert.True(d.GetProperty("excecaoMultiplosTitulares").GetBoolean());

        var mesmoTitular = await _c.PostDependenteAsync(p.AccessToken, titularA, "Filho", 10, cpf, justificativa: "x");
        Assert.Equal(HttpStatusCode.Conflict, mesmoTitular.StatusCode);
    }

    [Fact]
    public async Task Titular_nao_pode_ser_dependente_e_associado_nao_vira_dependente()
    {
        var (p, titular, _) = await CenarioAsync();
        var outro = await _c.CriarAssociadoAsync(p.AccessToken);
        var cpfOutro = outro.GetProperty("pessoa").GetProperty("cpf").GetString();
        var r = await _c.PostDependenteAsync(p.AccessToken, titular, "Conjuge", 40, cpfOutro);
        Assert.Equal("dependente.pessoa_associada", await Cenario.CodigoProblemaAsync(r));
    }

    [Fact]
    public async Task Aprovacao_rejeicao_transferencia_e_historico()
    {
        var (p, titularA, _) = await CenarioAsync();
        var b = await _c.CriarAssociadoAsync(p.AccessToken);
        var titularB = b.GetProperty("id").GetGuid();
        var cliente = api.Cliente(p.AccessToken);

        // Usuário sem permissão de aprovar inclui → pendente.
        await cliente.PostAsJsonAsync("/api/v1/perfis", new { nome = "Recepção", permissoes = new[] { "dependentes.visualizar", "dependentes.cadastrar" } });
        var (_, email) = await _c.CriarUsuarioAsync(p, "Recepção");
        var recepcao = await _c.LoginAsync(email);
        var pendente = await _c.CriarDependenteAsync(recepcao.AccessToken, titularA, "Filho", 7);
        Assert.Equal("Pendente", pendente.GetProperty("situacao").GetString());
        var id = pendente.GetProperty("id").GetGuid();

        var semMotivo = await cliente.PostAsJsonAsync($"/api/v1/dependentes/{id}/rejeitar", new { motivo = (string?)null });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, semMotivo.StatusCode);
        var aprovado = await (await cliente.PostAsync($"/api/v1/dependentes/{id}/aprovar", null)).Content.ReadFromJsonAsync<JsonElement>(Cenario.Json);
        Assert.Equal("Ativo", aprovado.GetProperty("situacao").GetString());

        var t = await cliente.PostAsJsonAsync($"/api/v1/dependentes/{id}/transferir", new
        {
            novoTitularId = titularB, parentesco = "Filho", motivo = "Mudança de guarda",
        });
        Assert.True(t.StatusCode == HttpStatusCode.OK, await t.Content.ReadAsStringAsync());
        var transferido = await t.Content.ReadFromJsonAsync<JsonElement>(Cenario.Json);
        Assert.Equal(titularB, transferido.GetProperty("titular").GetProperty("id").GetGuid());
        Assert.StartsWith(b.GetProperty("matricula").GetString()!, transferido.GetProperty("numeroIdentificacao").GetString());
        var vinculos = transferido.GetProperty("vinculos").EnumerateArray().ToList();
        Assert.Equal(2, vinculos.Count);
        Assert.NotEqual(JsonValueKind.Null, vinculos[0].GetProperty("fim").ValueKind);

        var hist = await cliente.GetFromJsonAsync<JsonElement>($"/api/v1/dependentes/{id}/historico", Cenario.Json);
        var eventos = hist.EnumerateArray().Select(h => h.GetProperty("evento").GetString()).ToList();
        Assert.Contains("Inclusao", eventos);
        Assert.Contains("Aprovar", eventos);
        Assert.Contains("Transferencia", eventos);

        Assert.Equal(HttpStatusCode.NoContent, (await cliente.DeleteAsync($"/api/v1/dependentes/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync($"/api/v1/dependentes/{id}")).StatusCode);
    }

    [Fact]
    public async Task Permissoes_do_dependente_e_regra_de_menores()
    {
        var (p, titular, _) = await CenarioAsync();
        var crianca = await _c.CriarDependenteAsync(p.AccessToken, titular, "Filho", 8);
        var id = crianca.GetProperty("id").GetGuid();
        var cliente = api.Cliente(p.AccessToken);

        var permissoes = new
        {
            acessoClube = true, participacaoEventos = true, usoEspacosEsportivos = false, inscricaoAtividades = true,
            solicitacaoReservas = true, recebimentoComunicados = true, utilizacaoServicos = true,
        };
        var r = await cliente.PutAsJsonAsync($"/api/v1/dependentes/{id}/permissoes", permissoes);
        Assert.Equal("dependente.menor_reservas", await Cenario.CodigoProblemaAsync(r));

        var ok = await cliente.PutAsJsonAsync($"/api/v1/dependentes/{id}/permissoes", permissoes with { solicitacaoReservas = false });
        var d = await ok.Content.ReadFromJsonAsync<JsonElement>(Cenario.Json);
        Assert.False(d.GetProperty("permissoes").GetProperty("usoEspacosEsportivos").GetBoolean());
    }

    [Fact]
    public async Task Clube_que_exige_documento_so_aprova_com_documento_anexado()
    {
        var (p, titular, _) = await CenarioAsync();
        await AtualizarConfigAsync(p.AccessToken, c => c["exigeDocumentoComprobatorio"] = true);
        var d = await _c.CriarDependenteAsync(p.AccessToken, titular, "Conjuge", 40);
        Assert.Equal("Pendente", d.GetProperty("situacao").GetString()); // documento exigido: sempre passa por aprovação
        var id = d.GetProperty("id").GetGuid();
        var cliente = api.Cliente(p.AccessToken);

        var sem = await cliente.PostAsync($"/api/v1/dependentes/{id}/aprovar", null);
        Assert.Equal("dependente.documento_obrigatorio", await Cenario.CodigoProblemaAsync(sem));

        await ArquivosTests.EnviarDocumentoAsync(api, p.AccessToken, d.GetProperty("pessoa").GetProperty("id").GetGuid());
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsync($"/api/v1/dependentes/{id}/aprovar", null)).StatusCode);
    }
}
