using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ProjetoClubes.IntegrationTests.Infra;

namespace ProjetoClubes.IntegrationTests;

/// <summary>Critérios de aceitação 16 a 21: eventos, inscrições, espaços, reservas, calendário e solicitações.</summary>
[Collection(ColecaoApi.Nome)]
public class EventosTests(ApiFixture api)
{
    private readonly Cenario _c = new(api);
    private static readonly DateTimeOffset Daqui7Dias = DateTimeOffset.UtcNow.Date.AddDays(7).AddHours(14);

    private async Task<JsonElement> CriarEventoAsync(HttpClient c, int? capacidade, string publico = "AssociadosEDependentes",
        int maxAcompanhantes = 1, Guid? espacoId = null, DateTimeOffset? inicio = null)
    {
        var i = inicio ?? Daqui7Dias;
        var r = await c.PostAsJsonAsync("/api/v1/eventos", new
        {
            nome = "Almoço do Dia das Mães", inicio = i, fim = i.AddHours(5), publico, capacidade, valor = 50m,
            maximoAcompanhantes = maxAcompanhantes, espacoId,
        });
        Assert.True(r.StatusCode == HttpStatusCode.Created, await r.Content.ReadAsStringAsync());
        return await r.Content.ReadFromJsonAsync<JsonElement>(Cenario.Json);
    }

    private static async Task<HttpResponseMessage> InscreverAsync(HttpClient c, Guid eventoId, Guid pessoaId, int acompanhantes = 0) =>
        await c.PostAsJsonAsync($"/api/v1/eventos/{eventoId}/inscricoes", new { pessoaId, acompanhantes });

    private static async Task<string> SituacaoAsync(HttpResponseMessage r)
    {
        Assert.True(r.StatusCode == HttpStatusCode.Created, await r.Content.ReadAsStringAsync());
        return (await r.Content.ReadFromJsonAsync<JsonElement>(Cenario.Json)).GetProperty("situacao").GetString()!;
    }

    [Fact]
    public async Task Capacidade_conta_acompanhantes_lista_de_espera_e_promocao()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var p = await _c.LoginAsync(clube.EmailPresidente);
        var c = api.Cliente(p.AccessToken);
        var evento = (await CriarEventoAsync(c, capacidade: 3)).GetProperty("id").GetGuid();
        Guid Pessoa(JsonElement a) => a.GetProperty("pessoa").GetProperty("id").GetGuid();
        var a = await _c.CriarAssociadoAsync(p.AccessToken);
        var b = await _c.CriarAssociadoAsync(p.AccessToken);
        var d = await _c.CriarAssociadoAsync(p.AccessToken);

