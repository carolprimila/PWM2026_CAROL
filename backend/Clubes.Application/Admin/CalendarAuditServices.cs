using Clubes.Application.Clubs;
using Clubes.Application.Common;
using Clubes.Domain;
using Microsoft.EntityFrameworkCore;

namespace Clubes.Application.Admin;

public enum CalendarItemType { Reservation, HallRental, Sports, Event, Assembly, Maintenance, Block, Holiday }
public record CalendarItemDto(string Id, CalendarItemType Type, string Title, DateTime StartAt, DateTime EndAt, bool AllDay, Guid? SpaceId, string? SpaceName,
    string? Status, bool HasConflict, string? ConflictReason, Guid? EntityId);

public class CalendarQuery
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public Guid? SpaceId { get; set; }
    public List<CalendarItemType>? Types { get; set; }
    public string? Status { get; set; }
}

/// <summary>Calendário administrativo unificado: reservas, eventos, assembleias, manutenções, bloqueios e feriados.</summary>
public class CalendarService(IAppDb db, ClubClock clock)
{
    public async Task<List<CalendarItemDto>> GetAsync(CalendarQuery query, CancellationToken ct = default)
    {
        var from = query.From.ToUniversalTime();
        var to = query.To.ToUniversalTime();
        if (to <= from || (to - from).TotalDays > 100) throw new DomainException("Período inválido (máximo de 100 dias).", "periodo_invalido", 400);
        bool Wants(CalendarItemType type) => query.Types is not { Count: > 0 } || query.Types.Contains(type);
        var items = new List<CalendarItemDto>();

        var blocks = await db.Set<SpaceBlock>().Where(b => !b.IsCancelled && b.StartAt < to && b.EndAt > from && (query.SpaceId == null || b.SpaceId == query.SpaceId || b.SpaceId == null))
            .Select(b => new { b.Id, b.SpaceId, SpaceName = b.Space!.Name, b.StartAt, b.EndAt, b.Reason, b.Kind }).ToListAsync(ct);

        var reservations = await db.Set<Reservation>()
            .Where(r => r.StartAt < to && r.EndAt > from && (query.SpaceId == null || r.SpaceId == query.SpaceId)
                && (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Approved || r.Status == ReservationStatus.Suspended || r.Status == ReservationStatus.Completed))
            .Select(r => new
            {
                r.Id, r.Code, r.SpaceId, SpaceName = r.Space.Name, SpaceType = r.Space.Type, r.Kind, r.Title, Member = r.Member!.FullName, r.Status,
                Slots = r.Slots.Where(s => s.IsActive || r.Status == ReservationStatus.Completed).Select(s => new { s.Id, s.StartAt, s.EndAt }).ToList(),
            }).ToListAsync(ct);

        var events = await db.Set<ClubEvent>().Where(e => e.StartAt < to && e.EndAt > from && e.Status != EventStatus.Cancelled && (query.SpaceId == null || e.SpaceId == query.SpaceId))
            .Select(e => new { e.Id, e.Title, e.StartAt, e.EndAt, e.SpaceId, e.Status }).ToListAsync(ct);

        foreach (var r in reservations)
        {
            var type = r.SpaceType == SpaceType.PartyHall ? CalendarItemType.HallRental : r.SpaceType == SpaceType.Court ? CalendarItemType.Sports : CalendarItemType.Reservation;
            if (!Wants(type) || (query.Status is not null && r.Status.ToString() != query.Status)) continue;
            foreach (var slot in r.Slots.Where(s => s.StartAt < to && s.EndAt > from))
            {
                var block = blocks.FirstOrDefault(b => (b.SpaceId == null || b.SpaceId == r.SpaceId) && b.StartAt < slot.EndAt && b.EndAt > slot.StartAt);
                var clash = events.FirstOrDefault(e => e.SpaceId == r.SpaceId && e.StartAt < slot.EndAt && e.EndAt > slot.StartAt);
                var conflict = block is not null ? $"Conflito com bloqueio: {block.Reason}" : clash is not null ? $"Conflito com evento: {clash.Title}" : null;
                var title = r.Kind == ReservationKind.Member ? $"{r.Code} · {r.Member}" : $"{r.Code} · {r.Title}";
                items.Add(new CalendarItemDto($"res-{slot.Id}", type, title, slot.StartAt, slot.EndAt, false, r.SpaceId, r.SpaceName, r.Status.ToString(), conflict is not null, conflict, r.Id));
            }
        }

        if (Wants(CalendarItemType.Event))
        {
            var spaceNames = await db.Set<Space>().ToDictionaryAsync(s => s.Id, s => s.Name, ct);
            items.AddRange(events.Select(e => new CalendarItemDto($"evt-{e.Id}", CalendarItemType.Event, e.Title, e.StartAt, e.EndAt, false, e.SpaceId,
                e.SpaceId is { } sid ? spaceNames.GetValueOrDefault(sid) : null, e.Status.ToString(), false, null, e.Id)));
        }

        if (Wants(CalendarItemType.Assembly) && query.SpaceId is null)
        {
            var assemblies = await db.Set<Assembly>().Where(a => a.ScheduledAt >= from && a.ScheduledAt < to && a.Status != AssemblyStatus.Cancelled)
                .Select(a => new { a.Id, a.Title, a.ScheduledAt, a.Status }).ToListAsync(ct);
            items.AddRange(assemblies.Select(a => new CalendarItemDto($"asm-{a.Id}", CalendarItemType.Assembly, a.Title, a.ScheduledAt, a.ScheduledAt.AddHours(2), false, null, null, a.Status.ToString(), false, null, a.Id)));
        }

        if (Wants(CalendarItemType.Block))
            items.AddRange(blocks.Where(b => b.Kind != BlockKind.Maintenance || !Wants(CalendarItemType.Maintenance))
                .Select(b => new CalendarItemDto($"blk-{b.Id}", CalendarItemType.Block, b.Reason, b.StartAt, b.EndAt, false, b.SpaceId, b.SpaceName ?? "Todos os espaços", b.Kind.ToString(), false, null, b.Id)));

        if (Wants(CalendarItemType.Maintenance))
        {
            items.AddRange(blocks.Where(b => b.Kind == BlockKind.Maintenance)
                .Select(b => new CalendarItemDto($"mnb-{b.Id}", CalendarItemType.Maintenance, b.Reason, b.StartAt, b.EndAt, false, b.SpaceId, b.SpaceName, "Bloqueio", false, null, b.Id)));
            var fromDate = DateOnly.FromDateTime(await clock.ToLocalAsync(from, ct));
            var toDate = DateOnly.FromDateTime(await clock.ToLocalAsync(to, ct));
            var orders = await db.Set<MaintenanceOrder>().Where(m => m.DueDate >= fromDate && m.DueDate <= toDate && m.Status != MaintenanceStatus.Cancelled
                    && m.Status != MaintenanceStatus.Completed && (query.SpaceId == null || m.SpaceId == query.SpaceId))
                .Select(m => new { m.Id, m.Code, m.Title, m.DueDate, m.SpaceId, m.Status }).ToListAsync(ct);
            foreach (var m in orders)
            {
                var start = await clock.ToUtcAsync(m.DueDate!.Value, 0, ct);
                items.Add(new CalendarItemDto($"mnt-{m.Id}", CalendarItemType.Maintenance, $"{m.Code} · prazo: {m.Title}", start, start.AddDays(1), true, m.SpaceId, null, m.Status.ToString(), false, null, m.Id));
            }
        }

        if (Wants(CalendarItemType.Holiday))
        {
            var fromDate = DateOnly.FromDateTime(await clock.ToLocalAsync(from, ct));
            var toDate = DateOnly.FromDateTime(await clock.ToLocalAsync(to, ct));
            var holidays = await db.Set<Holiday>().Where(h => h.Date >= fromDate && h.Date <= toDate).ToListAsync(ct);
            foreach (var h in holidays)
            {
                var start = await clock.ToUtcAsync(h.Date, 0, ct);
                items.Add(new CalendarItemDto($"hol-{h.Id}", CalendarItemType.Holiday, h.Name, start, start.AddDays(1), true, null, null,
                    h.IsClosed ? "Fechado" : h.OpenMinute is not null ? "Horário especial" : h.Scope.ToString(), false, null, h.Id));
            }
        }
        return items.OrderBy(i => i.StartAt).ToList();
    }
}

