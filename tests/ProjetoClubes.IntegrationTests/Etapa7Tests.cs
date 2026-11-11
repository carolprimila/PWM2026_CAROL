using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ProjetoClubes.IntegrationTests.Infra;

namespace ProjetoClubes.IntegrationTests;

/// <summary>Etapa 7: comunicação e notificações.</summary>
[Collection(ColecaoApi.Nome)]
public class ComunicacaoTests(ApiFixture api)
{
    private readonly Cenario _c = new(api);

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage r, HttpStatusCode esperado = HttpStatusCode.OK)
    {
        Assert.True(r.StatusCode == esperado, $"{(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        return await r.Content.ReadFromJsonAsync<JsonElement>(Cenario.Json);
    }

    [Fact]
    public async Task Comunicado_publicado_chega_ao_app_notifica_e_registra_confirmacao()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var p = await _c.LoginAsync(clube.EmailPresidente);
        var c = api.Cliente(p.AccessToken);
        var (assoc, _, _) = await _c.AssociadoComAppAsync(p);
        var app = api.Cliente(assoc.AccessToken);

        var com = await JsonAsync(await c.PostAsJsonAsync("/api/v1/comunicados", new
        {
            titulo = "Piscina fechada para manutenção", texto = "Sexta-feira a piscina ficará fechada.", publico = "Todos",
            exigeConfirmacao = true, fixado = true, publicar = false,
        }), HttpStatusCode.Created);
        var id = com.GetProperty("id").GetGuid();
        Assert.Equal(JsonValueKind.Null, com.GetProperty("publicadoEm").ValueKind);
        Assert.Empty((await app.GetFromJsonAsync<JsonElement>("/api/v1/portal/comunicados", Cenario.Json)).EnumerateArray()); // rascunho

        await JsonAsync(await c.PostAsync($"/api/v1/comunicados/{id}/publicar", null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await c.PostAsync($"/api/v1/comunicados/{id}/publicar", null)).StatusCode);

        var caixa = await app.GetFromJsonAsync<JsonElement>("/api/v1/notificacoes", Cenario.Json);
        Assert.Equal(1, caixa.GetProperty("naoLidas").GetInt32());
        var notif = caixa.GetProperty("itens")[0];
        Assert.Equal("Comunicado", notif.GetProperty("tipo").GetString());
        Assert.Equal(id, notif.GetProperty("referenciaId").GetGuid());

        var mural = (await app.GetFromJsonAsync<JsonElement>("/api/v1/portal/comunicados", Cenario.Json)).EnumerateArray().Single();
        Assert.False(mural.GetProperty("confirmado").GetBoolean());

        var antes = await c.GetFromJsonAsync<JsonElement>($"/api/v1/comunicados/{id}", Cenario.Json);
        Assert.Equal(1, antes.GetProperty("comunicado").GetProperty("destinatarios").GetInt32());
        Assert.Single(antes.GetProperty("pendentes").EnumerateArray());

        var lido = await JsonAsync(await app.PostAsync($"/api/v1/portal/comunicados/{id}/leitura?confirmar=true", null));
        Assert.True(lido.GetProperty("confirmado").GetBoolean());
        var depois = await c.GetFromJsonAsync<JsonElement>($"/api/v1/comunicados/{id}", Cenario.Json);
        Assert.Equal(1, depois.GetProperty("comunicado").GetProperty("confirmacoes").GetInt32());
        Assert.Empty(depois.GetProperty("pendentes").EnumerateArray());

        Assert.Equal(HttpStatusCode.NoContent, (await app.PostAsync("/api/v1/notificacoes/lidas", null)).StatusCode);
        Assert.Equal(0, (await app.GetFromJsonAsync<JsonElement>("/api/v1/notificacoes", Cenario.Json)).GetProperty("naoLidas").GetInt32());
    }

    [Fact]
    public async Task Associado_nao_publica_e_outro_clube_nao_ve_comunicados()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var p = await _c.LoginAsync(clube.EmailPresidente);
        var (assoc, _, _) = await _c.AssociadoComAppAsync(p);
        var id = (await JsonAsync(await api.Cliente(p.AccessToken).PostAsJsonAsync("/api/v1/comunicados", new
        {
            titulo = "Assembleia em dezembro", texto = "Reserve a data.", publico = "Titulares", exigeConfirmacao = false, fixado = false, publicar = true,
        }), HttpStatusCode.Created)).GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.Forbidden, (await api.Cliente(assoc.AccessToken).PostAsJsonAsync("/api/v1/comunicados", new
        {
            titulo = "x", texto = "y", publico = "Todos", exigeConfirmacao = false, fixado = false,
        })).StatusCode);

        var outro = await _c.CriarClubeComPresidenteAsync();
        var p2 = await _c.LoginAsync(outro.EmailPresidente);
        Assert.Equal(HttpStatusCode.NotFound, (await api.Cliente(p2.AccessToken).GetAsync($"/api/v1/comunicados/{id}")).StatusCode);
        var (assoc2, _, _) = await _c.AssociadoComAppAsync(p2);
        var mural = await api.Cliente(assoc2.AccessToken).GetFromJsonAsync<JsonElement>("/api/v1/portal/comunicados", Cenario.Json);
        Assert.DoesNotContain(mural.EnumerateArray(), x => x.GetProperty("id").GetGuid() == id);
        Assert.Equal(HttpStatusCode.NotFound, (await api.Cliente(assoc2.AccessToken).PostAsync($"/api/v1/portal/comunicados/{id}/leitura", null)).StatusCode);
    }

    [Fact]
    public async Task Aprovacao_de_reserva_e_resposta_a_solicitacao_notificam_o_associado()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var p = await _c.LoginAsync(clube.EmailPresidente);
        var c = api.Cliente(p.AccessToken);
        var (assoc, _, _) = await _c.AssociadoComAppAsync(p);
        var app = api.Cliente(assoc.AccessToken);

        var espaco = (await JsonAsync(await c.PostAsJsonAsync("/api/v1/espacos", new
        {
            nome = "Quiosque 2", tipo = "Quiosque", capacidade = 20, valorLocacao = 50m, taxaAdicional = 0m, exigeAprovacao = true,
        }), HttpStatusCode.Created)).GetProperty("id").GetGuid();
        var inicio = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(12), TimeSpan.Zero).AddHours(15);
        var reserva = (await JsonAsync(await app.PostAsJsonAsync("/api/v1/portal/reservas", new
        {
            espacoId = espaco, inicio, fim = inicio.AddHours(4), finalidade = "Aniversário",
        }), HttpStatusCode.Created)).GetProperty("id").GetGuid();
        await JsonAsync(await c.PostAsync($"/api/v1/reservas/{reserva}/aprovar", null));

        var protocolo = (await JsonAsync(await app.PostAsJsonAsync("/api/v1/portal/solicitacoes", new
        {
            tipo = "Solicitacao", assunto = "Segunda via do boleto", descricao = "Preciso da segunda via.",
        }), HttpStatusCode.Created)).GetProperty("id").GetGuid();
        await JsonAsync(await c.PostAsJsonAsync($"/api/v1/solicitacoes/{protocolo}/respostas", new { texto = "Anotação só da equipe", interna = true }));
        await JsonAsync(await c.PostAsJsonAsync($"/api/v1/solicitacoes/{protocolo}/respostas", new { texto = "Enviamos por e-mail.", interna = false }));

        var itens = (await app.GetFromJsonAsync<JsonElement>("/api/v1/notificacoes", Cenario.Json)).GetProperty("itens").EnumerateArray().ToList();
        Assert.Contains(itens, n => n.GetProperty("titulo").GetString() == "Reserva aprovada" && n.GetProperty("texto").GetString()!.Contains("Quiosque 2"));
        var resposta = Assert.Single(itens, n => n.GetProperty("tipo").GetString() == "Solicitacao"); // a anotação interna não notifica
        Assert.Equal("Enviamos por e-mail.", resposta.GetProperty("texto").GetString());
    }
}

