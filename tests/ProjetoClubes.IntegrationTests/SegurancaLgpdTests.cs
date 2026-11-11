using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ProjetoClubes.IntegrationTests.Infra;

namespace ProjetoClubes.IntegrationTests;

/// <summary>Etapa 8: cabeçalhos de segurança e direitos do titular (LGPD).</summary>
[Collection(ColecaoApi.Nome)]
public class SegurancaLgpdTests(ApiFixture api)
{
    private readonly Cenario _c = new(api);

    [Fact]
    public async Task Respostas_da_api_trazem_cabecalhos_de_seguranca_e_sem_cache()
    {
        var r = await api.Cliente().GetAsync("/api/v1/auth/status");
        Assert.Equal("nosniff", r.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", r.Headers.GetValues("X-Frame-Options").Single());
        Assert.Contains("frame-ancestors 'none'", r.Headers.GetValues("Content-Security-Policy").Single());
        Assert.True(r.Headers.CacheControl?.NoStore);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Cliente().GetAsync("/api/v1/portal/meus-dados")).StatusCode);
    }

    [Fact]
    public async Task Inclusoes_simultaneas_de_dependentes_respeitam_o_limite_da_categoria()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var p = await _c.LoginAsync(clube.EmailPresidente);
        var c = api.Cliente(p.AccessToken);
        var cat = await c.PostAsJsonAsync("/api/v1/categorias-associados", new
        {
            nome = "Individual", descricao = (string?)null, direitoVoto = true, podeSerVotado = false, permiteDependentes = true,
            limiteDependentes = 2, ordem = 9,
        });
        Assert.Equal(HttpStatusCode.Created, cat.StatusCode);
        var titular = (await _c.CriarAssociadoAsync(p.AccessToken, "Individual")).GetProperty("id").GetGuid();

        var respostas = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => _c.PostDependenteAsync(p.AccessToken, titular, cpf: Cenario.NovoCpf())));
        Assert.Equal(2, respostas.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(3, respostas.Count(r => r.StatusCode == HttpStatusCode.UnprocessableEntity));
    }

    [Fact]
    public async Task Associado_baixa_os_proprios_dados_e_a_administracao_de_outro_clube_nao()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var p = await _c.LoginAsync(clube.EmailPresidente);
        var (assoc, assocId, pessoaId) = await _c.AssociadoComAppAsync(p);
        var outroAssociado = await _c.CriarAssociadoAsync(p.AccessToken);
        var nomeOutro = outroAssociado.GetProperty("pessoa").GetProperty("nomeCompleto").GetString()!;

        var r = await api.Cliente(assoc.AccessToken).GetAsync("/api/v1/portal/meus-dados");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("application/json", r.Content.Headers.ContentType?.MediaType);
        Assert.StartsWith("meus-dados-", r.Content.Headers.ContentDisposition?.FileName?.Trim('"'));
        var texto = await r.Content.ReadAsStringAsync();
        var dados = JsonDocument.Parse(texto).RootElement;
        Assert.Equal(assocId, dados.GetProperty("associado")[0].GetProperty("id").GetGuid());
        Assert.False(string.IsNullOrEmpty(dados.GetProperty("pessoa").GetProperty("nomeCompleto").GetString()));
        Assert.True(dados.TryGetProperty("mensalidades", out _));
        Assert.DoesNotContain(nomeOutro, texto); // só os próprios dados

        Assert.Equal(HttpStatusCode.OK, (await api.Cliente(p.AccessToken).GetAsync($"/api/v1/pessoas/{pessoaId}/dados-pessoais")).StatusCode);
        var outro = await _c.CriarClubeComPresidenteAsync();
        var p2 = await _c.LoginAsync(outro.EmailPresidente);
        Assert.Equal(HttpStatusCode.NotFound, (await api.Cliente(p2.AccessToken).GetAsync($"/api/v1/pessoas/{pessoaId}/dados-pessoais")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await api.Cliente(assoc.AccessToken).GetAsync($"/api/v1/pessoas/{outroAssociado.GetProperty("pessoa").GetProperty("id")}/dados-pessoais")).StatusCode);
    }

    [Fact]
    public async Task Anonimizacao_so_sem_vinculo_vigente_apaga_dados_login_qr_e_auditoria()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var p = await _c.LoginAsync(clube.EmailPresidente);
        var c = api.Cliente(p.AccessToken);
        var (assoc, assocId, pessoaId) = await _c.AssociadoComAppAsync(p);
        var emailAntigo = assoc.Usuario.GetProperty("email").GetString()!;
        var qrAntigo = (await c.GetFromJsonAsync<JsonElement>($"/api/v1/associados/{assocId}/carteirinha", Cenario.Json))
            .GetProperty("qrCodeConteudo").GetString()!;

        Assert.Equal(HttpStatusCode.Forbidden,
            (await api.Cliente(assoc.AccessToken).PostAsJsonAsync($"/api/v1/pessoas/{pessoaId}/anonimizar", new { motivo = "Quero sair" })).StatusCode);
        var vigente = await c.PostAsJsonAsync($"/api/v1/pessoas/{pessoaId}/anonimizar", new { motivo = "Pedido do titular" });
        Assert.Equal("lgpd.vinculo_vigente", await Cenario.CodigoProblemaAsync(vigente));

        var inativo = await c.PostAsJsonAsync($"/api/v1/associados/{assocId}/inativar", new { motivo = "Pedido de desligamento" });
        Assert.True(inativo.IsSuccessStatusCode, await inativo.Content.ReadAsStringAsync());
        var r = await c.PostAsJsonAsync($"/api/v1/pessoas/{pessoaId}/anonimizar", new { motivo = "Pedido do titular por e-mail" });
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        var resultado = await r.Content.ReadFromJsonAsync<JsonElement>(Cenario.Json);
        Assert.StartsWith("Pessoa anonimizada", resultado.GetProperty("novoNome").GetString());
        Assert.True(resultado.GetProperty("loginDesativado").GetBoolean());

        var detalhe = await c.GetFromJsonAsync<JsonElement>($"/api/v1/associados/{assocId}", Cenario.Json);
        var pessoa = detalhe.GetProperty("pessoa");
        Assert.StartsWith("Pessoa anonimizada", pessoa.GetProperty("nomeCompleto").GetString());
        Assert.Equal(JsonValueKind.Null, pessoa.GetProperty("email").ValueKind);

        // Login antigo não entra mais; a sessão aberta caiu; o QR antigo deixou de valer.
        var login = await api.Cliente().PostAsJsonAsync("/api/v1/auth/login", new { email = emailAntigo, senha = Cenario.SenhaPadrao });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Cliente(assoc.AccessToken).GetAsync("/api/v1/portal/perfil")).StatusCode);
        var validacao = await (await c.PostAsJsonAsync("/api/v1/controle-acesso/validar", new { codigo = qrAntigo }))
            .Content.ReadFromJsonAsync<JsonElement>(Cenario.Json);
        Assert.False(validacao.GetProperty("autorizado").GetBoolean());

        // A auditoria mantém o registro de que houve alterações, mas não os valores antigos.
        var comConteudo = await api.ExecutarSqlAsync(
            $"UPDATE \"LogsAuditoria\" SET \"Alteracoes\" = \"Alteracoes\" WHERE \"EntidadeId\" = '{pessoaId}' AND \"Alteracoes\" IS NOT NULL");
        Assert.Equal(0, comConteudo);
        var registros = await api.ExecutarSqlAsync(
            $"UPDATE \"LogsAuditoria\" SET \"Alteracoes\" = NULL WHERE \"EntidadeId\" = '{pessoaId}'");
        Assert.True(registros > 0);

        var denovo = await c.PostAsJsonAsync($"/api/v1/pessoas/{pessoaId}/anonimizar", new { motivo = "Pedido do titular" });
        Assert.Equal("pessoa.anonimizada", await Cenario.CodigoProblemaAsync(denovo));
    }
}
