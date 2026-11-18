using System.ComponentModel.DataAnnotations;
using Clubes.Application.Clubs;
using Clubes.Application.Common;
using Clubes.Application.Finance;
using Clubes.Application.Spaces;
using Clubes.Domain;
using Microsoft.EntityFrameworkCore;

namespace Clubes.Application.Operations;

// ---------- Manutenção ----------

public record MaintenanceDto(Guid Id, string Code, string Title, string? Description, MaintenanceKind Kind, Guid? SpaceId, string? SpaceName, Guid? AssetId,
    string? AssetName, Guid? ResponsibleEmployeeId, string? ResponsibleName, DateTime OpenedAt, DateOnly? DueDate, MaintenanceStatus Status, decimal? EstimatedCost,
    decimal? ActualCost, DateTime? CompletedAt, bool IsLate, bool HasActiveBlock);
public record SaveMaintenanceRequest([Required, StringLength(150, MinimumLength = 3)] string Title, [StringLength(4000)] string? Description, MaintenanceKind Kind,
    Guid? SpaceId, Guid? AssetId, Guid? ResponsibleEmployeeId, [StringLength(150)] string? ResponsibleName, DateOnly? DueDate, [Range(0, 100_000_000)] decimal? EstimatedCost);
public record MaintenanceStatusRequest(MaintenanceStatus Status, [StringLength(1000)] string? Note, [Range(0, 100_000_000)] decimal? ActualCost, bool CreateExpense);
public record MaintenanceBlockRequest(DateTime StartAt, DateTime EndAt);

public class MaintenanceQuery : PageQuery
{
    public MaintenanceStatus? Status { get; set; }
    public Guid? SpaceId { get; set; }
    public bool? OpenOnly { get; set; }
}

public class MaintenanceService(IAppDb db, RequestContext ctx, SpaceService spaces, ClubClock clock, NotificationService notifications)
{
    private static readonly MaintenanceStatus[] Closed = [MaintenanceStatus.Completed, MaintenanceStatus.Cancelled];

    public async Task<PagedResult<MaintenanceDto>> ListAsync(MaintenanceQuery query, CancellationToken ct = default)
    {
        var q = db.Set<MaintenanceOrder>().AsNoTracking();
        if (query.Status is not null) q = q.Where(m => m.Status == query.Status);
        if (query.SpaceId is not null) q = q.Where(m => m.SpaceId == query.SpaceId);
        if (query.OpenOnly is true) q = q.Where(m => !Closed.Contains(m.Status));
        if (!string.IsNullOrWhiteSpace(query.Search)) { var like = query.Search.Like(); q = q.Where(m => EF.Functions.ILike(m.Title, like) || EF.Functions.ILike(m.Code, like)); }
        return await Project(q.OrderByDescending(m => m.OpenedAt), await clock.TodayAsync(ct)).ToPagedAsync(query, ct);
    }

    public async Task<MaintenanceDto> GetAsync(Guid id, CancellationToken ct = default)
        => await Project(db.Set<MaintenanceOrder>().Where(m => m.Id == id), await clock.TodayAsync(ct)).FirstOr404Async("Manutenção", ct);

    private IQueryable<MaintenanceDto> Project(IQueryable<MaintenanceOrder> q, DateOnly today)
        => from m in q
           join s in db.Set<Space>() on m.SpaceId equals s.Id into spaceRows
           from s in spaceRows.DefaultIfEmpty()
           join a in db.Set<Asset>() on m.AssetId equals a.Id into assetRows
           from a in assetRows.DefaultIfEmpty()
           join e in db.Set<Employee>() on m.ResponsibleEmployeeId equals e.Id into employees
           from e in employees.DefaultIfEmpty()
           select new MaintenanceDto(m.Id, m.Code, m.Title, m.Description, m.Kind, m.SpaceId, s.Name, m.AssetId, a.Name, m.ResponsibleEmployeeId, e.FullName ?? m.ResponsibleName,
               m.OpenedAt, m.DueDate, m.Status, m.EstimatedCost, m.ActualCost, m.CompletedAt,
               m.DueDate != null && m.DueDate < today && m.Status != MaintenanceStatus.Completed && m.Status != MaintenanceStatus.Cancelled,
               db.Set<SpaceBlock>().Any(b => b.MaintenanceOrderId == m.Id && !b.IsCancelled && b.EndAt > DateTime.UtcNow));

