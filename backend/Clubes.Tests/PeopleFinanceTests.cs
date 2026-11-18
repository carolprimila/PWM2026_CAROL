using System.Net;
using Clubes.Application.Clubs;
using Clubes.Application.Common;
using Clubes.Application.Finance;
using Clubes.Application.People;
using Clubes.Domain;
using Microsoft.EntityFrameworkCore;

namespace Clubes.Tests;

[Collection("api")]
public class MemberTests(ApiFixture fixture)
{
    [Fact]
    public async Task Cadastro_valida_cpf_e_impede_duplicidade()
    {
        var club = await fixture.NewClubAsync();
        await club.Admin.FailAsync(HttpMethod.Post, "/api/associados", HttpStatusCode.BadRequest, new SaveMemberRequest { FullName = "CPF Inválido", Cpf = "111.111.111-11" }, "cpf_invalido");
        var cpf = ClubScope.NewCpf();
        await club.Admin.PostAsync<MemberDto>("/api/associados", new SaveMemberRequest { FullName = "Primeiro Cadastro", Cpf = cpf });
        await club.Admin.FailAsync(HttpMethod.Post, "/api/associados", HttpStatusCode.Conflict, new SaveMemberRequest { FullName = "Mesmo CPF", Cpf = cpf });
        await club.Admin.FailAsync(HttpMethod.Post, "/api/associados", HttpStatusCode.BadRequest, new SaveMemberRequest { FullName = "", Cpf = ClubScope.NewCpf() }, "validacao");
    }

    [Fact]
    public async Task Mesmo_cpf_pode_existir_em_clubes_diferentes()
    {
        var a = await fixture.NewClubAsync();
        var b = await fixture.NewClubAsync();
        var cpf = ClubScope.NewCpf();
        await a.Admin.PostAsync<MemberDto>("/api/associados", new SaveMemberRequest { FullName = "Pessoa no Clube A", Cpf = cpf });
        await b.Admin.PostAsync<MemberDto>("/api/associados", new SaveMemberRequest { FullName = "Pessoa no Clube B", Cpf = cpf });
    }

    [Fact]
    public async Task Fluxo_de_aprovacao_suspensao_e_cancelamento_com_historico()
    {
        var club = await fixture.NewClubAsync();
        var member = await club.NewMemberAsync(approve: false);
        Assert.Equal(MemberStatus.PendingApproval, member.Status);
        // Associado ainda não aprovado não pode receber acesso ao aplicativo.
        await club.Admin.FailAsync(HttpMethod.Post, $"/api/associados/{member.Id}/acesso", HttpStatusCode.Conflict, new CreateAccessRequest("x@teste.local"));

        member = await club.Admin.PostAsync<MemberDto>($"/api/associados/{member.Id}/aprovar");
        Assert.Equal(MemberStatus.Active, member.Status);
        Assert.NotNull(member.JoinedAt);
        member = await club.Admin.PostAsync<MemberDto>($"/api/associados/{member.Id}/suspender", new StatusChangeRequest("Motivo da suspensão"));
        Assert.Equal(MemberStatus.Suspended, member.Status);
        await club.Admin.FailAsync(HttpMethod.Post, $"/api/associados/{member.Id}/aprovar", HttpStatusCode.Conflict);
        member = await club.Admin.PostAsync<MemberDto>($"/api/associados/{member.Id}/cancelar", new StatusChangeRequest("Pedido do associado"));
        Assert.Equal(MemberStatus.Cancelled, member.Status);

        var history = await club.Admin.GetAsync<List<HistoryDto>>($"/api/historico/Member/{member.Id}");
        Assert.Equal(["Cancelled", "Suspended", "Active", "PendingApproval"], history.Select(h => h.ToStatus));
    }

    [Fact]
    public async Task Associado_cancelado_perde_o_acesso()
    {
        var club = await fixture.NewClubAsync();
        var (member, api) = await club.NewMemberWithLoginAsync();
        await club.Admin.PostAsync<MemberDto>($"/api/associados/{member.Id}/cancelar", new StatusChangeRequest("Cancelamento"));
        await fixture.Anonymous().FailAsync(HttpMethod.Post, "/api/auth/refresh", HttpStatusCode.Unauthorized, new Application.Access.RefreshRequest(api.Auth!.RefreshToken));
    }
}

