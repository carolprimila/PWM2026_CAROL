using System.ComponentModel.DataAnnotations;
using Clubes.Application.Clubs;
using Clubes.Application.Common;
using Clubes.Application.Finance;
using Clubes.Application.People;
using Clubes.Domain;
using Microsoft.EntityFrameworkCore;

namespace Clubes.Application.Spaces;

public record GuestDto(Guid Id, Guid MemberId, string MemberName, string FullName, string? Document, string? Phone, DateOnly VisitDate, Guid? SpaceId,
    string? SpaceName, Guid? ReservationId, decimal FeeAmount, Guid? ChargeId, string PaymentStatus, GuestAuthorization Authorization, string? AuthorizationNote,
    DateTime? EnteredAt, DateTime? ExitedAt, bool IsCancelled, DateTime CreatedAt);
public record GuestInput([Required, StringLength(150, MinimumLength = 3)] string FullName, [StringLength(30)] string? Document, [StringLength(30)] string? Phone);
public record CreateGuestRequest([Required, StringLength(150, MinimumLength = 3)] string FullName, [StringLength(30)] string? Document, [StringLength(30)] string? Phone,
    DateOnly VisitDate, Guid? SpaceId, Guid? MemberId);
public record AuthorizeGuestRequest(bool Authorized, [StringLength(300)] string? Note);

public class GuestQuery : PageQuery
{
    public DateOnly? Date { get; set; }
    public Guid? MemberId { get; set; }
    public GuestAuthorization? Authorization { get; set; }
    public bool? Inside { get; set; }
}