        var ra = await InscreverAsync(c, evento, Pessoa(a), 1);
        var inscricaoA = (await ra.Content.ReadFromJsonAsync<JsonElement>(Cenario.Json)).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Created, ra.StatusCode);                        // 2 lugares
        Assert.Equal("ListaEspera", await SituacaoAsync(await InscreverAsync(c, evento, Pessoa(b), 1))); // 2 + 2 > 3
        Assert.Equal("Confirmada", await SituacaoAsync(await InscreverAsync(c, evento, Pessoa(d), 0)));  // 3 lugares

        Assert.Equal(HttpStatusCode.Conflict, (await InscreverAsync(c, evento, Pessoa(d))).StatusCode);
        var excesso = await InscreverAsync(c, evento, Pessoa(await _c.CriarAssociadoAsync(p.AccessToken)), 2);
        Assert.Equal("evento.acompanhantes", await Cenario.CodigoProblemaAsync(excesso));

        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/v1/eventos/inscricoes/{inscricaoA}")).StatusCode);
        var detalhe = await c.GetFromJsonAsync<JsonElement>($"/api/v1/eventos/{evento}", Cenario.Json);
        var inscB = detalhe.GetProperty("inscricoes").EnumerateArray().Single(i => i.GetProperty("pessoaId").GetGuid() == Pessoa(b));
        Assert.Equal("Confirmada", inscB.GetProperty("situacao").GetString());
        Assert.Equal(3, detalhe.GetProperty("evento").GetProperty("lugaresOcupados").GetInt32());
    }

    [Fact]
    public async Task Publico_somente_associados_recusa_dependente_e_presenca_por_qr()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var p = await _c.LoginAsync(clube.EmailPresidente);
        var c = api.Cliente(p.AccessToken);
        var evento = (await CriarEventoAsync(c, capacidade: null, publico: "Associados")).GetProperty("id").GetGuid();
        var titular = await _c.CriarAssociadoAsync(p.AccessToken);
        var dep = await _c.CriarDependenteAsync(p.AccessToken, titular.GetProperty("id").GetGuid(), "Conjuge", 40);

        var r = await InscreverAsync(c, evento, dep.GetProperty("pessoa").GetProperty("id").GetGuid());
        Assert.Equal("evento.somente_associados", await Cenario.CodigoProblemaAsync(r));

        var pessoaTitular = titular.GetProperty("pessoa").GetProperty("id").GetGuid();
        await SituacaoAsync(await InscreverAsync(c, evento, pessoaTitular));
        var codigo = (await c.GetFromJsonAsync<JsonElement>($"/api/v1/associados/{titular.GetProperty("id")}/carteirinha", Cenario.Json))
            .GetProperty("qrCodeConteudo").GetString();
        var presenca = await c.PostAsJsonAsync($"/api/v1/eventos/{evento}/presenca", new { codigo });
        Assert.True(presenca.IsSuccessStatusCode, await presenca.Content.ReadAsStringAsync());
        Assert.NotEqual(JsonValueKind.Null, (await presenca.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("presencaEm").ValueKind);

        var depCodigo = (await c.GetFromJsonAsync<JsonElement>($"/api/v1/dependentes/{dep.GetProperty("id")}/carteirinha", Cenario.Json))
            .GetProperty("qrCodeConteudo").GetString();
        var semInscricao = await c.PostAsJsonAsync($"/api/v1/eventos/{evento}/presenca", new { codigo = depCodigo });
        Assert.Equal("evento.sem_inscricao", await Cenario.CodigoProblemaAsync(semInscricao));
    }

    [Fact]
    public async Task Associado_se_inscreve_pelo_app_com_dependente_e_cancelamento_de_evento()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var p = await _c.LoginAsync(clube.EmailPresidente);
        var c = api.Cliente(p.AccessToken);
        var evento = (await CriarEventoAsync(c, capacidade: 50)).GetProperty("id").GetGuid();
        var (assoc, assocId, _) = await _c.AssociadoComAppAsync(p);
        var dep = await _c.CriarDependenteAsync(p.AccessToken, assocId, "Filho", 10);
        var app = api.Cliente(assoc.AccessToken);

        var lista = await app.GetFromJsonAsync<JsonElement>("/api/v1/portal/eventos", Cenario.Json);
        Assert.Contains(lista.EnumerateArray(), e => e.GetProperty("evento").GetProperty("id").GetGuid() == evento);

        Assert.Equal("Confirmada", await SituacaoAsync(await app.PostAsJsonAsync($"/api/v1/portal/eventos/{evento}/inscricoes", new { acompanhantes = 1 })));
        Assert.Equal("Confirmada", await SituacaoAsync(await app.PostAsJsonAsync($"/api/v1/portal/eventos/{evento}/inscricoes",
            new { pessoaId = dep.GetProperty("pessoa").GetProperty("id").GetGuid(), acompanhantes = 0 })));

        // Não pode inscrever pessoa de outra família.
        var outro = await _c.CriarAssociadoAsync(p.AccessToken);
        var alheio = await app.PostAsJsonAsync($"/api/v1/portal/eventos/{evento}/inscricoes",
            new { pessoaId = outro.GetProperty("pessoa").GetProperty("id").GetGuid(), acompanhantes = 0 });
        Assert.Equal(HttpStatusCode.NotFound, alheio.StatusCode);

        lista = await app.GetFromJsonAsync<JsonElement>("/api/v1/portal/eventos", Cenario.Json);
        Assert.Equal(2, lista.EnumerateArray().Single(e => e.GetProperty("evento").GetProperty("id").GetGuid() == evento)
            .GetProperty("minhasInscricoes").GetArrayLength());

        await c.PostAsJsonAsync($"/api/v1/eventos/{evento}/cancelar", new { motivo = "Previsão de tempestade" });
        var depois = await app.PostAsJsonAsync($"/api/v1/portal/eventos/{evento}/inscricoes", new { acompanhantes = 0 });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, depois.StatusCode);
    }
}

