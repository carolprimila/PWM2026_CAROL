using System.Net;
using Clubes.Application.Clubs;
using Clubes.Application.Common;
using Clubes.Application.Communication;
using Clubes.Application.Finance;
using Clubes.Application.Operations;
using Clubes.Application.People;
using Clubes.Application.Spaces;
using Clubes.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Clubes.Tests;

[Collection("api")]
public class ReservationTests(ApiFixture fixture)
{
    private static CreateReservationRequest Request(Guid spaceId, DateOnly date, int[] hours, int people = 4, Guid? memberId = null, params GuestInput[] guests)
        => new() { SpaceId = spaceId, Date = date, StartMinutes = hours.Select(h => h * 60).ToList(), PeopleCount = people, MemberId = memberId, Guests = guests.ToList() };

    [Fact]
    public async Task Associado_solicita_e_administracao_aprova_com_notificacoes_e_historico()
    {
        var club = await fixture.NewClubAsync();
        var (member, api) = await club.NewMemberWithLoginAsync();
        var court = await club.SpaceAsync("Quadra de Tênis");
        var date = ClubScope.Next(DayOfWeek.Wednesday);

        var created = await api.PostAsync<ReservationDetailDto>("/api/reservas", Request(court.Id, date, [9, 10, 14]));
        Assert.Equal(ReservationStatus.Pending, created.Reservation.Status);
        Assert.Equal(member.Id, created.Reservation.MemberId);
        Assert.Equal(2, created.Reservation.Slots.Count); // 9h–11h (unidos) e 14h–15h

        // O horário solicitado deixa de aparecer como disponível imediatamente.
        var day = await api.GetAsync<DayAvailabilityDto>($"/api/espacos/{court.Id}/disponibilidade?date={date:yyyy-MM-dd}");
        Assert.Equal(SlotState.Reserved, day.Slots.Single(s => s.StartMinute == 540).State);
        Assert.Equal(SlotState.Available, day.Slots.Single(s => s.StartMinute == 660).State);
        Assert.Equal((480, 1320), (day.OpenMinute, day.CloseMinute));

        var approved = await club.Admin.PostAsync<ReservationDetailDto>($"/api/reservas/{created.Reservation.Id}/aprovar", new DecisionRequest(null));
        Assert.Equal(ReservationStatus.Approved, approved.Reservation.Status);
        Assert.Equal(["Approved", "Pending"], approved.History.Select(h => h.ToStatus));

        var notifications = await api.GetAsync<PagedResult<NotificationDto>>("/api/notificacoes");
        Assert.Contains(notifications.Items, n => n.Type == "reservation.received");
        Assert.Contains(notifications.Items, n => n.Type == "reservation.approved");
        Assert.Contains((await club.Admin.GetAsync<PagedResult<NotificationDto>>("/api/notificacoes")).Items, n => n.Type == "reservation.pending");
    }

    [Fact]
    public async Task Conflito_de_horario_e_recusado()
    {
        var club = await fixture.NewClubAsync();
        var (_, first) = await club.NewMemberWithLoginAsync();
        var (_, second) = await club.NewMemberWithLoginAsync();
        var court = await club.SpaceAsync("Quadra de Vôlei");
        var date = ClubScope.Next(DayOfWeek.Thursday);
        await first.PostAsync<ReservationDetailDto>("/api/reservas", Request(court.Id, date, [10, 11]));
        await second.FailAsync(HttpMethod.Post, "/api/reservas", HttpStatusCode.Conflict, Request(court.Id, date, [11, 12]));
        await second.PostAsync<ReservationDetailDto>("/api/reservas", Request(court.Id, date, [12]));
    }

    [Fact]
    public async Task Solicitacoes_simultaneas_para_o_mesmo_horario_geram_uma_unica_reserva()
    {
        var club = await fixture.NewClubAsync();
        var court = await club.SpaceAsync("Quadra de Futsal");
        var date = ClubScope.Next(DayOfWeek.Tuesday);
        var apis = new List<Api>();
        for (var i = 0; i < 6; i++) apis.Add((await club.NewMemberWithLoginAsync()).Api);

        var responses = await Task.WhenAll(apis.Select(api => api.SendAsync(HttpMethod.Post, "/api/reservas", Request(court.Id, date, [18, 19]))));
        Assert.Equal(1, responses.Count(r => r.IsSuccessStatusCode));
        Assert.All(responses.Where(r => !r.IsSuccessStatusCode), r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
    }

    [Fact]
    public async Task Banco_impede_sobreposicao_mesmo_fora_da_aplicacao()
    {
        var club = await fixture.NewClubAsync();
        var member = await club.NewMemberAsync();
        var court = await club.SpaceAsync("Quadra de Futcampo");
        var date = ClubScope.Next(DayOfWeek.Monday);
        var created = await club.Admin.PostAsync<ReservationDetailDto>("/api/reservas", Request(court.Id, date, [8], memberId: member.Id));
        var slot = created.Reservation.Slots.Single();

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => fixture.DbAsync(club.ClubId, async db =>
        {
            db.Set<ReservationSlot>().Add(new ReservationSlot { ReservationId = created.Reservation.Id, SpaceId = court.Id, StartAt = slot.StartAt.AddMinutes(30), EndAt = slot.EndAt.AddMinutes(30) });
            return await db.SaveChangesAsync();
        }));
        Assert.Contains(ReservationService.OverlapConstraint, error.InnerException!.Message);
    }

    [Fact]
    public async Task Respeita_horarios_de_funcionamento_das_quadras_e_quiosques()
    {
        var club = await fixture.NewClubAsync();
        var member = await club.NewMemberAsync();
        var court = await club.SpaceAsync("Quadra de Tênis");
        var kiosk = await club.SpaceAsync("Quiosque Aberto 1");

        // Quadras: 08h às 22h.
        await club.Admin.FailAsync(HttpMethod.Post, "/api/reservas", HttpStatusCode.BadRequest, Request(court.Id, ClubScope.Next(DayOfWeek.Monday), [7], memberId: member.Id));
        await club.Admin.FailAsync(HttpMethod.Post, "/api/reservas", HttpStatusCode.BadRequest, Request(court.Id, ClubScope.Next(DayOfWeek.Monday), [22], memberId: member.Id));
        await club.Admin.PostAsync<ReservationDetailDto>("/api/reservas", Request(court.Id, ClubScope.Next(DayOfWeek.Monday), [21], memberId: member.Id));

        // Quiosques: 12h às 22h; sextas e sábados até 00h.
        await club.Admin.FailAsync(HttpMethod.Post, "/api/reservas", HttpStatusCode.BadRequest, Request(kiosk.Id, ClubScope.Next(DayOfWeek.Monday), [11], memberId: member.Id));
        await club.Admin.FailAsync(HttpMethod.Post, "/api/reservas", HttpStatusCode.BadRequest, Request(kiosk.Id, ClubScope.Next(DayOfWeek.Monday), [22], memberId: member.Id));
        var friday = await club.Admin.PostAsync<ReservationDetailDto>("/api/reservas", Request(kiosk.Id, ClubScope.Next(DayOfWeek.Friday), [22, 23], memberId: member.Id));
        Assert.Equal(TimeSpan.FromHours(2), friday.Reservation.EndAt - friday.Reservation.StartAt);
        await club.Admin.PostAsync<ReservationDetailDto>("/api/reservas", Request(kiosk.Id, ClubScope.Next(DayOfWeek.Saturday), [23], memberId: member.Id));
    }