    public async Task<MaintenanceDto> SaveAsync(Guid? id, SaveMaintenanceRequest req, CancellationToken ct = default)
    {
        if (req.SpaceId is null && req.AssetId is null) throw new DomainException("Informe o espaço ou o bem afetado.", "manutencao_invalida", 400);
        MaintenanceOrder m;
        if (id is null)
        {
            m = new MaintenanceOrder { Code = $"MAN-{await db.NextSequenceAsync("maintenance", ct):D5}", OpenedByUserId = ctx.RequireUser() };
            db.Set<MaintenanceOrder>().Add(m);
            db.AddHistory(ctx, nameof(MaintenanceOrder), m.Id, null, MaintenanceStatus.Requested, "Solicitação aberta");
        }
        else
        {
            m = await db.Set<MaintenanceOrder>().Where(x => x.Id == id).FirstOr404Async("Manutenção", ct);
            if (Closed.Contains(m.Status)) throw DomainException.Conflict("Manutenção encerrada não pode ser alterada.");
        }
        (m.Title, m.Description, m.Kind, m.SpaceId, m.AssetId) = (req.Title.Trim(), req.Description, req.Kind, req.SpaceId, req.AssetId);
        (m.ResponsibleEmployeeId, m.ResponsibleName, m.DueDate, m.EstimatedCost) = (req.ResponsibleEmployeeId, req.ResponsibleName, req.DueDate, req.EstimatedCost);
        if (id is null)
            await notifications.ToStaffAsync(Permissions.MaintenanceManage, NotificationCategory.General, "maintenance.new", "Nova solicitação de manutenção", $"{m.Code} — {m.Title}", nameof(MaintenanceOrder), m.Id, ct);
        await db.SaveChangesAsync(ct);
        return await GetAsync(m.Id, ct);
    }

    public async Task<MaintenanceDto> SetStatusAsync(Guid id, MaintenanceStatusRequest req, CancellationToken ct = default)
    {
        var m = await db.Set<MaintenanceOrder>().Where(x => x.Id == id).FirstOr404Async("Manutenção", ct);
        if (Closed.Contains(m.Status)) throw DomainException.Conflict("Manutenção já encerrada.");
        if (req.Status == MaintenanceStatus.Cancelled && string.IsNullOrWhiteSpace(req.Note)) throw new DomainException("Informe o motivo do cancelamento.", "motivo_obrigatorio", 400);
        db.AddHistory(ctx, nameof(MaintenanceOrder), id, m.Status, req.Status, req.Note);
        m.Status = req.Status;
        if (req.ActualCost is not null) m.ActualCost = req.ActualCost;

        if (Closed.Contains(req.Status))
        {
            if (req.Status == MaintenanceStatus.Completed) m.CompletedAt = DateTime.UtcNow;
            // Encerrada a manutenção, os bloqueios vinculados ainda vigentes deixam de valer imediatamente.
            var blocks = await db.Set<SpaceBlock>().Where(b => b.MaintenanceOrderId == id && !b.IsCancelled && b.EndAt > DateTime.UtcNow).ToListAsync(ct);
            foreach (var block in blocks)
            {
                if (block.StartAt >= DateTime.UtcNow) block.IsCancelled = true; else block.EndAt = DateTime.UtcNow;
            }
        }
        if (req is { Status: MaintenanceStatus.Completed, CreateExpense: true } && m.ActualCost is > 0)
            db.Set<FinancialEntry>().Add(new FinancialEntry
            {
                Kind = FinancialEntryKind.Expense, Category = "Manutenção", Description = $"{m.Code} — {m.Title}", Amount = m.ActualCost.Value,
                DueDate = await clock.TodayAsync(ct), MaintenanceOrderId = m.Id, CreatedByUserId = ctx.UserId,
            });
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    /// <summary>Bloqueia o espaço da manutenção no período informado; reflete imediatamente na disponibilidade das reservas.</summary>
    public async Task<BlockResult> BlockSpaceAsync(Guid id, MaintenanceBlockRequest req, CancellationToken ct = default)
    {
        var m = await db.Set<MaintenanceOrder>().Where(x => x.Id == id).FirstOr404Async("Manutenção", ct);
        if (m.SpaceId is null) throw new DomainException("Esta manutenção não está vinculada a um espaço.", "manutencao_invalida", 400);
        if (Closed.Contains(m.Status)) throw DomainException.Conflict("Manutenção já encerrada.");
        return await spaces.CreateBlockAsync(new CreateBlockRequest(m.SpaceId, req.StartAt, req.EndAt, $"Manutenção {m.Code}: {m.Title}", BlockKind.Maintenance, m.Id), ct);
    }
}

// ---------- Vistorias ----------

public record InspectionItemDto(InspectionCategory Category, [Required, StringLength(200)] string Description, bool IsOk, bool IsDamage, [StringLength(500)] string? Notes);
public record InspectionDto(Guid Id, Guid SpaceId, string SpaceName, Guid? ReservationId, string? ReservationCode, Guid? MemberId, string? MemberName,
    InspectionMoment Moment, string InspectorName, DateTime PerformedAt, string? Notes, bool HasDamage, IReadOnlyList<InspectionItemDto> Items, Guid? OccurrenceId);
public record CreateInspectionRequest(Guid SpaceId, Guid? ReservationId, InspectionMoment Moment, [StringLength(2000)] string? Notes,
    [Required, MinLength(1)] List<InspectionItemDto> Items, [StringLength(2000)] string? DamageDescription);

public class InspectionQuery : PageQuery
{
    public Guid? SpaceId { get; set; }
    public Guid? ReservationId { get; set; }
    public bool? DamageOnly { get; set; }
}

public class InspectionService(IAppDb db, RequestContext ctx, NotificationService notifications)
{
    public async Task<PagedResult<InspectionDto>> ListAsync(InspectionQuery query, CancellationToken ct = default)
    {
        var q = db.Set<Inspection>().AsNoTracking();
        if (query.SpaceId is not null) q = q.Where(i => i.SpaceId == query.SpaceId);
        if (query.ReservationId is not null) q = q.Where(i => i.ReservationId == query.ReservationId);
        if (query.DamageOnly is true) q = q.Where(i => i.HasDamage);
        return await Project(q.OrderByDescending(i => i.PerformedAt)).ToPagedAsync(query, ct);
    }

