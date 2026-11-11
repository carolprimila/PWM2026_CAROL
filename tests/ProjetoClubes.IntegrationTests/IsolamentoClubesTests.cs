using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ProjetoClubes.IntegrationTests.Infra;

namespace ProjetoClubes.IntegrationTests;

/// <summary>Critério de aceitação 26: um usuário de um clube não acessa dados de outro clube.</summary>
[Collection(ColecaoApi.Nome)]
public class IsolamentoClubesTests(ApiFixture api)
{
    private readonly Cenario _c = new(api);

    [Fact]
    public async Task Administrador_do_clube_A_nao_ve_nem_altera_dados_do_clube_B()
    {
        var a = await _c.CriarClubeComPresidenteAsync();
        var b = await _c.CriarClubeComPresidenteAsync();
        var presidenteA = await _c.LoginAsync(a.EmailPresidente);
        var cliente = api.Cliente(presidenteA.AccessToken);
        Assert.Equal(a.Id, presidenteA.ClubeAtualId);

        // Clubes: lista só o próprio; o outro "não existe".
        var lista = await cliente.GetFromJsonAsync<JsonElement>("/api/v1/clubes?tamanhoPagina=100", Cenario.Json);
        var ids = lista.GetProperty("itens").EnumerateArray().Select(c => c.GetProperty("id").GetGuid()).ToList();
        Assert.Equal([a.Id], ids);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync($"/api/v1/clubes/{b.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.PutAsJsonAsync($"/api/v1/clubes/{b.Id}",
            new { nome = "Invadido", sigla = b.Sigla })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.PutAsJsonAsync($"/api/v1/clubes/{b.Id}/configuracoes",
            new { corPrimaria = "#000000", fusoHorario = "America/Sao_Paulo", exigeAprovacaoAssociados = false, exigeAprovacaoDependentes = false })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync($"/api/v1/clubes/{b.Id}/historico")).StatusCode);

        // Usuários: o presidente de B não aparece e não pode ser consultado nem alterado.
        var usuarios = await cliente.GetFromJsonAsync<JsonElement>("/api/v1/usuarios?tamanhoPagina=100", Cenario.Json);
        Assert.DoesNotContain(usuarios.GetProperty("itens").EnumerateArray(), u => u.GetProperty("id").GetGuid() == b.PresidenteId);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync($"/api/v1/usuarios/{b.PresidenteId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.PutAsJsonAsync($"/api/v1/usuarios/{b.PresidenteId}",
            new { nome = "Alterado" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.PostAsync($"/api/v1/usuarios/{b.PresidenteId}/inativar", null)).StatusCode);

        // Perfis: só os do próprio clube; usar perfil de B é recusado.
        var presidenteB = await _c.LoginAsync(b.EmailPresidente);
        var perfilDeB = await _c.PerfilIdAsync(presidenteB.AccessToken, "Presidente");
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync($"/api/v1/perfis/{perfilDeB}")).StatusCode);
        var criar = await cliente.PostAsJsonAsync("/api/v1/usuarios", new
        {
            nome = "Com perfil alheio", email = $"alheio.{Guid.NewGuid():N}@testes.local", perfilIds = new[] { perfilDeB },
            senhaInicial = Cenario.SenhaPadrao,
        });
        Assert.Equal(HttpStatusCode.BadRequest, criar.StatusCode);

        // B continua íntegro.
        var nomeB = await api.Cliente(presidenteB.AccessToken).GetFromJsonAsync<JsonElement>($"/api/v1/clubes/{b.Id}", Cenario.Json);
        Assert.NotEqual("Invadido", nomeB.GetProperty("nome").GetString());
    }

    [Fact]
    public async Task Nao_e_possivel_selecionar_clube_sem_vinculo()
    {
        var a = await _c.CriarClubeComPresidenteAsync();
        var b = await _c.CriarClubeComPresidenteAsync();
        var presidenteA = await _c.LoginAsync(a.EmailPresidente);

        var troca = await api.Cliente(presidenteA.AccessToken).PostAsJsonAsync("/api/v1/auth/trocar-clube", new { clubeId = b.Id });
        Assert.Equal(HttpStatusCode.Forbidden, troca.StatusCode);

        var login = await api.Cliente().PostAsJsonAsync("/api/v1/auth/login",
            new { email = a.EmailPresidente, senha = Cenario.SenhaPadrao, clubeId = b.Id });
        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);
    }

    [Fact]
    public async Task Usuario_com_dois_clubes_troca_de_clube_e_token_antigo_perde_validade()
    {
        var a = await _c.CriarClubeComPresidenteAsync();
        var b = await _c.CriarClubeComPresidenteAsync();
        var sa = await _c.SuperAdminAsync();

        // Vincula o presidente de A também ao clube B, como tesoureiro.
        var r = await api.Cliente(sa.AccessToken).PostAsJsonAsync($"/api/v1/clubes/{b.Id}/administradores",
            new { nome = "Ignorado", email = a.EmailPresidente, perfilChave = "tesoureiro" });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);

        var sessaoA = await _c.LoginAsync(a.EmailPresidente, clubeId: a.Id);
        Assert.Equal(2, sessaoA.Usuario.GetProperty("clubes").GetArrayLength());

        var troca = await api.Cliente(sessaoA.AccessToken).PostAsJsonAsync("/api/v1/auth/trocar-clube", new { clubeId = b.Id });
        Assert.Equal(HttpStatusCode.OK, troca.StatusCode);
        var sessaoB = await Cenario.LerSessaoAsync(troca);
        Assert.Equal(b.Id, sessaoB.ClubeAtualId);
        Assert.DoesNotContain("usuarios.gerenciar", sessaoB.Permissoes); // tesoureiro em B

        // O access token emitido para o clube A não vale mais.
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Cliente(sessaoA.AccessToken).GetAsync("/api/v1/auth/me")).StatusCode);
        // No clube B ele não pode gerenciar usuários.
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Cliente(sessaoB.AccessToken).GetAsync("/api/v1/usuarios")).StatusCode);
    }

    [Fact]
    public async Task Clube_desativado_bloqueia_acesso_dos_seus_usuarios()
    {
        var a = await _c.CriarClubeComPresidenteAsync();
        var presidente = await _c.LoginAsync(a.EmailPresidente);
        var sa = await _c.SuperAdminAsync();

        Assert.Equal(HttpStatusCode.NoContent, (await api.Cliente(sa.AccessToken).PostAsync($"/api/v1/clubes/{a.Id}/desativar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Cliente(presidente.AccessToken).GetAsync("/api/v1/auth/me")).StatusCode);
        var login = await api.Cliente().PostAsJsonAsync("/api/v1/auth/login", new { email = a.EmailPresidente, senha = Cenario.SenhaPadrao });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await api.Cliente(sa.AccessToken).PostAsync($"/api/v1/clubes/{a.Id}/ativar", null)).StatusCode);
        await _c.LoginAsync(a.EmailPresidente);
    }
}