    [Fact]
    public async Task Respeita_capacidade_limite_de_convidados_e_piscina_nao_reservavel()
    {
        var club = await fixture.NewClubAsync();
        var member = await club.NewMemberAsync();
        var kiosk = await club.SpaceAsync("Quiosque Fechado 2");
        var pool = await club.SpaceAsync("Piscinas");
        var date = ClubScope.Next(DayOfWeek.Sunday);

        await club.Admin.FailAsync(HttpMethod.Post, "/api/reservas", (HttpStatusCode)422, Request(kiosk.Id, date, [13], people: 21, memberId: member.Id), "capacidade_excedida");
        var guests = Enumerable.Range(1, 4).Select(i => new GuestInput($"Convidado {i}", null, null)).ToArray();
        await club.Admin.FailAsync(HttpMethod.Post, "/api/reservas", (HttpStatusCode)422, Request(kiosk.Id, date, [13], people: 10, memberId: member.Id, guests: guests), "limite_convidados");
        await club.Admin.FailAsync(HttpMethod.Post, "/api/reservas", HttpStatusCode.BadRequest, Request(pool.Id, date, [13], memberId: member.Id));

        var ok = await club.Admin.PostAsync<ReservationDetailDto>("/api/reservas", Request(kiosk.Id, date, [13], people: 20, memberId: member.Id, guests: guests[..3]));
        Assert.Equal(3, ok.Guests.Count);
        Assert.All(ok.Guests, g => Assert.Equal(date, g.VisitDate));
    }

    [Fact]
    public async Task Salao_de_festas_e_reservado_por_dois_dias_com_limpeza_incluida_no_total()
    {
        var club = await fixture.NewClubAsync(s => s.DueDay = 10);
        var member = await club.NewMemberAsync();
        var hall = await club.SpaceAsync("Salão de Festas");
        Assert.Equal((250, BookingMode.MultiDay, 2), (hall.Capacity, hall.BookingMode, hall.FixedDays));
        await club.Admin.PutAsync<SpaceDto>($"/api/espacos/{hall.Id}", new SaveSpaceRequest
        {
            Name = hall.Name, Type = hall.Type, Capacity = 250, IsReservable = true, BookingMode = BookingMode.MultiDay, FixedDays = 2, PeriodPrice = 1500,
            CleaningFee = 300, ChargeMode = SpaceChargeMode.NextMonthlyBill, IsActive = true,
        });
        var date = ClubScope.Today.AddDays(20);

        var quote = await club.Admin.PostAsync<QuoteDto>("/api/reservas/simular", Request(hall.Id, date, [], people: 200, memberId: member.Id));
        Assert.Equal((1500m, 300m, 1800m), (quote.RentalAmount, quote.CleaningFee, quote.TotalAmount));
        Assert.Equal(TimeSpan.FromDays(2), quote.EndAt - quote.StartAt);
        await club.Admin.FailAsync(HttpMethod.Post, "/api/reservas", (HttpStatusCode)422, Request(hall.Id, date, [], people: 251, memberId: member.Id), "capacidade_excedida");

        var created = await club.Admin.PostAsync<ReservationDetailDto>("/api/reservas", Request(hall.Id, date, [], people: 200, memberId: member.Id));
        Assert.Equal(ReservationStatus.Approved, created.Reservation.Status);
        // O segundo dia já está ocupado pelo período de montagem/limpeza: não aceita outra reserva.
        await club.Admin.FailAsync(HttpMethod.Post, "/api/reservas", HttpStatusCode.Conflict, Request(hall.Id, date.AddDays(1), [], people: 50, memberId: member.Id));
        var period = await club.Admin.GetAsync<List<PeriodDayDto>>($"/api/espacos/{hall.Id}/disponibilidade-periodo?from={date.AddDays(-1):yyyy-MM-dd}&to={date.AddDays(2):yyyy-MM-dd}");
        Assert.Equal([SlotState.Reserved, SlotState.Reserved, SlotState.Reserved, SlotState.Available], period.Select(p => p.State));

        var charge = await club.Admin.GetAsync<ChargeDetailDto>($"/api/financeiro/cobrancas/{created.Reservation.ChargeId}");
        Assert.Equal((ChargeType.Reservation, 1800m), (charge.Charge.Type, charge.Charge.Total));
        Assert.Contains("limpeza", charge.Charge.Description);
    }

    [Fact]
    public async Task Bloqueio_e_feriado_refletem_na_disponibilidade_e_impedem_reservas()
    {
        var club = await fixture.NewClubAsync();
        var member = await club.NewMemberAsync();
        var court = await club.SpaceAsync("Quadra de Tênis");
        var date = ClubScope.Next(DayOfWeek.Wednesday).AddDays(7);
        var existing = await club.Admin.PostAsync<ReservationDetailDto>("/api/reservas", Request(court.Id, date, [15], memberId: member.Id));

        var start = existing.Reservation.StartAt.AddHours(-1);
        var block = await club.Admin.PostAsync<BlockResult>("/api/espacos/bloqueios", new CreateBlockRequest(court.Id, start, start.AddHours(3), "Troca do piso", BlockKind.Maintenance, null));
        Assert.Equal([existing.Reservation.Id], block.Conflicts.Select(c => c.ReservationId)); // conflito identificado para a administração

        var day = await club.Admin.GetAsync<DayAvailabilityDto>($"/api/espacos/{court.Id}/disponibilidade?date={date:yyyy-MM-dd}");
        Assert.Equal(SlotState.Blocked, day.Slots.Single(s => s.StartMinute == 14 * 60).State);
        await club.Admin.FailAsync(HttpMethod.Post, "/api/reservas", HttpStatusCode.Conflict, Request(court.Id, date, [16], memberId: member.Id), "espaco_bloqueado");

        var holiday = date.AddDays(1);
        await club.Admin.PostAsync<HolidayDto>("/api/espacos/feriados", new SaveHolidayRequest(holiday, "Fechamento excepcional", HolidayScope.ExceptionalClosure, true, null, null, null));
        var closed = await club.Admin.GetAsync<DayAvailabilityDto>($"/api/espacos/{court.Id}/disponibilidade?date={holiday:yyyy-MM-dd}");
        Assert.False(closed.IsOpen);
        await club.Admin.FailAsync(HttpMethod.Post, "/api/reservas", (HttpStatusCode)422, Request(court.Id, holiday, [10], memberId: member.Id), "espaco_fechado");
    }

    [Fact]
    public async Task Inadimplente_nao_reserva_quando_a_politica_esta_habilitada()
    {
        var club = await fixture.NewClubAsync(s => { s.GraceDays = 0; s.BlockReservationsWhenDelinquent = true; });
        var (member, api) = await club.NewMemberWithLoginAsync();
        await club.Admin.PostAsync<ChargeDetailDto>("/api/financeiro/cobrancas", new CreateChargeRequest(member.Id, ChargeType.Fee, "Vencida", 100, ClubScope.Today.AddDays(-30)));
        var court = await club.SpaceAsync("Quadra de Tênis");
        await api.FailAsync(HttpMethod.Post, "/api/reservas", HttpStatusCode.Forbidden, Request(court.Id, ClubScope.Next(DayOfWeek.Monday), [9]), "bloqueio_financeiro");
        await api.FailAsync(HttpMethod.Post, "/api/convidados", HttpStatusCode.Forbidden, new CreateGuestRequest("Convidado", null, null, ClubScope.Today.AddDays(1), null, null), "bloqueio_financeiro");
    }

    [Fact]
    public async Task Limites_configurados_de_antecedencia_e_reservas_simultaneas()
    {
        var club = await fixture.NewClubAsync(s => { s.MaxAdvanceDays = 7; s.MaxConcurrentReservations = 1; s.ReservationApprovalMode = ApprovalMode.Automatic; });
        var (_, api) = await club.NewMemberWithLoginAsync();
        var court = await club.SpaceAsync("Quadra de Tênis");
        await api.FailAsync(HttpMethod.Post, "/api/reservas", (HttpStatusCode)422, Request(court.Id, ClubScope.Today.AddDays(30), [9]), "antecedencia_excedida");

        var first = await api.PostAsync<ReservationDetailDto>("/api/reservas", Request(court.Id, ClubScope.Today.AddDays(2), [9]));
        Assert.Equal(ReservationStatus.Approved, first.Reservation.Status); // aprovação automática configurada
        await api.FailAsync(HttpMethod.Post, "/api/reservas", (HttpStatusCode)422, Request(court.Id, ClubScope.Today.AddDays(3), [9]), "limite_reservas");
    }