    public async Task<InspectionDto> GetAsync(Guid id, CancellationToken ct = default) => await Project(db.Set<Inspection>().Where(i => i.Id == id)).FirstOr404Async("Vistoria", ct);

    private IQueryable<InspectionDto> Project(IQueryable<Inspection> q)
        => from i in q
           join r in db.Set<Reservation>() on i.ReservationId equals r.Id into reservations
           from r in reservations.DefaultIfEmpty()
           join m in db.Set<Member>() on i.MemberId equals m.Id into members
           from m in members.DefaultIfEmpty()
           join u in db.Set<User>() on i.InspectorUserId equals u.Id
           select new InspectionDto(i.Id, i.SpaceId, i.Space.Name, i.ReservationId, r.Code, i.MemberId, m.FullName, i.Moment, u.Name, i.PerformedAt, i.Notes, i.HasDamage,
               i.Items.OrderBy(x => x.Category).Select(x => new InspectionItemDto(x.Category, x.Description, x.IsOk, x.IsDamage, x.Notes)).ToList(),
               db.Set<Occurrence>().Where(o => o.InspectionId == i.Id).Select(o => (Guid?)o.Id).FirstOrDefault());

    /// <summary>Registra o checklist. Havendo dano, gera automaticamente uma ocorrência vinculada à reserva para análise administrativa.</summary>
    public async Task<InspectionDto> CreateAsync(CreateInspectionRequest req, CancellationToken ct = default)
    {
        var space = await db.Set<Space>().Where(s => s.Id == req.SpaceId).FirstOr404Async("Espaço", ct);
        Guid? memberId = null;
        string? code = null;
        if (req.ReservationId is { } reservationId)
        {
            var reservation = await db.Set<Reservation>().Where(r => r.Id == reservationId).Select(r => new { r.SpaceId, r.MemberId, r.Code }).FirstOr404Async("Reserva", ct);
            if (reservation.SpaceId != req.SpaceId) throw new DomainException("A reserva informada é de outro espaço.", "vistoria_invalida", 400);
            (memberId, code) = (reservation.MemberId, reservation.Code);
        }
        var inspection = new Inspection
        {
            SpaceId = req.SpaceId, ReservationId = req.ReservationId, MemberId = memberId, Moment = req.Moment, InspectorUserId = ctx.RequireUser(), Notes = req.Notes,
            HasDamage = req.Items.Any(i => i.IsDamage),
        };
        inspection.Items.AddRange(req.Items.Select(i => new InspectionItem { Category = i.Category, Description = i.Description.Trim(), IsOk = i.IsOk && !i.IsDamage, IsDamage = i.IsDamage, Notes = i.Notes }));
        db.Set<Inspection>().Add(inspection);

        if (inspection.HasDamage)
        {
            var damages = string.Join("; ", req.Items.Where(i => i.IsDamage).Select(i => i.Description + (i.Notes is null ? "" : $" ({i.Notes})")));
            var occurrence = new Occurrence
            {
                Code = $"OC-{await db.NextSequenceAsync("occurrence", ct):D5}", Kind = OccurrenceKind.Damage, SpaceId = req.SpaceId, ReservationId = req.ReservationId,
                MemberId = memberId, InspectionId = inspection.Id, ReportedByUserId = inspection.InspectorUserId,
                Description = req.DamageDescription ?? $"Danos identificados na vistoria de {space.Name}" + (code is null ? "" : $" (reserva {code})") + $": {damages}",
            };
            db.Set<Occurrence>().Add(occurrence);
            await notifications.ToStaffAsync(Permissions.OccurrencesManage, NotificationCategory.General, "occurrence.damage", "Dano identificado em vistoria",
                $"{occurrence.Code} — {space.Name}. Analise a ocorrência.", nameof(Occurrence), occurrence.Id, ct);
        }
        await db.SaveChangesAsync(ct);
        return await GetAsync(inspection.Id, ct);
    }
}

// ---------- Ocorrências ----------

public record OccurrenceDto(Guid Id, string Code, OccurrenceKind Kind, string Description, Guid? SpaceId, string? SpaceName, Guid? ReservationId, string? ReservationCode,
    Guid? MemberId, string? MemberName, Guid? InspectionId, string ReportedBy, DateTime OccurredAt, OccurrenceStatus Status, string? Resolution,
    decimal? ProposedChargeAmount, ChargeDecision ChargeDecision, string? ChargeDecisionReason, Guid? ChargeId);
public record CreateOccurrenceRequest(OccurrenceKind Kind, [Required, StringLength(4000, MinimumLength = 5)] string Description, Guid? SpaceId, Guid? ReservationId,
    Guid? MemberId, DateTime? OccurredAt);
public record UpdateOccurrenceRequest(OccurrenceStatus Status, [StringLength(2000)] string? Resolution);
public record ProposeChargeRequest([Range(0.01, 100_000_000)] decimal Amount, [Required, StringLength(1000, MinimumLength = 5)] string Justification, Guid? MemberId);
public record DecideChargeRequest(bool Approved, [Required, StringLength(1000, MinimumLength = 3)] string Reason);

public class OccurrenceQuery : PageQuery
{
    public OccurrenceKind? Kind { get; set; }
    public OccurrenceStatus? Status { get; set; }
    public ChargeDecision? ChargeDecision { get; set; }
}

public class OccurrenceService(IAppDb db, RequestContext ctx, ChargeService charges, SettingsService settings, ClubClock clock, NotificationService notifications)
{
    public async Task<PagedResult<OccurrenceDto>> ListAsync(OccurrenceQuery query, CancellationToken ct = default)
    {
        var q = db.Set<Occurrence>().AsNoTracking();
        if (query.Kind is not null) q = q.Where(o => o.Kind == query.Kind);
        if (query.Status is not null) q = q.Where(o => o.Status == query.Status);
        if (query.ChargeDecision is not null) q = q.Where(o => o.ChargeDecision == query.ChargeDecision);
        if (!string.IsNullOrWhiteSpace(query.Search)) { var like = query.Search.Like(); q = q.Where(o => EF.Functions.ILike(o.Description, like) || EF.Functions.ILike(o.Code, like)); }
        return await Project(q.OrderByDescending(o => o.OccurredAt)).ToPagedAsync(query, ct);
    }

