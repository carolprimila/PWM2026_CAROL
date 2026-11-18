using System.ComponentModel.DataAnnotations;
using Clubes.Application.Clubs;
using Clubes.Application.Common;
using Clubes.Application.Finance;
using Clubes.Application.People;
using Clubes.Domain;
using Microsoft.EntityFrameworkCore;

namespace Clubes.Application.Spaces;

public record ReservationSlotDto(DateTime StartAt, DateTime EndAt);
public record ReservationDto(Guid Id, string Code, Guid SpaceId, string SpaceName, SpaceType SpaceType, ReservationKind Kind, string? Title, Guid? MemberId,
    string? MemberName, string RequestedBy, bool RequestedByDependent, DateTime StartAt, DateTime EndAt, IReadOnlyList<ReservationSlotDto> Slots, int PeopleCount,
    ReservationStatus Status, decimal RentalAmount, decimal CleaningFee, decimal TotalAmount, SpaceChargeMode? ChargeMode, Guid? ChargeId, string? Notes,
    string? DecisionReason, string? CancellationReason, AttendanceStatus Attendance, string? AttendanceNote, Guid? PenaltyChargeId, int Guests, DateTime CreatedAt);
public record ReservationDetailDto(ReservationDto Reservation, IReadOnlyList<GuestDto> Guests, IReadOnlyList<HistoryDto> History);
public record QuoteDto(decimal RentalAmount, decimal CleaningFee, decimal TotalAmount, SpaceChargeMode? ChargeMode, bool RequiresApproval, DateTime StartAt, DateTime EndAt,
    IReadOnlyList<string> Notes);

public class CreateReservationRequest
{
    public Guid SpaceId { get; set; }
    public DateOnly Date { get; set; }
    /// <summary>Início de cada intervalo escolhido, em minutos desde 00:00 (espaços por hora). Ignorado para reservas por período.</summary>
    public List<int> StartMinutes { get; set; } = [];
    [Range(1, 100_000)] public int PeopleCount { get; set; }
    public List<GuestInput> Guests { get; set; } = [];
    [StringLength(1000)] public string? Notes { get; set; }
    /// <summary>Somente equipe: associado em nome de quem a reserva é feita.</summary>
    public Guid? MemberId { get; set; }
    /// <summary>Somente equipe: reserva administrativa ou evento interno.</summary>
    public ReservationKind Kind { get; set; } = ReservationKind.Member;
    [StringLength(150)] public string? Title { get; set; }
}

public record RescheduleRequest(DateOnly Date, List<int> StartMinutes, [StringLength(500)] string? Reason);
public record DecisionRequest([StringLength(500)] string? Reason);
public record AttendanceRequest(AttendanceStatus Attendance, [StringLength(500)] string? Note);

public class ReservationQuery : PageQuery
{
    public ReservationStatus? Status { get; set; }
    public Guid? SpaceId { get; set; }
    public Guid? MemberId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public AttendanceStatus? Attendance { get; set; }
}