    [Fact]
    public async Task Dependente_so_solicita_com_permissao_e_sempre_passa_por_aprovacao()
    {
        var club = await fixture.NewClubAsync(s => s.ReservationApprovalMode = ApprovalMode.Automatic);
        var (member, memberApi) = await club.NewMemberWithLoginAsync();
        var (dependent, api) = await club.NewDependentWithLoginAsync(member.Id);
        var court = await club.SpaceAsync("Quadra de Tênis");
        var request = Request(court.Id, ClubScope.Next(DayOfWeek.Monday), [10]);

        await api.FailAsync(HttpMethod.Post, "/api/reservas", HttpStatusCode.Forbidden, request);
        var permission = new SetDependentPermissionRequest(DependentPermissions.RequestReservations, true);
        await club.Admin.PutAsync<DependentDto>($"/api/dependentes/{dependent.Id}/permissoes", permission);
        await api.FailAsync(HttpMethod.Post, "/api/reservas", HttpStatusCode.Forbidden, request); // falta o associado habilitar
        await memberApi.PutAsync<DependentDto>($"/api/dependentes/{dependent.Id}/permissoes", permission);

        var created = await api.PostAsync<ReservationDetailDto>("/api/reservas", request);
        // Mesmo com aprovação automática configurada, a solicitação do dependente aguarda a administração.
        Assert.Equal(ReservationStatus.Pending, created.Reservation.Status);
        Assert.True(created.Reservation.RequestedByDependent);
        Assert.Equal(member.Id, created.Reservation.MemberId);
        Assert.Single((await api.GetAsync<PagedResult<ReservationDto>>("/api/reservas")).Items);
    }

    [Fact]
    public async Task Cancelamento_libera_o_horario_e_multa_so_existe_se_configurada()
    {
        var club = await fixture.NewClubAsync(s => { s.ReservationApprovalMode = ApprovalMode.Automatic; s.FreeCancellationHours = 24 * 365; s.LateCancellationFee = 40; });
        var (member, api) = await club.NewMemberWithLoginAsync();
        var court = await club.SpaceAsync("Quadra de Tênis");
        var date = ClubScope.Next(DayOfWeek.Monday);
        var created = await api.PostAsync<ReservationDetailDto>("/api/reservas", Request(court.Id, date, [9]));

        var cancelled = await api.PostAsync<ReservationDetailDto>($"/api/reservas/{created.Reservation.Id}/cancelar", new DecisionRequest("Imprevisto"));
        Assert.Equal(ReservationStatus.Cancelled, cancelled.Reservation.Status);
        Assert.NotNull(cancelled.Reservation.PenaltyChargeId); // fora do prazo configurado → multa configurada lançada
        var charges = await club.Admin.GetAsync<PagedResult<ChargeDto>>($"/api/financeiro/cobrancas?memberId={member.Id}&type=Penalty");
        Assert.Equal(40m, charges.Items.Single().Total);

        var day = await api.GetAsync<DayAvailabilityDto>($"/api/espacos/{court.Id}/disponibilidade?date={date:yyyy-MM-dd}");
        Assert.Equal(SlotState.Available, day.Slots.Single(s => s.StartMinute == 540).State);

        // Sem regra configurada, o cancelamento não gera penalidade.
        await club.ConfigureAsync(s => { s.FreeCancellationHours = null; s.LateCancellationFee = null; });
        var again = await api.PostAsync<ReservationDetailDto>("/api/reservas", Request(court.Id, date, [9]));
        var free = await api.PostAsync<ReservationDetailDto>($"/api/reservas/{again.Reservation.Id}/cancelar", new DecisionRequest(null));
        Assert.Null(free.Reservation.PenaltyChargeId);
    }

    [Fact]
    public async Task Associado_nao_ve_nem_cancela_reserva_de_outro()
    {
        var club = await fixture.NewClubAsync();
        var (_, owner) = await club.NewMemberWithLoginAsync();
        var (_, stranger) = await club.NewMemberWithLoginAsync();
        var court = await club.SpaceAsync("Quadra de Tênis");
        var created = await owner.PostAsync<ReservationDetailDto>("/api/reservas", Request(court.Id, ClubScope.Next(DayOfWeek.Monday), [9]));
        await stranger.FailAsync(HttpMethod.Get, $"/api/reservas/{created.Reservation.Id}", HttpStatusCode.NotFound);
        await stranger.FailAsync(HttpMethod.Post, $"/api/reservas/{created.Reservation.Id}/cancelar", HttpStatusCode.NotFound, new DecisionRequest("x"));
        await stranger.FailAsync(HttpMethod.Post, $"/api/reservas/{created.Reservation.Id}/aprovar", HttpStatusCode.Forbidden, new DecisionRequest(null));
        Assert.Empty((await stranger.GetAsync<PagedResult<ReservationDto>>("/api/reservas")).Items);
    }
}

[Collection("api")]
public class GateTests(ApiFixture fixture)
{
    [Fact]
    public async Task Qr_code_e_de_uso_unico_e_alterna_entre_entrada_e_saida()
    {
        var club = await fixture.NewClubAsync();
        var (member, api) = await club.NewMemberWithLoginAsync();
        var gate = await club.NewStaffAsync("Portaria");

        var entryToken = await api.PostAsync<QrTokenDto>("/api/qr-code");
        Assert.Equal(AccessOperation.Entry, entryToken.Operation);
        var entry = await gate.PostAsync<AccessResultDto>("/api/portaria/validar", new ValidateRequest(entryToken.Token, "leitor-1"));
        Assert.True(entry.Authorized);
        Assert.Equal((PersonType.Member, member.Id, AccessOperation.Entry), (entry.PersonType, entry.PersonId, entry.Operation));

        // O mesmo código não pode ser reutilizado.
        var reuse = await gate.PostAsync<AccessResultDto>("/api/portaria/validar", new ValidateRequest(entryToken.Token, "leitor-1"));
        Assert.False(reuse.Authorized);
        Assert.Contains("já utilizado", reuse.Message);

        // Para a saída é gerado outro token, com outro código numérico.
        var exitToken = await api.PostAsync<QrTokenDto>("/api/qr-code");
        Assert.Equal(AccessOperation.Exit, exitToken.Operation);
        Assert.NotEqual(entryToken.Token, exitToken.Token);
        Assert.NotEqual(entryToken.DisplayCode, exitToken.DisplayCode);
        var exit = await gate.PostAsync<AccessResultDto>("/api/portaria/validar", new ValidateRequest(exitToken.DisplayCode, null)); // validação pelo código numérico
        Assert.True(exit.Authorized);
        Assert.Equal(AccessOperation.Exit, exit.Operation);

        var history = await gate.GetAsync<PagedResult<AccessLogDto>>("/api/acessos");
        Assert.Equal(3, history.Total);
        Assert.Equal(1, history.Items.Count(l => l.Result == AccessResult.Denied));
        Assert.All(history.Items, l => Assert.Equal(gate.Auth!.User.Name, l.StaffName));
    }

    [Fact]
    public async Task Token_novo_invalida_o_anterior_e_token_expirado_e_recusado()
    {
        var club = await fixture.NewClubAsync();
        var (_, api) = await club.NewMemberWithLoginAsync();
        var gate = await club.NewStaffAsync("Portaria");

        var first = await api.PostAsync<QrTokenDto>("/api/qr-code");
        var second = await api.PostAsync<QrTokenDto>("/api/qr-code");
        var stale = await gate.PostAsync<AccessResultDto>("/api/portaria/validar", new ValidateRequest(first.Token, null));
        Assert.False(stale.Authorized);
        Assert.Contains("expirado", stale.Message);

        await fixture.DbAsync(club.ClubId, db => db.Set<QrToken>().Where(t => t.ConsumedAt == null).ExecuteUpdateAsync(s => s.SetProperty(t => t.ExpiresAt, DateTime.UtcNow.AddSeconds(-1))));
        var expired = await gate.PostAsync<AccessResultDto>("/api/portaria/validar", new ValidateRequest(second.Token, null));
        Assert.False(expired.Authorized);

        var invalid = await gate.PostAsync<AccessResultDto>("/api/portaria/validar", new ValidateRequest("CLB1:token-que-nao-existe", null));
        Assert.False(invalid.Authorized);
        Assert.Empty(await gate.GetAsync<List<PresenceDto>>("/api/portaria/presentes"));
    }