    public async Task<OccurrenceDto> GetAsync(Guid id, CancellationToken ct = default) => await Project(db.Set<Occurrence>().Where(o => o.Id == id)).FirstOr404Async("Ocorrência", ct);

    private IQueryable<OccurrenceDto> Project(IQueryable<Occurrence> q)
        => from o in q
           join s in db.Set<Space>() on o.SpaceId equals s.Id into spaceRows
           from s in spaceRows.DefaultIfEmpty()
           join r in db.Set<Reservation>() on o.ReservationId equals r.Id into reservations
           from r in reservations.DefaultIfEmpty()
           join m in db.Set<Member>() on o.MemberId equals m.Id into members
           from m in members.DefaultIfEmpty()
           join u in db.Set<User>() on o.ReportedByUserId equals u.Id
           select new OccurrenceDto(o.Id, o.Code, o.Kind, o.Description, o.SpaceId, s.Name, o.ReservationId, r.Code, o.MemberId, m.FullName, o.InspectionId, u.Name,
               o.OccurredAt, o.Status, o.Resolution, o.ProposedChargeAmount, o.ChargeDecision, o.ChargeDecisionReason, o.ChargeId);

    public async Task<OccurrenceDto> CreateAsync(CreateOccurrenceRequest req, CancellationToken ct = default)
    {
        var memberId = req.MemberId;
        if (req.ReservationId is { } reservationId)
        {
            var reservation = await db.Set<Reservation>().Where(r => r.Id == reservationId).Select(r => new { r.MemberId, r.SpaceId }).FirstOr404Async("Reserva", ct);
            memberId ??= reservation.MemberId;
        }
        var occurrence = new Occurrence
        {
            Code = $"OC-{await db.NextSequenceAsync("occurrence", ct):D5}", Kind = req.Kind, Description = req.Description.Trim(), SpaceId = req.SpaceId,
            ReservationId = req.ReservationId, MemberId = memberId, ReportedByUserId = ctx.RequireUser(), OccurredAt = req.OccurredAt?.ToUniversalTime() ?? DateTime.UtcNow,
        };
        db.Set<Occurrence>().Add(occurrence);
        await db.SaveChangesAsync(ct);
        return await GetAsync(occurrence.Id, ct);
    }

