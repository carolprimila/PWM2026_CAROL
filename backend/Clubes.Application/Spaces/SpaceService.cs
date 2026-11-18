using System.ComponentModel.DataAnnotations;
using Clubes.Application.Clubs;
using Clubes.Application.Common;
using Clubes.Domain;
using Microsoft.EntityFrameworkCore;

namespace Clubes.Application.Spaces;

public record ScheduleDto(DayOfWeek DayOfWeek, [Range(0, 1439)] int OpenMinute, [Range(1, 1440)] int CloseMinute);
public record SpaceDto(Guid Id, string Name, SpaceType Type, string? Subtype, string? Description, int? Capacity, bool IsReservable, BookingMode BookingMode,
    int SlotMinutes, int FixedDays, decimal? HourlyPrice, decimal? PeriodPrice, decimal? CleaningFee, SpaceChargeMode? ChargeMode, ApprovalMode? ApprovalMode,
    string? UsageRules, Guid? ImageFileId, bool IsActive, int SortOrder, IReadOnlyList<ScheduleDto> Schedules);

public class SaveSpaceRequest
{
    [Required, StringLength(100, MinimumLength = 2)] public string Name { get; set; } = "";
    public SpaceType Type { get; set; }
    [StringLength(50)] public string? Subtype { get; set; }
    [StringLength(2000)] public string? Description { get; set; }
    [Range(1, 100_000)] public int? Capacity { get; set; }
    public bool IsReservable { get; set; }
    public BookingMode BookingMode { get; set; }
    [Range(15, 720)] public int SlotMinutes { get; set; } = 60;
    [Range(1, 30)] public int FixedDays { get; set; } = 1;
    [Range(0, 1_000_000)] public decimal? HourlyPrice { get; set; }
    [Range(0, 1_000_000)] public decimal? PeriodPrice { get; set; }
    [Range(0, 1_000_000)] public decimal? CleaningFee { get; set; }
    public SpaceChargeMode? ChargeMode { get; set; }
    public ApprovalMode? ApprovalMode { get; set; }
    [StringLength(4000)] public string? UsageRules { get; set; }
    public Guid? ImageFileId { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public List<ScheduleDto> Schedules { get; set; } = [];
}

public record BlockDto(Guid Id, Guid? SpaceId, string? SpaceName, DateTime StartAt, DateTime EndAt, string Reason, BlockKind Kind, Guid? MaintenanceOrderId, bool IsCancelled, DateTime CreatedAt);
public record CreateBlockRequest(Guid? SpaceId, DateTime StartAt, DateTime EndAt, [Required, StringLength(300, MinimumLength = 3)] string Reason, BlockKind Kind, Guid? MaintenanceOrderId);
public record BlockResult(BlockDto Block, IReadOnlyList<ConflictDto> Conflicts);
public record ConflictDto(Guid ReservationId, string Code, string SpaceName, string? MemberName, DateTime StartAt, DateTime EndAt, ReservationStatus Status);
public record HolidayDto(Guid Id, DateOnly Date, string Name, HolidayScope Scope, bool IsClosed, int? OpenMinute, int? CloseMinute, string? Notes);
public record SaveHolidayRequest(DateOnly Date, [Required, StringLength(120)] string Name, HolidayScope Scope, bool IsClosed,
    [Range(0, 1439)] int? OpenMinute, [Range(1, 1440)] int? CloseMinute, [StringLength(500)] string? Notes);

public enum SlotState { Available, Reserved, Blocked, Past, Closed }
public record SlotDto(int StartMinute, int EndMinute, DateTime StartAt, DateTime EndAt, SlotState State, string? Reason, bool Mine);
public record DayAvailabilityDto(Guid SpaceId, DateOnly Date, bool IsOpen, string? ClosedReason, int? OpenMinute, int? CloseMinute, IReadOnlyList<SlotDto> Slots);
public record PeriodDayDto(DateOnly Date, SlotState State, string? Reason);

public class SpaceService(IAppDb db, RequestContext ctx, ClubClock clock, IRealtimeNotifier realtime)
{
    public async Task<List<SpaceDto>> ListAsync(bool includeInactive, CancellationToken ct = default)
    {
        var spaces = await db.Set<Space>().AsNoTracking().Include(s => s.Schedules).Where(s => includeInactive || s.IsActive)
            .OrderBy(s => s.SortOrder).ThenBy(s => s.Name).ToListAsync(ct);
        return spaces.Select(Map).ToList();
    }

