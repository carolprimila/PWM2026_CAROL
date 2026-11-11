using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ProjetoClubes.IntegrationTests.Infra;

namespace ProjetoClubes.IntegrationTests;

/// <summary>
/// Pedidos do cliente após a Etapa 8: contrato do associado gerando as mensalidades, dependente que vira associado,
/// filtro por gênero, eventos com vários locais e divulgação, limite de reservas por tipo de espaço e diretoria com mandato.
/// </summary>
[Collection(ColecaoApi.Nome)]
public class NovidadesTests(ApiFixture api)
{
    private readonly Cenario _c = new(api);

    private static async Task<JsonElement> OkAsync(HttpResponseMessage r)
    {
        Assert.True(r.IsSuccessStatusCode, $"{(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        return r.StatusCode == HttpStatusCode.NoContent ? default : await r.Content.ReadFromJsonAsync<JsonElement>(Cenario.Json);
    }

    private async Task<(Sessao P, HttpClient C)> ClubeAsync()
    {
        var clube = await _c.CriarClubeComPresidenteAsync();
        var p = await _c.LoginAsync(clube.EmailPresidente);
        return (p, api.Cliente(p.AccessToken));
    }

    private static async Task<Guid> EspacoAsync(HttpClient c, string tipo, int capacidade, int? limite = null) =>
        (await OkAsync(await c.PostAsJsonAsync("/api/v1/espacos", new
        {
            nome = $"{tipo} {Guid.NewGuid():N}"[..20], tipo, capacidade, valorLocacao = 0m, taxaAdicional = 0m, exigeAprovacao = false,
            limiteReservasPorMes = limite,
        }))).GetProperty("id").GetGuid();

    [Fact]
    public async Task Contrato_aprovado_gera_12_parcelas_e_desligamento_cancela_as_futuras()
    {
        var (p, c) = await ClubeAsync();
        await OkAsync(await c.PostAsJsonAsync("/api/v1/financeiro/planos", new
        {
            nome = "Plano sócio", categoriaId = await _c.CategoriaIdAsync(p.AccessToken, "Sócio associado"), valorMensal = 150m, diaVencimento = 10,
        }));
        var a = await _c.CriarAssociadoAsync(p.AccessToken);
        var id = a.GetProperty("id").GetGuid();

        var contrato = await OkAsync(await c.PostAsJsonAsync($"/api/v1/associados/{id}/contratos", new { }));
        Assert.Equal("EmAnalise", contrato.GetProperty("situacao").GetString());
        Assert.Equal(150m, contrato.GetProperty("valorMensal").GetDecimal());
        Assert.Equal(0, contrato.GetProperty("parcelas").GetArrayLength());
        var outro = await c.PostAsJsonAsync($"/api/v1/associados/{id}/contratos", new { });
        Assert.Equal("contrato.em_analise", await Cenario.CodigoProblemaAsync(outro));

        var cid = contrato.GetProperty("id").GetGuid();
        var aprovado = await OkAsync(await c.PostAsync($"/api/v1/contratos/{cid}/aprovar", null));
        Assert.Equal("Aprovado", aprovado.GetProperty("situacao").GetString());
        var parcelas = aprovado.GetProperty("parcelas").EnumerateArray().ToList();
        Assert.Equal(12, parcelas.Count);
        Assert.All(parcelas, m =>
        {
            Assert.Equal(contrato.GetProperty("numero").GetString(), m.GetProperty("contrato").GetString());
            Assert.Equal(0m, m.GetProperty("multa").GetDecimal());
            Assert.Equal(10, m.GetProperty("vencimento").GetDateTime().Day);
        });

        // Renovação: novo contrato do ano seguinte, em análise, começando no dia seguinte ao fim do atual.
        var renovacao = await OkAsync(await c.PostAsJsonAsync($"/api/v1/contratos/{cid}/renovar", new { }));
        Assert.Equal("EmAnalise", renovacao.GetProperty("situacao").GetString());
        Assert.Equal(DateOnly.Parse(aprovado.GetProperty("fim").GetString()!).AddDays(1),
            DateOnly.Parse(renovacao.GetProperty("inicio").GetString()!));
        await OkAsync(await c.PostAsJsonAsync($"/api/v1/contratos/{renovacao.GetProperty("id")}/reprovar", new { motivo = "Valor a rever" }));

        var desligado = await OkAsync(await c.PostAsJsonAsync($"/api/v1/contratos/{cid}/desligar", new { motivo = "Mudou de cidade" }));
        Assert.Equal("Desligado", desligado.GetProperty("situacao").GetString());
        Assert.All(desligado.GetProperty("parcelas").EnumerateArray(), m => Assert.Equal("Cancelada", m.GetProperty("situacao").GetString()));
        var assoc = await c.GetFromJsonAsync<JsonElement>($"/api/v1/associados/{id}", Cenario.Json);
        Assert.Equal("Inativo", assoc.GetProperty("situacao").GetString());

        var historico = await c.GetFromJsonAsync<JsonElement>($"/api/v1/associados/{id}/historico", Cenario.Json);
        Assert.Contains(historico.EnumerateArray(), h => h.GetProperty("descricao").GetString()!.Contains("desligado")
                                                         && h.GetProperty("usuarioNome").ValueKind == JsonValueKind.String);
    }

    [Fact]
    public async Task Dependente_maior_vira_associado_e_filtro_por_genero()
    {
        var (p, c) = await ClubeAsync();
        var titular = await _c.CriarAssociadoAsync(p.AccessToken);
        var conjuge = await _c.CriarDependenteAsync(p.AccessToken, titular.GetProperty("id").GetGuid(), "Conjuge", 40, cpf: Cenario.NovoCpf());
        var categoria = await _c.CategoriaIdAsync(p.AccessToken, "Sócio associado");

        var novo = await OkAsync(await c.PostAsJsonAsync($"/api/v1/dependentes/{conjuge.GetProperty("id")}/tornar-associado", new { categoriaId = categoria }));
        Assert.Equal(conjuge.GetProperty("pessoa").GetProperty("id").GetGuid(), novo.GetProperty("pessoa").GetProperty("id").GetGuid());
        Assert.Equal("Ativo", novo.GetProperty("situacao").GetString());
        var dep = await c.GetFromJsonAsync<JsonElement>($"/api/v1/dependentes/{conjuge.GetProperty("id")}", Cenario.Json);
        Assert.Equal("Inativo", dep.GetProperty("situacao").GetString());

        var maria = await OkAsync(await c.PostAsJsonAsync("/api/v1/associados", new
        {
            pessoa = new
            {
                nomeCompleto = "Maria Filtro Genero", cpf = Cenario.NovoCpf(), dataNascimento = "1990-05-01", sexo = "Feminino",
                profissao = "Engenheira", endereco = new { logradouro = "Rua A", numero = "10", cidade = "Foz do Iguaçu", uf = "PR" },
            },
            categoriaId = categoria,
        }));
        Assert.Equal("Engenheira", maria.GetProperty("pessoa").GetProperty("profissao").GetString());
        string[] Nomes(JsonElement pg) => pg.GetProperty("itens").EnumerateArray().Select(x => x.GetProperty("nome").GetString()!).ToArray();
        Assert.Contains("Maria Filtro Genero", Nomes(await c.GetFromJsonAsync<JsonElement>("/api/v1/associados?sexo=Feminino", Cenario.Json)));
        Assert.DoesNotContain("Maria Filtro Genero", Nomes(await c.GetFromJsonAsync<JsonElement>("/api/v1/associados?sexo=Masculino", Cenario.Json)));
    }

    [Fact]
    public async Task Evento_com_varios_locais_agenda_capacidade_e_divulgacao()
    {
        var (p, c) = await ClubeAsync();
        var salao = await EspacoAsync(c, "SalaoSocial", 50);
        var quadra = await EspacoAsync(c, "Quadra", 30);
        var inicio = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(20), TimeSpan.Zero).AddHours(15);
        var fim = inicio.AddHours(4);

        var grande = await c.PostAsJsonAsync("/api/v1/eventos", new
        { nome = "Festa junina", inicio, fim, publico = "AssociadosEDependentes", capacidade = 70, valor = 0m, maximoAcompanhantes = 1, espacoId = salao });
        Assert.Equal("evento.capacidade", await Cenario.CodigoProblemaAsync(grande));

        var evento = await OkAsync(await c.PostAsJsonAsync("/api/v1/eventos", new
        { nome = "Festa junina", inicio, fim, publico = "AssociadosEDependentes", capacidade = 50, valor = 0m, maximoAcompanhantes = 1, espacoId = salao }));
        var id = evento.GetProperty("id").GetGuid();

        var url = $"/api/v1/espacos/disponiveis?inicio={Uri.EscapeDataString(inicio.ToString("o"))}&fim={Uri.EscapeDataString(fim.ToString("o"))}&pessoas=40";
        var agenda = (await c.GetFromJsonAsync<JsonElement>(url, Cenario.Json)).EnumerateArray().ToDictionary(x => x.GetProperty("id").GetGuid());
        Assert.False(agenda[salao].GetProperty("livre").GetBoolean());
        Assert.True(agenda[quadra].GetProperty("livre").GetBoolean());
        Assert.False(agenda[quadra].GetProperty("comporta").GetBoolean());

        await OkAsync(await c.PostAsJsonAsync($"/api/v1/eventos/{id}/locais", new { espacoId = quadra }));
        var repetido = await c.PostAsJsonAsync($"/api/v1/eventos/{id}/locais", new { espacoId = quadra });
        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);
        var detalhe = await c.GetFromJsonAsync<JsonElement>($"/api/v1/eventos/{id}", Cenario.Json);
        Assert.Equal(2, detalhe.GetProperty("evento").GetProperty("locais").GetArrayLength());