    public async Task<OccurrenceDto> UpdateAsync(Guid id, UpdateOccurrenceRequest req, CancellationToken ct = default)
    {
        var o = await db.Set<Occurrence>().Where(x => x.Id == id).FirstOr404Async("Ocorrência", ct);
        if (req.Status is OccurrenceStatus.Resolved or OccurrenceStatus.Dismissed && string.IsNullOrWhiteSpace(req.Resolution))
            throw new DomainException("Descreva a resolução ou o motivo do arquivamento.", "ocorrencia_invalida", 400);
        if (req.Status is OccurrenceStatus.Resolved or OccurrenceStatus.Dismissed && o.ChargeDecision == ChargeDecision.Pending)
            throw DomainException.Conflict("Há uma cobrança proposta aguardando decisão administrativa.");
        db.AddHistory(ctx, nameof(Occurrence), id, o.Status, req.Status, req.Resolution);
        (o.Status, o.Resolution) = (req.Status, req.Resolution ?? o.Resolution);
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    /// <summary>Propõe a cobrança pelo dano. Nada vai ao financeiro antes da aprovação administrativa.</summary>
    public async Task<OccurrenceDto> ProposeChargeAsync(Guid id, ProposeChargeRequest req, CancellationToken ct = default)
    {
        var o = await db.Set<Occurrence>().Where(x => x.Id == id).FirstOr404Async("Ocorrência", ct);
        if (o.ChargeDecision == ChargeDecision.Approved) throw DomainException.Conflict("A cobrança desta ocorrência já foi aprovada.");
        o.MemberId = req.MemberId ?? o.MemberId ?? throw new DomainException("Informe o associado responsável pela cobrança.", "associado_obrigatorio", 400);
        o.ProposedChargeAmount = decimal.Round(req.Amount, 2);
        o.ChargeDecision = ChargeDecision.Pending;
        o.ChargeDecisionReason = null;
        if (o.Status == OccurrenceStatus.Open) o.Status = OccurrenceStatus.UnderReview;
        ctx.Justification = req.Justification;
        db.AddHistory(ctx, nameof(Occurrence), id, "Cobrança", "Proposta", $"{o.ProposedChargeAmount:C} — {req.Justification}");
        await notifications.ToStaffAsync(Permissions.OccurrencesApproveCharge, NotificationCategory.Finance, "occurrence.charge", "Cobrança por dano aguardando aprovação",
            $"{o.Code}: {o.ProposedChargeAmount:C}.", nameof(Occurrence), id, ct);
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<OccurrenceDto> DecideChargeAsync(Guid id, DecideChargeRequest req, CancellationToken ct = default)
    {
        var o = await db.Set<Occurrence>().Where(x => x.Id == id).FirstOr404Async("Ocorrência", ct);
        if (o.ChargeDecision != ChargeDecision.Pending || o.ProposedChargeAmount is not { } amount || o.MemberId is not { } memberId)
            throw DomainException.Conflict("Não há cobrança pendente de decisão nesta ocorrência.");
        o.ChargeDecision = req.Approved ? ChargeDecision.Approved : ChargeDecision.Rejected;
        o.ChargeDecisionByUserId = ctx.RequireUser();
        o.ChargeDecisionAt = DateTime.UtcNow;
        o.ChargeDecisionReason = req.Reason;
        ctx.Justification = req.Reason;
        db.AddHistory(ctx, nameof(Occurrence), id, "Cobrança proposta", req.Approved ? "Cobrança aprovada" : "Cobrança rejeitada", req.Reason);
        if (req.Approved)
        {
            var charge = charges.New(await settings.LoadAsync(ct), memberId, ChargeType.Damage, $"Cobrança por danos — ocorrência {o.Code}", amount, (await clock.TodayAsync(ct)).AddDays(30));
            charge.OccurrenceId = o.Id;
            charge.ReservationId = o.ReservationId;
            o.ChargeId = charge.Id;
            await charges.NotifyNewAsync(charge, ct);
        }
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }
}

// ---------- Entrega e devolução de chaves ----------

public record KeyHandoverDto(Guid Id, Guid ReservationId, string ReservationCode, Guid SpaceId, string SpaceName, Guid? MemberId, string? MemberName, string ReceiverName,
    string DeliveredBy, DateTime PickedUpAt, DateTime ExpectedReturnAt, string? ConditionOnDelivery, DateTime? ReturnedAt, string? ReceivedBy, string? ConditionOnReturn,
    string? Pendencies, bool PendenciesResolved, bool IsLate, int LateMinutes);
public record DeliverKeyRequest(Guid ReservationId, [Required, StringLength(150, MinimumLength = 3)] string ReceiverName, [StringLength(1000)] string? ConditionOnDelivery);
public record ReturnKeyRequest([StringLength(1000)] string? ConditionOnReturn, [StringLength(1000)] string? Pendencies);

public class KeyHandoverQuery : PageQuery
{
    public bool? OutstandingOnly { get; set; }
    public Guid? SpaceId { get; set; }
}

public class KeyHandoverService(IAppDb db, RequestContext ctx, NotificationService notifications)
{
    public async Task<PagedResult<KeyHandoverDto>> ListAsync(KeyHandoverQuery query, CancellationToken ct = default)
    {
        var q = db.Set<KeyHandover>().AsNoTracking();
        if (query.OutstandingOnly is true) q = q.Where(k => k.ReturnedAt == null || (k.Pendencies != null && !k.PendenciesResolved));
        if (query.SpaceId is not null) q = q.Where(k => k.SpaceId == query.SpaceId);
        var page = await Project(q.OrderByDescending(k => k.PickedUpAt)).ToPagedAsync(query, ct);
        return new PagedResult<KeyHandoverDto>(page.Items.Select(WithLate).ToList(), page.Total, page.Page, page.PageSize);
    }

    private async Task<KeyHandoverDto> GetAsync(Guid id, CancellationToken ct) => WithLate(await Project(db.Set<KeyHandover>().Where(k => k.Id == id)).FirstOr404Async("Entrega de chaves", ct));

    private static KeyHandoverDto WithLate(KeyHandoverDto k)
    {
        var minutes = (int)Math.Max(0, ((k.ReturnedAt ?? DateTime.UtcNow) - k.ExpectedReturnAt).TotalMinutes);
        return k with { IsLate = minutes > 0, LateMinutes = minutes };
    }

    private IQueryable<KeyHandoverDto> Project(IQueryable<KeyHandover> q)
        => from k in q
           join s in db.Set<Space>() on k.SpaceId equals s.Id
           join m in db.Set<Member>() on k.MemberId equals m.Id into members
           from m in members.DefaultIfEmpty()
           join d in db.Set<User>() on k.DeliveredByUserId equals d.Id
           join r in db.Set<User>() on k.ReceivedByUserId equals r.Id into receivers
           from r in receivers.DefaultIfEmpty()
           select new KeyHandoverDto(k.Id, k.ReservationId, k.Reservation.Code, k.SpaceId, s.Name, k.MemberId, m.FullName, k.ReceiverName, d.Name, k.PickedUpAt,
               k.ExpectedReturnAt, k.ConditionOnDelivery, k.ReturnedAt, r.Name, k.ConditionOnReturn, k.Pendencies, k.PendenciesResolved, false, 0);

    public async Task<KeyHandoverDto> DeliverAsync(DeliverKeyRequest req, CancellationToken ct = default)
    {
        var reservation = await db.Set<Reservation>().Where(r => r.Id == req.ReservationId).FirstOr404Async("Reserva", ct);
        if (reservation.Status != ReservationStatus.Approved) throw DomainException.Conflict("As chaves só podem ser entregues para reservas aprovadas.");
        if (await db.Set<KeyHandover>().AnyAsync(k => k.ReservationId == req.ReservationId && k.ReturnedAt == null, ct))
            throw DomainException.Conflict("As chaves desta reserva já foram entregues e ainda não foram devolvidas.");
        var handover = new KeyHandover
        {
            ReservationId = reservation.Id, SpaceId = reservation.SpaceId, MemberId = reservation.MemberId, ReceiverName = req.ReceiverName.Trim(),
            DeliveredByUserId = ctx.RequireUser(), PickedUpAt = DateTime.UtcNow, ExpectedReturnAt = reservation.EndAt, ConditionOnDelivery = req.ConditionOnDelivery,
        };
        db.Set<KeyHandover>().Add(handover);
        if (reservation.Attendance == AttendanceStatus.Unknown) reservation.Attendance = AttendanceStatus.Attended;
        await db.SaveChangesAsync(ct);
        return await GetAsync(handover.Id, ct);
    }

    public async Task<KeyHandoverDto> ReturnAsync(Guid id, ReturnKeyRequest req, CancellationToken ct = default)
    {
        var handover = await db.Set<KeyHandover>().Where(k => k.Id == id).FirstOr404Async("Entrega de chaves", ct);
        if (handover.ReturnedAt is not null) throw DomainException.Conflict("A devolução já foi registrada.");
        handover.ReturnedAt = DateTime.UtcNow;
        handover.ReceivedByUserId = ctx.RequireUser();
        handover.ConditionOnReturn = req.ConditionOnReturn;
        handover.Pendencies = string.IsNullOrWhiteSpace(req.Pendencies) ? null : req.Pendencies;
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<KeyHandoverDto> ResolvePendenciesAsync(Guid id, CancellationToken ct = default)
    {
        var handover = await db.Set<KeyHandover>().Where(k => k.Id == id).FirstOr404Async("Entrega de chaves", ct);
        handover.PendenciesResolved = true;
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    /// <summary>Rotina automática: avisa o associado e a equipe sobre chaves não devolvidas após o término da reserva.</summary>
    public async Task<int> NotifyOverdueAsync(CancellationToken ct = default)
    {
        var overdue = await db.Set<KeyHandover>().Include(k => k.Reservation)
            .Where(k => k.ReturnedAt == null && k.ExpectedReturnAt < DateTime.UtcNow.AddMinutes(-30)).ToListAsync(ct);
        var notified = 0;
        foreach (var k in overdue)
        {
            var already = await db.Set<Notification>().AnyAsync(n => n.Type == "key.overdue" && n.EntityId == k.Id && n.CreatedAt > DateTime.UtcNow.AddHours(-24), ct);
            if (already) continue;
            if (k.MemberId is { } memberId)
                await notifications.ToMemberAsync(memberId, NotificationCategory.Reservation, "key.overdue", "Pendência de devolução",
                    $"As chaves da reserva {k.Reservation.Code} ainda não foram devolvidas. Procure a administração.", nameof(KeyHandover), k.Id, true, ct);
            await notifications.ToStaffAsync(Permissions.InspectionsManage, NotificationCategory.Reservation, "key.overdue", "Chaves não devolvidas",
                $"Reserva {k.Reservation.Code}: devolução em atraso.", nameof(KeyHandover), k.Id, ct);
            notified++;
        }
        if (notified > 0) await db.SaveChangesAsync(ct);
        return notified;
    }
}