[Collection(ColecaoApi.Nome)]
public class ReservasTests(ApiFixture api)
{
    private readonly Cenario _c = new(api);
    private static readonly DateTimeOffset Base = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(10), TimeSpan.Zero).AddHours(14);

    private static async Task<Guid> CriarEspacoAsync(HttpClient c, bool exigeAprovacao = true)
    {
        var r = await c.PostAsJsonAsync("/api/v1/espacos", new
        {
            nome = $"Salão {Guid.NewGuid():N}"[..14], tipo = "SalaoSocial", capacidade = 100, valorLocacao = 500m, taxaAdicional = 100m, exigeAprovacao,
        });
        Assert.True(r.StatusCode == HttpStatusCode.Created, await r.Content.ReadAsStringAsync());
        return (await r.Content.ReadFromJsonAsync<JsonElement>(Cenario.Json)).GetProperty("id").GetGuid();
    }

    private static Task<HttpResponseMessage> SolicitarAsync(HttpClient app, Guid espaco, DateTimeOffset inicio, DateTimeOffset fim) =>
        app.PostAsJsonAsync("/api/v1/portal/reservas", new { espacoId = espaco, inicio, fim, finalidade = "Aniversário" });

    [Fact]
    public async Task Solicitacao_pelo_app_aprovacao_e_conflito_de_horario()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var p = await _c.LoginAsync(clube.EmailPresidente);
        var c = api.Cliente(p.AccessToken);
        var espaco = await CriarEspacoAsync(c);
        var (assoc, _, _) = await _c.AssociadoComAppAsync(p);
        var app = api.Cliente(assoc.AccessToken);

        var r = await SolicitarAsync(app, espaco, Base, Base.AddHours(6));
        Assert.True(r.StatusCode == HttpStatusCode.Created, await r.Content.ReadAsStringAsync());
        var reserva = await r.Content.ReadFromJsonAsync<JsonElement>(Cenario.Json);
        Assert.Equal("Pendente", reserva.GetProperty("situacao").GetString());
        Assert.Equal(600m, reserva.GetProperty("valor").GetDecimal());

        var sobreposta = await SolicitarAsync(app, espaco, Base.AddHours(5), Base.AddHours(8));
        Assert.Equal(HttpStatusCode.Conflict, sobreposta.StatusCode);
        Assert.Equal("conflito_horario", await Cenario.CodigoProblemaAsync(sobreposta));

        var colada = await SolicitarAsync(app, espaco, Base.AddHours(6), Base.AddHours(8)); // começa quando a outra termina
        Assert.Equal(HttpStatusCode.Created, colada.StatusCode);

        var id = reserva.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await c.PostAsJsonAsync($"/api/v1/reservas/{id}/recusar", new { motivo = "" })).StatusCode);
        var aprovada = await (await c.PostAsync($"/api/v1/reservas/{id}/aprovar", null)).Content.ReadFromJsonAsync<JsonElement>(Cenario.Json);
        Assert.Equal("Aprovada", aprovada.GetProperty("situacao").GetString());

        // Cancelada libera o horário.
        Assert.Equal(HttpStatusCode.OK, (await app.PostAsJsonAsync($"/api/v1/reservas/{id}/cancelar", new { motivo = "Mudança de planos" })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await SolicitarAsync(app, espaco, Base.AddHours(1), Base.AddHours(3))).StatusCode);
    }

    [Fact]
    public async Task Evento_com_espaco_ocupa_o_periodo_inteiro()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var p = await _c.LoginAsync(clube.EmailPresidente);
        var c = api.Cliente(p.AccessToken);
        var espaco = await CriarEspacoAsync(c);
        // Feijoada beneficente de um dia para o outro (11h até 02h), com horário local de Brasília como o Web e o app enviam.
        var inicio = Base.AddHours(-3).ToOffset(TimeSpan.FromHours(-3));
        var ev = await c.PostAsJsonAsync("/api/v1/eventos", new
        {
            nome = "Feijoada beneficente", inicio, fim = inicio.AddHours(15), publico = "Geral", valor = 40m, maximoAcompanhantes = 3, espacoId = espaco,
        });
        Assert.Equal(HttpStatusCode.Created, ev.StatusCode);

        var (assoc, _, _) = await _c.AssociadoComAppAsync(p);
        var app = api.Cliente(assoc.AccessToken);
        var madrugada = await SolicitarAsync(app, espaco, inicio.AddHours(14), inicio.AddHours(16));
        Assert.Equal(HttpStatusCode.Conflict, madrugada.StatusCode);

        var outroEvento = await c.PostAsJsonAsync("/api/v1/eventos", new
        {
            nome = "Bingo", inicio = inicio.AddHours(2), fim = inicio.AddHours(4), publico = "Geral", valor = 0m, maximoAcompanhantes = 0, espacoId = espaco,
        });
        Assert.Equal(HttpStatusCode.Conflict, outroEvento.StatusCode);

        var cal = await app.GetFromJsonAsync<JsonElement>(
            $"/api/v1/reservas/calendario?de={Uri.EscapeDataString(inicio.AddDays(-1).ToString("O"))}&ate={Uri.EscapeDataString(inicio.AddDays(2).ToString("O"))}&espacoId={espaco}", Cenario.Json);
        var item = cal.EnumerateArray().Single();
        Assert.Equal("Evento", item.GetProperty("tipo").GetString());
    }

    [Fact]
    public async Task Pedidos_simultaneos_para_o_mesmo_horario_so_um_passa()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var p = await _c.LoginAsync(clube.EmailPresidente);
        var espaco = await CriarEspacoAsync(api.Cliente(p.AccessToken), exigeAprovacao: false);
        var apps = new List<HttpClient>();
        for (var i = 0; i < 5; i++) apps.Add(api.Cliente((await _c.AssociadoComAppAsync(p)).Associado.AccessToken));

        var respostas = await Task.WhenAll(apps.Select(a => SolicitarAsync(a, espaco, Base.AddDays(3), Base.AddDays(3).AddHours(4))));
        Assert.Equal(1, respostas.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(4, respostas.Count(r => r.StatusCode == HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task Banco_recusa_sobreposicao_mesmo_sem_passar_pela_api()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var p = await _c.LoginAsync(clube.EmailPresidente);
        var c = api.Cliente(p.AccessToken);
        var espaco = await CriarEspacoAsync(c, exigeAprovacao: false);
        var r = await c.PostAsJsonAsync("/api/v1/reservas", new { espacoId = espaco, inicio = Base, fim = Base.AddHours(4), finalidade = "Reunião" });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);

        // No SQLite os Ids são texto em maiúsculas e as datas são instantes UTC em ticks.
        string Id(Guid g) => g.ToString().ToUpperInvariant();
        var ex = await Assert.ThrowsAsync<SqliteException>(() => api.ExecutarSqlAsync($"""
            INSERT INTO "Reservas" ("Id","ClubeId","EspacoId","Inicio","Fim","Valor","Situacao","CriadoEm")
            VALUES ('{Id(Guid.NewGuid())}', '{Id(clube.Id)}', '{Id(espaco)}', {Base.AddHours(1).UtcTicks}, {Base.AddHours(2).UtcTicks}, 0, 'Aprovada', {DateTimeOffset.UtcNow.UtcTicks})
            """));
        Assert.Contains("conflito_horario", ex.Message);

        // Reservas canceladas não ocupam o horário.
        await api.ExecutarSqlAsync($"""
            INSERT INTO "Reservas" ("Id","ClubeId","EspacoId","Inicio","Fim","Valor","Situacao","CriadoEm")
            VALUES ('{Id(Guid.NewGuid())}', '{Id(clube.Id)}', '{Id(espaco)}', {Base.AddHours(1).UtcTicks}, {Base.AddHours(2).UtcTicks}, 0, 'Cancelada', {DateTimeOffset.UtcNow.UtcTicks})
            """);
    }

    [Fact]
    public async Task Calendario_no_app_nao_expoe_quem_reservou_e_dependente_menor_nao_reserva()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var p = await _c.LoginAsync(clube.EmailPresidente);
        var c = api.Cliente(p.AccessToken);
        var espaco = await CriarEspacoAsync(c, exigeAprovacao: false);
        await c.PostAsJsonAsync("/api/v1/reservas", new { espacoId = espaco, inicio = Base, fim = Base.AddHours(4), finalidade = "Casamento da família Silva" });

        var (assoc, assocId, _) = await _c.AssociadoComAppAsync(p);
        var app = api.Cliente(assoc.AccessToken);
        var cal = await app.GetFromJsonAsync<JsonElement>(
            $"/api/v1/reservas/calendario?de={Uri.EscapeDataString(Base.AddDays(-1).ToString("O"))}&ate={Uri.EscapeDataString(Base.AddDays(1).ToString("O"))}&espacoId={espaco}", Cenario.Json);
        Assert.Equal("Ocupado", cal.EnumerateArray().Single().GetProperty("titulo").GetString());

        // Filho de 15 anos com login próprio: menor sem permissão de reserva por padrão.
        var emailFilho = $"filho.{Guid.NewGuid():N}@testes.local";
        var filho = await _c.CriarDependenteAsync(p.AccessToken, assocId, "Filho", 15, email: emailFilho);
        await c.PostAsJsonAsync($"/api/v1/dependentes/{filho.GetProperty("id")}/acesso-app", new { senhaInicial = Cenario.SenhaPadrao });
        var appFilho = api.Cliente((await _c.LoginAsync(emailFilho)).AccessToken);
        var negado = await SolicitarAsync(appFilho, espaco, Base.AddDays(2), Base.AddDays(2).AddHours(2));
        Assert.Equal(HttpStatusCode.Forbidden, negado.StatusCode);
    }
}

