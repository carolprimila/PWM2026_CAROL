using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ProjetoClubes.IntegrationTests.Infra;

namespace ProjetoClubes.IntegrationTests;

[Collection(ColecaoApi.Nome)]
public class AutenticacaoTests(ApiFixture api)
{
    private readonly Cenario _c = new(api);

    [Fact]
    public async Task Setup_ja_inicializado_nao_pode_ser_repetido()
    {
        var status = await api.Cliente().GetFromJsonAsync<JsonElement>("/api/v1/setup/status");
        Assert.True(status.GetProperty("inicializado").GetBoolean());

        var r = await api.Cliente().PostAsJsonAsync("/api/v1/setup/inicializar", new
        {
            tokenInicializacao = ApiFixture.TokenSetup, nome = "Invasor", email = "invasor@x.com", senha = "SenhaInvasor2026",
        });
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task Superadmin_sem_clube_entra_na_area_da_plataforma()
    {
        var sa = await _c.SuperAdminAsync();
        Assert.True(sa.Usuario.GetProperty("superAdmin").GetBoolean());
        var me = await api.Cliente(sa.AccessToken).GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
        Assert.Equal(ApiFixture.EmailSuperAdmin, me.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Credenciais_invalidas_e_email_inexistente_tem_a_mesma_resposta()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var a = await api.Cliente().PostAsJsonAsync("/api/v1/auth/login", new { email = clube.EmailPresidente, senha = "Errada12345" });
        var b = await api.Cliente().PostAsJsonAsync("/api/v1/auth/login", new { email = "naoexiste@testes.local", senha = "Errada12345" });
        Assert.Equal(HttpStatusCode.Unauthorized, a.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, b.StatusCode);
        var pa = await a.Content.ReadFromJsonAsync<JsonElement>();
        var pb = await b.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(pa.GetProperty("title").GetString(), pb.GetProperty("title").GetString());
        Assert.Equal("credenciais_invalidas", pa.GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task Bloqueia_temporariamente_apos_cinco_falhas()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        for (var i = 0; i < 5; i++)
            await api.Cliente().PostAsJsonAsync("/api/v1/auth/login", new { email = clube.EmailPresidente, senha = "Errada12345" });

        var r = await api.Cliente().PostAsJsonAsync("/api/v1/auth/login", new { email = clube.EmailPresidente, senha = Cenario.SenhaPadrao });
        Assert.Equal(HttpStatusCode.Locked, r.StatusCode);
    }

    [Fact]
    public async Task Refresh_rotaciona_e_reutilizacao_revoga_a_sessao()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var s1 = await _c.LoginAsync(clube.EmailPresidente);

        var r2 = await api.Cliente().PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = s1.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
        var s2 = await Cenario.LerSessaoAsync(r2);
        Assert.NotEqual(s1.RefreshToken, s2.RefreshToken);
        Assert.Equal(HttpStatusCode.OK, (await api.Cliente(s2.AccessToken).GetAsync("/api/v1/auth/me")).StatusCode);

        // Reuso do refresh token antigo (cenário de roubo): sessão inteira revogada.
        var reuso = await api.Cliente().PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = s1.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reuso.StatusCode);
        var novo = await api.Cliente().PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = s2.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, novo.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Cliente(s2.AccessToken).GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Logout_invalida_o_access_token_imediatamente()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var s = await _c.LoginAsync(clube.EmailPresidente);
        var cliente = api.Cliente(s.AccessToken);
        Assert.Equal(HttpStatusCode.NoContent, (await cliente.PostAsync("/api/v1/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Recuperacao_de_senha_completa_e_token_de_uso_unico()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var sessaoAntiga = await _c.LoginAsync(clube.EmailPresidente);

        var r = await api.Cliente().PostAsJsonAsync("/api/v1/auth/esqueci-senha", new { email = clube.EmailPresidente });
        Assert.Equal(HttpStatusCode.Accepted, r.StatusCode);
        var inexistente = await api.Cliente().PostAsJsonAsync("/api/v1/auth/esqueci-senha", new { email = "ninguem@testes.local" });
        Assert.Equal(HttpStatusCode.Accepted, inexistente.StatusCode);

        var token = api.Emails.UltimoToken(clube.EmailPresidente);
        const string nova = "NovaSenhaSegura2026";
        var redefinir = await api.Cliente().PostAsJsonAsync("/api/v1/auth/redefinir-senha", new { token, novaSenha = nova });
        Assert.Equal(HttpStatusCode.NoContent, redefinir.StatusCode);

        var reuso = await api.Cliente().PostAsJsonAsync("/api/v1/auth/redefinir-senha", new { token, novaSenha = "OutraSenha2026x" });
        Assert.Equal(HttpStatusCode.Conflict, reuso.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Cliente(sessaoAntiga.AccessToken).GetAsync("/api/v1/auth/me")).StatusCode);
        var antiga = await api.Cliente().PostAsJsonAsync("/api/v1/auth/login", new { email = clube.EmailPresidente, senha = Cenario.SenhaPadrao });
        Assert.Equal(HttpStatusCode.Unauthorized, antiga.StatusCode);
        await _c.LoginAsync(clube.EmailPresidente, nova);
    }

    [Fact]
    public async Task Alterar_senha_mantem_sessao_atual_e_encerra_as_outras()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var atual = await _c.LoginAsync(clube.EmailPresidente);
        var outra = await _c.LoginAsync(clube.EmailPresidente);

        var fraca = await api.Cliente(atual.AccessToken).PostAsJsonAsync("/api/v1/auth/alterar-senha",
            new { senhaAtual = Cenario.SenhaPadrao, novaSenha = "123" });
        Assert.Equal(HttpStatusCode.BadRequest, fraca.StatusCode);
        var erros = (await fraca.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.True(erros.TryGetProperty("novaSenha", out _));

        var r = await api.Cliente(atual.AccessToken).PostAsJsonAsync("/api/v1/auth/alterar-senha",
            new { senhaAtual = Cenario.SenhaPadrao, novaSenha = "SenhaAlterada2026" });
        Assert.Equal(HttpStatusCode.NoContent, r.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await api.Cliente(atual.AccessToken).GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Cliente(outra.AccessToken).GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Historico_de_autenticacao_registra_eventos()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        await api.Cliente().PostAsJsonAsync("/api/v1/auth/login", new { email = clube.EmailPresidente, senha = "Errada12345" });
        var s = await _c.LoginAsync(clube.EmailPresidente);
        var hist = await api.Cliente(s.AccessToken).GetFromJsonAsync<JsonElement>("/api/v1/auth/historico");
        var eventos = hist.EnumerateArray().Select(h => h.GetProperty("evento").GetString()).ToList();
        Assert.Contains("LoginSucesso", eventos);
        Assert.Contains("LoginFalha", eventos);
    }

    [Fact]
    public async Task Endpoints_protegidos_exigem_token()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Cliente().GetAsync("/api/v1/clubes")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Cliente("token.invalido.qualquer").GetAsync("/api/v1/clubes")).StatusCode);
    }
}