        // A quadra agora está ocupada pelo evento: ninguém mais reserva no mesmo horário.
        var (assoc, _, _) = await _c.AssociadoComAppAsync(p);
        var app = api.Cliente(assoc.AccessToken);
        var conflito = await app.PostAsJsonAsync("/api/v1/portal/reservas", new { espacoId = quadra, inicio, fim, finalidade = "Futebol" });
        Assert.Equal(HttpStatusCode.Conflict, conflito.StatusCode);

        await OkAsync(await c.DeleteAsync($"/api/v1/eventos/{id}/locais/{quadra}"));
        detalhe = await c.GetFromJsonAsync<JsonElement>($"/api/v1/eventos/{id}", Cenario.Json);
        Assert.Equal(1, detalhe.GetProperty("evento").GetProperty("locais").GetArrayLength());

        var divulgacao = await OkAsync(await c.PostAsync($"/api/v1/eventos/{id}/divulgar", null));
        Assert.True(divulgacao.GetProperty("avisosNoApp").GetInt32() >= 1);
        Assert.True(divulgacao.GetProperty("emails").GetInt32() >= 1);
        Assert.StartsWith("https://wa.me/?text=", divulgacao.GetProperty("linkWhatsApp").GetString());
        var avisos = await app.GetFromJsonAsync<JsonElement>("/api/v1/notificacoes", Cenario.Json);
        Assert.Contains("Festa junina", avisos.GetRawText());
    }

    [Fact]
    public async Task Limite_de_reservas_por_tipo_e_nome_de_quem_reservou_no_calendario()
    {
        var (p, c) = await ClubeAsync();
        var churrasqueira = await EspacoAsync(c, "Churrasqueira", 20, limite: 1);
        var (assoc, associadoId, _) = await _c.AssociadoComAppAsync(p);
        var app = api.Cliente(assoc.AccessToken);
        var mes = DateTime.UtcNow.Date.AddMonths(2);
        var dia = new DateTimeOffset(mes.Year, mes.Month, 5, 15, 0, 0, TimeSpan.Zero);

        await OkAsync(await app.PostAsJsonAsync("/api/v1/portal/reservas", new { espacoId = churrasqueira, inicio = dia, fim = dia.AddHours(4), finalidade = "Aniversário" }));
        var segunda = await app.PostAsJsonAsync("/api/v1/portal/reservas",
            new { espacoId = churrasqueira, inicio = dia.AddDays(7), fim = dia.AddDays(7).AddHours(4), finalidade = "Churrasco" });
        Assert.Equal("reserva.limite_tipo", await Cenario.CodigoProblemaAsync(segunda));

        var nome = (await c.GetFromJsonAsync<JsonElement>($"/api/v1/associados/{associadoId}", Cenario.Json)).GetProperty("pessoa").GetProperty("nomeCompleto").GetString()!;
        var de = DateOnly.FromDateTime(dia.UtcDateTime).ToString("yyyy-MM-dd");
        var ate = DateOnly.FromDateTime(dia.UtcDateTime).AddDays(1).ToString("yyyy-MM-dd");
        var calendario = await c.GetFromJsonAsync<JsonElement>($"/api/v1/reservas/calendario?de={de}&ate={ate}", Cenario.Json);
        Assert.Contains(calendario.EnumerateArray(), i => i.GetProperty("titulo").GetString()!.Contains(nome));
        // No app, os outros associados só veem que o horário está ocupado.
        var doApp = await app.GetFromJsonAsync<JsonElement>($"/api/v1/reservas/calendario?de={de}&ate={ate}", Cenario.Json);
        Assert.DoesNotContain(doApp.EnumerateArray(), i => i.GetProperty("titulo").GetString()!.Contains(nome));
    }

    [Fact]
    public async Task Diretoria_com_mandato_de_dois_anos_e_um_presidente_por_vez()
    {
        var (_, c) = await ClubeAsync();
        var vazia = await c.GetFromJsonAsync<JsonElement>("/api/v1/diretoria", Cenario.Json);
        Assert.Contains("sem presidente", vazia.GetProperty("aviso").GetString());

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var inicio = hoje.AddYears(-2).AddDays(30); // mandato termina em ~29 dias
        var d = await OkAsync(await c.PostAsJsonAsync("/api/v1/diretoria", new { nome = "Ana Presidente", cargo = "Presidente", inicioMandato = inicio }));
        var presidente = d.GetProperty("atual").EnumerateArray().Single();
        Assert.Equal(inicio.AddYears(2).AddDays(-1), DateOnly.Parse(presidente.GetProperty("fimMandato").GetString()!));
        Assert.Contains("Ana Presidente", d.GetProperty("aviso").GetString());

        var sobreposto = await c.PostAsJsonAsync("/api/v1/diretoria", new { nome = "Bruno", cargo = "presidente", inicioMandato = hoje });
        Assert.Equal("diretoria.presidente", await Cenario.CodigoProblemaAsync(sobreposto));

        var sucessor = inicio.AddYears(2);
        d = await OkAsync(await c.PostAsJsonAsync("/api/v1/diretoria", new { nome = "Bruno Sucessor", cargo = "Presidente", inicioMandato = sucessor }));
        Assert.Equal(JsonValueKind.Null, d.GetProperty("aviso").ValueKind);
        d = await OkAsync(await c.PostAsJsonAsync("/api/v1/diretoria", new { nome = "Carla", cargo = "Diretora social", inicioMandato = hoje }));
        Assert.Equal(3, d.GetProperty("atual").GetArrayLength());

        d = await OkAsync(await c.PostAsync($"/api/v1/diretoria/{presidente.GetProperty("id")}/encerrar", null));
        var ana = d.GetProperty("atual").EnumerateArray().Single(m => m.GetProperty("nome").GetString() == "Ana Presidente");
        Assert.Equal(hoje, DateOnly.Parse(ana.GetProperty("encerradoEm").GetString()!));
    }
}