[Collection("api")]
public class DependentTests(ApiFixture fixture)
{
    [Fact]
    public async Task Limite_de_cinco_dependentes_por_associado()
    {
        var club = await fixture.NewClubAsync();
        var member = await club.NewMemberAsync();
        for (var i = 1; i <= 5; i++) await club.NewDependentAsync(member.Id, $"Dependente {i}");
        var error = await club.Admin.FailAsync(HttpMethod.Post, $"/api/dependentes?memberId={member.Id}", (HttpStatusCode)422,
            new SaveDependentRequest { FullName = "Sexto Dependente", Relationship = "Filho(a)" }, "limite_dependentes");
        Assert.Contains("5", error.Message);
    }

    [Fact]
    public async Task Limite_e_respeitado_com_cadastros_simultaneos()
    {
        var club = await fixture.NewClubAsync();
        var member = await club.NewMemberAsync();
        var attempts = Enumerable.Range(1, 10).Select(i => club.Admin.SendAsync(HttpMethod.Post, $"/api/dependentes?memberId={member.Id}",
            new SaveDependentRequest { FullName = $"Simultâneo {i}", Relationship = "Filho(a)" }));
        var responses = await Task.WhenAll(attempts);
        Assert.Equal(5, responses.Count(r => r.IsSuccessStatusCode));
        var list = await club.Admin.GetAsync<PagedResult<DependentDto>>($"/api/dependentes?memberId={member.Id}");
        Assert.Equal(5, list.Total);
    }

    [Fact]
    public async Task Dependente_cadastrado_pelo_associado_aguarda_aprovacao_e_notifica()
    {
        var club = await fixture.NewClubAsync();
        var (member, api) = await club.NewMemberWithLoginAsync();
        var dependent = await api.PostAsync<DependentDto>("/api/dependentes", new SaveDependentRequest { FullName = "Filho do Associado", Relationship = "Filho(a)" });
        Assert.Equal(DependentStatus.PendingApproval, dependent.Status);
        Assert.Equal(member.Id, dependent.MemberId);

        var adminNotifications = await club.Admin.GetAsync<PagedResult<NotificationDto>>("/api/notificacoes");
        Assert.Contains(adminNotifications.Items, n => n.Type == "dependent.received" && n.EntityId == dependent.Id);

        await club.Admin.PostAsync<DependentDto>($"/api/dependentes/{dependent.Id}/aprovar");
        var memberNotifications = await api.GetAsync<PagedResult<NotificationDto>>("/api/notificacoes");
        Assert.Contains(memberNotifications.Items, n => n.Type == "dependent.approved");
    }

    [Fact]
    public async Task Dependente_tem_login_individual_e_enxerga_apenas_o_proprio_cadastro()
    {
        var club = await fixture.NewClubAsync();
        var member = await club.NewMemberAsync();
        var (dependent, api) = await club.NewDependentWithLoginAsync(member.Id);
        var other = await club.NewDependentAsync(member.Id);

        Assert.Equal(UserKind.Dependent, api.Auth!.User.Kind);
        Assert.Equal(dependent.Id, api.Auth.User.DependentId);
        Assert.Equal(dependent.Id, (await api.GetAsync<DependentDto>("/api/dependentes/eu")).Id);
        await api.FailAsync(HttpMethod.Get, $"/api/dependentes/{other.Id}", HttpStatusCode.NotFound);
        await api.FailAsync(HttpMethod.Get, "/api/dependentes", HttpStatusCode.Forbidden);
        await api.FailAsync(HttpMethod.Get, "/api/financeiro/cobrancas", HttpStatusCode.Forbidden);
        await api.FailAsync(HttpMethod.Get, "/api/associados/eu", HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Permissao_so_vale_com_liberacao_da_administracao_e_do_associado()
    {
        var club = await fixture.NewClubAsync();
        var (member, memberApi) = await club.NewMemberWithLoginAsync();
        var dependent = await club.NewDependentAsync(member.Id);
        var body = new SetDependentPermissionRequest(DependentPermissions.RequestReservations, true);

        // O associado não consegue habilitar o que a administração ainda não liberou.
        await memberApi.FailAsync(HttpMethod.Put, $"/api/dependentes/{dependent.Id}/permissoes", HttpStatusCode.Forbidden, body);

        var afterAdmin = await club.Admin.PutAsync<DependentDto>($"/api/dependentes/{dependent.Id}/permissoes", body);
        Assert.False(afterAdmin.Permissions.First(p => p.Code == body.Code).Effective);
        var afterMember = await memberApi.PutAsync<DependentDto>($"/api/dependentes/{dependent.Id}/permissoes", body);
        Assert.True(afterMember.Permissions.First(p => p.Code == body.Code).Effective);

        // Associado de outro cadastro não altera permissões de dependente alheio.
        var (_, strangerApi) = await club.NewMemberWithLoginAsync();
        await strangerApi.FailAsync(HttpMethod.Put, $"/api/dependentes/{dependent.Id}/permissoes", HttpStatusCode.NotFound, body);
    }
}

public class DependentPricingTests
{
    private static DependentPricingRule Rule(int from, int to, decimal amount) => new() { FromCount = from, ToCount = to, Amount = amount };

    [Fact]
    public void Sem_politica_definida_a_cobranca_nao_e_calculada()
        => Assert.Equal("configuracao_pendente", Assert.Throws<DomainException>(() => DependentPricing.Calculate(DependentPricingMode.NotConfigured, null, [], 2)).Code);

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 50)]
    [InlineData(5, 250)]
    public void Valor_fixo_por_dependente(int dependents, decimal expected)
        => Assert.Equal(expected, DependentPricing.Calculate(DependentPricingMode.FixedPerDependent, 50m, [], dependents));

