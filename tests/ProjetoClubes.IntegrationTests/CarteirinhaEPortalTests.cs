using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ProjetoClubes.IntegrationTests.Infra;

namespace ProjetoClubes.IntegrationTests;

/// <summary>Critérios de aceitação 12, 23, 24 e 25: carteirinha, QR Code, portaria e aplicativo do associado.</summary>
[Collection(ColecaoApi.Nome)]
public class CarteirinhaEControleAcessoTests(ApiFixture api)
{
    private readonly Cenario _c = new(api);

    private async Task<(Sessao Presidente, Sessao Portaria)> ClubeComPortariaAsync()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var p = await _c.LoginAsync(clube.EmailPresidente);
        var (_, email) = await _c.CriarUsuarioAsync(p, "Funcionário");
        return (p, await _c.LoginAsync(email));
    }

    private async Task<JsonElement> RegistrarAsync(string token, string codigo, string tipo = "Entrada")
    {
        var r = await api.Cliente(token).PostAsJsonAsync("/api/v1/controle-acesso/registrar", new { codigo, tipo });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return await r.Content.ReadFromJsonAsync<JsonElement>(Cenario.Json);
    }

    [Fact]
    public async Task Qr_code_nao_expoe_cpf_e_associado_ativo_entra_e_sai()
    {
        var (p, portaria) = await ClubeComPortariaAsync();
        var a = await _c.CriarAssociadoAsync(p.AccessToken);
        var cpf = a.GetProperty("pessoa").GetProperty("cpf").GetString()!;
        var cart = await api.Cliente(p.AccessToken).GetFromJsonAsync<JsonElement>($"/api/v1/associados/{a.GetProperty("id")}/carteirinha", Cenario.Json);
        var codigo = cart.GetProperty("qrCodeConteudo").GetString()!;
        Assert.StartsWith("PCLUBES:1:", codigo);
        Assert.DoesNotContain(cpf, codigo);
        Assert.Equal(a.GetProperty("matricula").GetString(), cart.GetProperty("numeroIdentificacao").GetString());

        var png = await api.Cliente(p.AccessToken).GetAsync($"/api/v1/pessoas/{cart.GetProperty("pessoaId")}/qrcode");
        Assert.Equal("image/png", png.Content.Headers.ContentType?.MediaType);

        var entrada = await RegistrarAsync(portaria.AccessToken, codigo);
        Assert.True(entrada.GetProperty("autorizado").GetBoolean());
        Assert.Equal("Associado", entrada.GetProperty("tipo").GetString());

        var presentes = await api.Cliente(portaria.AccessToken).GetFromJsonAsync<JsonElement>("/api/v1/controle-acesso/presentes", Cenario.Json);
        Assert.Contains(presentes.EnumerateArray(), x => x.GetProperty("pessoaId").GetGuid() == cart.GetProperty("pessoaId").GetGuid());

        var saida = await RegistrarAsync(portaria.AccessToken, codigo, "Saida");
        Assert.True(saida.GetProperty("autorizado").GetBoolean());
        presentes = await api.Cliente(portaria.AccessToken).GetFromJsonAsync<JsonElement>("/api/v1/controle-acesso/presentes", Cenario.Json);
        Assert.DoesNotContain(presentes.EnumerateArray(), x => x.GetProperty("pessoaId").GetGuid() == cart.GetProperty("pessoaId").GetGuid());

        var historico = await api.Cliente(portaria.AccessToken).GetFromJsonAsync<JsonElement>(
            $"/api/v1/controle-acesso?pessoaId={cart.GetProperty("pessoaId")}", Cenario.Json);
        var itens = historico.GetProperty("itens").EnumerateArray().ToList();
        Assert.Equal(2, itens.Count);
        Assert.All(itens, i => Assert.Equal(portaria.UsuarioId, i.GetProperty("registradoPor").GetGuid()));
    }

    [Fact]
    public async Task Dependente_pendente_sem_permissao_ou_com_titular_suspenso_e_barrado()
    {
        var (p, portaria) = await ClubeComPortariaAsync();
        var titular = (await _c.CriarAssociadoAsync(p.AccessToken)).GetProperty("id").GetGuid();
        var dep = await _c.CriarDependenteAsync(p.AccessToken, titular, "Filho", 15);
        var depId = dep.GetProperty("id").GetGuid();
        var cliente = api.Cliente(p.AccessToken);
        var codigo = (await cliente.GetFromJsonAsync<JsonElement>($"/api/v1/dependentes/{depId}/carteirinha", Cenario.Json))
            .GetProperty("qrCodeConteudo").GetString()!;

        var ok = await RegistrarAsync(portaria.AccessToken, codigo);
        Assert.True(ok.GetProperty("autorizado").GetBoolean());
        Assert.Equal("Dependente", ok.GetProperty("tipo").GetString());
        Assert.NotNull(ok.GetProperty("titularNome").GetString());

        await cliente.PostAsJsonAsync($"/api/v1/associados/{titular}/suspender", new { motivo = "Inadimplência" });
        var titularSuspenso = await RegistrarAsync(portaria.AccessToken, codigo);
        Assert.False(titularSuspenso.GetProperty("autorizado").GetBoolean());
        Assert.Equal("O titular está suspenso.", titularSuspenso.GetProperty("mensagem").GetString());
        await cliente.PostAsync($"/api/v1/associados/{titular}/reativar", null);

        await cliente.PutAsJsonAsync($"/api/v1/dependentes/{depId}/permissoes", new
        {
            acessoClube = false, participacaoEventos = true, usoEspacosEsportivos = true, inscricaoAtividades = true,
            solicitacaoReservas = false, recebimentoComunicados = true, utilizacaoServicos = true,
        });
        var semPermissao = await RegistrarAsync(portaria.AccessToken, codigo);
        Assert.Equal("Dependente sem permissão de acesso ao clube.", semPermissao.GetProperty("mensagem").GetString());

        // Tentativas recusadas também ficam registradas.
        var recusas = await api.Cliente(portaria.AccessToken).GetFromJsonAsync<JsonElement>(
            $"/api/v1/controle-acesso?pessoaId={dep.GetProperty("pessoa").GetProperty("id")}&autorizado=false", Cenario.Json);
        Assert.Equal(2, recusas.GetProperty("totalItens").GetInt32());
    }

    [Fact]
    public async Task Carteirinha_de_outro_clube_e_codigo_regenerado_nao_sao_reconhecidos()
    {
        var (p, portaria) = await ClubeComPortariaAsync();
        var outro = await _c.CriarClubeComPresidenteAsync();
        var po = await _c.LoginAsync(outro.EmailPresidente);
        var deOutroClube = await _c.CriarAssociadoAsync(po.AccessToken);
        var codigoOutro = (await api.Cliente(po.AccessToken).GetFromJsonAsync<JsonElement>(
            $"/api/v1/associados/{deOutroClube.GetProperty("id")}/carteirinha", Cenario.Json)).GetProperty("qrCodeConteudo").GetString()!;

        var r = await api.Cliente(portaria.AccessToken).PostAsJsonAsync("/api/v1/controle-acesso/validar", new { codigo = codigoOutro });
        var v = await r.Content.ReadFromJsonAsync<JsonElement>(Cenario.Json);
        Assert.False(v.GetProperty("autorizado").GetBoolean());
        Assert.Equal(JsonValueKind.Null, v.GetProperty("nome").ValueKind); // não revela dados de outro clube

        var a = await _c.CriarAssociadoAsync(p.AccessToken);
        var pessoaId = a.GetProperty("pessoa").GetProperty("id").GetGuid();
        var cliente = api.Cliente(p.AccessToken);
        var antigo = (await cliente.GetFromJsonAsync<JsonElement>($"/api/v1/associados/{a.GetProperty("id")}/carteirinha", Cenario.Json))
            .GetProperty("qrCodeConteudo").GetString()!;
        Assert.Equal(HttpStatusCode.NoContent, (await cliente.PostAsync($"/api/v1/pessoas/{pessoaId}/carteirinha/regenerar", null)).StatusCode);
        var novo = (await cliente.GetFromJsonAsync<JsonElement>($"/api/v1/associados/{a.GetProperty("id")}/carteirinha", Cenario.Json))
            .GetProperty("qrCodeConteudo").GetString()!;
        Assert.NotEqual(antigo, novo);

        var validaAntigo = await (await api.Cliente(portaria.AccessToken).PostAsJsonAsync("/api/v1/controle-acesso/validar", new { codigo = antigo }))
            .Content.ReadFromJsonAsync<JsonElement>(Cenario.Json);
        Assert.False(validaAntigo.GetProperty("autorizado").GetBoolean());
        var validaNovo = await (await api.Cliente(portaria.AccessToken).PostAsJsonAsync("/api/v1/controle-acesso/validar", new { codigo = novo }))
            .Content.ReadFromJsonAsync<JsonElement>(Cenario.Json);
        Assert.True(validaNovo.GetProperty("autorizado").GetBoolean());
    }

    [Fact]
    public async Task Somente_quem_tem_permissao_valida_carteirinha()
    {
        var (p, _) = await ClubeComPortariaAsync();
        var (_, email) = await _c.CriarUsuarioAsync(p, "Tesoureiro");
        var tesoureiro = await _c.LoginAsync(email);
        var r = await api.Cliente(tesoureiro.AccessToken).PostAsJsonAsync("/api/v1/controle-acesso/validar", new { codigo = "PCLUBES:1:abcdefghijklmnopqrstuvwxyz" });
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }
}