public class ReservationService(IAppDb db, RequestContext ctx, SpaceService spaces, SettingsService settings, ClubClock clock, DelinquencyService delinquency,
    DependentService dependents, GuestService guests, ChargeService charges, NotificationService notifications, HistoryService history, IRealtimeNotifier realtime)
{
    public const string OverlapConstraint = "EX_ReservationSlot_NoOverlap";
    private static readonly ReservationStatus[] Live = [ReservationStatus.Pending, ReservationStatus.Approved, ReservationStatus.Suspended];

    // ---------- Consulta ----------

    public async Task<PagedResult<ReservationDto>> ListAsync(ReservationQuery query, CancellationToken ct = default)
    {
        var q = db.Set<Reservation>().AsNoTracking();
        if (!ctx.IsStaff)
        {
            var mine = await ResponsibleMemberAsync(ct);
            q = ctx.DependentId is null ? q.Where(r => r.MemberId == mine) : q.Where(r => r.RequestedByUserId == ctx.UserId);
        }
        else if (query.MemberId is not null) q = q.Where(r => r.MemberId == query.MemberId);
        if (query.Status is not null) q = q.Where(r => r.Status == query.Status);
        if (query.SpaceId is not null) q = q.Where(r => r.SpaceId == query.SpaceId);
        if (query.Attendance is not null) q = q.Where(r => r.Attendance == query.Attendance);
        if (query.From is not null) q = q.Where(r => r.EndAt >= query.From.Value.ToUniversalTime());
        if (query.To is not null) q = q.Where(r => r.StartAt <= query.To.Value.ToUniversalTime());
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var like = query.Search.Like();
            q = q.Where(r => EF.Functions.ILike(r.Code, like) || EF.Functions.ILike(r.Member!.FullName, like) || EF.Functions.ILike(r.Space.Name, like) || EF.Functions.ILike(r.Title!, like));
        }
        var total = await q.CountAsync(ct);
        q = query.Sort == "created" ? q.OrderByDescending(r => r.CreatedAt) : query.Desc ? q.OrderByDescending(r => r.StartAt) : q.OrderBy(r => r.StartAt);
        var items = await q.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Include(r => r.Space).Include(r => r.Member).Include(r => r.Slots).AsSplitQuery().ToListAsync(ct);
        return new PagedResult<ReservationDto>(await MapAsync(items, ct), total, query.Page, query.PageSize);
    }

    public async Task<ReservationDetailDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var reservation = await LoadAsync(id, tracking: false, ct);
        return new ReservationDetailDto((await MapAsync([reservation], ct))[0], await guests.ListByReservationAsync(id, ct),
            await history.ListAsync(nameof(Reservation), id, ct));
    }

    private async Task<Reservation> LoadAsync(Guid id, bool tracking, CancellationToken ct)
    {
        var q = db.Set<Reservation>().Include(r => r.Space).Include(r => r.Member).Include(r => r.Slots).AsSplitQuery().Where(r => r.Id == id);
        var reservation = await (tracking ? q : q.AsNoTracking()).FirstOr404Async("Reserva", ct);
        if (!ctx.IsStaff)
        {
            var allowed = ctx.DependentId is null ? reservation.MemberId == ctx.MemberId : reservation.RequestedByUserId == ctx.UserId;
            if (!allowed) throw DomainException.NotFound("Reserva");
        }
        return reservation;
    }

    private async Task<Guid> ResponsibleMemberAsync(CancellationToken ct)
    {
        if (ctx.MemberId is { } memberId) return memberId;
        if (ctx.DependentId is { } dependentId) return await db.Set<Dependent>().Where(d => d.Id == dependentId).Select(d => d.MemberId).FirstAsync(ct);
        throw DomainException.Forbidden();
    }

    // ---------- Solicitação ----------

    private record Plan(Space Space, List<(DateTime Start, DateTime End)> Ranges, decimal Rental, decimal Cleaning, SpaceChargeMode? ChargeMode, bool AutoApprove, List<string> Notes);

    /// <summary>Valida horários, bloqueios, funcionamento e capacidade, e calcula valores. Não grava nada.</summary>
    private async Task<Plan> PlanAsync(Guid spaceId, DateOnly date, List<int> startMinutes, int peopleCount, int guestCount, bool enforceMemberLimits, CancellationToken ct)
    {
        var space = await db.Set<Space>().Include(s => s.Schedules).Where(s => s.Id == spaceId && s.IsActive).FirstOr404Async("Espaço", ct);
        if (!space.IsReservable) throw Invalid("Este espaço não pode ser reservado.");
        var s = await settings.LoadAsync(ct);
        var today = await clock.TodayAsync(ct);
        if (date < today) throw Invalid("Não é possível reservar datas passadas.");
        if (enforceMemberLimits && s.MaxAdvanceDays is { } maxDays && date > today.AddDays(maxDays))
            throw new DomainException($"As reservas podem ser feitas com até {maxDays} dias de antecedência.", "antecedencia_excedida");
        if (space.Capacity is { } capacity && peopleCount > capacity)
            throw new DomainException($"A capacidade máxima de {space.Name} é de {capacity} pessoas (incluindo associados, dependentes e convidados).", "capacidade_excedida");
        if (guestCount > s.MaxGuestsPerMember) throw new DomainException($"Cada associado pode levar até {s.MaxGuestsPerMember} convidados.", "limite_convidados");
        if (guestCount + 1 > peopleCount) throw Invalid("A quantidade de pessoas deve incluir o responsável e todos os convidados.");

        var ranges = new List<(DateTime Start, DateTime End)>();
        decimal rental;
        var notes = new List<string>();
        if (space.BookingMode == BookingMode.MultiDay)
        {
            if (date <= today) throw Invalid("Reservas por período devem começar em data futura.");
            var start = await clock.ToUtcAsync(date, 0, ct);
            var end = await clock.ToUtcAsync(date.AddDays(space.FixedDays), 0, ct);
            var closure = await db.Set<Holiday>().Where(h => h.IsClosed && h.Date >= date && h.Date < date.AddDays(space.FixedDays)).Select(h => h.Name).FirstOrDefaultAsync(ct);
            if (closure is not null) throw new DomainException($"O clube estará fechado no período ({closure}).", "espaco_bloqueado");
            ranges.Add((start, end));
            rental = space.PeriodPrice ?? 0;
            notes.Add($"Locação por {space.FixedDays} dias: montagem, preparação, evento, retirada da decoração e limpeza.");
            if (space.PeriodPrice is null) notes.Add("O valor da locação ainda não foi definido pela administração.");
        }
        else
        {
            var minutes = startMinutes.Distinct().Order().ToList();
            if (minutes.Count == 0) throw Invalid("Selecione ao menos um horário.");
            var (open, close, closedReason) = await spaces.WindowAsync(space, date, ct);
            if (closedReason is not null) throw new DomainException($"{space.Name} não funciona nesta data. {closedReason}.", "espaco_fechado");
            if (minutes.Any(m => m < open || m + space.SlotMinutes > close || (m - open) % space.SlotMinutes != 0))
                throw Invalid("Horário fora do funcionamento do espaço.");

            // Intervalos consecutivos são unidos em uma única faixa.
            var rangeStart = minutes[0];
            for (var i = 0; i < minutes.Count; i++)
            {
                var isLast = i == minutes.Count - 1 || minutes[i + 1] != minutes[i] + space.SlotMinutes;
                if (!isLast) continue;
                ranges.Add((await clock.ToUtcAsync(date, rangeStart, ct), await clock.ToUtcAsync(date, minutes[i] + space.SlotMinutes, ct)));
                if (i < minutes.Count - 1) rangeStart = minutes[i + 1];
            }
            if (ranges[0].Start < DateTime.UtcNow) throw Invalid("O horário selecionado já passou.");
            rental = (space.HourlyPrice ?? 0) * minutes.Count * space.SlotMinutes / 60m;
        }

        foreach (var (start, end) in ranges)
        {
            if (await db.Set<SpaceBlock>().AnyAsync(b => !b.IsCancelled && (b.SpaceId == spaceId || b.SpaceId == null) && b.StartAt < end && b.EndAt > start, ct))
                throw new DomainException("O espaço está bloqueado em parte do período selecionado.", "espaco_bloqueado", 409);
        }

        var cleaning = space.CleaningFee ?? 0;
        if (cleaning > 0) notes.Add("A limpeza é realizada pelo clube e a taxa de limpeza está incluída no valor total.");
        SpaceChargeMode? chargeMode = space.ChargeMode ?? s.RequireAdvancePayment switch { true => SpaceChargeMode.Advance, false => SpaceChargeMode.NextMonthlyBill, _ => null };
        if (rental + cleaning > 0)
            notes.Add(chargeMode switch
            {
                SpaceChargeMode.Advance => "Pagamento antecipado: a cobrança é gerada na aprovação da reserva.",
                SpaceChargeMode.NextMonthlyBill => "O valor será cobrado junto ao próximo vencimento da mensalidade.",
                _ => "A forma de cobrança ainda não foi definida pela administração.",
            });
        var autoApprove = (space.ApprovalMode ?? s.ReservationApprovalMode) == ApprovalMode.Automatic;
        return new Plan(space, ranges, decimal.Round(rental, 2), cleaning, chargeMode, autoApprove, notes);
    }

    public async Task<QuoteDto> QuoteAsync(CreateReservationRequest req, CancellationToken ct = default)
    {
        var plan = await PlanAsync(req.SpaceId, req.Date, req.StartMinutes, req.PeopleCount, req.Guests.Count, !ctx.IsStaff, ct);
        var manual = !plan.AutoApprove || ctx.DependentId is not null;
        return new QuoteDto(plan.Rental, plan.Cleaning, plan.Rental + plan.Cleaning, plan.ChargeMode, manual && !ctx.IsStaff,
            plan.Ranges.Min(r => r.Start), plan.Ranges.Max(r => r.End), plan.Notes);
    }

    public async Task<ReservationDetailDto> CreateAsync(CreateReservationRequest req, CancellationToken ct = default)
    {
        var userId = ctx.RequireUser();
        Guid? memberId;
        var kind = ReservationKind.Member;
        if (ctx.IsStaff)
        {
            kind = req.Kind;
            memberId = kind == ReservationKind.Member ? req.MemberId ?? throw Invalid("Informe o associado responsável pela reserva.") : null;
            if (kind != ReservationKind.Member && string.IsNullOrWhiteSpace(req.Title)) throw Invalid("Informe um título para a reserva administrativa.");
            if (kind != ReservationKind.Member && req.Guests.Count > 0) throw Invalid("Reservas administrativas não possuem convidados.");
        }
        else
        {
            // Dependente só solicita se a permissão individual estiver concedida — validado aqui, no servidor.
            await dependents.EnsureDependentCanAsync(DependentPermissions.RequestReservations, ct);
            memberId = await ResponsibleMemberAsync(ct);
        }

        var s = await settings.LoadAsync(ct);
        if (memberId is { } owner)
        {
            var status = await db.Set<Member>().Where(m => m.Id == owner).Select(m => (MemberStatus?)m.Status).FirstOrDefaultAsync(ct) ?? throw DomainException.NotFound("Associado");
            if (status != MemberStatus.Active) throw DomainException.Conflict("O cadastro do associado não está ativo.");
            await delinquency.EnsureCanReserveAsync(owner, ct);
        }

        var plan = await PlanAsync(req.SpaceId, req.Date, req.StartMinutes, req.PeopleCount, req.Guests.Count, !ctx.IsStaff, ct);
        var approveNow = ctx.IsStaff || (plan.AutoApprove && ctx.DependentId is null);

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (memberId is { } m && !ctx.IsStaff && s.MaxConcurrentReservations is { } max)
        {
            await db.Database.ExecuteSqlAsync($"""SELECT 1 FROM "Member" WHERE "Id" = {m} FOR UPDATE""", ct);
            var open = await db.Set<Reservation>().CountAsync(r => r.MemberId == m && Live.Contains(r.Status) && r.EndAt > DateTime.UtcNow, ct);
            if (open >= max) throw new DomainException($"Limite de {max} reservas simultâneas atingido.", "limite_reservas");
        }

        var reservation = new Reservation
        {
            Code = $"R-{await db.NextSequenceAsync("reservation", ct):D6}", SpaceId = plan.Space.Id, Kind = kind, Title = req.Title, MemberId = memberId,
            RequestedByUserId = userId, RequestedByDependentId = ctx.DependentId, StartAt = plan.Ranges.Min(r => r.Start), EndAt = plan.Ranges.Max(r => r.End),
            PeopleCount = req.PeopleCount, Notes = req.Notes, RentalAmount = kind == ReservationKind.Member ? plan.Rental : 0,
            CleaningFee = kind == ReservationKind.Member ? plan.Cleaning : 0, ChargeMode = plan.ChargeMode, Status = ReservationStatus.Pending,
        };
        reservation.TotalAmount = reservation.RentalAmount + reservation.CleaningFee;
        reservation.Slots.AddRange(plan.Ranges.Select(r => new ReservationSlot { SpaceId = plan.Space.Id, StartAt = r.Start, EndAt = r.End }));
        db.Set<Reservation>().Add(reservation);
        db.AddHistory(ctx, nameof(Reservation), reservation.Id, null, ReservationStatus.Pending, "Solicitação registrada");

        if (memberId is { } guestOwner)
            foreach (var guest in req.Guests)
                await guests.AddAsync(guestOwner, guest, req.Date, plan.Space.Id, reservation.Id, ct);

        if (approveNow)
        {
            // A reserva é gravada antes da cobrança: as duas se referenciam e não podem ser inseridas no mesmo passo.
            await SaveSlotsAsync(ct);
            await ApproveCoreAsync(reservation, plan.Space, ctx.IsStaff ? "Reserva criada pela administração" : "Aprovação automática", s, ct);
        }
        else
        {
            if (memberId is { } notify)
                await notifications.ToMemberAsync(notify, NotificationCategory.Reservation, "reservation.received", "Solicitação de reserva recebida",
                    $"Sua solicitação {reservation.Code} para {plan.Space.Name} foi recebida e aguarda aprovação.", nameof(Reservation), reservation.Id, false, ct);
            if (ctx.DependentId is { } dep)
                await notifications.ToDependentAsync(dep, NotificationCategory.Dependent, "reservation.received", "Solicitação de reserva enviada",
                    $"Sua solicitação {reservation.Code} para {plan.Space.Name} aguarda aprovação da administração.", nameof(Reservation), reservation.Id, false, ct);
            await notifications.ToStaffAsync(Permissions.ReservationsManage, NotificationCategory.Reservation, "reservation.pending", "Nova solicitação de reserva",
                $"{reservation.Code} — {plan.Space.Name} aguarda análise.", nameof(Reservation), reservation.Id, ct);
        }

        await SaveSlotsAsync(ct);
        await tx.CommitAsync(ct);
        await PublishAsync(plan.Space.Id, ct);
        return await GetAsync(reservation.Id, ct);
    }

    /// <summary>Salva traduzindo a violação da restrição de exclusão do banco em erro de conflito de horário.</summary>
    private async Task SaveSlotsAsync(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when ((ex.InnerException?.Message ?? ex.Message).Contains(OverlapConstraint))
        {
            throw DomainException.Conflict("Um ou mais horários selecionados acabaram de ser reservados. Atualize a disponibilidade e escolha outro horário.");
        }
    }

    // ---------- Decisão administrativa ----------

    public async Task<ReservationDetailDto> ApproveAsync(Guid id, DecisionRequest req, CancellationToken ct = default)
    {
        var reservation = await LoadAsync(id, tracking: true, ct);
        if (reservation.Status is not (ReservationStatus.Pending or ReservationStatus.Suspended)) throw DomainException.Conflict("Somente reservas pendentes ou suspensas podem ser aprovadas.");
        if (reservation.EndAt <= DateTime.UtcNow) throw DomainException.Conflict("O período desta reserva já passou.");
        if (reservation.MemberId is { } memberId) await delinquency.EnsureCanReserveAsync(memberId, ct);
        await ApproveCoreAsync(reservation, reservation.Space, req.Reason ?? "Reserva aprovada", await settings.LoadAsync(ct), ct);
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    private async Task ApproveCoreAsync(Reservation reservation, Space space, string note, ClubSettings s, CancellationToken ct)
    {
        var from = reservation.Status;
        reservation.Status = ReservationStatus.Approved;
        reservation.DecisionByUserId = ctx.UserId;
        reservation.DecisionAt = DateTime.UtcNow;
        reservation.DecisionReason = note;
        db.AddHistory(ctx, nameof(Reservation), reservation.Id, from, ReservationStatus.Approved, note);

        if (reservation is { TotalAmount: > 0, MemberId: { } memberId, ChargeId: null })
        {
            var today = await clock.TodayAsync(ct);
            var mode = reservation.ChargeMode ?? throw DomainException.NotConfigured($"forma de cobrança das reservas de {space.Name} (antecipada ou na mensalidade seguinte)");
            DateOnly due;
            if (mode == SpaceChargeMode.Advance) due = today;
            else
            {
                var dueDay = s.DueDay ?? throw DomainException.NotConfigured("dia de vencimento das mensalidades");
                var next = today.AddMonths(1);
                due = new DateOnly(next.Year, next.Month, Math.Min(dueDay, DateTime.DaysInMonth(next.Year, next.Month)));
            }
            var description = reservation.CleaningFee > 0
                ? $"Reserva {reservation.Code} — {space.Name} (locação {reservation.RentalAmount:C} + limpeza {reservation.CleaningFee:C})"
                : $"Reserva {reservation.Code} — {space.Name}";
            var charge = charges.New(s, memberId, ChargeType.Reservation, description, reservation.TotalAmount, due);
            charge.ReservationId = reservation.Id;
            reservation.ChargeId = charge.Id;
        }
        await NotifyOwnerAsync(reservation, "reservation.approved", "Reserva aprovada",
            $"Sua reserva {reservation.Code} em {space.Name} foi aprovada." + (reservation.TotalAmount > 0 ? $" Valor total: {reservation.TotalAmount:C}." : ""), ct);
    }

    public async Task<ReservationDetailDto> RejectAsync(Guid id, DecisionRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Reason)) throw Invalid("Informe o motivo da recusa.");
        var reservation = await LoadAsync(id, tracking: true, ct);
        if (reservation.Status != ReservationStatus.Pending) throw DomainException.Conflict("Somente reservas pendentes podem ser recusadas.");
        reservation.Status = ReservationStatus.Rejected;
        reservation.DecisionByUserId = ctx.UserId;
        reservation.DecisionAt = DateTime.UtcNow;
        reservation.DecisionReason = req.Reason;
        await ReleaseAsync(reservation, "Reserva recusada", ct);
        db.AddHistory(ctx, nameof(Reservation), id, ReservationStatus.Pending, ReservationStatus.Rejected, req.Reason);
        await NotifyOwnerAsync(reservation, "reservation.rejected", "Reserva recusada", $"Sua solicitação {reservation.Code} em {reservation.Space.Name} foi recusada. Motivo: {req.Reason}", ct);
        await db.SaveChangesAsync(ct);
        await PublishAsync(reservation.SpaceId, ct);
        return await GetAsync(id, ct);
    }

    /// <summary>
    /// Cancelamento pelo solicitante ou pela administração. A multa por cancelamento tardio só é lançada
    /// quando o prazo e o valor foram configurados pela administração, e nunca em cancelamentos administrativos.
    /// </summary>
    public async Task<ReservationDetailDto> CancelAsync(Guid id, DecisionRequest req, CancellationToken ct = default)
    {
        var reservation = await LoadAsync(id, tracking: true, ct);
        if (!Live.Contains(reservation.Status)) throw DomainException.Conflict("Esta reserva não pode mais ser cancelada.");
        if (!ctx.IsStaff && reservation.StartAt <= DateTime.UtcNow) throw DomainException.Conflict("A reserva já iniciou e não pode ser cancelada pelo aplicativo.");
        if (ctx.IsStaff && string.IsNullOrWhiteSpace(req.Reason)) throw Invalid("Informe o motivo do cancelamento.");

        var s = await settings.LoadAsync(ct);
        var from = reservation.Status;
        var note = req.Reason ?? "Cancelada pelo solicitante";
        if (!ctx.IsStaff && from == ReservationStatus.Approved && reservation.MemberId is { } memberId
            && s is { FreeCancellationHours: { } hours, LateCancellationFee: > 0 } && DateTime.UtcNow > reservation.StartAt.AddHours(-hours))
        {
            var penalty = charges.New(s, memberId, ChargeType.Penalty, $"Multa por cancelamento fora do prazo — reserva {reservation.Code}", s.LateCancellationFee.Value, await clock.TodayAsync(ct));
            penalty.ReservationId = reservation.Id;
            reservation.PenaltyChargeId = penalty.Id;
            await charges.NotifyNewAsync(penalty, ct);
            note += $" (fora do prazo de {hours}h: multa de {s.LateCancellationFee:C} lançada)";
        }

        reservation.Status = ReservationStatus.Cancelled;
        reservation.CancelledAt = DateTime.UtcNow;
        reservation.CancelledByUserId = ctx.UserId;
        reservation.CancellationReason = note;
        await ReleaseAsync(reservation, "Reserva cancelada", ct);
        db.AddHistory(ctx, nameof(Reservation), id, from, ReservationStatus.Cancelled, note);
        await NotifyOwnerAsync(reservation, "reservation.cancelled", "Reserva cancelada", $"A reserva {reservation.Code} em {reservation.Space.Name} foi cancelada. {note}", ct);
        if (!ctx.IsStaff)
            await notifications.ToStaffAsync(Permissions.ReservationsManage, NotificationCategory.Reservation, "reservation.cancelled", "Reserva cancelada pelo solicitante",
                $"{reservation.Code} — {reservation.Space.Name}.", nameof(Reservation), id, ct);
        await db.SaveChangesAsync(ct);
        await PublishAsync(reservation.SpaceId, ct);
        return await GetAsync(id, ct);
    }

    /// <summary>Libera os horários, cancela convites vinculados e a cobrança ainda não paga.</summary>
    private async Task ReleaseAsync(Reservation reservation, string reason, CancellationToken ct)
    {
        foreach (var slot in reservation.Slots) slot.IsActive = false;
        var linked = await db.Set<Guest>().Where(g => g.ReservationId == reservation.Id && !g.IsCancelled && g.EnteredAt == null).ToListAsync(ct);
        foreach (var guest in linked)
        {
            guest.IsCancelled = true;
            await guests.CancelChargeAsync(guest, reason, ct);
        }
        if (reservation.ChargeId is { } chargeId)
        {
            var charge = await db.Set<Charge>().FirstOrDefaultAsync(c => c.Id == chargeId, ct);
            if (charge is { Status: ChargeStatus.Open, PaidAmount: 0 })
            {
                charge.Status = ChargeStatus.Cancelled;
                charge.CancelReason = reason;
            }
        }
    }

    public async Task<ReservationDetailDto> RescheduleAsync(Guid id, RescheduleRequest req, CancellationToken ct = default)
    {
        var reservation = await LoadAsync(id, tracking: true, ct);
        if (reservation.Status is not (ReservationStatus.Pending or ReservationStatus.Approved)) throw DomainException.Conflict("Esta reserva não pode ser alterada.");
        var guestCount = await db.Set<Guest>().CountAsync(g => g.ReservationId == id && !g.IsCancelled, ct);

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // Remove os intervalos atuais antes de validar os novos, para permitir sobreposição com a própria reserva.
        // (O horário anterior fica registrado no histórico da reserva.)
        var previous = await clock.ToLocalAsync(reservation.StartAt, ct);
        db.Set<ReservationSlot>().RemoveRange(reservation.Slots);
        reservation.Slots.Clear();
        await db.SaveChangesAsync(ct);

        var plan = await PlanAsync(reservation.SpaceId, req.Date, req.StartMinutes, reservation.PeopleCount, guestCount, false, ct);
        reservation.StartAt = plan.Ranges.Min(r => r.Start);
        reservation.EndAt = plan.Ranges.Max(r => r.End);
        foreach (var (start, end) in plan.Ranges)
            db.Set<ReservationSlot>().Add(new ReservationSlot { ReservationId = id, SpaceId = reservation.SpaceId, StartAt = start, EndAt = end });
        await db.Set<Guest>().Where(g => g.ReservationId == id && !g.IsCancelled && g.EnteredAt == null).ExecuteUpdateAsync(u => u.SetProperty(g => g.VisitDate, req.Date), ct);

        var note = $"Horário alterado pela administração (antes: {previous:dd/MM/yyyy HH:mm})" + (string.IsNullOrWhiteSpace(req.Reason) ? "" : $": {req.Reason}");
        db.AddHistory(ctx, nameof(Reservation), id, reservation.Status, reservation.Status, note);
        var local = await clock.ToLocalAsync(reservation.StartAt, ct);
        await NotifyOwnerAsync(reservation, "reservation.rescheduled", "Alteração de horário",
            $"A reserva {reservation.Code} em {reservation.Space.Name} foi alterada para {local:dd/MM/yyyy 'às' HH:mm}.", ct);
        await SaveSlotsAsync(ct);
        await tx.CommitAsync(ct);
        await PublishAsync(reservation.SpaceId, ct);
        return await GetAsync(id, ct);
    }

    /// <summary>
    /// Registro de comparecimento. Em caso de falta, a penalidade só é lançada se a administração
    /// configurou o valor; o registro da ocorrência fica no histórico da reserva.
    /// </summary>
    public async Task<ReservationDetailDto> SetAttendanceAsync(Guid id, AttendanceRequest req, CancellationToken ct = default)
    {
        var reservation = await LoadAsync(id, tracking: true, ct);
        if (reservation.Status is not (ReservationStatus.Approved or ReservationStatus.Completed)) throw DomainException.Conflict("Somente reservas aprovadas possuem controle de comparecimento.");
        if (reservation.StartAt > DateTime.UtcNow) throw DomainException.Conflict("A reserva ainda não iniciou.");
        if (req.Attendance == AttendanceStatus.NoShow && string.IsNullOrWhiteSpace(req.Note)) throw Invalid("Descreva a ocorrência de não comparecimento.");

        var s = await settings.LoadAsync(ct);
        reservation.Attendance = req.Attendance;
        reservation.AttendanceNote = req.Note;
        var note = req.Attendance switch { AttendanceStatus.NoShow => "Não comparecimento registrado", AttendanceStatus.Attended => "Comparecimento registrado", _ => "Comparecimento indefinido" };
        if (req.Attendance == AttendanceStatus.NoShow && reservation is { MemberId: { } memberId, PenaltyChargeId: null } && s.NoShowFee is > 0)
        {
            var penalty = charges.New(s, memberId, ChargeType.Penalty, $"Penalidade por não comparecimento — reserva {reservation.Code}", s.NoShowFee.Value, await clock.TodayAsync(ct));
            penalty.ReservationId = reservation.Id;
            reservation.PenaltyChargeId = penalty.Id;
            await charges.NotifyNewAsync(penalty, ct);
            note += $" — penalidade de {s.NoShowFee:C} lançada";
        }
        if (reservation.EndAt <= DateTime.UtcNow && reservation.Status == ReservationStatus.Approved) reservation.Status = ReservationStatus.Completed;
        db.AddHistory(ctx, nameof(Reservation), id, reservation.Status, reservation.Status, note + (req.Note is null ? "" : $": {req.Note}"));
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    // ---------- Rotinas automáticas (chamadas pelo serviço em segundo plano, por clube) ----------

    /// <summary>Aplica a política configurada às reservas aprovadas de associados inadimplentes e restaura as suspensas regularizadas.</summary>
    public async Task<int> ApplyDelinquencyPolicyAsync(CancellationToken ct = default)
    {
        var s = await settings.LoadAsync(ct);
        if (s.DelinquentReservationPolicy is not (DelinquentReservationPolicy.Suspend or DelinquentReservationPolicy.Cancel)) return 0;
        var changed = 0;
        var candidates = await db.Set<Reservation>().Include(r => r.Space).Include(r => r.Slots)
            .Where(r => r.MemberId != null && r.EndAt > DateTime.UtcNow && (r.Status == ReservationStatus.Approved || r.Status == ReservationStatus.Suspended)).ToListAsync(ct);
        foreach (var group in candidates.GroupBy(r => r.MemberId!.Value))
        {
            var blocked = (await delinquency.GetAsync(group.Key, ct)).ReservationsBlocked;
            foreach (var r in group)
            {
                if (blocked && r.Status == ReservationStatus.Approved)
                {
                    var cancel = s.DelinquentReservationPolicy == DelinquentReservationPolicy.Cancel;
                    var to = cancel ? ReservationStatus.Cancelled : ReservationStatus.Suspended;
                    r.Status = to;
                    if (cancel)
                    {
                        r.CancelledAt = DateTime.UtcNow;
                        r.CancellationReason = "Cancelada por inadimplência, conforme política do clube";
                        await ReleaseAsync(r, "Reserva cancelada por inadimplência", ct);
                    }
                    db.AddHistory(ctx, nameof(Reservation), r.Id, ReservationStatus.Approved, to, "Inadimplência — política financeira do clube");
                    await NotifyOwnerAsync(r, "reservation.delinquency", cancel ? "Reserva cancelada" : "Reserva suspensa",
                        $"A reserva {r.Code} em {r.Space.Name} foi {(cancel ? "cancelada" : "suspensa")} por pendências financeiras.", ct, mandatory: true);
                    changed++;
                }
                else if (!blocked && r.Status == ReservationStatus.Suspended)
                {
                    r.Status = ReservationStatus.Approved;
                    db.AddHistory(ctx, nameof(Reservation), r.Id, ReservationStatus.Suspended, ReservationStatus.Approved, "Situação financeira regularizada");
                    await NotifyOwnerAsync(r, "reservation.approved", "Reserva restabelecida", $"A reserva {r.Code} em {r.Space.Name} voltou a ficar aprovada.", ct);
                    changed++;
                }
            }
        }
        if (changed > 0) await db.SaveChangesAsync(ct);
        return changed;
    }

    /// <summary>Lembretes de reservas das próximas 24 horas e conclusão das reservas encerradas.</summary>
    public async Task<int> SendRemindersAndCompleteAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var upcoming = await db.Set<Reservation>().Include(r => r.Space)
            .Where(r => r.Status == ReservationStatus.Approved && r.ReminderSentAt == null && r.StartAt > now && r.StartAt <= now.AddHours(24)).ToListAsync(ct);
        foreach (var r in upcoming)
        {
            var local = await clock.ToLocalAsync(r.StartAt, ct);
            await NotifyOwnerAsync(r, "reservation.reminder", "Lembrete de reserva", $"Sua reserva {r.Code} em {r.Space.Name} começa em {local:dd/MM 'às' HH:mm}.", ct);
            r.ReminderSentAt = now;
        }
        var ended = await db.Set<Reservation>().Where(r => r.Status == ReservationStatus.Approved && r.EndAt <= now && r.Attendance == AttendanceStatus.Attended).ToListAsync(ct);
        foreach (var r in ended) r.Status = ReservationStatus.Completed;
        if (upcoming.Count + ended.Count > 0) await db.SaveChangesAsync(ct);
        return upcoming.Count;
    }

    // ---------- Apoio ----------

    private async Task NotifyOwnerAsync(Reservation r, string type, string title, string body, CancellationToken ct, bool mandatory = false)
    {
        if (r.MemberId is { } memberId)
            await notifications.ToMemberAsync(memberId, NotificationCategory.Reservation, type, title, body, nameof(Reservation), r.Id, mandatory, ct);
        if (r.RequestedByDependentId is { } dependentId)
            await notifications.ToDependentAsync(dependentId, NotificationCategory.Dependent, type, title, body, nameof(Reservation), r.Id, mandatory, ct);
    }

    private Task PublishAsync(Guid spaceId, CancellationToken ct) => realtime.PublishAsync(ctx.RequireClub(), "availabilityChanged", new { spaceId }, ct);

    private static DomainException Invalid(string message) => new(message, "reserva_invalida", 400);

    private async Task<List<ReservationDto>> MapAsync(List<Reservation> items, CancellationToken ct)
    {
        var ids = items.Select(r => r.Id).ToList();
        var userIds = items.Select(r => r.RequestedByUserId).Distinct().ToList();
        var names = await db.Set<User>().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Name, ct);
        var guestCounts = await db.Set<Guest>().Where(g => g.ReservationId != null && ids.Contains(g.ReservationId.Value) && !g.IsCancelled)
            .GroupBy(g => g.ReservationId!.Value).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        return items.Select(r => new ReservationDto(r.Id, r.Code, r.SpaceId, r.Space.Name, r.Space.Type, r.Kind, r.Title, r.MemberId, r.Member?.FullName,
            names.GetValueOrDefault(r.RequestedByUserId, ""), r.RequestedByDependentId is not null, r.StartAt, r.EndAt,
            r.Slots.Where(s => s.IsActive || !Live.Contains(r.Status)).OrderBy(s => s.StartAt).Select(s => new ReservationSlotDto(s.StartAt, s.EndAt)).ToList(),
            r.PeopleCount, r.Status, r.RentalAmount, r.CleaningFee, r.TotalAmount, r.ChargeMode, r.ChargeId, r.Notes, r.DecisionReason, r.CancellationReason,
            r.Attendance, r.AttendanceNote, r.PenaltyChargeId, guestCounts.GetValueOrDefault(r.Id), r.CreatedAt)).ToList();
    }
}