    [Theory]
    [InlineData(1, 80)]
    [InlineData(2, 140)]
    [InlineData(3, 180)]
    public void Progressiva_soma_o_valor_de_cada_posicao(int dependents, decimal expected)
        => Assert.Equal(expected, DependentPricing.Calculate(DependentPricingMode.Progressive, null, [Rule(1, 1, 80), Rule(2, 2, 60), Rule(3, 3, 40)], dependents));

    [Theory]
    [InlineData(1, 100)]
    [InlineData(2, 100)]
    [InlineData(3, 180)]
    [InlineData(5, 180)]
    public void Faixas_usam_o_valor_da_faixa_em_que_a_quantidade_se_encaixa(int dependents, decimal expected)
        => Assert.Equal(expected, DependentPricing.Calculate(DependentPricingMode.Tiers, null, [Rule(1, 2, 100), Rule(3, 5, 180)], dependents));

    [Fact]
    public void Faixa_ou_posicao_nao_configurada_impede_a_cobranca()
    {
        Assert.Throws<DomainException>(() => DependentPricing.Calculate(DependentPricingMode.Tiers, null, [Rule(1, 2, 100)], 3));
        Assert.Throws<DomainException>(() => DependentPricing.Calculate(DependentPricingMode.Progressive, null, [Rule(1, 1, 80)], 2));
    }

    [Fact]
    public void Sem_cobranca_por_dependente() => Assert.Equal(0, DependentPricing.Calculate(DependentPricingMode.None, null, [], 4));
}

[Collection("api")]
public class FinanceTests(ApiFixture fixture)
{
    private static readonly string Competence = ClubScope.Today.ToString("yyyy-MM");

    private static void Configure(SettingsDto s)
    {
        s.MonthlyBaseFee = 300;
        s.DueDay = 10;
        s.GraceDays = 5;
        s.DependentPricingMode = DependentPricingMode.FixedPerDependent;
        s.DependentFixedAmount = 50;
    }

    [Fact]
    public async Task Mensalidade_nao_e_gerada_sem_configuracao_da_administracao()
    {
        var club = await fixture.NewClubAsync();
        await club.NewMemberAsync();
        var error = await club.Admin.FailAsync(HttpMethod.Post, "/api/mensalidades/gerar", (HttpStatusCode)422, new GenerateMonthlyRequest(Competence, null), "configuracao_pendente");
        Assert.Contains("mensalidade base", error.Message);
    }

    [Fact]
    public async Task Mensalidade_considera_base_e_dependentes_e_a_geracao_e_idempotente()
    {
        var club = await fixture.NewClubAsync(Configure);
        var solo = await club.NewMemberAsync();
        var family = await club.NewMemberAsync();
        await club.NewDependentAsync(family.Id);
        await club.NewDependentAsync(family.Id);
        await club.NewMemberAsync(approve: false); // não aprovado: não recebe mensalidade

        var first = await club.Admin.PostAsync<GenerateMonthlyResult>("/api/mensalidades/gerar", new GenerateMonthlyRequest(Competence, null));
        Assert.Equal(2, first.Created);
        Assert.Equal(700m, first.TotalAmount);
        var second = await club.Admin.PostAsync<GenerateMonthlyResult>("/api/mensalidades/gerar", new GenerateMonthlyRequest(Competence, null));
        Assert.Equal(0, second.Created);
        Assert.Equal(2, second.AlreadyExisting);

        var charges = await club.Admin.GetAsync<PagedResult<ChargeDto>>($"/api/mensalidades?competence={Competence}");
        Assert.Equal(300m, charges.Items.Single(c => c.MemberId == solo.Id).Total);
        var familyCharge = charges.Items.Single(c => c.MemberId == family.Id);
        Assert.Equal((300m, 2, 100m, 400m), (familyCharge.BaseFee, familyCharge.DependentsCount, familyCharge.DependentsAmount, familyCharge.Total));
    }