/// <summary>Etapa 7: assembleias e votações.</summary>
[Collection(ColecaoApi.Nome)]
public class AssembleiasTests(ApiFixture api)
{
    private readonly Cenario _c = new(api);

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage r, HttpStatusCode esperado = HttpStatusCode.OK)
    {
        Assert.True(r.StatusCode == esperado, $"{(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        return await r.Content.ReadFromJsonAsync<JsonElement>(Cenario.Json);
    }

    private static async Task<JsonElement> CriarAsync(HttpClient c, bool exigeAdimplencia = false) =>
        await JsonAsync(await c.PostAsJsonAsync("/api/v1/assembleias", new
        {
            titulo = "Assembleia Geral Ordinária", tipo = "Ordinaria", dataHora = DateTimeOffset.UtcNow.AddDays(15), local = "Salão social",
            convocacao = "Ficam convocados os associados...", exigeAdimplencia,
        }), HttpStatusCode.Created);

    private async Task<string> QrAsync(HttpClient c, string tipo, Guid id) =>
        (await c.GetFromJsonAsync<JsonElement>($"/api/v1/{tipo}/{id}/carteirinha", Cenario.Json)).GetProperty("qrCodeConteudo").GetString()!;

    [Fact]
    public async Task Fluxo_completo_de_convocacao_presenca_votacao_apuracao_e_ata()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var p = await _c.LoginAsync(clube.EmailPresidente);
        var c = api.Cliente(p.AccessToken);
        var (votante, votanteId, _) = await _c.AssociadoComAppAsync(p);
        var (convidado, convidadoId, _) = await _c.AssociadoComAppAsync(p, "Sócio convidado"); // categoria sem direito a voto
        var dependente = (await _c.CriarDependenteAsync(p.AccessToken, votanteId, "Filho", 20)).GetProperty("id").GetGuid();
        var appVotante = api.Cliente(votante.AccessToken);

        var a = await CriarAsync(c);
        var id = a.GetProperty("id").GetGuid();
        Assert.Equal("Rascunho", a.GetProperty("situacao").GetString());
        a = await JsonAsync(await c.PostAsJsonAsync($"/api/v1/assembleias/{id}/pautas", new { titulo = "Prestação de contas 2026", comVotacao = true }));
        await JsonAsync(await c.PostAsJsonAsync($"/api/v1/assembleias/{id}/pautas", new { titulo = "Informes da diretoria", comVotacao = false }));
        var pauta = a.GetProperty("pautas")[0].GetProperty("id").GetGuid();
        Assert.Empty((await appVotante.GetFromJsonAsync<JsonElement>("/api/v1/portal/assembleias", Cenario.Json)).EnumerateArray());

        await JsonAsync(await c.PostAsync($"/api/v1/assembleias/{id}/convocar", null));
        var notifs = (await appVotante.GetFromJsonAsync<JsonElement>("/api/v1/notificacoes", Cenario.Json)).GetProperty("itens").EnumerateArray().ToList();
        Assert.Contains(notifs, n => n.GetProperty("tipo").GetString() == "Assembleia");
        await JsonAsync(await c.PostAsync($"/api/v1/assembleias/{id}/instalar", null));

        // Presenças: titular apto pelo QR; QR de dependente recusado; categoria sem voto entra, mas inapto.
        var presenca = await JsonAsync(await c.PostAsJsonAsync($"/api/v1/assembleias/{id}/presencas",
            new { codigo = await QrAsync(c, "associados", votanteId) }), HttpStatusCode.Created);
        Assert.True(presenca.GetProperty("aptoVotar").GetBoolean());
        var duplicada = await c.PostAsJsonAsync($"/api/v1/assembleias/{id}/presencas", new { associadoId = votanteId });
        Assert.Equal(HttpStatusCode.Conflict, duplicada.StatusCode);
        var dep = await c.PostAsJsonAsync($"/api/v1/assembleias/{id}/presencas", new { codigo = await QrAsync(c, "dependentes", dependente) });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, dep.StatusCode);
        Assert.Equal("assembleia.dependente", await Cenario.CodigoProblemaAsync(dep));
        var inapto = await JsonAsync(await c.PostAsJsonAsync($"/api/v1/assembleias/{id}/presencas", new { associadoId = convidadoId }), HttpStatusCode.Created);
        Assert.False(inapto.GetProperty("aptoVotar").GetBoolean());
        Assert.Contains("categoria", inapto.GetProperty("motivoInapto").GetString());

        var cedo = await appVotante.PostAsJsonAsync($"/api/v1/portal/assembleias/{id}/pautas/{pauta}/voto", new { opcao = 0 });
        Assert.Equal("voto.fechado", await Cenario.CodigoProblemaAsync(cedo));

        await JsonAsync(await c.PostAsync($"/api/v1/assembleias/{id}/pautas/{pauta}/abrir-votacao", null));
        notifs = (await appVotante.GetFromJsonAsync<JsonElement>("/api/v1/notificacoes", Cenario.Json)).GetProperty("itens").EnumerateArray().ToList();
        Assert.Contains(notifs, n => n.GetProperty("tipo").GetString() == "Votacao");

        var votou = await JsonAsync(await appVotante.PostAsJsonAsync($"/api/v1/portal/assembleias/{id}/pautas/{pauta}/voto", new { opcao = 0 }));
        var minhaPauta = votou.GetProperty("pautas")[0];
        Assert.True(minhaPauta.GetProperty("votei").GetBoolean());
        Assert.Equal(JsonValueKind.Null, minhaPauta.GetProperty("apuracao").ValueKind); // sem parcial durante a votação
        Assert.Equal(HttpStatusCode.Conflict,
            (await appVotante.PostAsJsonAsync($"/api/v1/portal/assembleias/{id}/pautas/{pauta}/voto", new { opcao = 1 })).StatusCode);
        var semDireito = await api.Cliente(convidado.AccessToken).PostAsJsonAsync($"/api/v1/portal/assembleias/{id}/pautas/{pauta}/voto", new { opcao = 1 });
        Assert.Equal("voto.inapto", await Cenario.CodigoProblemaAsync(semDireito));

        var admin = await c.GetFromJsonAsync<JsonElement>($"/api/v1/assembleias/{id}", Cenario.Json);
        Assert.Equal(1, admin.GetProperty("pautas")[0].GetProperty("totalVotos").GetInt32());
        Assert.Equal(JsonValueKind.Null, admin.GetProperty("pautas")[0].GetProperty("apuracao").ValueKind);
        Assert.Equal(2, admin.GetProperty("presentes").GetInt32());
        Assert.Equal(1, admin.GetProperty("aptos").GetInt32());

        // Não encerra com votação aberta.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await c.PostAsync($"/api/v1/assembleias/{id}/encerrar", null)).StatusCode);
        var fechada = await JsonAsync(await c.PostAsync($"/api/v1/assembleias/{id}/pautas/{pauta}/encerrar-votacao", null));
        var apuracao = fechada.GetProperty("pautas")[0].GetProperty("apuracao").EnumerateArray().ToList();
        Assert.Equal(1, apuracao.Single(x => x.GetProperty("opcao").GetString() == "Sim").GetProperty("votos").GetInt32());
        Assert.Equal(0, apuracao.Single(x => x.GetProperty("opcao").GetString() == "Não").GetProperty("votos").GetInt32());

        await JsonAsync(await c.PostAsJsonAsync($"/api/v1/assembleias/{id}/pautas/{pauta}/decisao", new { texto = "Contas aprovadas." }));
        var encerrada = await JsonAsync(await c.PostAsync($"/api/v1/assembleias/{id}/encerrar", null));
        var sugerida = encerrada.GetProperty("ataSugerida").GetString()!;
        Assert.Contains("Prestação de contas 2026", sugerida);
        Assert.Contains("Sim: 1", sugerida);
        Assert.Contains("Contas aprovadas.", sugerida);
        Assert.Contains($"/{DateTimeOffset.UtcNow.AddDays(15).Year} às", sugerida); // ata com a data completa

        var comAta = await JsonAsync(await c.PostAsJsonAsync($"/api/v1/assembleias/{id}/ata", new { texto = sugerida }));
        Assert.NotEqual(JsonValueKind.Null, comAta.GetProperty("ataRegistradaEm").ValueKind);
        var outra = await c.PostAsJsonAsync($"/api/v1/assembleias/{id}/ata", new { texto = "Ata alterada" });
        Assert.Equal("assembleia.ata_registrada", await Cenario.CodigoProblemaAsync(outra));

        var portal = (await appVotante.GetFromJsonAsync<JsonElement>("/api/v1/portal/assembleias", Cenario.Json))[0];
        Assert.Equal("Encerrada", portal.GetProperty("situacao").GetString());
        Assert.Equal(3, portal.GetProperty("pautas")[0].GetProperty("apuracao").GetArrayLength());
    }

    [Fact]
    public async Task Adimplencia_exigida_isolamento_e_permissoes()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var p = await _c.LoginAsync(clube.EmailPresidente);
        var c = api.Cliente(p.AccessToken);
        var r = await c.PostAsJsonAsync("/api/v1/financeiro/planos", new
        {
            nome = "Plano associado", categoriaId = await _c.CategoriaIdAsync(p.AccessToken, "Sócio associado"), valorMensal = 100m, diaVencimento = 10,
        });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var (assoc, assocId, _) = await _c.AssociadoComAppAsync(p);
        var passada = DateTime.UtcNow.AddMonths(-3).ToString("yyyy-MM");
        Assert.True((await c.PostAsJsonAsync("/api/v1/financeiro/mensalidades/gerar", new { competencia = passada })).IsSuccessStatusCode);

        var id = (await CriarAsync(c, exigeAdimplencia: true)).GetProperty("id").GetGuid();
        await JsonAsync(await c.PostAsJsonAsync($"/api/v1/assembleias/{id}/pautas", new { titulo = "Reforma do estatuto", comVotacao = true }));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, // só depois de convocada recebe presença
            (await c.PostAsJsonAsync($"/api/v1/assembleias/{id}/presencas", new { associadoId = assocId })).StatusCode);
        await JsonAsync(await c.PostAsync($"/api/v1/assembleias/{id}/convocar", null));
        var presenca = await JsonAsync(await c.PostAsJsonAsync($"/api/v1/assembleias/{id}/presencas", new { associadoId = assocId }), HttpStatusCode.Created);
        Assert.False(presenca.GetProperty("aptoVotar").GetBoolean());
        Assert.Contains("vencidas", presenca.GetProperty("motivoInapto").GetString());

        Assert.Equal(HttpStatusCode.Forbidden, (await api.Cliente(assoc.AccessToken).PostAsync($"/api/v1/assembleias/{id}/instalar", null)).StatusCode);
        var outro = await _c.CriarClubeComPresidenteAsync();
        var p2 = await _c.LoginAsync(outro.EmailPresidente);
        var c2 = api.Cliente(p2.AccessToken);
        Assert.Equal(HttpStatusCode.NotFound, (await c2.GetAsync($"/api/v1/assembleias/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c2.PostAsJsonAsync($"/api/v1/assembleias/{id}/presencas", new { associadoId = assocId })).StatusCode);
        Assert.DoesNotContain((await c2.GetFromJsonAsync<JsonElement>("/api/v1/assembleias", Cenario.Json)).EnumerateArray(),
            x => x.GetProperty("id").GetGuid() == id);
    }
}

/// <summary>Etapa 7: estoque, patrimônio e funcionários.</summary>
[Collection(ColecaoApi.Nome)]
public class OperacionalTests(ApiFixture api)
{
    private readonly Cenario _c = new(api);

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage r, HttpStatusCode esperado = HttpStatusCode.OK)
    {
        Assert.True(r.StatusCode == esperado, $"{(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        return await r.Content.ReadFromJsonAsync<JsonElement>(Cenario.Json);
    }

    private async Task<HttpClient> PresidenteAsync()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        return api.Cliente((await _c.LoginAsync(clube.EmailPresidente)).AccessToken);
    }

    private static async Task<Guid> ProdutoAsync(HttpClient c, string nome, decimal minimo, decimal inicial) =>
        (await JsonAsync(await c.PostAsJsonAsync("/api/v1/estoque/produtos", new
        {
            nome, unidade = "kg", estoqueMinimo = minimo, categoria = "Piscina", quantidadeInicial = inicial,
        }), HttpStatusCode.Created)).GetProperty("id").GetGuid();

    [Fact]
    public async Task Estoque_movimenta_recusa_saldo_negativo_e_compra_gera_entrada()
    {
        var c = await PresidenteAsync();
        var cloro = await ProdutoAsync(c, "Cloro granulado", 10, 5);
        async Task<JsonElement> CloroAsync() => (await c.GetFromJsonAsync<JsonElement>("/api/v1/estoque/produtos", Cenario.Json)).EnumerateArray().Single();
        Assert.Equal(JsonValueKind.Null, (await CloroAsync()).GetProperty("compraEmAberto").ValueKind); // sem pedido não é "0"
        var saida = await c.PostAsJsonAsync($"/api/v1/estoque/produtos/{cloro}/movimentacoes", new { tipo = "Saida", quantidade = 6 });
        Assert.Equal("estoque.insuficiente", await Cenario.CodigoProblemaAsync(saida));
        var p = await JsonAsync(await c.PostAsJsonAsync($"/api/v1/estoque/produtos/{cloro}/movimentacoes", new { tipo = "Entrada", quantidade = 3 }));
        Assert.Equal(8m, p.GetProperty("quantidade").GetDecimal());
        Assert.True(p.GetProperty("abaixoDoMinimo").GetBoolean());

        var geradas = await JsonAsync(await c.PostAsync("/api/v1/estoque/compras/abaixo-do-minimo", null));
        var compra = geradas.EnumerateArray().Single();
        Assert.Equal(12m, compra.GetProperty("quantidade").GetDecimal()); // dobro do mínimo menos o saldo
        Assert.Equal(12m, (await CloroAsync()).GetProperty("compraEmAberto").GetDecimal());
        Assert.Empty((await JsonAsync(await c.PostAsync("/api/v1/estoque/compras/abaixo-do-minimo", null))).EnumerateArray()); // já há pedido aberto

        var compraId = compra.GetProperty("id").GetGuid();
        await JsonAsync(await c.PostAsJsonAsync($"/api/v1/estoque/compras/{compraId}/atender", new { quantidadeRecebida = 12, valorTotal = 240m }));
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await c.PostAsJsonAsync($"/api/v1/estoque/compras/{compraId}/atender", new { quantidadeRecebida = 12 })).StatusCode);
        var produto = await CloroAsync();
        Assert.Equal(20m, produto.GetProperty("quantidade").GetDecimal());
        Assert.False(produto.GetProperty("abaixoDoMinimo").GetBoolean());
        Assert.Equal(JsonValueKind.Null, produto.GetProperty("compraEmAberto").ValueKind);
        var movs = await c.GetFromJsonAsync<JsonElement>($"/api/v1/estoque/movimentacoes?produtoId={cloro}", Cenario.Json);
        Assert.Equal([20m, 8m, 5m], movs.EnumerateArray().Select(m => m.GetProperty("saldoApos").GetDecimal()));
    }

    [Fact]
    public async Task Saidas_simultaneas_nunca_deixam_o_saldo_negativo()
    {
        var c = await PresidenteAsync();
        var gelo = await ProdutoAsync(c, "Gelo", 0, 10);
        var tarefas = Enumerable.Range(0, 8).Select(_ =>
            c.PostAsJsonAsync($"/api/v1/estoque/produtos/{gelo}/movimentacoes", new { tipo = "Saida", quantidade = 2, motivo = "Bar" }));
        var respostas = await Task.WhenAll(tarefas);
        Assert.Equal(5, respostas.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(3, respostas.Count(r => r.StatusCode == HttpStatusCode.UnprocessableEntity));
        var produto = (await c.GetFromJsonAsync<JsonElement>("/api/v1/estoque/produtos", Cenario.Json)).EnumerateArray().Single();
        Assert.Equal(0m, produto.GetProperty("quantidade").GetDecimal());

        // O banco recusa saldo negativo mesmo sem passar pela API.
        var ex = await Assert.ThrowsAsync<SqliteException>(() =>
            api.ExecutarSqlAsync($"UPDATE \"Produtos\" SET \"Quantidade\" = -1 WHERE \"Id\" = '{gelo.ToString().ToUpperInvariant()}'"));
        Assert.Equal(275, ex.SqliteExtendedErrorCode); // SQLITE_CONSTRAINT_CHECK
    }

    [Fact]
    public async Task Patrimonio_com_codigo_sequencial_manutencao_baixa_e_historico()
    {
        var c = await PresidenteAsync();
        async Task<JsonElement> Bem(string nome, decimal valor) => await JsonAsync(await c.PostAsJsonAsync("/api/v1/patrimonio", new
        {
            nome, localizacao = "Depósito", valorAquisicao = valor, dataAquisicao = "2024-03-01", estado = "Bom",
        }), HttpStatusCode.Created);
        var cortador = await Bem("Cortador de grama", 2500m);
        var mesa = await Bem("Mesa de sinuca", 4000m);
        Assert.Equal("PAT-000001", cortador.GetProperty("codigo").GetString());
        Assert.Equal("PAT-000002", mesa.GetProperty("codigo").GetString());
        var id = cortador.GetProperty("id").GetGuid();

        await JsonAsync(await c.PostAsJsonAsync($"/api/v1/patrimonio/{id}/movimentar", new { localizacao = "Garagem", estado = "Regular" }));
        var emManutencao = await JsonAsync(await c.PostAsJsonAsync($"/api/v1/patrimonio/{id}/manutencoes",
            new { descricao = "Troca da lâmina", inicio = "2026-11-01", fornecedor = "Oficina Silva" }));
        Assert.Equal("EmManutencao", emManutencao.GetProperty("situacao").GetString());
        var manutencao = emManutencao.GetProperty("manutencoes")[0].GetProperty("id").GetGuid();
        var resumo = await c.GetFromJsonAsync<JsonElement>("/api/v1/patrimonio/resumo", Cenario.Json);
        Assert.Equal(1, resumo.GetProperty("emManutencao").GetInt32());
        Assert.Equal(6500m, resumo.GetProperty("valorEmUso").GetDecimal());

        var concluido = await JsonAsync(await c.PostAsJsonAsync($"/api/v1/patrimonio/{id}/manutencoes/{manutencao}/concluir",
            new { fim = "2026-11-05", custo = 180m, estado = "Bom" }));
        Assert.Equal(180m, concluido.GetProperty("custoManutencoes").GetDecimal());
        var baixado = await JsonAsync(await c.PostAsJsonAsync($"/api/v1/patrimonio/{id}/baixar", new { motivo = "Substituído" }));
        Assert.Equal("Baixado", baixado.GetProperty("situacao").GetString());
        var eventos = baixado.GetProperty("historico").EnumerateArray().Select(h => h.GetProperty("evento").GetString()).ToList();
        Assert.Equal(["Baixa", "ManutencaoConcluida", "Manutencao", "Movimentacao", "Cadastro"], eventos);
        Assert.Contains("Garagem", baixado.GetProperty("historico")[3].GetProperty("descricao").GetString());

        var lista = await c.GetFromJsonAsync<JsonElement>("/api/v1/patrimonio", Cenario.Json);
        Assert.Equal(1, lista.GetProperty("totalItens").GetInt32()); // baixados saem da lista padrão
    }

    [Fact]
    public async Task Funcionarios_com_ferias_desligamento_permissao_e_isolamento()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var p = await _c.LoginAsync(clube.EmailPresidente);
        var c = api.Cliente(p.AccessToken);
        var cpf = Cenario.NovoCpf();
        var f = await JsonAsync(await c.PostAsJsonAsync("/api/v1/funcionarios", new
        {
            nome = "Marta Lima", cpf, cargo = "Recepcionista", departamento = "Secretaria", telefone = "45 99999-0000",
            escala = "Seg a Sex, 8h às 17h", admissao = "2023-02-01",
        }), HttpStatusCode.Created);
        var id = f.GetProperty("id").GetGuid();
        var dup = await c.PostAsJsonAsync("/api/v1/funcionarios", new { nome = "Outra", cpf, cargo = "Zeladora", admissao = "2024-01-01" });
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var ferias = await JsonAsync(await c.PostAsJsonAsync($"/api/v1/funcionarios/{id}/ausencias",
            new { tipo = "Ferias", inicio = hoje.AddDays(-2), fim = hoje.AddDays(10) }));
        Assert.Equal("Ferias", ferias.GetProperty("situacao").GetString());
        var sobreposta = await c.PostAsJsonAsync($"/api/v1/funcionarios/{id}/ausencias",
            new { tipo = "Afastamento", inicio = hoje.AddDays(5), fim = hoje.AddDays(15) });
        Assert.Equal("funcionario.ausencia_sobreposta", await Cenario.CodigoProblemaAsync(sobreposta));
        var lista = await c.GetFromJsonAsync<JsonElement>("/api/v1/funcionarios", Cenario.Json);
        Assert.Equal("Ferias", lista.GetProperty("itens")[0].GetProperty("situacao").GetString());

        await JsonAsync(await c.PostAsJsonAsync($"/api/v1/funcionarios/{id}/desligar", new { data = hoje, motivo = "Pedido de demissão" }));
        Assert.Equal(0, (await c.GetFromJsonAsync<JsonElement>("/api/v1/funcionarios", Cenario.Json)).GetProperty("totalItens").GetInt32());
        var desligados = await c.GetFromJsonAsync<JsonElement>("/api/v1/funcionarios?situacao=Desligado", Cenario.Json);
        Assert.Equal(1, desligados.GetProperty("totalItens").GetInt32());
        var detalhe = await c.GetFromJsonAsync<JsonElement>($"/api/v1/funcionarios/{id}", Cenario.Json);
        Assert.Equal(["Desligamento", "Ferias", "Admissao"],
            detalhe.GetProperty("historico").EnumerateArray().Select(h => h.GetProperty("evento").GetString()));

        var (assoc, _, _) = await _c.AssociadoComAppAsync(p);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Cliente(assoc.AccessToken).GetAsync("/api/v1/funcionarios")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Cliente(assoc.AccessToken).GetAsync("/api/v1/estoque/produtos")).StatusCode);
        var outro = await _c.CriarClubeComPresidenteAsync();
        var c2 = api.Cliente((await _c.LoginAsync(outro.EmailPresidente)).AccessToken);
        Assert.Equal(HttpStatusCode.NotFound, (await c2.GetAsync($"/api/v1/funcionarios/{id}")).StatusCode);
    }
}