[Collection(ColecaoApi.Nome)]
public class PortalTests(ApiFixture api)
{
    private readonly Cenario _c = new(api);

    private async Task<(Sessao Presidente, Sessao Associado, Guid AssociadoId)> AssociadoComAppAsync()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var p = await _c.LoginAsync(clube.EmailPresidente);
        var email = $"associado.{Guid.NewGuid():N}@testes.local";
        var a = await _c.CriarAssociadoAsync(p.AccessToken, email: email);
        var id = a.GetProperty("id").GetGuid();
        var r = await api.Cliente(p.AccessToken).PostAsJsonAsync($"/api/v1/associados/{id}/acesso-app", new { senhaInicial = Cenario.SenhaPadrao });
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        return (p, await _c.LoginAsync(email), id);
    }

    [Fact]
    public async Task Associado_ve_perfil_e_carteirinhas_e_nao_acessa_area_administrativa()
    {
        var (p, assoc, associadoId) = await AssociadoComAppAsync();
        await _c.CriarDependenteAsync(p.AccessToken, associadoId, "Conjuge", 38);
        var cliente = api.Cliente(assoc.AccessToken);

        Assert.Equal(["portal.acessar"], assoc.Permissoes);
        var perfil = await cliente.GetFromJsonAsync<JsonElement>("/api/v1/portal/perfil", Cenario.Json);
        Assert.Equal("Associado", perfil.GetProperty("tipo").GetString());
        Assert.Equal(associadoId, perfil.GetProperty("associadoId").GetGuid());

        var cartoes = await cliente.GetFromJsonAsync<JsonElement>("/api/v1/portal/carteirinhas", Cenario.Json);
        Assert.Equal(2, cartoes.GetArrayLength());

        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/v1/associados")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/v1/dependentes")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync($"/api/v1/associados/{associadoId}/historico")).StatusCode);
    }

    [Fact]
    public async Task Titular_inclui_dependente_pendente_e_so_enxerga_os_proprios()
    {
        var (p, assoc, _) = await AssociadoComAppAsync();
        var cliente = api.Cliente(assoc.AccessToken);

        var r = await cliente.PostAsJsonAsync("/api/v1/portal/dependentes", new
        {
            pessoa = Cenario.DadosPessoa("Luiza Pereira Gomes", idade: 6), parentesco = "Filho",
        });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var dep = await r.Content.ReadFromJsonAsync<JsonElement>(Cenario.Json);
        Assert.Equal("Pendente", dep.GetProperty("situacao").GetString());
        var depId = dep.GetProperty("id").GetGuid();
        var pessoaDep = dep.GetProperty("pessoa").GetProperty("id").GetGuid();

        // Enquanto pendente, o titular corrige dados e anexa documento.
        var editar = await cliente.PutAsJsonAsync($"/api/v1/portal/dependentes/{depId}", new
        {
            pessoa = Cenario.DadosPessoa("Luiza Pereira Gomes Silva", idade: 6), parentesco = "Filho",
        });
        Assert.Equal(HttpStatusCode.OK, editar.StatusCode);
        await ArquivosTests.EnviarDocumentoAsync(api, assoc.AccessToken, pessoaDep);

        // A administração aprova; depois disso o titular não edita mais pelo app.
        Assert.Equal(HttpStatusCode.OK, (await api.Cliente(p.AccessToken).PostAsync($"/api/v1/dependentes/{depId}/aprovar", null)).StatusCode);
        var bloqueado = await cliente.PutAsJsonAsync($"/api/v1/portal/dependentes/{depId}", new
        {
            pessoa = Cenario.DadosPessoa("Outro Nome Qualquer", idade: 6), parentesco = "Filho",
        });
        Assert.Equal(HttpStatusCode.Conflict, bloqueado.StatusCode);

        // Outro associado do mesmo clube não enxerga este dependente nem seus documentos.
        var email2 = $"associado.{Guid.NewGuid():N}@testes.local";
        var outro = await _c.CriarAssociadoAsync(p.AccessToken, email: email2);
        await api.Cliente(p.AccessToken).PostAsJsonAsync($"/api/v1/associados/{outro.GetProperty("id")}/acesso-app", new { senhaInicial = Cenario.SenhaPadrao });
        var clienteOutro = api.Cliente((await _c.LoginAsync(email2)).AccessToken);
        Assert.Equal(HttpStatusCode.NotFound, (await clienteOutro.GetAsync($"/api/v1/portal/dependentes/{depId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clienteOutro.GetAsync($"/api/v1/pessoas/{pessoaDep}/documentos")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clienteOutro.GetAsync($"/api/v1/pessoas/{pessoaDep}/qrcode")).StatusCode);
        Assert.Empty((await clienteOutro.GetFromJsonAsync<JsonElement>("/api/v1/portal/dependentes", Cenario.Json)).EnumerateArray());
    }

    [Fact]
    public async Task Titular_autoriza_login_do_dependente_respeitando_idade()
    {
        var (p, assoc, associadoId) = await AssociadoComAppAsync();
        var emailFilho = $"filho.{Guid.NewGuid():N}@testes.local";
        var crianca = await _c.CriarDependenteAsync(p.AccessToken, associadoId, "Filho", 9, email: $"c.{Guid.NewGuid():N}@testes.local");
        var adolescente = await _c.CriarDependenteAsync(p.AccessToken, associadoId, "Filho", 15, email: emailFilho);
        var cliente = api.Cliente(assoc.AccessToken);

        var negado = await cliente.PostAsync($"/api/v1/portal/dependentes/{crianca.GetProperty("id")}/acesso-app", null);
        Assert.Equal("dependente.idade_login", await Cenario.CodigoProblemaAsync(negado));

        var ok = await cliente.PostAsync($"/api/v1/portal/dependentes/{adolescente.GetProperty("id")}/acesso-app", null);
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.True((await ok.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("conviteEnviado").GetBoolean());

        // O dependente define a senha pelo convite e entra no app vendo apenas a própria carteirinha.
        var token = api.Emails.UltimoToken(emailFilho);
        await api.Cliente().PostAsJsonAsync("/api/v1/auth/redefinir-senha", new { token, novaSenha = "SenhaDoFilho2026" });
        var filho = api.Cliente((await _c.LoginAsync(emailFilho, "SenhaDoFilho2026")).AccessToken);
        var perfil = await filho.GetFromJsonAsync<JsonElement>("/api/v1/portal/perfil", Cenario.Json);
        Assert.Equal("Dependente", perfil.GetProperty("tipo").GetString());
        Assert.False(perfil.GetProperty("podeEditarContato").GetBoolean()); // menor de idade
        Assert.Single((await filho.GetFromJsonAsync<JsonElement>("/api/v1/portal/carteirinhas", Cenario.Json)).EnumerateArray());
        Assert.Equal(HttpStatusCode.Forbidden, (await filho.GetAsync("/api/v1/portal/dependentes")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await filho.PutAsJsonAsync("/api/v1/portal/perfil/contato", new { telefone = "45988887777" })).StatusCode);
    }
}
