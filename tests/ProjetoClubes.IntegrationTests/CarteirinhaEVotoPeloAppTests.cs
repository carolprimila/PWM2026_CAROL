using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ProjetoClubes.IntegrationTests.Infra;

namespace ProjetoClubes.IntegrationTests;

/// <summary>Código curto da carteirinha (digitado na portaria) e assembleia com participação e voto pelo app.</summary>
[Collection(ColecaoApi.Nome)]
public class CarteirinhaEVotoPeloAppTests(ApiFixture api)
{
    private readonly Cenario _c = new(api);

    private static async Task<JsonElement> OkAsync(HttpResponseMessage r)
    {
        Assert.True(r.IsSuccessStatusCode, $"{(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        return await r.Content.ReadFromJsonAsync<JsonElement>(Cenario.Json);
    }

    [Fact]
    public async Task Codigo_curto_funciona_na_portaria_e_muda_ao_regenerar()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var p = await _c.LoginAsync(clube.EmailPresidente);
        var c = api.Cliente(p.AccessToken);
        var a = await _c.CriarAssociadoAsync(p.AccessToken);
        var cart = await c.GetFromJsonAsync<JsonElement>($"/api/v1/associados/{a.GetProperty("id")}/carteirinha", Cenario.Json);
        var codigo = cart.GetProperty("codigoAcesso").GetString()!;
        Assert.Matches("^[0-9A-Z]{4}-[0-9A-Z]{4}$", codigo);

        // Digitado de qualquer jeito: minúsculas, sem hífen, com espaços.
        var digitado = " " + codigo.Replace("-", "").ToLowerInvariant() + " ";
        var ok = await OkAsync(await c.PostAsJsonAsync("/api/v1/controle-acesso/validar", new { codigo = digitado }));
        Assert.True(ok.GetProperty("autorizado").GetBoolean());
        Assert.Equal(a.GetProperty("pessoa").GetProperty("nomeCompleto").GetString(), ok.GetProperty("nome").GetString());

        // O link do QR Code também funciona inteiro.
        var link = cart.GetProperty("qrCodeConteudo").GetString()!;
        Assert.True((await OkAsync(await c.PostAsJsonAsync("/api/v1/controle-acesso/validar", new { codigo = link }))).GetProperty("autorizado").GetBoolean());

        var pessoaId = a.GetProperty("pessoa").GetProperty("id").GetGuid();
        Assert.True((await c.PostAsync($"/api/v1/pessoas/{pessoaId}/carteirinha/regenerar", null)).IsSuccessStatusCode);
        var antigo = await OkAsync(await c.PostAsJsonAsync("/api/v1/controle-acesso/validar", new { codigo }));
        Assert.False(antigo.GetProperty("autorizado").GetBoolean());
        var novo = (await c.GetFromJsonAsync<JsonElement>($"/api/v1/associados/{a.GetProperty("id")}/carteirinha", Cenario.Json)).GetProperty("codigoAcesso").GetString();
        Assert.NotEqual(codigo, novo);
    }

    private static async Task<(Guid Id, Guid Pauta)> AssembleiaEmVotacaoAsync(HttpClient c, bool peloApp)
    {
        var a = await OkAsync(await c.PostAsJsonAsync("/api/v1/assembleias", new
        {
            titulo = "Assembleia extraordinária", tipo = "Extraordinaria", dataHora = DateTimeOffset.UtcNow.AddDays(5), local = "Salão",
            convocacao = "Ficam convocados...", exigeAdimplencia = false, participacaoPeloApp = peloApp,
        }));
        var id = a.GetProperty("id").GetGuid();
        Assert.Equal(peloApp, a.GetProperty("participacaoPeloApp").GetBoolean());
        a = await OkAsync(await c.PostAsJsonAsync($"/api/v1/assembleias/{id}/pautas", new { titulo = "Cobertura da piscina", comVotacao = true }));
        var pauta = a.GetProperty("pautas")[0].GetProperty("id").GetGuid();
        await OkAsync(await c.PostAsync($"/api/v1/assembleias/{id}/convocar", null));
        await OkAsync(await c.PostAsync($"/api/v1/assembleias/{id}/instalar", null));
        await OkAsync(await c.PostAsync($"/api/v1/assembleias/{id}/pautas/{pauta}/abrir-votacao", null));
        return (id, pauta);
    }

    [Fact]
    public async Task Associado_participa_e_vota_pelo_app_quando_a_assembleia_permite()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var p = await _c.LoginAsync(clube.EmailPresidente);
        var c = api.Cliente(p.AccessToken);
        var (assoc, associadoId, _) = await _c.AssociadoComAppAsync(p);
        var app = api.Cliente(assoc.AccessToken);

        var (id, pauta) = await AssembleiaEmVotacaoAsync(c, peloApp: true);
        var semPresenca = await app.PostAsJsonAsync($"/api/v1/portal/assembleias/{id}/pautas/{pauta}/voto", new { opcao = 0 });
        Assert.Equal("voto.sem_presenca", await Cenario.CodigoProblemaAsync(semPresenca));

        var portal = await OkAsync(await app.PostAsync($"/api/v1/portal/assembleias/{id}/participar", null));
        Assert.True(portal.GetProperty("presente").GetBoolean());
        Assert.True(portal.GetProperty("presentePeloApp").GetBoolean());
        Assert.True(portal.GetProperty("aptoVotar").GetBoolean());
        Assert.Equal(HttpStatusCode.Conflict, (await app.PostAsync($"/api/v1/portal/assembleias/{id}/participar", null)).StatusCode);

        var votou = await OkAsync(await app.PostAsJsonAsync($"/api/v1/portal/assembleias/{id}/pautas/{pauta}/voto", new { opcao = 0 }));
        Assert.True(votou.GetProperty("pautas")[0].GetProperty("votei").GetBoolean());

        var detalhe = await c.GetFromJsonAsync<JsonElement>($"/api/v1/assembleias/{id}", Cenario.Json);
        var presenca = detalhe.GetProperty("presencas").EnumerateArray().Single(x => x.GetProperty("associadoId").GetGuid() == associadoId);
        Assert.True(presenca.GetProperty("peloApp").GetBoolean());

        // Assembleia só presencial: o app não registra presença.
        var (presencial, _) = await AssembleiaEmVotacaoAsync(c, peloApp: false);
        Assert.Equal("assembleia.sem_app", await Cenario.CodigoProblemaAsync(await app.PostAsync($"/api/v1/portal/assembleias/{presencial}/participar", null)));
    }

    [Fact]
    public async Task Cpf_invalido_volta_400_com_a_mensagem_do_campo()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var p = await _c.LoginAsync(clube.EmailPresidente);
        var r = await api.Cliente(p.AccessToken).PostAsJsonAsync("/api/v1/associados", new
        {
            pessoa = Cenario.DadosPessoa(cpf: "12345678900"),
            categoriaId = await _c.CategoriaIdAsync(p.AccessToken, "Sócio associado"),
        });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Contains("CPF inválido", await r.Content.ReadAsStringAsync());
    }
}