[Collection(ColecaoApi.Nome)]
public class SolicitacoesTests(ApiFixture api)
{
    private readonly Cenario _c = new(api);

    [Fact]
    public async Task Protocolo_respostas_internas_e_encerramento()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var p = await _c.LoginAsync(clube.EmailPresidente);
        var c = api.Cliente(p.AccessToken);
        var (assoc, _, _) = await _c.AssociadoComAppAsync(p);
        var app = api.Cliente(assoc.AccessToken);

        var r = await app.PostAsJsonAsync("/api/v1/portal/solicitacoes", new
        {
            tipo = "ProblemaEstrutural", assunto = "Lâmpada queimada", descricao = "A lâmpada da quadra 2 está queimada.",
        });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var s = await r.Content.ReadFromJsonAsync<JsonElement>(Cenario.Json);
        var id = s.GetProperty("id").GetGuid();
        Assert.Equal($"{DateTime.UtcNow.Year}-000001", s.GetProperty("protocolo").GetString());

        var lista = await c.GetFromJsonAsync<JsonElement>("/api/v1/solicitacoes?somenteAbertas=true", Cenario.Json);
        Assert.Equal(1, lista.GetProperty("totalItens").GetInt32());

        await c.PostAsJsonAsync($"/api/v1/solicitacoes/{id}/respostas", new { texto = "Verificar estoque de lâmpadas.", interna = true });
        var resp = await c.PostAsJsonAsync($"/api/v1/solicitacoes/{id}/respostas", new { texto = "A troca está agendada para sexta." });
        Assert.Equal("EmAndamento", (await resp.Content.ReadFromJsonAsync<JsonElement>(Cenario.Json)).GetProperty("situacao").GetString());

