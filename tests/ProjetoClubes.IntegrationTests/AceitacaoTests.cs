using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ProjetoClubes.IntegrationTests.Infra;

namespace ProjetoClubes.IntegrationTests;

/// <summary>
/// Roteiro de aceitação do projeto (seção 25 do escopo), de ponta a ponta, num clube novo e contra o PostgreSQL real.
/// Itens 1–3 (banco, migrations, API no ar) são a própria preparação do <see cref="ApiFixture"/>: banco novo e vazio por
/// execução, migrations aplicadas na partida e a API servindo as requisições abaixo.
/// </summary>
[Collection(ColecaoApi.Nome)]
public class AceitacaoTests(ApiFixture api)
{
    private readonly Cenario _c = new(api);

    private static async Task<JsonElement> Ok(Task<HttpResponseMessage> chamada, HttpStatusCode esperado = HttpStatusCode.OK)
    {
        var r = await chamada;
        Assert.True(r.StatusCode == esperado, $"{r.RequestMessage?.Method} {r.RequestMessage?.RequestUri?.PathAndQuery}: {(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        return r.StatusCode == HttpStatusCode.NoContent ? default : await r.Content.ReadFromJsonAsync<JsonElement>(Cenario.Json);
    }

    [Fact]
    public async Task Roteiro_de_aceitacao_do_projeto_itens_1_a_27()
    {
        // 4. Superadmin pelo processo seguro (token de configuração inicial usado na partida do fixture).
        var status = await Ok(api.Cliente().GetAsync("/api/v1/setup/status"));
        Assert.True(status.GetProperty("inicializado").GetBoolean());
        var sa = await _c.SuperAdminAsync();
        Assert.True(sa.Usuario.GetProperty("superAdmin").GetBoolean());

        // 5–6. Clube, presidente e demais usuários.
        var clube = await _c.CriarClubeComPresidenteAsync("Clube Aceitação");
        var presidente = await _c.LoginAsync(clube.EmailPresidente);
        var p = api.Cliente(presidente.AccessToken);
        var (_, emailTesoureiro) = await _c.CriarUsuarioAsync(presidente, "Tesoureiro");
        var tesoureiro = api.Cliente((await _c.LoginAsync(emailTesoureiro)).AccessToken);

        // 7. Permissões configuráveis: perfil próprio só de portaria.
        var perfilPortaria = await Ok(p.PostAsJsonAsync("/api/v1/perfis",
            new { nome = "Portaria noturna", permissoes = new[] { "acesso.validar" } }), HttpStatusCode.Created);
        var emailPortaria = $"portaria.{Guid.NewGuid():N}"[..20] + "@testes.local";
        await Ok(p.PostAsJsonAsync("/api/v1/usuarios", new
        {
            nome = "Porteiro da noite", email = emailPortaria, perfilIds = new[] { perfilPortaria.GetProperty("id").GetGuid() }, senhaInicial = Cenario.SenhaPadrao,
        }), HttpStatusCode.Created);
        var portaria = api.Cliente((await _c.LoginAsync(emailPortaria)).AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await portaria.GetAsync("/api/v1/associados")).StatusCode);

        // 8. Categoria de associado.
        await Ok(p.PostAsJsonAsync("/api/v1/categorias-associados", new
        {
            nome = "Sócio remido", descricao = "Isento após 30 anos", direitoVoto = true, podeSerVotado = true, permiteDependentes = true,
            limiteDependentes = 4, ordem = 10,
        }), HttpStatusCode.Created);

        // 9. Associado (com login no app) na nova categoria.
        var (assocSessao, assocId, pessoaId) = await _c.AssociadoComAppAsync(presidente, "Sócio remido");
        var app = api.Cliente(assocSessao.AccessToken);

        // 10–11. Dependentes incluídos pelo app ficam pendentes; um é aprovado e outro rejeitado.
        var filha = await Ok(app.PostAsJsonAsync("/api/v1/portal/dependentes",
            new { pessoa = Cenario.DadosPessoa("Marina Aceitação", idade: 9), parentesco = "Filho" }), HttpStatusCode.Created);
        var outro = await Ok(app.PostAsJsonAsync("/api/v1/portal/dependentes",
            new { pessoa = Cenario.DadosPessoa("Pedro Aceitação", idade: 15), parentesco = "Irmao" }), HttpStatusCode.Created);
        Assert.Equal("Pendente", filha.GetProperty("situacao").GetString());
        var filhaId = filha.GetProperty("id").GetGuid();
        Assert.Equal("Ativo", (await Ok(p.PostAsync($"/api/v1/dependentes/{filhaId}/aprovar", null))).GetProperty("situacao").GetString());
        Assert.Equal("Rejeitado", (await Ok(p.PostAsJsonAsync($"/api/v1/dependentes/{outro.GetProperty("id")}/rejeitar",
            new { motivo = "Parentesco não comprovado" }))).GetProperty("situacao").GetString());

        // 12. Carteirinhas digitais com QR Code (titular e dependente).
        var cart = await Ok(p.GetAsync($"/api/v1/associados/{assocId}/carteirinha"));
        var qrTitular = cart.GetProperty("qrCodeConteudo").GetString()!;
        Assert.DoesNotContain(cart.GetProperty("pessoaId").GetGuid().ToString(), qrTitular);
        var png = await p.GetAsync($"/api/v1/pessoas/{pessoaId}/qrcode");
        Assert.Equal("image/png", png.Content.Headers.ContentType?.MediaType);
        var qrFilha = (await Ok(p.GetAsync($"/api/v1/dependentes/{filhaId}/carteirinha"))).GetProperty("qrCodeConteudo").GetString()!;

        // 13. Situação cadastral de associados e dependentes.
        var detalhe = await Ok(p.GetAsync($"/api/v1/associados/{assocId}"));
        Assert.Equal("Ativo", detalhe.GetProperty("situacao").GetString());
        Assert.Contains(detalhe.GetProperty("dependentes").EnumerateArray(), d => d.GetProperty("situacao").GetString() == "Ativo");

        // 14–15. Mensalidade, pagamento (tesoureiro) e situação financeira.
        await Ok(tesoureiro.PostAsJsonAsync("/api/v1/financeiro/planos", new
        {
            nome = "Remido", categoriaId = await _c.CategoriaIdAsync(presidente.AccessToken, "Sócio remido"), valorMensal = 60m, diaVencimento = 10,
        }), HttpStatusCode.Created);
        var competencia = DateTime.UtcNow.AddMonths(1).ToString("yyyy-MM");
        await Ok(tesoureiro.PostAsJsonAsync("/api/v1/financeiro/mensalidades/gerar", new { competencia }));
        var mensalidade = (await Ok(tesoureiro.GetAsync($"/api/v1/financeiro/mensalidades?associadoId={assocId}&competencia={competencia}")))
            .GetProperty("itens")[0];
        var pagamento = await Ok(tesoureiro.PostAsJsonAsync($"/api/v1/financeiro/mensalidades/{mensalidade.GetProperty("id")}/pagamento", new { forma = "Pix" }));
        Assert.Equal("Paga", pagamento.GetProperty("situacao").GetString());
        Assert.True(pagamento.GetProperty("pagamento").GetProperty("numeroRecibo").GetInt64() > 0);
        Assert.True((await Ok(tesoureiro.GetAsync($"/api/v1/financeiro/associados/{assocId}"))).GetProperty("adimplente").GetBoolean());

        // 16–17. Evento e inscrição pelo app.
        var dia = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(20), TimeSpan.Zero).AddHours(15);
        var evento = await Ok(p.PostAsJsonAsync("/api/v1/eventos", new
        {
            nome = "Baile de aniversário", inicio = dia, fim = dia.AddHours(5), publico = "AssociadosEDependentes", capacidade = 200, valor = 30m,
            maximoAcompanhantes = 2,
        }), HttpStatusCode.Created);
        await Ok(app.PostAsJsonAsync($"/api/v1/portal/eventos/{evento.GetProperty("id")}/inscricoes", new { acompanhantes = 1 }), HttpStatusCode.Created);