    public async Task<SpaceDto> GetAsync(Guid id, CancellationToken ct = default)
        => Map(await db.Set<Space>().AsNoTracking().Include(s => s.Schedules).Where(s => s.Id == id).FirstOr404Async("Espaço", ct));

    public async Task<SpaceDto> SaveAsync(Guid? id, SaveSpaceRequest req, CancellationToken ct = default)
    {
        if (req.Schedules.Any(s => s.CloseMinute <= s.OpenMinute)) throw Invalid("O horário de fechamento deve ser posterior ao de abertura.");
        if (req.Schedules.GroupBy(s => s.DayOfWeek).Any(g => g.Count() > 1)) throw Invalid("Informe um único horário por dia da semana.");
        if (req.Schedules.Any(s => (s.CloseMinute - s.OpenMinute) % req.SlotMinutes != 0)) throw Invalid("O horário de funcionamento deve ser múltiplo da duração do intervalo.");
        if (req.Type == SpaceType.Pool && req.IsReservable) throw Invalid("Piscinas não podem ser reservadas individualmente.");
        if (req.IsReservable && req.Capacity is null) throw Invalid("Informe a capacidade máxima do espaço reservável.");
        if (req.IsReservable && req.BookingMode == BookingMode.Hourly && req.Schedules.Count == 0) throw Invalid("Informe os horários de funcionamento para reservas.");

        Space space;
        if (id is null) { space = new Space(); db.Set<Space>().Add(space); }
        else space = await db.Set<Space>().Include(s => s.Schedules).Where(s => s.Id == id).FirstOr404Async("Espaço", ct);
        if (await db.Set<Space>().AnyAsync(s => s.Name == req.Name.Trim() && s.Id != space.Id, ct)) throw DomainException.Conflict("Já existe um espaço com este nome.");

        (space.Name, space.Type, space.Subtype, space.Description, space.Capacity) = (req.Name.Trim(), req.Type, req.Subtype, req.Description, req.Capacity);
        (space.IsReservable, space.BookingMode, space.SlotMinutes, space.FixedDays) = (req.IsReservable, req.BookingMode, req.SlotMinutes, req.FixedDays);
        (space.HourlyPrice, space.PeriodPrice, space.CleaningFee, space.ChargeMode, space.ApprovalMode) = (req.HourlyPrice, req.PeriodPrice, req.CleaningFee, req.ChargeMode, req.ApprovalMode);
        (space.UsageRules, space.ImageFileId, space.IsActive, space.SortOrder) = (req.UsageRules, req.ImageFileId, req.IsActive, req.SortOrder);

        db.Set<SpaceSchedule>().RemoveRange(space.Schedules.Where(s => req.Schedules.All(r => r.DayOfWeek != s.DayOfWeek)).ToList());
        foreach (var r in req.Schedules)
        {
            var row = space.Schedules.FirstOrDefault(s => s.DayOfWeek == r.DayOfWeek);
            if (row is null) db.Set<SpaceSchedule>().Add(row = new SpaceSchedule { Space = space, DayOfWeek = r.DayOfWeek });
            (row.OpenMinute, row.CloseMinute) = (r.OpenMinute, r.CloseMinute);
        }
        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(ctx.RequireClub(), "availabilityChanged", new { spaceId = space.Id }, ct);
        return await GetAsync(space.Id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var space = await db.Set<Space>().Where(s => s.Id == id).FirstOr404Async("Espaço", ct);
        if (await db.Set<ReservationSlot>().AnyAsync(s => s.SpaceId == id && s.IsActive && s.EndAt > DateTime.UtcNow, ct))
            throw DomainException.Conflict("Há reservas futuras para este espaço. Cancele-as ou desative o espaço.");
        db.Set<Space>().Remove(space);
        await db.SaveChangesAsync(ct);
    }

    // ---------- Bloqueios ----------

    public async Task<List<BlockDto>> BlocksAsync(DateTime? from, DateTime? to, Guid? spaceId, CancellationToken ct = default)
    {
        var q = db.Set<SpaceBlock>().AsNoTracking().Where(b => !b.IsCancelled);
        if (from is not null) q = q.Where(b => b.EndAt >= from.Value.ToUniversalTime());
        if (to is not null) q = q.Where(b => b.StartAt <= to.Value.ToUniversalTime());
        if (spaceId is not null) q = q.Where(b => b.SpaceId == spaceId || b.SpaceId == null);
        return await q.OrderBy(b => b.StartAt).Select(b => new BlockDto(b.Id, b.SpaceId, b.Space!.Name, b.StartAt, b.EndAt, b.Reason, b.Kind, b.MaintenanceOrderId, b.IsCancelled, b.CreatedAt)).ToListAsync(ct);
    }

    /// <summary>Cria o bloqueio (efeito imediato na disponibilidade) e devolve as reservas existentes que conflitam com ele.</summary>
    public async Task<BlockResult> CreateBlockAsync(CreateBlockRequest req, CancellationToken ct = default)
    {
        var start = req.StartAt.ToUniversalTime();
        var end = req.EndAt.ToUniversalTime();
        if (end <= start) throw Invalid("O término do bloqueio deve ser posterior ao início.");
        if (req.SpaceId is { } spaceId) await db.Set<Space>().Where(s => s.Id == spaceId).FirstOr404Async("Espaço", ct);

        var block = new SpaceBlock { SpaceId = req.SpaceId, StartAt = start, EndAt = end, Reason = req.Reason, Kind = req.Kind, MaintenanceOrderId = req.MaintenanceOrderId, CreatedByUserId = ctx.RequireUser() };
        db.Set<SpaceBlock>().Add(block);
        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(ctx.RequireClub(), "availabilityChanged", new { spaceId = req.SpaceId }, ct);

        var conflicts = await ConflictsAsync(req.SpaceId, start, end, ct);
        var dto = (await db.Set<SpaceBlock>().Where(b => b.Id == block.Id)
            .Select(b => new BlockDto(b.Id, b.SpaceId, b.Space!.Name, b.StartAt, b.EndAt, b.Reason, b.Kind, b.MaintenanceOrderId, b.IsCancelled, b.CreatedAt)).FirstAsync(ct));
        return new BlockResult(dto, conflicts);
    }

    public async Task<List<ConflictDto>> ConflictsAsync(Guid? spaceId, DateTime start, DateTime end, CancellationToken ct = default)
        => await db.Set<ReservationSlot>().Where(s => s.IsActive && (spaceId == null || s.SpaceId == spaceId) && s.StartAt < end && s.EndAt > start)
            .Select(s => s.Reservation).Distinct()
            .Select(r => new ConflictDto(r.Id, r.Code, r.Space.Name, r.Member!.FullName, r.StartAt, r.EndAt, r.Status)).ToListAsync(ct);

    public async Task CancelBlockAsync(Guid id, CancellationToken ct = default)
    {
        var block = await db.Set<SpaceBlock>().Where(b => b.Id == id).FirstOr404Async("Bloqueio", ct);
        block.IsCancelled = true;
        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(ctx.RequireClub(), "availabilityChanged", new { spaceId = block.SpaceId }, ct);
    }

    // ---------- Feriados e datas excepcionais ----------

    public async Task<List<HolidayDto>> HolidaysAsync(int? year, CancellationToken ct = default)
        => await db.Set<Holiday>().Where(h => year == null || h.Date.Year == year).OrderBy(h => h.Date)
            .Select(h => new HolidayDto(h.Id, h.Date, h.Name, h.Scope, h.IsClosed, h.OpenMinute, h.CloseMinute, h.Notes)).ToListAsync(ct);

    public async Task<HolidayDto> SaveHolidayAsync(Guid? id, SaveHolidayRequest req, CancellationToken ct = default)
    {
        if (!req.IsClosed && (req.OpenMinute is null) != (req.CloseMinute is null)) throw Invalid("Informe abertura e fechamento do horário especial.");
        if (req.OpenMinute >= req.CloseMinute) throw Invalid("O horário de fechamento deve ser posterior ao de abertura.");
        Holiday holiday;
        if (id is null) { holiday = new Holiday(); db.Set<Holiday>().Add(holiday); }
        else holiday = await db.Set<Holiday>().Where(h => h.Id == id).FirstOr404Async("Feriado", ct);
        (holiday.Date, holiday.Name, holiday.Scope, holiday.IsClosed, holiday.Notes) = (req.Date, req.Name.Trim(), req.Scope, req.IsClosed, req.Notes);
        (holiday.OpenMinute, holiday.CloseMinute) = req.IsClosed ? (null, null) : (req.OpenMinute, req.CloseMinute);
        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(ctx.RequireClub(), "availabilityChanged", new { date = req.Date }, ct);
        return new HolidayDto(holiday.Id, holiday.Date, holiday.Name, holiday.Scope, holiday.IsClosed, holiday.OpenMinute, holiday.CloseMinute, holiday.Notes);
    }

    public async Task DeleteHolidayAsync(Guid id, CancellationToken ct = default)
    {
        var holiday = await db.Set<Holiday>().Where(h => h.Id == id).FirstOr404Async("Feriado", ct);
        db.Set<Holiday>().Remove(holiday);
        await db.SaveChangesAsync(ct);
    }

    // ---------- Disponibilidade ----------

    /// <summary>Janela de funcionamento do espaço em uma data, considerando feriados, fechamentos e horários especiais.</summary>
    public async Task<(int Open, int Close, string? ClosedReason)> WindowAsync(Space space, DateOnly date, CancellationToken ct = default)
    {
        var holidays = await db.Set<Holiday>().Where(h => h.Date == date).ToListAsync(ct);
        var closed = holidays.FirstOrDefault(h => h.IsClosed);
        if (closed is not null) return (0, 0, $"Fechado: {closed.Name}");
        var schedule = space.Schedules.FirstOrDefault(s => s.DayOfWeek == date.DayOfWeek);
        if (schedule is null) return (0, 0, "Sem funcionamento neste dia da semana");
        var special = holidays.FirstOrDefault(h => h.OpenMinute is not null && h.CloseMinute is not null);
        if (special is null) return (schedule.OpenMinute, schedule.CloseMinute, null);

        // Horário especial: vale a interseção com a grade do espaço, alinhada aos intervalos.
        var open = Math.Max(schedule.OpenMinute, special.OpenMinute!.Value);
        var close = Math.Min(schedule.CloseMinute, special.CloseMinute!.Value);
        open = schedule.OpenMinute + (int)Math.Ceiling((open - schedule.OpenMinute) / (double)space.SlotMinutes) * space.SlotMinutes;
        close = schedule.OpenMinute + (close - schedule.OpenMinute) / space.SlotMinutes * space.SlotMinutes;
        return close > open ? (open, close, null) : (0, 0, $"Horário especial: {special.Name}");
    }

    public async Task<DayAvailabilityDto> DayAsync(Guid spaceId, DateOnly date, CancellationToken ct = default)
    {
        var space = await db.Set<Space>().AsNoTracking().Include(s => s.Schedules).Where(s => s.Id == spaceId && s.IsActive).FirstOr404Async("Espaço", ct);
        if (!space.IsReservable) throw Invalid("Este espaço não é reservável.");
        if (space.BookingMode != BookingMode.Hourly) throw Invalid("Este espaço é reservado por período de dias. Consulte a disponibilidade por período.");

        var (open, close, closedReason) = await WindowAsync(space, date, ct);
        if (closedReason is not null) return new DayAvailabilityDto(spaceId, date, false, closedReason, null, null, []);

        var dayStart = await clock.ToUtcAsync(date, open, ct);
        var dayEnd = await clock.ToUtcAsync(date, close, ct);
        var taken = await db.Set<ReservationSlot>().Where(s => s.SpaceId == spaceId && s.IsActive && s.StartAt < dayEnd && s.EndAt > dayStart)
            .Select(s => new { s.StartAt, s.EndAt, s.Reservation.MemberId, s.Reservation.RequestedByUserId }).ToListAsync(ct);
        var blocks = await db.Set<SpaceBlock>().Where(b => !b.IsCancelled && (b.SpaceId == spaceId || b.SpaceId == null) && b.StartAt < dayEnd && b.EndAt > dayStart)
            .Select(b => new { b.StartAt, b.EndAt, b.Reason }).ToListAsync(ct);

        var now = DateTime.UtcNow;
        var slots = new List<SlotDto>();
        for (var minute = open; minute + space.SlotMinutes <= close; minute += space.SlotMinutes)
        {
            var start = dayStart.AddMinutes(minute - open);
            var end = start.AddMinutes(space.SlotMinutes);
            var block = blocks.FirstOrDefault(b => b.StartAt < end && b.EndAt > start);
            var reservation = taken.FirstOrDefault(t => t.StartAt < end && t.EndAt > start);
            var mine = reservation is not null && ((ctx.MemberId is not null && reservation.MemberId == ctx.MemberId) || reservation.RequestedByUserId == ctx.UserId);
            (SlotState state, string? reason) = block is not null ? (SlotState.Blocked, (string?)(ctx.IsStaff ? block.Reason : "Indisponível"))
                : reservation is not null ? (SlotState.Reserved, null)
                : start < now ? (SlotState.Past, null)
                : (SlotState.Available, null);
            slots.Add(new SlotDto(minute, minute + space.SlotMinutes, start, end, state, reason, mine));
        }
        return new DayAvailabilityDto(spaceId, date, true, null, open, close, slots);
    }

    /// <summary>Disponibilidade de espaços reservados por período (salão de festas): situação de cada data de início possível.</summary>
    public async Task<List<PeriodDayDto>> PeriodAsync(Guid spaceId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        if (to < from || to.DayNumber - from.DayNumber > 100) throw Invalid("Período inválido.");
        var space = await db.Set<Space>().AsNoTracking().Where(s => s.Id == spaceId && s.IsActive).FirstOr404Async("Espaço", ct);
        if (!space.IsReservable || space.BookingMode != BookingMode.MultiDay) throw Invalid("Este espaço não é reservado por período de dias.");

        var rangeStart = await clock.ToUtcAsync(from, 0, ct);
        var rangeEnd = await clock.ToUtcAsync(to.AddDays(space.FixedDays), 0, ct);
        var taken = await db.Set<ReservationSlot>().Where(s => s.SpaceId == spaceId && s.IsActive && s.StartAt < rangeEnd && s.EndAt > rangeStart).Select(s => new { s.StartAt, s.EndAt }).ToListAsync(ct);
        var blocks = await db.Set<SpaceBlock>().Where(b => !b.IsCancelled && (b.SpaceId == spaceId || b.SpaceId == null) && b.StartAt < rangeEnd && b.EndAt > rangeStart).Select(b => new { b.StartAt, b.EndAt }).ToListAsync(ct);
        var closures = await db.Set<Holiday>().Where(h => h.IsClosed && h.Date >= from && h.Date <= to.AddDays(space.FixedDays)).Select(h => new { h.Date, h.Name }).ToListAsync(ct);
        var today = await clock.TodayAsync(ct);

        var result = new List<PeriodDayDto>();
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var start = await clock.ToUtcAsync(date, 0, ct);
            var end = await clock.ToUtcAsync(date.AddDays(space.FixedDays), 0, ct);
            var closure = closures.FirstOrDefault(c => c.Date >= date && c.Date < date.AddDays(space.FixedDays));
            var (state, reason) = date <= today ? (SlotState.Past, (string?)null)
                : closure is not null ? (SlotState.Closed, (string?)$"Fechado: {closure.Name}")
                : blocks.Any(b => b.StartAt < end && b.EndAt > start) ? (SlotState.Blocked, (string?)"Indisponível")
                : taken.Any(t => t.StartAt < end && t.EndAt > start) ? (SlotState.Reserved, null)
                : (SlotState.Available, null);
            result.Add(new PeriodDayDto(date, state, reason));
        }
        return result;
    }

    private static DomainException Invalid(string message) => new(message, "espaco_invalido", 400);

    internal static SpaceDto Map(Space s) => new(s.Id, s.Name, s.Type, s.Subtype, s.Description, s.Capacity, s.IsReservable, s.BookingMode, s.SlotMinutes, s.FixedDays,
        s.HourlyPrice, s.PeriodPrice, s.CleaningFee, s.ChargeMode, s.ApprovalMode, s.UsageRules, s.ImageFileId, s.IsActive, s.SortOrder,
        s.Schedules.OrderBy(x => x.DayOfWeek).Select(x => new ScheduleDto(x.DayOfWeek, x.OpenMinute, x.CloseMinute)).ToList());
}