public class GuestService(IAppDb db, RequestContext ctx, SettingsService settings, ChargeService charges, DelinquencyService delinquency,
    DependentService dependents, NotificationService notifications, ClubClock clock)
{
    public async Task<PagedResult<GuestDto>> ListAsync(GuestQuery query, CancellationToken ct = default)
    {
        var q = db.Set<Guest>().AsNoTracking();
        if (!ctx.IsStaff)
        {
            var mine = await ResponsibleMember(ct);
            q = q.Where(g => g.MemberId == mine);
        }
        else if (query.MemberId is not null) q = q.Where(g => g.MemberId == query.MemberId);
        if (query.Date is not null) q = q.Where(g => g.VisitDate == query.Date);
        if (query.Authorization is not null) q = q.Where(g => g.Authorization == query.Authorization);
        if (query.Inside is true) q = q.Where(g => g.EnteredAt != null && g.ExitedAt == null);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var like = query.Search.Like();
            q = q.Where(g => EF.Functions.ILike(g.FullName, like) || EF.Functions.ILike(g.Member.FullName, like) || EF.Functions.ILike(g.Document!, like));
        }
        return await Project(q.OrderByDescending(g => g.VisitDate).ThenBy(g => g.FullName)).ToPagedAsync(query, ct);
    }

    public async Task<List<GuestDto>> ListByReservationAsync(Guid reservationId, CancellationToken ct = default)
        => await Project(db.Set<Guest>().Where(g => g.ReservationId == reservationId).OrderBy(g => g.FullName)).ToListAsync(ct);

    public async Task<GuestDto> GetAsync(Guid id, CancellationToken ct = default)
        => await Project(db.Set<Guest>().Where(g => g.Id == id)).FirstOr404Async("Convidado", ct);

    private IQueryable<GuestDto> Project(IQueryable<Guest> q)
        => from g in q
           join s in db.Set<Space>() on g.SpaceId equals s.Id into spaces
           from s in spaces.DefaultIfEmpty()
           join c in db.Set<Charge>() on g.ChargeId equals c.Id into chargeRows
           from c in chargeRows.DefaultIfEmpty()
           select new GuestDto(g.Id, g.MemberId, g.Member.FullName, g.FullName, g.Document, g.Phone, g.VisitDate, g.SpaceId, s.Name, g.ReservationId, g.FeeAmount,
               g.ChargeId, g.FeeAmount == 0 ? "Isento" : c == null ? "Pendente" : c.Status == ChargeStatus.Paid ? "Pago" : c.Status == ChargeStatus.Cancelled ? "Cancelado" : "Pendente",
               g.Authorization, g.AuthorizationNote, g.EnteredAt, g.ExitedAt, g.IsCancelled, g.CreatedAt);

    private async Task<Guid> ResponsibleMember(CancellationToken ct = default)
    {
        if (ctx.MemberId is { } memberId) return memberId;
        if (ctx.DependentId is { } dependentId)
            return await db.Set<Dependent>().Where(d => d.Id == dependentId).Select(d => d.MemberId).FirstAsync(ct);
        throw DomainException.Forbidden();
    }

    public async Task<GuestDto> CreateAsync(CreateGuestRequest req, CancellationToken ct = default)
    {
        Guid memberId;
        if (ctx.IsStaff) memberId = req.MemberId ?? throw new DomainException("Informe o associado responsável.", "associado_obrigatorio", 400);
        else
        {
            await dependents.EnsureDependentCanAsync(DependentPermissions.RegisterGuests, ct);
            memberId = await ResponsibleMember(ct);
        }
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var guest = await AddAsync(memberId, new GuestInput(req.FullName, req.Document, req.Phone), req.VisitDate, req.SpaceId, null, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return await GetAsync(guest.Id, ct);
    }

    /// <summary>
    /// Adiciona um convidado ao contexto (sem salvar). Deve ser chamado dentro de uma transação: a linha
    /// do associado é bloqueada para que o limite de convidados por dia seja respeitado sob concorrência.
    /// </summary>
    public async Task<Guest> AddAsync(Guid memberId, GuestInput input, DateOnly visitDate, Guid? spaceId, Guid? reservationId, CancellationToken ct = default)
    {
        var s = await settings.LoadAsync(ct);
        var today = await clock.TodayAsync(ct);
        if (visitDate < today) throw new DomainException("A data da visita não pode ser passada.", "data_invalida", 400);
        if (!ctx.IsStaff && s.GuestAdvanceRegistrationRequired == true && visitDate <= today)
            throw new DomainException("O clube exige cadastro antecipado de convidados. Cadastre com pelo menos um dia de antecedência ou procure a secretaria.", "cadastro_antecipado");
        if (s.GuestDocumentRequired == true && string.IsNullOrWhiteSpace(input.Document))
            throw new DomainException("Informe o documento do convidado.", "documento_obrigatorio", 400);
        if (spaceId is { } sid) await db.Set<Space>().Where(x => x.Id == sid).FirstOr404Async("Espaço", ct);

        await db.Database.ExecuteSqlAsync($"""SELECT 1 FROM "Member" WHERE "Id" = {memberId} FOR UPDATE""", ct);
        var member = await db.Set<Member>().Where(m => m.Id == memberId).Select(m => new { m.Status, m.FullName }).FirstOr404Async("Associado", ct);
        if (member.Status != MemberStatus.Active) throw DomainException.Conflict("O associado responsável não está com o cadastro ativo.");
        if ((await delinquency.GetAsync(memberId, ct)).ReservationsBlocked)
            throw new DomainException("Há pendências financeiras em aberto. Regularize a situação para cadastrar convidados.", "bloqueio_financeiro", 403);

        var pendingAdds = db.Set<Guest>().Local.Count(g => g.MemberId == memberId && g.VisitDate == visitDate && db.Set<Guest>().Entry(g).State == EntityState.Added);
        var existing = await db.Set<Guest>().CountAsync(g => g.MemberId == memberId && g.VisitDate == visitDate && !g.IsCancelled && g.Authorization != GuestAuthorization.Denied, ct);
        if (existing + pendingAdds >= s.MaxGuestsPerMember)
            throw new DomainException($"Limite de {s.MaxGuestsPerMember} convidados por associado por dia atingido.", "limite_convidados");

        var guest = new Guest
        {
            MemberId = memberId, FullName = input.FullName.Trim(), Document = input.Document, Phone = input.Phone, VisitDate = visitDate, SpaceId = spaceId,
            ReservationId = reservationId, RegisteredByUserId = ctx.RequireUser(), FeeAmount = s.GuestFee ?? 0,
            Authorization = ctx.IsStaff ? GuestAuthorization.Authorized : GuestAuthorization.Pending, AuthorizedByUserId = ctx.IsStaff ? ctx.UserId : null,
        };
        db.Set<Guest>().Add(guest);
        if (guest.FeeAmount > 0)
        {
            var charge = charges.New(s, memberId, ChargeType.Guest, $"Taxa de convidado — {guest.FullName} ({visitDate:dd/MM/yyyy})", guest.FeeAmount, visitDate);
            charge.GuestId = guest.Id;
            guest.ChargeId = charge.Id;
        }
        if (!ctx.IsStaff)
            await notifications.ToStaffAsync(Permissions.GuestsManage, NotificationCategory.General, "guest.pending", "Convidado aguardando autorização",
                $"{member.FullName} cadastrou o convidado {guest.FullName} para {visitDate:dd/MM/yyyy}.", nameof(Guest), guest.Id, ct);
        return guest;
    }

    public async Task<GuestDto> AuthorizeAsync(Guid id, AuthorizeGuestRequest req, CancellationToken ct = default)
    {
        var guest = await db.Set<Guest>().Where(g => g.Id == id).FirstOr404Async("Convidado", ct);
        if (guest.IsCancelled) throw DomainException.Conflict("Convite cancelado.");
        if (guest.EnteredAt is not null) throw DomainException.Conflict("O convidado já entrou no clube.");
        if (!req.Authorized && string.IsNullOrWhiteSpace(req.Note)) throw new DomainException("Informe o motivo da recusa.", "motivo_obrigatorio", 400);
        guest.Authorization = req.Authorized ? GuestAuthorization.Authorized : GuestAuthorization.Denied;
        guest.AuthorizationNote = req.Note;
        guest.AuthorizedByUserId = ctx.RequireUser();
        if (!req.Authorized) await CancelChargeAsync(guest, "Convidado não autorizado", ct);
        await notifications.ToMemberAsync(guest.MemberId, NotificationCategory.General, "guest.decision", req.Authorized ? "Convidado autorizado" : "Convidado não autorizado",
            req.Authorized ? $"{guest.FullName} está autorizado(a) a entrar em {guest.VisitDate:dd/MM/yyyy}." : $"{guest.FullName} não foi autorizado(a). Motivo: {req.Note}",
            nameof(Guest), id, false, ct);
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task CancelAsync(Guid id, CancellationToken ct = default)
    {
        var guest = await db.Set<Guest>().Where(g => g.Id == id).FirstOr404Async("Convidado", ct);
        if (!ctx.IsStaff)
        {
            if (guest.MemberId != await ResponsibleMember(ct)) throw DomainException.NotFound("Convidado");
            await dependents.EnsureDependentCanAsync(DependentPermissions.RegisterGuests, ct);
        }
        if (guest.EnteredAt is not null) throw DomainException.Conflict("O convidado já entrou no clube.");
        if (guest.IsCancelled) return;
        guest.IsCancelled = true;
        await CancelChargeAsync(guest, "Convite cancelado", ct);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>A taxa só é cancelada se ainda não houve pagamento; valores pagos exigem estorno pelo financeiro.</summary>
    public async Task CancelChargeAsync(Guest guest, string reason, CancellationToken ct = default)
    {
        if (guest.ChargeId is not { } chargeId) return;
        var charge = await db.Set<Charge>().FirstOrDefaultAsync(c => c.Id == chargeId, ct);
        if (charge is { Status: ChargeStatus.Open, PaidAmount: 0 })
        {
            charge.Status = ChargeStatus.Cancelled;
            charge.CancelReason = reason;
        }
    }
}