    [Fact]
    public async Task Cobrancas_antigas_preservam_valores_e_composicao_da_epoca()
    {
        var club = await fixture.NewClubAsync(Configure);
        var member = await club.NewMemberAsync();
        await club.NewDependentAsync(member.Id, "Primeiro Dependente");
        await club.Admin.PostAsync<GenerateMonthlyResult>("/api/mensalidades/gerar", new GenerateMonthlyRequest(Competence, null));

        // Depois da geração: a regra muda e a família cresce.
        await club.ConfigureAsync(s => { s.MonthlyBaseFee = 500; s.DependentFixedAmount = 90; });
        await club.NewDependentAsync(member.Id, "Segundo Dependente");
        var next = ClubScope.Today.AddMonths(1).ToString("yyyy-MM");
        await club.Admin.PostAsync<GenerateMonthlyResult>("/api/mensalidades/gerar", new GenerateMonthlyRequest(next, null));

        var old = (await club.Admin.GetAsync<PagedResult<ChargeDto>>($"/api/mensalidades?competence={Competence}")).Items.Single();
        Assert.Equal((300m, 1, 50m, 350m), (old.BaseFee, old.DependentsCount, old.DependentsAmount, old.Total));
        var detail = await club.Admin.GetAsync<ChargeDetailDto>($"/api/financeiro/cobrancas/{old.Id}");
        Assert.Equal(["Primeiro Dependente"], detail.Composition);

        var future = (await club.Admin.GetAsync<PagedResult<ChargeDto>>($"/api/mensalidades?competence={next}")).Items.Single();
        Assert.Equal((500m, 2, 180m, 680m), (future.BaseFee, future.DependentsCount, future.DependentsAmount, future.Total));
    }

    [Fact]
    public async Task Pagamento_na_secretaria_e_idempotente_e_nao_excede_o_saldo()
    {
        var club = await fixture.NewClubAsync(Configure);
        var member = await club.NewMemberAsync();
        var charge = (await club.Admin.PostAsync<ChargeDetailDto>("/api/financeiro/cobrancas", new CreateChargeRequest(member.Id, ChargeType.Fee, "Taxa de teste", 200, ClubScope.Today.AddDays(10)))).Charge;

        await club.Admin.FailAsync(HttpMethod.Post, "/api/pagamentos", HttpStatusCode.BadRequest, new RegisterPaymentRequest(charge.Id, 250, PaymentMethod.Cash, null, null, null, Guid.NewGuid().ToString()), "valor_invalido");

        var key = Guid.NewGuid().ToString();
        var request = new RegisterPaymentRequest(charge.Id, 120, PaymentMethod.Pix, null, "Parcial", null, key);
        var first = await club.Admin.PostAsync<PaymentDto>("/api/pagamentos", request);
        var repeated = await club.Admin.PostAsync<PaymentDto>("/api/pagamentos", request);
        Assert.Equal(first.Id, repeated.Id);

        var detail = await club.Admin.GetAsync<ChargeDetailDto>($"/api/financeiro/cobrancas/{charge.Id}");
        Assert.Single(detail.Payments);
        Assert.Equal((120m, 80m, ChargeStatus.Open), (detail.Charge.PaidAmount, detail.Charge.Balance, detail.Charge.Status));

        await club.Admin.PostAsync<PaymentDto>("/api/pagamentos", new RegisterPaymentRequest(charge.Id, 80, PaymentMethod.Cash, null, null, null, Guid.NewGuid().ToString()));
        detail = await club.Admin.GetAsync<ChargeDetailDto>($"/api/financeiro/cobrancas/{charge.Id}");
        Assert.Equal((0m, ChargeStatus.Paid), (detail.Charge.Balance, detail.Charge.Status));
    }