public record AuditLogDto(Guid Id, DateTime At, Guid? UserId, string? UserName, string Operation, string EntityType, string? EntityId, string? BeforeJson,
    string? AfterJson, string? Origin, string? Justification);

public class AuditQuery : PageQuery
{
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public Guid? UserId { get; set; }
    public string? Operation { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

/// <summary>Consulta da auditoria. Os registros são somente leitura: não há operação de alteração ou exclusão, e o banco as rejeita por gatilho.</summary>
public class AuditService(IAppDb db)
{
    public async Task<PagedResult<AuditLogDto>> ListAsync(AuditQuery query, CancellationToken ct = default)
    {
        var q = db.Set<AuditLog>().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.EntityType)) q = q.Where(a => a.EntityType == query.EntityType);
        if (!string.IsNullOrWhiteSpace(query.EntityId)) q = q.Where(a => a.EntityId == query.EntityId);
        if (!string.IsNullOrWhiteSpace(query.Operation)) q = q.Where(a => a.Operation == query.Operation);
        if (query.UserId is not null) q = q.Where(a => a.UserId == query.UserId);
        if (query.From is not null) q = q.Where(a => a.CreatedAt >= query.From.Value.ToUniversalTime());
        if (query.To is not null) q = q.Where(a => a.CreatedAt <= query.To.Value.ToUniversalTime());
        if (!string.IsNullOrWhiteSpace(query.Search)) { var like = query.Search.Like(); q = q.Where(a => EF.Functions.ILike(a.UserName!, like) || EF.Functions.ILike(a.Justification!, like)); }
        return await q.OrderByDescending(a => a.CreatedAt)
            .Select(a => new AuditLogDto(a.Id, a.CreatedAt, a.UserId, a.UserName, a.Operation, a.EntityType, a.EntityId, a.BeforeJson, a.AfterJson, a.Origin, a.Justification))
            .ToPagedAsync(query, ct);
    }

    public async Task<List<string>> EntityTypesAsync(CancellationToken ct = default)
        => await db.Set<AuditLog>().Select(a => a.EntityType).Distinct().OrderBy(t => t).ToListAsync(ct);
}