        // 18–19. Espaço físico; reserva pedida pelo app e aprovada pela administração.
        var espaco = await Ok(p.PostAsJsonAsync("/api/v1/espacos", new
        {
            nome = "Churrasqueira 3", tipo = "Churrasqueira", capacidade = 25, valorLocacao = 80m, taxaAdicional = 0m, exigeAprovacao = true,
        }), HttpStatusCode.Created);
        var espacoId = espaco.GetProperty("id").GetGuid();
        var reserva = await Ok(app.PostAsJsonAsync("/api/v1/portal/reservas", new
        {
            espacoId, inicio = dia.AddDays(2), fim = dia.AddDays(2).AddHours(6), finalidade = "Aniversário da Marina",
        }), HttpStatusCode.Created);
        Assert.Equal("Pendente", reserva.GetProperty("situacao").GetString());
        Assert.Equal("Aprovada", (await Ok(p.PostAsync($"/api/v1/reservas/{reserva.GetProperty("id")}/aprovar", null))).GetProperty("situacao").GetString());

        // 20. Calendário de locações (no app, sem identificar quem reservou).
        var de = Uri.EscapeDataString(dia.ToString("O"));
        var ate = Uri.EscapeDataString(dia.AddDays(5).ToString("O"));
        var calendario = await Ok(app.GetAsync($"/api/v1/reservas/calendario?de={de}&ate={ate}&espacoId={espacoId}"));
        Assert.Single(calendario.EnumerateArray());