    [Fact]
    public async Task Leituras_simultaneas_do_mesmo_qr_registram_uma_unica_entrada()
    {
        var club = await fixture.NewClubAsync();
        var (_, api) = await club.NewMemberWithLoginAsync();
        var gate = await club.NewStaffAsync("Portaria");
        var token = await api.PostAsync<QrTokenDto>("/api/qr-code");

        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => gate.PostAsync<AccessResultDto>("/api/portaria/validar", new ValidateRequest(token.Token, null))));
        Assert.Equal(1, results.Count(r => r.Authorized));
        Assert.Single(await gate.GetAsync<List<PresenceDto>>("/api/portaria/presentes"));
    }

    [Fact]
    public async Task Dependente_tem_qr_proprio_e_acesso_identificado_individualmente()
    {
        var club = await fixture.NewClubAsync();
        var member = await club.NewMemberAsync();
        var (dependent, api) = await club.NewDependentWithLoginAsync(member.Id);
        var gate = await club.NewStaffAsync("Portaria");

        var card = await api.GetAsync<CardDto>("/api/cartoes/meu");
        Assert.Equal((PersonType.Dependent, dependent.Id, member.FullName), (card.PersonType, card.PersonId, card.ResponsibleName));
        var result = await gate.PostAsync<AccessResultDto>("/api/portaria/validar", new ValidateRequest((await api.PostAsync<QrTokenDto>("/api/qr-code")).Token, null));
        Assert.True(result.Authorized);
        Assert.Equal((PersonType.Dependent, dependent.Id, member.FullName), (result.PersonType, result.PersonId, result.ResponsibleName));

        var occupancy = await gate.GetAsync<OccupancyDto>("/api/portaria/ocupacao");
        Assert.Equal((0, 1, 0, 1), (occupancy.Members, occupancy.Dependents, occupancy.Guests, occupancy.Total));
    }

    [Fact]
    public async Task Inadimplencia_e_suspensao_negam_a_entrada()
    {
        var club = await fixture.NewClubAsync(s => { s.GraceDays = 0; s.BlockAccessWhenDelinquent = true; });
        var gate = await club.NewStaffAsync("Portaria");
        var (debtor, debtorApi) = await club.NewMemberWithLoginAsync();
        var debtorToken = await debtorApi.PostAsync<QrTokenDto>("/api/qr-code");
        await club.Admin.PostAsync<ChargeDetailDto>("/api/financeiro/cobrancas", new CreateChargeRequest(debtor.Id, ChargeType.Fee, "Vencida", 100, ClubScope.Today.AddDays(-15)));
        var denied = await gate.PostAsync<AccessResultDto>("/api/portaria/validar", new ValidateRequest(debtorToken.Token, null));
        Assert.False(denied.Authorized);
        Assert.Contains("pendências financeiras", denied.Message);

        // Liberação excepcional justificada restabelece o acesso.
        await club.Admin.PostAsync<ReleaseDto>("/api/financeiro/liberacoes", new GrantReleaseRequest(debtor.Id, "Liberação para regularização", ClubScope.Today.AddDays(2)));
        var released = await gate.PostAsync<AccessResultDto>("/api/portaria/validar", new ValidateRequest((await debtorApi.PostAsync<QrTokenDto>("/api/qr-code")).Token, null));
        Assert.True(released.Authorized);

        var (suspended, suspendedApi) = await club.NewMemberWithLoginAsync();
        var token = await suspendedApi.PostAsync<QrTokenDto>("/api/qr-code");
        await club.Admin.PostAsync<MemberDto>($"/api/associados/{suspended.Id}/suspender", new StatusChangeRequest("Suspensão disciplinar"));
        var blocked = await gate.PostAsync<AccessResultDto>("/api/portaria/validar", new ValidateRequest(token.Token, null));
        Assert.False(blocked.Authorized);
        await suspendedApi.FailAsync(HttpMethod.Post, "/api/qr-code", HttpStatusCode.Forbidden);

        var deniedLogs = await gate.GetAsync<PagedResult<AccessLogDto>>("/api/acessos?result=Denied");
        Assert.Equal(2, deniedLogs.Total);
    }

    [Fact]
    public async Task Somente_a_portaria_valida_e_o_associado_nao_valida_o_proprio_qr()
    {
        var club = await fixture.NewClubAsync();
        var (_, api) = await club.NewMemberWithLoginAsync();
        var token = await api.PostAsync<QrTokenDto>("/api/qr-code");
        await api.FailAsync(HttpMethod.Post, "/api/portaria/validar", HttpStatusCode.Forbidden, new ValidateRequest(token.Token, null));
        var finance = await club.NewStaffAsync("Financeiro");
        await finance.FailAsync(HttpMethod.Post, "/api/portaria/validar", HttpStatusCode.Forbidden, new ValidateRequest(token.Token, null));
    }

    [Fact]
    public async Task Convidados_respeitam_limite_autorizacao_e_data()
    {
        var club = await fixture.NewClubAsync(s => s.GuestFee = 25);
        var (member, api) = await club.NewMemberWithLoginAsync();
        var gate = await club.NewStaffAsync("Portaria");
        var today = ClubScope.Today;

        var guests = new List<GuestDto>();
        for (var i = 1; i <= 3; i++) guests.Add(await api.PostAsync<GuestDto>("/api/convidados", new CreateGuestRequest($"Convidado {i}", "RG 123", null, today, null, null)));
        await api.FailAsync(HttpMethod.Post, "/api/convidados", (HttpStatusCode)422, new CreateGuestRequest("Quarto Convidado", null, null, today, null, null), "limite_convidados");
        Assert.All(guests, g => Assert.Equal((GuestAuthorization.Pending, 25m), (g.Authorization, g.FeeAmount)));
        Assert.Equal(75m, (await club.Admin.GetAsync<PagedResult<ChargeDto>>($"/api/financeiro/cobrancas?memberId={member.Id}&type=Guest")).Items.Sum(c => c.Total));

        // Sem autorização da administração, a entrada é negada.
        var denied = await gate.PostAsync<AccessResultDto>($"/api/portaria/convidados/{guests[0].Id}/entrada");
        Assert.False(denied.Authorized);
        await club.Admin.PostAsync<GuestDto>($"/api/convidados/{guests[0].Id}/autorizacao", new AuthorizeGuestRequest(true, null));
        var entry = await gate.PostAsync<AccessResultDto>($"/api/portaria/convidados/{guests[0].Id}/entrada");
        Assert.True(entry.Authorized);
        Assert.Equal(member.FullName, entry.ResponsibleName);
        Assert.Equal(1, (await gate.GetAsync<OccupancyDto>("/api/portaria/ocupacao")).Guests);
        Assert.True((await gate.PostAsync<AccessResultDto>($"/api/portaria/convidados/{guests[0].Id}/saida")).Authorized);

        // Convidado não autorizado tem a taxa cancelada.
        await club.Admin.PostAsync<GuestDto>($"/api/convidados/{guests[1].Id}/autorizacao", new AuthorizeGuestRequest(false, "Documento divergente"));
        Assert.Equal(1, (await club.Admin.GetAsync<PagedResult<ChargeDto>>($"/api/financeiro/cobrancas?memberId={member.Id}&type=Guest&status=Cancelled")).Total);
    }
}

