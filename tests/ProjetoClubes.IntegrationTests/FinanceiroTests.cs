using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ProjetoClubes.IntegrationTests.Infra;

namespace ProjetoClubes.IntegrationTests;

/// <summary>Critérios de aceitação 14 e 15: mensalidades, pagamentos e situação financeira.</summary>
[Collection(ColecaoApi.Nome)]
public class FinanceiroTests(ApiFixture api)
{
    private readonly Cenario _c = new(api);
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.UtcNow);
    private static string Comp(DateOnly d) => d.ToString("yyyy-MM");
    private static readonly string Passada = Comp(Hoje.AddMonths(-3));
    private static readonly string Futura = Comp(Hoje.AddMonths(1));

    private async Task<(Sessao Presidente, HttpClient Cliente)> ClubeAsync()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var p = await _c.LoginAsync(clube.EmailPresidente);
        return (p, api.Cliente(p.AccessToken));
    }

    private async Task CriarPlanoAsync(Sessao p, string categoria, decimal valor)
    {
        var r = await api.Cliente(p.AccessToken).PostAsJsonAsync("/api/v1/financeiro/planos", new
        {
            nome = $"Plano {categoria}", categoriaId = await _c.CategoriaIdAsync(p.AccessToken, categoria), valorMensal = valor, diaVencimento = 10,
        });
        Assert.True(r.StatusCode == HttpStatusCode.Created, await r.Content.ReadAsStringAsync());
    }

    private static async Task<JsonElement> GerarAsync(HttpClient c, string competencia)
    {
        var r = await c.PostAsJsonAsync("/api/v1/financeiro/mensalidades/gerar", new { competencia });
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        return await r.Content.ReadFromJsonAsync<JsonElement>(Cenario.Json);
    }

    private static async Task<JsonElement> MensalidadeAsync(HttpClient c, Guid associadoId, string competencia) =>
        (await c.GetFromJsonAsync<JsonElement>($"/api/v1/financeiro/mensalidades?associadoId={associadoId}&competencia={competencia}", Cenario.Json))
        .GetProperty("itens")[0];

    [Fact]
    public async Task Geracao_por_plano_da_categoria_e_idempotente()
    {
        var (p, c) = await ClubeAsync();
        await CriarPlanoAsync(p, "Sócio associado", 100m);
        await _c.CriarAssociadoAsync(p.AccessToken, "Sócio associado");
        var convidado = await _c.CriarAssociadoAsync(p.AccessToken, "Sócio convidado");

        var r1 = await GerarAsync(c, Futura);
        Assert.Equal(1, r1.GetProperty("geradas").GetInt32());
        Assert.Equal(100m, r1.GetProperty("valorTotal").GetDecimal());
        Assert.Contains(convidado.GetProperty("pessoa").GetProperty("nomeCompleto").GetString(),
            r1.GetProperty("semPlano").EnumerateArray().Select(x => x.GetString()));

        var r2 = await GerarAsync(c, Futura);
        Assert.Equal(0, r2.GetProperty("geradas").GetInt32());
        Assert.Equal(1, r2.GetProperty("jaExistentes").GetInt32());

        var dup = await c.PostAsJsonAsync("/api/v1/financeiro/planos", new
        {
            nome = "Outro", categoriaId = await _c.CategoriaIdAsync(p.AccessToken, "Sócio associado"), valorMensal = 50m, diaVencimento = 5,
        });
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);
    }

    [Fact]
    public async Task Clube_que_cobra_por_dependente_soma_o_adicional()
    {
        var (p, c) = await ClubeAsync();
        var cfg = await c.GetFromJsonAsync<Dictionary<string, object?>>("/api/v1/dependentes/configuracoes", Cenario.Json);
        cfg!["incluidosNaMensalidadeDoTitular"] = false;
        cfg["valorAdicionalPorDependente"] = 25m;
        Assert.True((await c.PutAsJsonAsync("/api/v1/dependentes/configuracoes", cfg)).IsSuccessStatusCode);
        await CriarPlanoAsync(p, "Sócio associado", 100m);
        var a = await _c.CriarAssociadoAsync(p.AccessToken);
        var id = a.GetProperty("id").GetGuid();
        await _c.CriarDependenteAsync(p.AccessToken, id, "Conjuge", 40);
        await _c.CriarDependenteAsync(p.AccessToken, id, "Filho", 8);

        await GerarAsync(c, Futura);
        var m = await MensalidadeAsync(c, id, Futura);
        Assert.Equal(150m, m.GetProperty("valor").GetDecimal());
        Assert.Equal(2, m.GetProperty("quantidadeDependentes").GetInt32());
    }

    [Fact]
    public async Task Pagamento_em_dia_gera_recibo_e_entra_no_resumo()
    {
        var (p, c) = await ClubeAsync();
        await CriarPlanoAsync(p, "Sócio associado", 100m);
        var id = (await _c.CriarAssociadoAsync(p.AccessToken)).GetProperty("id").GetGuid();
        await GerarAsync(c, Futura);
        var m = await MensalidadeAsync(c, id, Futura);
        Assert.Equal("Aberta", m.GetProperty("situacao").GetString());

        var r = await c.PostAsJsonAsync($"/api/v1/financeiro/mensalidades/{m.GetProperty("id")}/pagamento", new { forma = "Pix" });
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        var paga = await r.Content.ReadFromJsonAsync<JsonElement>(Cenario.Json);
        Assert.Equal("Paga", paga.GetProperty("situacao").GetString());
        var pagamento = paga.GetProperty("pagamento");
        Assert.Equal(1, pagamento.GetProperty("numeroRecibo").GetInt64());

        var recibo = await c.GetFromJsonAsync<JsonElement>($"/api/v1/financeiro/pagamentos/{pagamento.GetProperty("id")}/recibo", Cenario.Json);
        Assert.Equal(100m, recibo.GetProperty("valorPago").GetDecimal());
        Assert.Equal("Pix", recibo.GetProperty("forma").GetString());

        var resumo = await c.GetFromJsonAsync<JsonElement>("/api/v1/financeiro/resumo", Cenario.Json);
        Assert.Equal(100m, resumo.GetProperty("receitasMensalidades").GetDecimal());

        var denovo = await c.PostAsJsonAsync($"/api/v1/financeiro/mensalidades/{m.GetProperty("id")}/pagamento", new { forma = "Pix" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, denovo.StatusCode);
    }

    [Fact]
    public async Task Atrasada_cobra_multa_e_juros_e_aparece_na_inadimplencia()
    {
        var (p, c) = await ClubeAsync();
        await CriarPlanoAsync(p, "Sócio associado", 100m);
        var id = (await _c.CriarAssociadoAsync(p.AccessToken)).GetProperty("id").GetGuid();
        await GerarAsync(c, Passada);

        var m = await MensalidadeAsync(c, id, Passada);
        Assert.Equal("Vencida", m.GetProperty("situacao").GetString());
        var encargos = m.GetProperty("encargos").GetDecimal();
        Assert.True(encargos > 2m, $"encargos {encargos}");

        var inad = await c.GetFromJsonAsync<JsonElement>("/api/v1/financeiro/inadimplentes", Cenario.Json);
        Assert.Contains(inad.EnumerateArray(), i => i.GetProperty("associadoId").GetGuid() == id);
        var resumo = await c.GetFromJsonAsync<JsonElement>("/api/v1/financeiro/resumo", Cenario.Json);
        Assert.Equal(1, resumo.GetProperty("inadimplencia").GetProperty("associados").GetInt32());

        var menor = await c.PostAsJsonAsync($"/api/v1/financeiro/mensalidades/{m.GetProperty("id")}/pagamento", new { valor = 100m, forma = "Dinheiro" });
        Assert.Equal("pagamento.valor_insuficiente", await Cenario.CodigoProblemaAsync(menor));

        var ok = await (await c.PostAsJsonAsync($"/api/v1/financeiro/mensalidades/{m.GetProperty("id")}/pagamento", new { forma = "Dinheiro" }))
            .Content.ReadFromJsonAsync<JsonElement>(Cenario.Json);
        Assert.Equal(encargos, ok.GetProperty("pagamento").GetProperty("encargos").GetDecimal());

        var situacao = await c.GetFromJsonAsync<JsonElement>($"/api/v1/financeiro/associados/{id}", Cenario.Json);
        Assert.True(situacao.GetProperty("adimplente").GetBoolean());
    }

    [Fact]
    public async Task Estorno_reabre_e_isencao_exige_motivo()
    {
        var (p, c) = await ClubeAsync();
        await CriarPlanoAsync(p, "Sócio associado", 100m);
        var id = (await _c.CriarAssociadoAsync(p.AccessToken)).GetProperty("id").GetGuid();
        await GerarAsync(c, Futura);
        var m = await MensalidadeAsync(c, id, Futura);
        var mid = m.GetProperty("id").GetGuid();

        var paga = await (await c.PostAsJsonAsync($"/api/v1/financeiro/mensalidades/{mid}/pagamento", new { forma = "Boleto" }))
            .Content.ReadFromJsonAsync<JsonElement>(Cenario.Json);
        var pagamentoId = paga.GetProperty("pagamento").GetProperty("id").GetGuid();

        var estorno = await c.PostAsJsonAsync($"/api/v1/financeiro/pagamentos/{pagamentoId}/estornar", new { motivo = "Cheque devolvido" });
        Assert.Equal("Aberta", (await estorno.Content.ReadFromJsonAsync<JsonElement>(Cenario.Json)).GetProperty("situacao").GetString());
        var recibo = await c.GetFromJsonAsync<JsonElement>($"/api/v1/financeiro/pagamentos/{pagamentoId}/recibo", Cenario.Json);
        Assert.True(recibo.GetProperty("estornado").GetBoolean());

        var semMotivo = await c.PostAsJsonAsync($"/api/v1/financeiro/mensalidades/{mid}/isentar", new { motivo = "" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, semMotivo.StatusCode);
        var isenta = await c.PostAsJsonAsync($"/api/v1/financeiro/mensalidades/{mid}/isentar", new { motivo = "Isenção aprovada em assembleia" });
        Assert.Equal("Isenta", (await isenta.Content.ReadFromJsonAsync<JsonElement>(Cenario.Json)).GetProperty("situacao").GetString());
    }

    [Fact]
    public async Task Permissoes_e_isolamento_do_financeiro()
    {
        var (p, c) = await ClubeAsync();
        await CriarPlanoAsync(p, "Sócio associado", 100m);
        var id = (await _c.CriarAssociadoAsync(p.AccessToken)).GetProperty("id").GetGuid();
        await GerarAsync(c, Futura);
        var mid = (await MensalidadeAsync(c, id, Futura)).GetProperty("id").GetGuid();

        var (_, emailSec) = await _c.CriarUsuarioAsync(p, "Secretário");
        var secretario = api.Cliente((await _c.LoginAsync(emailSec)).AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await secretario.GetAsync("/api/v1/financeiro/mensalidades")).StatusCode);

        var (_, emailTes) = await _c.CriarUsuarioAsync(p, "Tesoureiro");
        var tesoureiro = api.Cliente((await _c.LoginAsync(emailTes)).AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await tesoureiro.GetAsync("/api/v1/financeiro/resumo")).StatusCode);

        var (outroP, outro) = await ClubeAsync();
        Assert.Equal(HttpStatusCode.NotFound,
            (await outro.PostAsJsonAsync($"/api/v1/financeiro/mensalidades/{mid}/pagamento", new { forma = "Pix" })).StatusCode);
        var listaOutro = await outro.GetFromJsonAsync<JsonElement>("/api/v1/financeiro/mensalidades", Cenario.Json);
        Assert.Equal(0, listaOutro.GetProperty("totalItens").GetInt32());
        _ = outroP;
    }

    [Fact]
    public async Task Associado_ve_so_as_proprias_mensalidades_no_app()
    {
        var (p, c) = await ClubeAsync();
        await CriarPlanoAsync(p, "Sócio associado", 100m);
        var email = $"associado.{Guid.NewGuid():N}@testes.local";
        var a = await _c.CriarAssociadoAsync(p.AccessToken, email: email);
        await _c.CriarAssociadoAsync(p.AccessToken);
        await c.PostAsJsonAsync($"/api/v1/associados/{a.GetProperty("id")}/acesso-app", new { senhaInicial = Cenario.SenhaPadrao });
        await GerarAsync(c, Futura);

        var app = api.Cliente((await _c.LoginAsync(email)).AccessToken);
        var fin = await app.GetFromJsonAsync<JsonElement>("/api/v1/portal/financeiro", Cenario.Json);
        var lista = fin.GetProperty("mensalidades").EnumerateArray().ToList();
        Assert.Single(lista);
        Assert.Equal(a.GetProperty("id").GetGuid(), lista[0].GetProperty("associadoId").GetGuid());
        Assert.Equal(HttpStatusCode.Forbidden, (await app.GetAsync("/api/v1/financeiro/mensalidades")).StatusCode);
    }

    [Fact]
    public async Task Receitas_despesas_contas_e_fluxo_de_caixa()
    {
        var (_, c) = await ClubeAsync();
        var categorias = (await c.GetFromJsonAsync<JsonElement>("/api/v1/financeiro/categorias", Cenario.Json)).EnumerateArray().ToList();
        var energia = categorias.First(x => x.GetProperty("nome").GetString() == "Energia elétrica").GetProperty("id").GetGuid();
        var eventos = categorias.First(x => x.GetProperty("nome").GetString() == "Eventos").GetProperty("id").GetGuid();

        var errada = await c.PostAsJsonAsync("/api/v1/financeiro/lancamentos", new
        {
            tipo = "Receita", categoriaId = energia, descricao = "x", valor = 10m, vencimento = Hoje,
        });
        Assert.Equal(HttpStatusCode.BadRequest, errada.StatusCode);

        await c.PostAsJsonAsync("/api/v1/financeiro/lancamentos", new
        {
            tipo = "Despesa", categoriaId = energia, descricao = "Conta de luz", valor = 350.40m, vencimento = Hoje, pagoEm = Hoje,
        });
        var receita = await (await c.PostAsJsonAsync("/api/v1/financeiro/lancamentos", new
        {
            tipo = "Receita", categoriaId = eventos, descricao = "Ingressos da feijoada", valor = 1200m, vencimento = Hoje.AddDays(5),
        })).Content.ReadFromJsonAsync<JsonElement>(Cenario.Json);
        Assert.Equal("Em aberto", receita.GetProperty("situacao").GetString());

        var resumo = await c.GetFromJsonAsync<JsonElement>("/api/v1/financeiro/resumo", Cenario.Json);
        Assert.Equal(350.40m, resumo.GetProperty("despesas").GetDecimal());
        Assert.Equal(1200m, resumo.GetProperty("aReceberOutros").GetDecimal());

        await c.PostAsync($"/api/v1/financeiro/lancamentos/{receita.GetProperty("id")}/pagar", null);
        var fluxo = await c.GetFromJsonAsync<JsonElement>("/api/v1/financeiro/fluxo-caixa", Cenario.Json);
        Assert.Equal(1200m - 350.40m, fluxo.GetProperty("saldo").GetDecimal());
        Assert.Equal(2, fluxo.GetProperty("movimentos").GetArrayLength());
    }
}