    [Fact]
    public async Task Requisicoes_simultaneas_com_a_mesma_chave_registram_um_unico_pagamento()
    {
        var club = await fixture.NewClubAsync(Configure);
        var member = await club.NewMemberAsync();
        var charge = (await club.Admin.PostAsync<ChargeDetailDto>("/api/financeiro/cobrancas", new CreateChargeRequest(member.Id, ChargeType.Fee, "Taxa concorrente", 100, ClubScope.Today.AddDays(10)))).Charge;
        var request = new RegisterPaymentRequest(charge.Id, 100, PaymentMethod.Cash, null, null, null, Guid.NewGuid().ToString());
        await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => club.Admin.SendAsync(HttpMethod.Post, "/api/pagamentos", request)));

        var detail = await club.Admin.GetAsync<ChargeDetailDto>($"/api/financeiro/cobrancas/{charge.Id}");
        Assert.Single(detail.Payments);
        Assert.Equal(100m, detail.Charge.PaidAmount);
    }

    [Fact]
    public async Task Estorno_preserva_o_pagamento_no_historico_e_reabre_a_cobranca()
    {
        var club = await fixture.NewClubAsync(Configure);
        var member = await club.NewMemberAsync();
        var charge = (await club.Admin.PostAsync<ChargeDetailDto>("/api/financeiro/cobrancas", new CreateChargeRequest(member.Id, ChargeType.Other, "Cobrança estornável", 90, ClubScope.Today.AddDays(5)))).Charge;
        var payment = await club.Admin.PostAsync<PaymentDto>("/api/pagamentos", new RegisterPaymentRequest(charge.Id, 90, PaymentMethod.Cash, null, null, null, Guid.NewGuid().ToString()));
        // Cobrança com pagamento não pode ser cancelada diretamente.
        var paid = await club.Admin.GetAsync<ChargeDetailDto>($"/api/financeiro/cobrancas/{charge.Id}");
        Assert.Equal(ChargeStatus.Paid, paid.Charge.Status);

        var refunded = await club.Admin.PostAsync<PaymentDto>($"/api/pagamentos/{payment.Id}/estornar", new RefundRequest("Pagamento em duplicidade"));
        Assert.Equal(PaymentStatus.Refunded, refunded.Status);
        var detail = await club.Admin.GetAsync<ChargeDetailDto>($"/api/financeiro/cobrancas/{charge.Id}");
        Assert.Equal((ChargeStatus.Open, 0m, 90m), (detail.Charge.Status, detail.Charge.PaidAmount, detail.Charge.Balance));
        Assert.Single(detail.Payments, p => p.Status == PaymentStatus.Refunded);
    }

    [Fact]
    public async Task Multa_e_juros_usam_os_percentuais_registrados_na_cobranca()
    {
        var club = await fixture.NewClubAsync(s => { Configure(s); s.GraceDays = 0; s.LateFinePercent = 2; s.DailyInterestPercent = 0.1m; });
        var member = await club.NewMemberAsync();
        var charge = (await club.Admin.PostAsync<ChargeDetailDto>("/api/financeiro/cobrancas", new CreateChargeRequest(member.Id, ChargeType.Fee, "Vencida há 10 dias", 1000, ClubScope.Today.AddDays(-10)))).Charge;

        // A regra muda depois: a cobrança existente continua com os percentuais da época.
        await club.ConfigureAsync(s => { s.LateFinePercent = 50; s.DailyInterestPercent = 5; });
        await club.Admin.OkAsync(HttpMethod.Post, "/api/mensalidades/atualizar-encargos");
        await club.Admin.OkAsync(HttpMethod.Post, "/api/mensalidades/atualizar-encargos"); // repetir não duplica a multa

        var detail = await club.Admin.GetAsync<ChargeDetailDto>($"/api/financeiro/cobrancas/{charge.Id}");
        Assert.Equal((20m, 10m, 1030m), (detail.Charge.FineAmount, detail.Charge.InterestAmount, detail.Charge.Total));
        Assert.Equal(2, detail.Adjustments.Count(a => a.IsAutomatic));
    }

    [Fact]
    public async Task Inadimplencia_segue_a_tolerancia_e_pode_ser_liberada_com_justificativa()
    {
        var club = await fixture.NewClubAsync(s => { Configure(s); s.GraceDays = 5; s.BlockAccessWhenDelinquent = true; s.BlockReservationsWhenDelinquent = true; });
        var member = await club.NewMemberAsync();
        await club.Admin.PostAsync<ChargeDetailDto>("/api/financeiro/cobrancas", new CreateChargeRequest(member.Id, ChargeType.Fee, "Vencida dentro da tolerância", 100, ClubScope.Today.AddDays(-3)));
        var status = await club.Admin.GetAsync<FinancialStatusDto>($"/api/associados/{member.Id}/situacao-financeira");
        Assert.False(status.IsDelinquent);

        await club.Admin.PostAsync<ChargeDetailDto>("/api/financeiro/cobrancas", new CreateChargeRequest(member.Id, ChargeType.Fee, "Vencida além da tolerância", 100, ClubScope.Today.AddDays(-20)));
        status = await club.Admin.GetAsync<FinancialStatusDto>($"/api/associados/{member.Id}/situacao-financeira");
        Assert.True(status is { IsDelinquent: true, AccessBlocked: true, ReservationsBlocked: true, OverdueCharges: 1 });
        Assert.Contains((await club.Admin.GetAsync<List<DelinquentDto>>("/api/financeiro/inadimplentes")), d => d.MemberId == member.Id);

        await club.Admin.FailAsync(HttpMethod.Post, "/api/financeiro/liberacoes", HttpStatusCode.BadRequest, new { memberId = member.Id, reason = "", validUntil = ClubScope.Today.AddDays(7) });
        await club.Admin.PostAsync<ReleaseDto>("/api/financeiro/liberacoes", new GrantReleaseRequest(member.Id, "Acordo de parcelamento assinado", ClubScope.Today.AddDays(7)));
        status = await club.Admin.GetAsync<FinancialStatusDto>($"/api/associados/{member.Id}/situacao-financeira");
        Assert.True(status is { IsDelinquent: true, HasActiveRelease: true, AccessBlocked: false, ReservationsBlocked: false });
    }

    [Fact]
    public async Task Bloqueio_por_inadimplencia_so_vale_se_a_politica_foi_habilitada()
    {
        var club = await fixture.NewClubAsync(Configure);
        var member = await club.NewMemberAsync();
        await club.Admin.PostAsync<ChargeDetailDto>("/api/financeiro/cobrancas", new CreateChargeRequest(member.Id, ChargeType.Fee, "Vencida", 100, ClubScope.Today.AddDays(-60)));
        var status = await club.Admin.GetAsync<FinancialStatusDto>($"/api/associados/{member.Id}/situacao-financeira");
        Assert.True(status is { IsDelinquent: true, AccessBlocked: false, ReservationsBlocked: false });
    }

    [Fact]
    public async Task Associado_consulta_apenas_as_proprias_cobrancas_e_paga_pelo_aplicativo()
    {
        var club = await fixture.NewClubAsync(Configure);
        var (member, api) = await club.NewMemberWithLoginAsync();
        var other = await club.NewMemberAsync();
        var mine = (await club.Admin.PostAsync<ChargeDetailDto>("/api/financeiro/cobrancas", new CreateChargeRequest(member.Id, ChargeType.Fee, "Minha taxa", 75, ClubScope.Today.AddDays(5)))).Charge;
        var theirs = (await club.Admin.PostAsync<ChargeDetailDto>("/api/financeiro/cobrancas", new CreateChargeRequest(other.Id, ChargeType.Fee, "Taxa de outro", 75, ClubScope.Today.AddDays(5)))).Charge;

        var list = await api.GetAsync<PagedResult<ChargeDto>>($"/api/financeiro/cobrancas?memberId={other.Id}");
        Assert.Equal([mine.Id], list.Items.Select(c => c.Id));
        await api.FailAsync(HttpMethod.Get, $"/api/financeiro/cobrancas/{theirs.Id}", HttpStatusCode.NotFound);
        await api.FailAsync(HttpMethod.Post, "/api/pagamentos/app", HttpStatusCode.NotFound, new StartAppPaymentRequest(theirs.Id, PaymentMethod.Pix, Guid.NewGuid().ToString()));

        // Pagamento pelo aplicativo: fica pendente até a confirmação assinada do gateway.
        var attemptKey = Guid.NewGuid().ToString();
        var started = await api.PostAsync<AppPaymentDto>("/api/pagamentos/app", new StartAppPaymentRequest(mine.Id, PaymentMethod.Pix, attemptKey));
        Assert.Equal(PaymentStatus.Pending, started.Payment.Status);
        // Repetir a mesma tentativa (mesma chave) devolve o mesmo pagamento e os mesmos dados de checkout.
        var replay = await api.PostAsync<AppPaymentDto>("/api/pagamentos/app", new StartAppPaymentRequest(mine.Id, PaymentMethod.Pix, attemptKey));
        Assert.Equal((started.Payment.Id, started.PixCopyPaste), (replay.Payment.Id, replay.PixCopyPaste));
        Assert.NotNull(replay.PixCopyPaste);
        Assert.Equal(ChargeStatus.Open, (await api.GetAsync<ChargeDetailDto>($"/api/financeiro/cobrancas/{mine.Id}")).Charge.Status);

        // Notificação sem assinatura válida é rejeitada.
        var forged = new HttpRequestMessage(HttpMethod.Post, "/api/pagamentos/webhook") { Content = new StringContent($$"""{"reference":"sbx_{{started.Payment.Id:N}}","paid":true}""") };
        forged.Headers.Add("X-Signature", "assinatura-falsa");
        Assert.Equal(HttpStatusCode.BadRequest, (await fixture.Anonymous().Http.SendAsync(forged)).StatusCode);

        var body = $$"""{"reference":"sbx_{{started.Payment.Id:N}}","paid":true}""";
        for (var i = 0; i < 2; i++) // a notificação repetida não credita duas vezes
        {
            var webhook = new HttpRequestMessage(HttpMethod.Post, "/api/pagamentos/webhook") { Content = new StringContent(body) };
            webhook.Headers.Add("X-Signature", Infrastructure.Services.SandboxPaymentGateway.Sign(body, ApiFixture.SandboxSecret));
            Assert.Equal(HttpStatusCode.OK, (await fixture.Anonymous().Http.SendAsync(webhook)).StatusCode);
        }
        var detail = await api.GetAsync<ChargeDetailDto>($"/api/financeiro/cobrancas/{mine.Id}");
        Assert.Equal((ChargeStatus.Paid, 75m), (detail.Charge.Status, detail.Charge.PaidAmount));
        Assert.Contains((await api.GetAsync<PagedResult<NotificationDto>>("/api/notificacoes")).Items, n => n.Type == "payment.confirmed");
    }

    [Fact]
    public async Task Fechamento_mensal_bloqueia_novas_movimentacoes_na_competencia()
    {
        var club = await fixture.NewClubAsync(Configure);
        await club.NewMemberAsync();
        var previous = ClubScope.Today.AddMonths(-1).ToString("yyyy-MM");
        await club.Admin.PostAsync<GenerateMonthlyResult>("/api/mensalidades/gerar", new GenerateMonthlyRequest(previous, null));
        await club.Admin.FailAsync(HttpMethod.Post, "/api/financeiro/fechamentos", HttpStatusCode.Conflict, new CloseMonthRequest(Competence, null));

        var closing = await club.Admin.PostAsync<ClosingDto>("/api/financeiro/fechamentos", new CloseMonthRequest(previous, "Fechamento de teste"));
        Assert.Equal(300m, closing.ChargesIssued);
        await club.Admin.FailAsync(HttpMethod.Post, "/api/mensalidades/gerar", HttpStatusCode.Conflict, new GenerateMonthlyRequest(previous, null));

        await club.Admin.OkAsync(HttpMethod.Post, $"/api/financeiro/fechamentos/{closing.Id}/reabrir", new CancelRequest("Correção de lançamento"));
        await club.Admin.PostAsync<GenerateMonthlyResult>("/api/mensalidades/gerar", new GenerateMonthlyRequest(previous, null));
    }

    [Fact]
    public async Task Banco_impede_duas_mensalidades_ativas_para_o_mesmo_associado_e_competencia()
    {
        var club = await fixture.NewClubAsync(Configure);
        var member = await club.NewMemberAsync();
        await club.Admin.PostAsync<GenerateMonthlyResult>("/api/mensalidades/gerar", new GenerateMonthlyRequest(Competence, null));
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.DbAsync(club.ClubId, async db =>
        {
            db.Set<Charge>().Add(new Charge { MemberId = member.Id, Type = ChargeType.Monthly, Competence = Competence, Description = "Duplicada", OriginalAmount = 1, DueDate = ClubScope.Today, DelinquentFrom = ClubScope.Today });
            return await db.SaveChangesAsync();
        }));
    }
}