[Collection("api")]
public class CommunicationTests(ApiFixture fixture)
{
    [Fact]
    public async Task Somente_associados_votam_e_apenas_uma_vez()
    {
        var club = await fixture.NewClubAsync();
        var (member, memberApi) = await club.NewMemberWithLoginAsync();
        var (_, dependentApi) = await club.NewDependentWithLoginAsync(member.Id);
        var staff = await club.NewStaffAsync("Secretaria");

        var poll = await club.Admin.PostAsync<PollDto>("/api/votacoes", new SavePollRequest { Title = "Reforma da piscina", Options = ["Aprovar", "Rejeitar"] });
        await memberApi.FailAsync(HttpMethod.Post, $"/api/votacoes/{poll.Id}/votos", HttpStatusCode.Conflict, new VoteRequest(poll.Options[0].Id)); // rascunho ainda não aceita votos
        poll = await club.Admin.PostAsync<PollDto>($"/api/votacoes/{poll.Id}/abrir");
        var option = poll.Options[0].Id;

        // Dependentes nunca votam nem enxergam votações ou assembleias; funcionários também não votam.
        await dependentApi.FailAsync(HttpMethod.Post, $"/api/votacoes/{poll.Id}/votos", HttpStatusCode.Forbidden, new VoteRequest(option));
        await dependentApi.FailAsync(HttpMethod.Get, "/api/votacoes", HttpStatusCode.Forbidden);
        await dependentApi.FailAsync(HttpMethod.Get, "/api/assembleias", HttpStatusCode.Forbidden);
        await staff.FailAsync(HttpMethod.Post, $"/api/votacoes/{poll.Id}/votos", HttpStatusCode.Forbidden, new VoteRequest(option));
        await club.Admin.FailAsync(HttpMethod.Post, $"/api/votacoes/{poll.Id}/votos", HttpStatusCode.Forbidden, new VoteRequest(option));

        var voted = await memberApi.PostAsync<PollDto>($"/api/votacoes/{poll.Id}/votos", new VoteRequest(option));
        Assert.True(voted.HasVoted);
        Assert.Null(voted.TotalVotes); // resultado parcial não é exposto ao associado
        await memberApi.FailAsync(HttpMethod.Post, $"/api/votacoes/{poll.Id}/votos", HttpStatusCode.Conflict, new VoteRequest(poll.Options[1].Id));

        var closed = await club.Admin.PostAsync<PollDto>($"/api/votacoes/{poll.Id}/encerrar", new { publishResults = true });
        Assert.Equal((PollStatus.Closed, 1), (closed.Status, closed.TotalVotes));
        Assert.Equal(1, (await memberApi.GetAsync<PollDto>($"/api/votacoes/{poll.Id}")).Options[0].Votes);
        await memberApi.FailAsync(HttpMethod.Post, $"/api/votacoes/{poll.Id}/votos", HttpStatusCode.Conflict, new VoteRequest(option));
    }

    [Fact]
    public async Task Votos_simultaneos_do_mesmo_associado_contam_uma_unica_vez()
    {
        var club = await fixture.NewClubAsync();
        var (_, memberApi) = await club.NewMemberWithLoginAsync();
        var poll = await club.Admin.PostAsync<PollDto>("/api/votacoes", new SavePollRequest { Title = "Votação concorrente", Options = ["Sim", "Não"] });
        poll = await club.Admin.PostAsync<PollDto>($"/api/votacoes/{poll.Id}/abrir");
        await Task.WhenAll(Enumerable.Range(0, 8).Select(i => memberApi.SendAsync(HttpMethod.Post, $"/api/votacoes/{poll.Id}/votos", new VoteRequest(poll.Options[i % 2].Id))));
        Assert.Equal(1, (await club.Admin.GetAsync<PollDto>($"/api/votacoes/{poll.Id}")).TotalVotes);
    }

    [Fact]
    public async Task Criterio_de_participacao_restringe_o_voto()
    {
        var club = await fixture.NewClubAsync();
        var (present, presentApi) = await club.NewMemberWithLoginAsync();
        var (_, absentApi) = await club.NewMemberWithLoginAsync();
        var assembly = await club.Admin.PostAsync<AssemblyDto>("/api/assembleias", new SaveAssemblyRequest("Assembleia Geral", DateTime.UtcNow.AddDays(1), "Salão", "Pauta"));
        await club.Admin.PostAsync<List<AttendanceDto>>($"/api/assembleias/{assembly.Id}/presencas", new { memberId = present.Id });
        var poll = await club.Admin.PostAsync<PollDto>("/api/votacoes", new SavePollRequest { AssemblyId = assembly.Id, Title = "Pauta 1", Options = ["A favor", "Contra"], Criterion = PollCriterion.PresentAtAssembly });
        poll = await club.Admin.PostAsync<PollDto>($"/api/votacoes/{poll.Id}/abrir");

        await absentApi.FailAsync(HttpMethod.Post, $"/api/votacoes/{poll.Id}/votos", HttpStatusCode.Forbidden, new VoteRequest(poll.Options[0].Id));
        await presentApi.PostAsync<PollDto>($"/api/votacoes/{poll.Id}/votos", new VoteRequest(poll.Options[0].Id));
    }

    [Fact]
    public async Task Comunicados_respeitam_a_segmentacao_e_dependentes_so_veem_os_autorizados()
    {
        var club = await fixture.NewClubAsync();
        var (member, memberApi) = await club.NewMemberWithLoginAsync();
        var (_, dependentApi) = await club.NewDependentWithLoginAsync(member.Id);
        var staff = await club.NewStaffAsync("Portaria");

        async Task<AnnouncementDto> Publish(string title, bool members, bool dependents, bool staffAudience, List<Guid>? targets = null) =>
            await club.Admin.PostAsync<AnnouncementDto>("/api/comunicados", new SaveAnnouncementRequest
            {
                Title = title, Body = "Conteúdo do comunicado", Category = AnnouncementCategory.Notice, AudienceMembers = members, AudienceDependents = dependents,
                AudienceStaff = staffAudience, IsSpecific = targets is not null, TargetUserIds = targets ?? [], Publish = true,
            });
        var onlyMembers = await Publish("Somente associados", true, false, false);
        await Publish("Para dependentes", false, true, false);
        await Publish("Para todos", true, true, true);
        await Publish("Equipe", false, false, true);
        await Publish("Individual", false, false, false, [memberApi.Auth!.User.Id]);
        await club.Admin.PostAsync<AnnouncementDto>("/api/comunicados", new SaveAnnouncementRequest { Title = "Rascunho", Body = "Não publicado", AudienceMembers = true, AudienceDependents = true, Publish = false });

        async Task<List<string>> Feed(Api api) => (await api.GetAsync<PagedResult<AnnouncementDto>>("/api/comunicados/mural")).Items.Select(a => a.Title).Order().ToList();
        Assert.Equal(["Individual", "Para todos", "Somente associados"], await Feed(memberApi));
        Assert.Equal(["Para dependentes", "Para todos"], await Feed(dependentApi));
        Assert.Equal(["Equipe", "Para todos"], await Feed(staff));

        await dependentApi.FailAsync(HttpMethod.Get, $"/api/comunicados/{onlyMembers.Id}", HttpStatusCode.NotFound);
        await dependentApi.FailAsync(HttpMethod.Post, $"/api/comunicados/{onlyMembers.Id}/lido", HttpStatusCode.NotFound);
        await memberApi.OkAsync(HttpMethod.Post, $"/api/comunicados/{onlyMembers.Id}/lido");
        Assert.Single(await club.Admin.GetAsync<List<AnnouncementReaderDto>>($"/api/comunicados/{onlyMembers.Id}/leituras"));

        // A publicação gera notificação apenas para o público do comunicado.
        Assert.DoesNotContain((await dependentApi.GetAsync<PagedResult<NotificationDto>>("/api/notificacoes")).Items, n => n.Title == "Somente associados");
        Assert.Contains((await memberApi.GetAsync<PagedResult<NotificationDto>>("/api/notificacoes")).Items, n => n.Title == "Somente associados");
    }

