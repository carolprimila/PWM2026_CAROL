using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ProjetoClubes.IntegrationTests.Infra;

namespace ProjetoClubes.IntegrationTests;

/// <summary>Critérios de aceitação 8, 9, 13 e 26 (categorias, associados, situação, isolamento).</summary>
[Collection(ColecaoApi.Nome)]
public class AssociadosTests(ApiFixture api)
{
    private readonly Cenario _c = new(api);

    [Fact]
    public async Task Clube_novo_recebe_categorias_interesses_e_regras_de_dependentes()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var p = await _c.LoginAsync(clube.EmailPresidente);
        var cliente = api.Cliente(p.AccessToken);

        var categorias = await cliente.GetFromJsonAsync<JsonElement>("/api/v1/categorias-associados", Cenario.Json);
        var fundador = categorias.EnumerateArray().Single(c => c.GetProperty("nome").GetString() == "Sócio fundador");
        var simpatizante = categorias.EnumerateArray().Single(c => c.GetProperty("nome").GetString() == "Simpatizante");
        Assert.True(fundador.GetProperty("direitoVoto").GetBoolean());
        Assert.False(simpatizante.GetProperty("direitoVoto").GetBoolean());
        Assert.False(simpatizante.GetProperty("permiteDependentes").GetBoolean());

        var interesses = await cliente.GetFromJsonAsync<JsonElement>("/api/v1/interesses", Cenario.Json);
        Assert.Contains(interesses.EnumerateArray(), i => i.GetProperty("nome").GetString() == "Pesca");