        // 21. Solicitação/reclamação com protocolo.
        var solicitacao = await Ok(app.PostAsJsonAsync("/api/v1/portal/solicitacoes", new
        {
            tipo = "Reclamacao", assunto = "Chuveiro frio no vestiário", descricao = "O chuveiro do vestiário masculino está sem aquecimento.",
        }), HttpStatusCode.Created);
        Assert.Matches(@"^\d{4}-\d{6}$", solicitacao.GetProperty("protocolo").GetString());

        // 22. Consultas da administração (as mesmas que o Web administrativo faz).
        Assert.True((await Ok(p.GetAsync("/api/v1/associados"))).GetProperty("totalItens").GetInt32() >= 1);
        Assert.Single((await Ok(p.GetAsync("/api/v1/solicitacoes?somenteAbertas=true"))).GetProperty("itens").EnumerateArray());
        Assert.Equal(1, (await Ok(p.GetAsync($"/api/v1/eventos/{evento.GetProperty("id")}"))).GetProperty("inscricoes").GetArrayLength());

        // 23. As mesmas informações pelo aplicativo.
        Assert.Equal(2, (await Ok(app.GetAsync("/api/v1/portal/carteirinhas"))).GetArrayLength());
        Assert.True((await Ok(app.GetAsync("/api/v1/portal/financeiro"))).GetProperty("adimplente").GetBoolean());
        Assert.Single((await Ok(app.GetAsync("/api/v1/portal/reservas"))).EnumerateArray());
        Assert.Single((await Ok(app.GetAsync("/api/v1/portal/solicitacoes"))).EnumerateArray());

        // 24–25. Portaria valida o QR do titular e da dependente e registra entrada e saída.
        Assert.True((await Ok(portaria.PostAsJsonAsync("/api/v1/controle-acesso/validar", new { codigo = qrFilha }))).GetProperty("autorizado").GetBoolean());
        Assert.True((await Ok(portaria.PostAsJsonAsync("/api/v1/controle-acesso/registrar", new { codigo = qrTitular, tipo = "Entrada" })))
            .GetProperty("autorizado").GetBoolean());
        Assert.Single((await Ok(portaria.GetAsync("/api/v1/controle-acesso/presentes"))).EnumerateArray());
        await Ok(portaria.PostAsJsonAsync("/api/v1/controle-acesso/registrar", new { codigo = qrTitular, tipo = "Saida" }));
        Assert.Empty((await Ok(portaria.GetAsync("/api/v1/controle-acesso/presentes"))).EnumerateArray());

        // 26. Outro clube não vê nada deste.
        var outroClube = await _c.CriarClubeComPresidenteAsync();
        var invasor = api.Cliente((await _c.LoginAsync(outroClube.EmailPresidente)).AccessToken);
        Assert.Equal(HttpStatusCode.NotFound, (await invasor.GetAsync($"/api/v1/associados/{assocId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await invasor.GetAsync($"/api/v1/pessoas/{pessoaId}/qrcode")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await invasor.GetAsync($"/api/v1/solicitacoes/{solicitacao.GetProperty("id")}")).StatusCode);
        var validacaoFora = await Ok(invasor.PostAsJsonAsync("/api/v1/controle-acesso/validar", new { codigo = qrTitular }));
        Assert.False(validacaoFora.GetProperty("autorizado").GetBoolean());
        Assert.Equal(0, (await Ok(invasor.GetAsync("/api/v1/associados"))).GetProperty("totalItens").GetInt32());

        // 27. Históricos e relatórios.
        Assert.NotEmpty((await Ok(p.GetAsync($"/api/v1/associados/{assocId}/historico"))).EnumerateArray());
        Assert.NotEmpty((await Ok(p.GetAsync($"/api/v1/dependentes/{filhaId}/historico"))).EnumerateArray());
        Assert.True((await Ok(p.GetAsync("/api/v1/auditoria?entidade=Associado"))).GetProperty("totalItens").GetInt32() > 0);
        Assert.Equal(60m, (await Ok(tesoureiro.GetAsync("/api/v1/financeiro/resumo"))).GetProperty("receitasMensalidades").GetDecimal());
        Assert.Equal(2, (await Ok(p.GetAsync("/api/v1/controle-acesso"))).GetProperty("totalItens").GetInt32());
        var resumo = await Ok(p.GetAsync("/api/v1/associados/resumo"));
        Assert.Equal(1, resumo.GetProperty("totalAssociados").GetInt32());
    }
}