    [Fact]
    public async Task Manifestacao_tem_protocolo_fluxo_completo_e_notifica_o_autor()
    {
        var club = await fixture.NewClubAsync();
        var (member, memberApi) = await club.NewMemberWithLoginAsync();
        var (_, dependentApi) = await club.NewDependentWithLoginAsync(member.Id);
        var types = await memberApi.GetAsync<List<ManifestationTypeDto>>("/api/manifestacoes/tipos");
        var suggestion = types.Single(t => t.IsSuggestion);
        var complaint = types.First(t => t.Name == "Reclamação");

        var created = await memberApi.PostAsync<ManifestationDetailDto>("/api/manifestacoes", new CreateManifestationRequest(suggestion.Id, "Sugiro instalar iluminação na quadra de futcampo.", null));
        Assert.Equal(ManifestationStatus.Received, created.Manifestation.Status);
        Assert.Matches(@"^\d{10}$", created.Manifestation.Protocol);
        var id = created.Manifestation.Id;
        Assert.Contains((await club.Admin.GetAsync<PagedResult<NotificationDto>>("/api/notificacoes")).Items, n => n.Type == "manifestation.new" && n.EntityId == id);

        await club.Admin.PostAsync<ManifestationDetailDto>($"/api/manifestacoes/{id}/andamento", new UpdateManifestationRequest(ManifestationStatus.UnderReview, null, "Infraestrutura", null));
        await club.Admin.PostAsync<ManifestationDetailDto>($"/api/manifestacoes/{id}/andamento", new UpdateManifestationRequest(ManifestationStatus.Accepted, null, null, null));
        await club.Admin.PostAsync<ManifestationDetailDto>($"/api/manifestacoes/{id}/respostas", new RespondManifestationRequest("Anotação interna", false));
        var detail = await memberApi.GetAsync<ManifestationDetailDto>($"/api/manifestacoes/{id}");
        Assert.Equal(ManifestationStatus.Accepted, detail.Manifestation.Status);
        Assert.DoesNotContain(detail.Updates, u => u.Message == "Anotação interna"); // nota interna não aparece para o autor

        var notifications = (await memberApi.GetAsync<PagedResult<NotificationDto>>("/api/notificacoes")).Items;
        Assert.Contains(notifications, n => n.Type == "manifestation.received");
        Assert.Contains(notifications, n => n.Type == "manifestation.review" && n.Body.Contains("analisada"));
        Assert.Contains(notifications, n => n.Type == "manifestation.accepted" && n.Body.Contains("assembleia"));

        // Reclamação não pode ser "aceita"; resolução exige resposta; dependente também envia e só vê as suas.
        var mine = await dependentApi.PostAsync<ManifestationDetailDto>("/api/manifestacoes", new CreateManifestationRequest(complaint.Id, "O vestiário estava sem iluminação ontem à noite.", null));
        await club.Admin.FailAsync(HttpMethod.Post, $"/api/manifestacoes/{mine.Manifestation.Id}/andamento", HttpStatusCode.BadRequest, new UpdateManifestationRequest(ManifestationStatus.Accepted, null, null, null));
        await club.Admin.FailAsync(HttpMethod.Post, $"/api/manifestacoes/{mine.Manifestation.Id}/andamento", HttpStatusCode.BadRequest, new UpdateManifestationRequest(ManifestationStatus.Resolved, null, null, null));
        await club.Admin.PostAsync<ManifestationDetailDto>($"/api/manifestacoes/{mine.Manifestation.Id}/andamento", new UpdateManifestationRequest(ManifestationStatus.Resolved, "Lâmpadas substituídas.", null, null));
        Assert.Single((await dependentApi.GetAsync<PagedResult<ManifestationDto>>("/api/manifestacoes")).Items);
        await dependentApi.FailAsync(HttpMethod.Get, $"/api/manifestacoes/{id}", HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Evento_respeita_publico_capacidade_e_inscricao_unica()
    {
        var club = await fixture.NewClubAsync();
        var (member, memberApi) = await club.NewMemberWithLoginAsync();
        var (_, otherApi) = await club.NewMemberWithLoginAsync();
        var (_, dependentApi) = await club.NewDependentWithLoginAsync(member.Id);
        var ev = await club.Admin.PostAsync<EventDto>("/api/eventos", new SaveEventRequest
        {
            Title = "Jantar dos associados", StartAt = DateTime.UtcNow.AddDays(5), EndAt = DateTime.UtcNow.AddDays(5).AddHours(4), Capacity = 1,
            Audience = EventAudience.MembersOnly, RegistrationRequired = true,
        });

        Assert.Empty((await dependentApi.GetAsync<PagedResult<EventDto>>("/api/eventos")).Items); // exclusivo para associados
        await dependentApi.FailAsync(HttpMethod.Post, $"/api/eventos/{ev.Id}/inscricao", HttpStatusCode.Forbidden);
        var registered = await memberApi.PostAsync<EventDto>($"/api/eventos/{ev.Id}/inscricao");
        Assert.True(registered is { IsRegistered: true, Registered: 1 });
        await memberApi.FailAsync(HttpMethod.Post, $"/api/eventos/{ev.Id}/inscricao", HttpStatusCode.Conflict);
        await otherApi.FailAsync(HttpMethod.Post, $"/api/eventos/{ev.Id}/inscricao", HttpStatusCode.Conflict, code: "evento_lotado");

        await memberApi.OkAsync(HttpMethod.Delete, $"/api/eventos/{ev.Id}/inscricao");
        await otherApi.PostAsync<EventDto>($"/api/eventos/{ev.Id}/inscricao");
        await club.Admin.PostAsync<EventDto>($"/api/eventos/{ev.Id}/cancelar", new StatusChangeRequest("Chuva forte"));
        Assert.Contains((await otherApi.GetAsync<PagedResult<NotificationDto>>("/api/notificacoes")).Items, n => n.Type == "event.cancelled");
    }

    [Fact]
    public async Task Notificacoes_sao_individuais_com_leitura_preferencias_e_registro_de_entrega()
    {
        var club = await fixture.NewClubAsync();
        var (member, api) = await club.NewMemberWithLoginAsync();
        var (_, otherApi) = await club.NewMemberWithLoginAsync();
        await club.Admin.PostAsync<MemberDto>($"/api/associados/{member.Id}/suspender", new StatusChangeRequest("Teste de notificação"));

        var mine = await api.GetAsync<PagedResult<NotificationDto>>("/api/notificacoes?unreadOnly=true");
        var notification = Assert.Single(mine.Items, n => n.Type == "member.status");
        Assert.True(notification.IsMandatory);
        Assert.DoesNotContain((await otherApi.GetAsync<PagedResult<NotificationDto>>("/api/notificacoes")).Items, n => n.Id == notification.Id);
        await otherApi.OkAsync(HttpMethod.Post, $"/api/notificacoes/{notification.Id}/lida"); // marcar a notificação de outro não tem efeito
        Assert.Contains((await api.GetAsync<PagedResult<NotificationDto>>("/api/notificacoes?unreadOnly=true")).Items, n => n.Id == notification.Id);
        await api.OkAsync(HttpMethod.Post, $"/api/notificacoes/{notification.Id}/lida");
        Assert.DoesNotContain((await api.GetAsync<PagedResult<NotificationDto>>("/api/notificacoes?unreadOnly=true")).Items, n => n.Id == notification.Id);

        // Preferência desativa avisos opcionais, mas notificações obrigatórias continuam chegando.
        await api.PutAsync<List<NotificationPreferenceDto>>("/api/notificacoes/preferencias", new[] { new NotificationPreferenceDto(NotificationCategory.General, false, false, false) });
        await club.Admin.PostAsync<MemberDto>($"/api/associados/{member.Id}/reativar", new StatusChangeRequest("Regularizado"));
        Assert.Equal(2, (await api.GetAsync<PagedResult<NotificationDto>>("/api/notificacoes")).Items.Count(n => n.Type == "member.status"));

        // O despachante registra o resultado de cada canal (aqui, sem provedor de push configurado).
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        await Infrastructure.Jobs.NotificationDispatcher.DispatchAsync(scope.ServiceProvider, CancellationToken.None);
        var deliveries = await club.Admin.GetAsync<PagedResult<NotificationDeliveryDto>>("/api/notificacoes/entregas");
        Assert.Contains(deliveries.Items, d => d.PushStatus == DeliveryStatus.NoDestination);
        Assert.DoesNotContain(deliveries.Items, d => d.PushStatus == DeliveryStatus.Pending);
    }
}

[Collection("api")]
public class OperationsTests(ApiFixture fixture)
{
    [Fact]
    public async Task Vistoria_com_dano_gera_ocorrencia_e_cobranca_so_apos_aprovacao()
    {
        var club = await fixture.NewClubAsync(s => s.ReservationApprovalMode = ApprovalMode.Automatic);
        var member = await club.NewMemberAsync();
        var kiosk = await club.SpaceAsync("Quiosque Fechado 1");
        var reservation = await club.Admin.PostAsync<ReservationDetailDto>("/api/reservas",
            new CreateReservationRequest { SpaceId = kiosk.Id, Date = ClubScope.Next(DayOfWeek.Saturday), StartMinutes = [780], PeopleCount = 10, MemberId = member.Id });

        var inspection = await club.Admin.PostAsync<InspectionDto>("/api/vistorias", new CreateInspectionRequest(kiosk.Id, reservation.Reservation.Id, InspectionMoment.After, null,
            [new InspectionItemDto(InspectionCategory.Cleaning, "Limpeza geral", true, false, null), new InspectionItemDto(InspectionCategory.Furniture, "Mesa de madeira", false, true, "Tampo quebrado")], null));
        Assert.True(inspection.HasDamage);
        Assert.Equal(member.Id, inspection.MemberId);
        var occurrence = await club.Admin.GetAsync<OccurrenceDto>($"/api/ocorrencias/{inspection.OccurrenceId}");
        Assert.Equal((OccurrenceKind.Damage, reservation.Reservation.Id, member.Id), (occurrence.Kind, occurrence.ReservationId, occurrence.MemberId));

        // A cobrança proposta ainda não existe no financeiro.
        await club.Admin.PostAsync<OccurrenceDto>($"/api/ocorrencias/{occurrence.Id}/cobranca", new ProposeChargeRequest(350, "Reposição do tampo da mesa", null));
        Assert.Equal(0, (await club.Admin.GetAsync<PagedResult<ChargeDto>>($"/api/financeiro/cobrancas?memberId={member.Id}&type=Damage")).Total);
        var secretary = await club.NewStaffAsync("Secretaria"); // registra ocorrências, mas não aprova cobranças
        await secretary.FailAsync(HttpMethod.Post, $"/api/ocorrencias/{occurrence.Id}/cobranca/decisao", HttpStatusCode.Forbidden, new DecideChargeRequest(true, "ok"));

        var decided = await club.Admin.PostAsync<OccurrenceDto>($"/api/ocorrencias/{occurrence.Id}/cobranca/decisao", new DecideChargeRequest(true, "Dano comprovado por fotos"));
        Assert.Equal(ChargeDecision.Approved, decided.ChargeDecision);
        var charge = await club.Admin.GetAsync<ChargeDetailDto>($"/api/financeiro/cobrancas/{decided.ChargeId}");
        Assert.Equal((ChargeType.Damage, 350m, reservation.Reservation.Id), (charge.Charge.Type, charge.Charge.Total, charge.Charge.ReservationId));
    }

    [Fact]
    public async Task Manutencao_bloqueia_o_espaco_e_libera_ao_concluir()
    {
        var club = await fixture.NewClubAsync();
        var member = await club.NewMemberAsync();
        var court = await club.SpaceAsync("Quadra de Vôlei");
        var date = ClubScope.Next(DayOfWeek.Wednesday);
        var order = await club.Admin.PostAsync<MaintenanceDto>("/api/manutencoes", new SaveMaintenanceRequest("Troca da rede", null, MaintenanceKind.Corrective, court.Id, null, null, "Zelador", date, 200));
        Assert.Equal(MaintenanceStatus.Requested, order.Status);

        var probe = await club.Admin.GetAsync<DayAvailabilityDto>($"/api/espacos/{court.Id}/disponibilidade?date={date:yyyy-MM-dd}");
        await club.Admin.PostAsync<BlockResult>($"/api/manutencoes/{order.Id}/bloqueio", new MaintenanceBlockRequest(probe.Slots[0].StartAt, probe.Slots[^1].EndAt));
        var request = new CreateReservationRequest { SpaceId = court.Id, Date = date, StartMinutes = [600], PeopleCount = 4, MemberId = member.Id };
        await club.Admin.FailAsync(HttpMethod.Post, "/api/reservas", HttpStatusCode.Conflict, request, "espaco_bloqueado");

        var done = await club.Admin.PostAsync<MaintenanceDto>($"/api/manutencoes/{order.Id}/situacao", new MaintenanceStatusRequest(MaintenanceStatus.Completed, "Rede trocada", 180, true));
        Assert.Equal((MaintenanceStatus.Completed, 180m, false), (done.Status, done.ActualCost, done.HasActiveBlock));
        await club.Admin.PostAsync<ReservationDetailDto>("/api/reservas", request); // bloqueio encerrado junto com a manutenção
        var expenses = await club.Admin.GetAsync<PagedResult<FinancialEntryDto>>("/api/financeiro/lancamentos?kind=Expense");
        Assert.Contains(expenses.Items, e => e.MaintenanceOrderId == order.Id && e.Amount == 180);
        Assert.Equal(["Completed", "Requested"], (await club.Admin.GetAsync<List<HistoryDto>>($"/api/historico/MaintenanceOrder/{order.Id}")).Select(h => h.ToStatus));
    }

    [Fact]
    public async Task Estoque_rastreia_movimentacoes_e_nao_permite_saldo_negativo()
    {
        var club = await fixture.NewClubAsync();
        var item = await club.Admin.PostAsync<StockItemDto>("/api/estoque/produtos", new SaveStockItemRequest("Cloro granulado", "Piscina", "kg", 10, null));
        await club.Admin.PostAsync<StockMovementDto>("/api/estoque/movimentacoes", new MoveStockRequest(item.Id, StockMovementKind.In, 25, 12.5m, "Compra mensal", null));
        await club.Admin.FailAsync(HttpMethod.Post, "/api/estoque/movimentacoes", (HttpStatusCode)422, new MoveStockRequest(item.Id, StockMovementKind.Out, 40, null, "Uso", null), "saldo_insuficiente");
        await club.Admin.FailAsync(HttpMethod.Post, "/api/estoque/movimentacoes", HttpStatusCode.BadRequest, new MoveStockRequest(item.Id, StockMovementKind.Out, 5, null, null, null), "motivo_obrigatorio");
        var movement = await club.Admin.PostAsync<StockMovementDto>("/api/estoque/movimentacoes", new MoveStockRequest(item.Id, StockMovementKind.Out, 18, null, "Tratamento da piscina", null));
        Assert.Equal(7m, movement.BalanceAfter);

        var low = await club.Admin.GetAsync<PagedResult<StockItemDto>>("/api/estoque/produtos?lowOnly=true");
        Assert.Single(low.Items, i => i.Id == item.Id && i.IsLow);
        Assert.Contains((await club.Admin.GetAsync<PagedResult<NotificationDto>>("/api/notificacoes")).Items, n => n.Type == "stock.low");
        Assert.Equal(2, (await club.Admin.GetAsync<PagedResult<StockMovementDto>>($"/api/estoque/movimentacoes?itemId={item.Id}")).Total);
    }

    [Fact]
    public async Task Entrega_e_devolucao_de_chaves_vinculadas_a_reserva()
    {
        var club = await fixture.NewClubAsync(s => s.ReservationApprovalMode = ApprovalMode.Automatic);
        var member = await club.NewMemberAsync();
        var kiosk = await club.SpaceAsync("Quiosque Aberto 3");
        var reservation = await club.Admin.PostAsync<ReservationDetailDto>("/api/reservas",
            new CreateReservationRequest { SpaceId = kiosk.Id, Date = ClubScope.Next(DayOfWeek.Sunday), StartMinutes = [720], PeopleCount = 8, MemberId = member.Id });
        var delivered = await club.Admin.PostAsync<KeyHandoverDto>("/api/chaves/entrega", new DeliverKeyRequest(reservation.Reservation.Id, member.FullName, "Tudo em ordem"));
        Assert.Equal(reservation.Reservation.EndAt, delivered.ExpectedReturnAt);
        await club.Admin.FailAsync(HttpMethod.Post, "/api/chaves/entrega", HttpStatusCode.Conflict, new DeliverKeyRequest(reservation.Reservation.Id, member.FullName, null));

        var returned = await club.Admin.PostAsync<KeyHandoverDto>($"/api/chaves/{delivered.Id}/devolucao", new ReturnKeyRequest("Grelha suja", "Limpar a grelha"));
        Assert.NotNull(returned.ReturnedAt);
        Assert.Single((await club.Admin.GetAsync<PagedResult<KeyHandoverDto>>("/api/chaves?outstandingOnly=true")).Items);
        await club.Admin.PostAsync<KeyHandoverDto>($"/api/chaves/{delivered.Id}/pendencias-resolvidas");
        Assert.Empty((await club.Admin.GetAsync<PagedResult<KeyHandoverDto>>("/api/chaves?outstandingOnly=true")).Items);
    }

    [Fact]
    public async Task Configuracoes_sao_validadas_e_alteracoes_ficam_no_historico()
    {
        var club = await fixture.NewClubAsync();
        var settings = await club.Admin.GetAsync<SettingsDto>("/api/configuracoes");
        Assert.Null(settings.MonthlyBaseFee); // nada é presumido: políticas começam indefinidas
        Assert.Contains(settings.Pending, p => p.Contains("dia de vencimento"));
        Assert.Equal((5, 3), (settings.MaxDependentsPerMember, settings.MaxGuestsPerMember));

        settings.DueDay = 31;
        await club.Admin.FailAsync(HttpMethod.Put, "/api/configuracoes", HttpStatusCode.BadRequest, new { settings, justification = "x" }, "validacao");
        settings.DueDay = 10;
        settings.DependentPricingMode = DependentPricingMode.Tiers;
        settings.DependentPricingRules = [new PricingRuleDto(1, 2, 80)];
        var error = await club.Admin.FailAsync(HttpMethod.Put, "/api/configuracoes", HttpStatusCode.BadRequest, new { settings, justification = "x" }, "configuracao_invalida");
        Assert.Contains("5 dependentes", error.Message);

        settings.DependentPricingRules = [new PricingRuleDto(1, 2, 80), new PricingRuleDto(3, 5, 150)];
        var saved = await club.Admin.PutAsync<SettingsDto>("/api/configuracoes", new { settings, justification = "Definição da assembleia" });
        Assert.DoesNotContain(saved.Pending, p => p.Contains("dia de vencimento"));
        var logs = await club.Admin.GetAsync<PagedResult<Application.Admin.AuditLogDto>>("/api/auditoria?entityType=ClubSettings&operation=Alteração");
        Assert.Contains(logs.Items, l => l.Justification == "Definição da assembleia" && l.AfterJson!.Contains("\"DueDay\": 10") && l.AfterJson.Contains("Tiers"));
    }

    [Fact]
    public async Task Relatorios_e_exportacoes_refletem_dados_reais()
    {
        var club = await fixture.NewClubAsync(s => { s.MonthlyBaseFee = 200; s.DueDay = 10; s.DependentPricingMode = DependentPricingMode.None; });
        var member = await club.NewMemberAsync();
        await club.NewMemberAsync(approve: false);
        await club.NewDependentAsync(member.Id);
        var competence = ClubScope.Today.ToString("yyyy-MM");
        await club.Admin.PostAsync<GenerateMonthlyResult>("/api/mensalidades/gerar", new GenerateMonthlyRequest(competence, null));
        var charge = (await club.Admin.GetAsync<PagedResult<ChargeDto>>($"/api/mensalidades?competence={competence}")).Items.Single();
        await club.Admin.PostAsync<PaymentDto>("/api/pagamentos", new RegisterPaymentRequest(charge.Id, 200, PaymentMethod.Pix, null, null, null, Guid.NewGuid().ToString()));

        var dashboard = await club.Admin.GetAsync<Application.Admin.DashboardDto>("/api/relatorios/dashboard");
        Assert.Equal((2, 1, 1, 1), (dashboard.Members, dashboard.ActiveMembers, dashboard.PendingMembers, dashboard.Dependents));
        Assert.Equal(200m, dashboard.Finance!.ReceivedThisMonth);

        var from = new DateOnly(ClubScope.Today.Year, ClubScope.Today.Month, 1);
        var range = $"from={from:yyyy-MM-dd}&to={from.AddMonths(1).AddDays(-1):yyyy-MM-dd}";
        var indicators = await club.Admin.GetAsync<Application.Admin.IndicatorsDto>($"/api/relatorios/indicadores?{range}");
        Assert.Equal((200m, 200m), (indicators.Finance!.Revenue, indicators.Finance.MonthlyReceived));
        Assert.Equal(2, indicators.Members.NewInPeriod);

        var pdf = await club.Admin.SendAsync(HttpMethod.Get, $"/api/relatorios/indicadores/pdf?{range}");
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType!.MediaType);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString((await pdf.Content.ReadAsByteArrayAsync())[..4]));
        var excel = await club.Admin.SendAsync(HttpMethod.Get, $"/api/relatorios/indicadores/excel?{range}");
        Assert.True(excel.IsSuccessStatusCode && (await excel.Content.ReadAsByteArrayAsync()).Length > 2000);
        var receipt = await club.Admin.SendAsync(HttpMethod.Get, $"/api/pagamentos/{(await club.Admin.GetAsync<PagedResult<PaymentDto>>("/api/pagamentos")).Items.Single().Id}/comprovante");
        Assert.Equal("application/pdf", receipt.Content.Headers.ContentType!.MediaType);

        // Quem não tem acesso ao financeiro não recebe indicadores financeiros.
        var secretary = await club.NewStaffAsync("Secretaria");
        Assert.NotNull((await secretary.GetAsync<Application.Admin.DashboardDto>("/api/relatorios/dashboard")).Finance); // secretaria consulta o financeiro
        var gate = await club.NewStaffAsync("Portaria");
        await gate.FailAsync(HttpMethod.Get, "/api/relatorios/dashboard", HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Arquivos_exigem_autenticacao_e_respeitam_escopo_e_clube()
    {
        var a = await fixture.NewClubAsync();
        var b = await fixture.NewClubAsync();
        var (member, memberApi) = await a.NewMemberWithLoginAsync();
        var (_, strangerApi) = await a.NewMemberWithLoginAsync();

        async Task<FileDto> Upload(Api api, string scope, Guid? owner = null)
        {
            var form = new MultipartFormDataContent();
            var content = new ByteArrayContent([0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34]);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
            form.Add(content, "File", "documento.pdf");
            form.Add(new StringContent(scope), "Scope");
            if (owner is not null) form.Add(new StringContent(owner.Value.ToString()), "OwnerMemberId");
            var response = await api.Http.PostAsync("/api/arquivos", form);
            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
            return System.Text.Json.JsonSerializer.Deserialize<FileDto>(await response.Content.ReadAsStringAsync(), ApiFixture.Json)!;
        }

        var document = await Upload(a.Admin, "Restricted", member.Id);
        await fixture.Anonymous().FailAsync(HttpMethod.Get, $"/api/arquivos/{document.Id}", HttpStatusCode.Unauthorized);
        Assert.True((await memberApi.SendAsync(HttpMethod.Get, $"/api/arquivos/{document.Id}")).IsSuccessStatusCode); // o titular acessa o próprio documento
        await strangerApi.FailAsync(HttpMethod.Get, $"/api/arquivos/{document.Id}", HttpStatusCode.NotFound);
        await b.Admin.FailAsync(HttpMethod.Get, $"/api/arquivos/{document.Id}", HttpStatusCode.NotFound); // outro clube
        var gate = await a.NewStaffAsync("Portaria"); // equipe sem permissão de documentos
        await gate.FailAsync(HttpMethod.Get, $"/api/arquivos/{document.Id}", HttpStatusCode.NotFound);

        var form = new MultipartFormDataContent();
        var exe = new ByteArrayContent([0x4D, 0x5A]);
        exe.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        form.Add(exe, "File", "programa.exe");
        Assert.Equal(HttpStatusCode.BadRequest, (await a.Admin.Http.PostAsync("/api/arquivos", form)).StatusCode);
    }
}