        var cfg = await cliente.GetFromJsonAsync<JsonElement>("/api/v1/dependentes/configuracoes", Cenario.Json);
        Assert.Equal(5, cfg.GetProperty("limitePadraoDependentes").GetInt32());
    }

    [Fact]
    public async Task Cadastro_gera_matricula_sequencial_e_bloqueia_cpf_duplicado()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var p = await _c.LoginAsync(clube.EmailPresidente);
        var cliente = api.Cliente(p.AccessToken);
        var categoria = await _c.CategoriaIdAsync(p.AccessToken, "Sócio fundador");
        var cpf = Cenario.NovoCpf();
        var pesca = (await cliente.GetFromJsonAsync<JsonElement>("/api/v1/interesses", Cenario.Json)).EnumerateArray()
            .First(i => i.GetProperty("nome").GetString() == "Pesca").GetProperty("id").GetGuid();

        var r1 = await cliente.PostAsJsonAsync("/api/v1/associados", new
        {
            pessoa = Cenario.DadosPessoa("Maria das Dores Silva", cpf), categoriaId = categoria, interesseIds = new[] { pesca },
        });
        Assert.Equal(HttpStatusCode.Created, r1.StatusCode);
        var a1 = await r1.Content.ReadFromJsonAsync<JsonElement>(Cenario.Json);
        Assert.Equal("000001", a1.GetProperty("matricula").GetString());
        Assert.Equal("Ativo", a1.GetProperty("situacao").GetString()); // presidente pode aprovar: dispensa etapa
        Assert.True(a1.GetProperty("aptoAVotar").GetBoolean());
        Assert.Equal("Pesca", a1.GetProperty("interesses")[0].GetProperty("nome").GetString());

        var a2 = await _c.CriarAssociadoAsync(p.AccessToken, "Sócio convidado");
        Assert.Equal("000002", a2.GetProperty("matricula").GetString());
        Assert.False(a2.GetProperty("aptoAVotar").GetBoolean()); // direito a voto vem da configuração da categoria

        var duplicado = await cliente.PostAsJsonAsync("/api/v1/associados", new
        {
            pessoa = Cenario.DadosPessoa("Outra Pessoa Qualquer", cpf), categoriaId = categoria,
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicado.StatusCode);

        var invalido = await cliente.PostAsJsonAsync("/api/v1/associados", new
        {
            pessoa = Cenario.DadosPessoa("Nome Valido Silva", "111.111.111-11"), categoriaId = categoria,
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);
        Assert.True((await invalido.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").TryGetProperty("pessoa.cpf", out _));

        // Busca por CPF (dígitos) e listagem com CPF mascarado.
        var busca = await cliente.GetFromJsonAsync<JsonElement>($"/api/v1/associados?busca={cpf[..6]}", Cenario.Json);
        var item = busca.GetProperty("itens").EnumerateArray().Single();
        Assert.Equal("Maria das Dores Silva", item.GetProperty("nome").GetString());
        Assert.DoesNotContain(cpf, item.GetProperty("cpfMascarado").GetString());
    }

    [Fact]
    public async Task Cadastro_por_quem_nao_aprova_fica_pendente_ate_aprovacao()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var presidente = await _c.LoginAsync(clube.EmailPresidente);
        await api.Cliente(presidente.AccessToken).PostAsJsonAsync("/api/v1/perfis", new
        {
            nome = "Atendimento", permissoes = new[] { "associados.visualizar", "associados.cadastrar" },
        });
        var (_, email) = await _c.CriarUsuarioAsync(presidente, "Atendimento");
        var atendente = await _c.LoginAsync(email);

        var a = await _c.CriarAssociadoAsync(atendente.AccessToken);
        Assert.Equal("Pendente", a.GetProperty("situacao").GetString());
        var id = a.GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.Forbidden, (await api.Cliente(atendente.AccessToken).PostAsync($"/api/v1/associados/{id}/aprovar", null)).StatusCode);
        var aprovado = await api.Cliente(presidente.AccessToken).PostAsync($"/api/v1/associados/{id}/aprovar", null);
        Assert.Equal("Ativo", (await aprovado.Content.ReadFromJsonAsync<JsonElement>(Cenario.Json)).GetProperty("situacao").GetString());

        var historico = await api.Cliente(presidente.AccessToken).GetFromJsonAsync<JsonElement>($"/api/v1/associados/{id}/historico", Cenario.Json);
        var eventos = historico.EnumerateArray().Select(h => h.GetProperty("evento").GetString()).ToList();
        Assert.Contains("Cadastro", eventos);
        Assert.Contains("Aprovar", eventos);
    }

    [Fact]
    public async Task Suspensao_exige_motivo_e_inativar_titular_inativa_dependentes()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var p = await _c.LoginAsync(clube.EmailPresidente);
        var cliente = api.Cliente(p.AccessToken);
        var a = await _c.CriarAssociadoAsync(p.AccessToken);
        var id = a.GetProperty("id").GetGuid();
        var dep = await _c.CriarDependenteAsync(p.AccessToken, id);

        var semMotivo = await cliente.PostAsJsonAsync($"/api/v1/associados/{id}/suspender", new { motivo = "" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, semMotivo.StatusCode);
        Assert.Equal("situacao.motivo_obrigatorio", await Cenario.CodigoProblemaAsync(semMotivo));

        var inativar = await cliente.PostAsJsonAsync($"/api/v1/associados/{id}/inativar", new { motivo = "Pedido de desligamento" });
        Assert.Equal("Inativo", (await inativar.Content.ReadFromJsonAsync<JsonElement>(Cenario.Json)).GetProperty("situacao").GetString());
        var d = await cliente.GetFromJsonAsync<JsonElement>($"/api/v1/dependentes/{dep.GetProperty("id").GetGuid()}", Cenario.Json);
        Assert.Equal("Inativo", d.GetProperty("situacao").GetString());

        var reativar = await cliente.PostAsync($"/api/v1/associados/{id}/reativar", null);
        Assert.Equal(HttpStatusCode.OK, reativar.StatusCode);
    }

    [Fact]
    public async Task Associados_de_um_clube_sao_invisiveis_para_outro()
    {
        var a = await _c.CriarClubeComPresidenteAsync();
        var b = await _c.CriarClubeComPresidenteAsync();
        var pa = await _c.LoginAsync(a.EmailPresidente);
        var pb = await _c.LoginAsync(b.EmailPresidente);
        var associadoA = await _c.CriarAssociadoAsync(pa.AccessToken);
        var idA = associadoA.GetProperty("id").GetGuid();
        var pessoaA = associadoA.GetProperty("pessoa").GetProperty("id").GetGuid();
        var clienteB = api.Cliente(pb.AccessToken);

        var listaB = await clienteB.GetFromJsonAsync<JsonElement>("/api/v1/associados?tamanhoPagina=100", Cenario.Json);
        Assert.DoesNotContain(listaB.GetProperty("itens").EnumerateArray(), x => x.GetProperty("id").GetGuid() == idA);
        Assert.Equal(HttpStatusCode.NotFound, (await clienteB.GetAsync($"/api/v1/associados/{idA}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clienteB.PostAsync($"/api/v1/associados/{idA}/aprovar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clienteB.GetAsync($"/api/v1/associados/{idA}/carteirinha")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clienteB.GetAsync($"/api/v1/pessoas/{pessoaA}/qrcode")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _c.PostDependenteAsync(pb.AccessToken, idA)).StatusCode);

        // A categoria de A não pode ser usada em B.
        var categoriaA = await _c.CategoriaIdAsync(pa.AccessToken, "Sócio fundador");
        var r = await clienteB.PostAsJsonAsync("/api/v1/associados", new { pessoa = Cenario.DadosPessoa(cpf: Cenario.NovoCpf()), categoriaId = categoriaA });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Mesmo_cpf_pode_existir_em_clubes_diferentes()
    {
        var a = await _c.CriarClubeComPresidenteAsync();
        var b = await _c.CriarClubeComPresidenteAsync();
        var pa = await _c.LoginAsync(a.EmailPresidente);
        var pb = await _c.LoginAsync(b.EmailPresidente);
        var cpf = Cenario.NovoCpf();
        foreach (var s in new[] { pa, pb })
        {
            var r = await api.Cliente(s.AccessToken).PostAsJsonAsync("/api/v1/associados", new
            {
                pessoa = Cenario.DadosPessoa("José Pereira Santos", cpf), categoriaId = await _c.CategoriaIdAsync(s.AccessToken, "Sócio associado"),
            });
            Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        }
    }
}