        var noApp = await app.GetFromJsonAsync<JsonElement>($"/api/v1/portal/solicitacoes/{id}", Cenario.Json);
        var textos = noApp.GetProperty("respostas").EnumerateArray().Select(x => x.GetProperty("texto").GetString()).ToList();
        Assert.Contains("A troca está agendada para sexta.", textos);
        Assert.DoesNotContain("Verificar estoque de lâmpadas.", textos); // anotação interna não aparece no app

        Assert.Equal(HttpStatusCode.OK, (await app.PostAsJsonAsync($"/api/v1/portal/solicitacoes/{id}/respostas", new { texto = "Obrigado!" })).StatusCode);
        await c.PutAsJsonAsync($"/api/v1/solicitacoes/{id}/situacao", new { situacao = "Concluida" });
        var depois = await app.PostAsJsonAsync($"/api/v1/portal/solicitacoes/{id}/respostas", new { texto = "Mais uma coisa" });
        Assert.Equal("solicitacao.encerrada", await Cenario.CodigoProblemaAsync(depois));
    }

    [Fact]
    public async Task Associado_so_ve_as_proprias_e_outro_clube_nao_ve_nada()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var p = await _c.LoginAsync(clube.EmailPresidente);
        var (a1, _, _) = await _c.AssociadoComAppAsync(p);
        var (a2, _, _) = await _c.AssociadoComAppAsync(p);
        var s = await (await api.Cliente(a1.AccessToken).PostAsJsonAsync("/api/v1/portal/solicitacoes", new
        {
            tipo = "Sugestao", assunto = "Horário da piscina", descricao = "Abrir a piscina às 7h.",
        })).Content.ReadFromJsonAsync<JsonElement>(Cenario.Json);
        var id = s.GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.NotFound, (await api.Cliente(a2.AccessToken).GetAsync($"/api/v1/portal/solicitacoes/{id}")).StatusCode);
        Assert.Empty((await api.Cliente(a2.AccessToken).GetFromJsonAsync<JsonElement>("/api/v1/portal/solicitacoes", Cenario.Json)).EnumerateArray());

        var outro = await _c.CriarClubeComPresidenteAsync();
        var po = api.Cliente((await _c.LoginAsync(outro.EmailPresidente)).AccessToken);
        Assert.Equal(HttpStatusCode.NotFound, (await po.GetAsync($"/api/v1/solicitacoes/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await po.PostAsJsonAsync($"/api/v1/solicitacoes/{id}/respostas", new { texto = "x" })).StatusCode);
    }
}
