using System.ComponentModel.DataAnnotations;
using Clubes.Application.Common;
using Clubes.Application.People;
using Clubes.Domain;
using Microsoft.EntityFrameworkCore;

namespace Clubes.Application.Communication;

public record EventDto(Guid Id, string Title, string? Description, Guid? ImageFileId, DateTime StartAt, DateTime EndAt, string? Location, Guid? SpaceId,
    int? Capacity, EventAudience Audience, bool AllowMembers, bool AllowDependents, bool AllowStaff, bool RegistrationRequired, DateTime? RegistrationDeadline,
    DateTime? CancellationDeadline, EventStatus Status, string? CancelReason, int Registered, bool IsRegistered, DateTime CreatedAt);
public record EventRegistrationDto(Guid Id, Guid UserId, string PersonName, UserKind PersonKind, RegistrationStatus Status, bool? Attended, DateTime RegisteredAt);

public class SaveEventRequest
{
    [Required, StringLength(150, MinimumLength = 3)] public string Title { get; set; } = "";
    [StringLength(8000)] public string? Description { get; set; }
    public Guid? ImageFileId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    [StringLength(200)] public string? Location { get; set; }
    public Guid? SpaceId { get; set; }
    [Range(1, 100_000)] public int? Capacity { get; set; }
    public EventAudience Audience { get; set; }
    public bool AllowMembers { get; set; } = true;
    public bool AllowDependents { get; set; }
    public bool AllowStaff { get; set; }
    public bool RegistrationRequired { get; set; }
    public DateTime? RegistrationDeadline { get; set; }
    public DateTime? CancellationDeadline { get; set; }
}

public record SetEventAttendanceRequest(bool? Attended);

public class EventService(IAppDb db, RequestContext ctx, NotificationService notifications, DependentService dependents)
{
    /// <summary>Público: todos. Somente associados: apenas associados. Específico: conforme as marcações do evento.</summary>
    private IQueryable<ClubEvent> VisibleToMe(IQueryable<ClubEvent> q)
    {
        if (ctx.IsStaff) return q;
        var kind = ctx.Kind;
        return q.Where(e => e.Audience == EventAudience.Public
            || (e.Audience == EventAudience.MembersOnly && kind == UserKind.Member)
            || (e.Audience == EventAudience.Specific && ((kind == UserKind.Member && e.AllowMembers) || (kind == UserKind.Dependent && e.AllowDependents))));
    }

    public async Task<PagedResult<EventDto>> ListAsync(PageQuery page, bool upcomingOnly, EventStatus? status, CancellationToken ct = default)
    {
        var q = VisibleToMe(db.Set<ClubEvent>().AsNoTracking());
        if (!ctx.IsStaff) q = q.Where(e => e.Status != EventStatus.Cancelled || e.Registrations.Any(r => r.UserId == ctx.UserId));
        if (upcomingOnly) q = q.Where(e => e.EndAt >= DateTime.UtcNow && e.Status == EventStatus.Scheduled);
        if (status is not null) q = q.Where(e => e.Status == status);
        if (!string.IsNullOrWhiteSpace(page.Search)) { var like = page.Search.Like(); q = q.Where(e => EF.Functions.ILike(e.Title, like)); }
        q = upcomingOnly ? q.OrderBy(e => e.StartAt) : q.OrderByDescending(e => e.StartAt);
        return await Project(q).ToPagedAsync(page, ct);
    }

    public async Task<EventDto> GetAsync(Guid id, CancellationToken ct = default)
        => await Project(VisibleToMe(db.Set<ClubEvent>().AsNoTracking()).Where(e => e.Id == id)).FirstOr404Async("Evento", ct);

    private IQueryable<EventDto> Project(IQueryable<ClubEvent> q)
    {
        var userId = ctx.UserId;
        return q.Select(e => new EventDto(e.Id, e.Title, e.Description, e.ImageFileId, e.StartAt, e.EndAt, e.Location, e.SpaceId, e.Capacity, e.Audience,
            e.AllowMembers, e.AllowDependents, e.AllowStaff, e.RegistrationRequired, e.RegistrationDeadline, e.CancellationDeadline, e.Status, e.CancelReason,
            e.Registrations.Count(r => r.Status == RegistrationStatus.Registered),
            e.Registrations.Any(r => r.UserId == userId && r.Status == RegistrationStatus.Registered), e.CreatedAt));
    }

    public async Task<EventDto> SaveAsync(Guid? id, SaveEventRequest req, CancellationToken ct = default)
    {
        var start = req.StartAt.ToUniversalTime();
        var end = req.EndAt.ToUniversalTime();
        if (end <= start) throw Invalid("O término do evento deve ser posterior ao início.");
        if (req.RegistrationDeadline?.ToUniversalTime() > start) throw Invalid("O prazo de inscrição deve ser anterior ao início do evento.");
        if (req.Audience == EventAudience.Specific && !(req.AllowMembers || req.AllowDependents || req.AllowStaff)) throw Invalid("Selecione ao menos um público para o evento.");
        if (req.SpaceId is { } spaceId) await db.Set<Space>().Where(s => s.Id == spaceId).FirstOr404Async("Espaço", ct);

        ClubEvent e;
        var isNew = id is null;
        if (isNew) { e = new ClubEvent { CreatedByUserId = ctx.RequireUser() }; db.Set<ClubEvent>().Add(e); }
        else
        {
            e = await db.Set<ClubEvent>().Where(x => x.Id == id).FirstOr404Async("Evento", ct);
            if (e.Status != EventStatus.Scheduled) throw DomainException.Conflict("Somente eventos agendados podem ser alterados.");
            var registered = await db.Set<EventRegistration>().CountAsync(r => r.EventId == id && r.Status == RegistrationStatus.Registered, ct);
            if (req.Capacity < registered) throw Invalid($"A capacidade não pode ser menor que o número de inscritos ({registered}).");
        }
        var rescheduled = !isNew && (e.StartAt != start || e.EndAt != end);
        (e.Title, e.Description, e.ImageFileId, e.StartAt, e.EndAt, e.Location, e.SpaceId, e.Capacity) = (req.Title.Trim(), req.Description, req.ImageFileId, start, end, req.Location, req.SpaceId, req.Capacity);
        (e.Audience, e.AllowMembers, e.AllowDependents, e.AllowStaff) = (req.Audience, req.AllowMembers, req.AllowDependents, req.AllowStaff);
        (e.RegistrationRequired, e.RegistrationDeadline, e.CancellationDeadline) = (req.RegistrationRequired, req.RegistrationDeadline?.ToUniversalTime(), req.CancellationDeadline?.ToUniversalTime());

        if (rescheduled)
        {
            var users = await db.Set<EventRegistration>().Where(r => r.EventId == e.Id && r.Status == RegistrationStatus.Registered).Select(r => r.UserId).ToListAsync(ct);
            await notifications.ToUsersAsync(users, NotificationCategory.Event, "event.rescheduled", "Evento com nova data", $"O evento \"{e.Title}\" teve data ou horário alterados.", nameof(ClubEvent), e.Id, false, ct);
        }
        await db.SaveChangesAsync(ct);
        return await GetAsync(e.Id, ct);
    }

    public async Task<EventDto> CancelAsync(Guid id, StatusChangeRequest req, CancellationToken ct = default)
    {
        var e = await db.Set<ClubEvent>().Where(x => x.Id == id).FirstOr404Async("Evento", ct);
        if (e.Status != EventStatus.Scheduled) throw DomainException.Conflict("Somente eventos agendados podem ser cancelados.");
        e.Status = EventStatus.Cancelled;
        e.CancelReason = req.Reason;
        var users = await db.Set<EventRegistration>().Where(r => r.EventId == id && r.Status == RegistrationStatus.Registered).Select(r => r.UserId).ToListAsync(ct);
        await notifications.ToUsersAsync(users, NotificationCategory.Event, "event.cancelled", "Evento cancelado", $"O evento \"{e.Title}\" foi cancelado. Motivo: {req.Reason}", nameof(ClubEvent), id, true, ct);
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<EventDto> FinishAsync(Guid id, CancellationToken ct = default)
    {
        var e = await db.Set<ClubEvent>().Where(x => x.Id == id).FirstOr404Async("Evento", ct);
        if (e.Status != EventStatus.Scheduled) throw DomainException.Conflict("Somente eventos agendados podem ser encerrados.");
        e.Status = EventStatus.Finished;
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<EventDto> RegisterAsync(Guid id, CancellationToken ct = default)
    {
        var userId = ctx.RequireUser();
        if (ctx.Kind is not (UserKind.Member or UserKind.Dependent)) throw DomainException.Forbidden("Inscrições são feitas por associados e dependentes.");
        await dependents.EnsureDependentCanAsync(DependentPermissions.RegisterEvents, ct);

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // Bloqueia o evento: a capacidade é respeitada mesmo com inscrições simultâneas.
        await db.Database.ExecuteSqlAsync($"""SELECT 1 FROM "ClubEvent" WHERE "Id" = {id} FOR UPDATE""", ct);
        var e = await VisibleToMe(db.Set<ClubEvent>()).Where(x => x.Id == id).FirstOr404Async("Evento", ct);
        if (e.Status != EventStatus.Scheduled) throw DomainException.Conflict("Este evento não está aberto para inscrições.");
        if (!e.RegistrationRequired) throw DomainException.Conflict("Este evento não exige inscrição.");
        if ((e.RegistrationDeadline ?? e.StartAt) <= DateTime.UtcNow) throw DomainException.Conflict("O prazo de inscrição foi encerrado.");
        var registered = await db.Set<EventRegistration>().CountAsync(r => r.EventId == id && r.Status == RegistrationStatus.Registered, ct);
        if (e.Capacity is { } capacity && registered >= capacity) throw new DomainException("As vagas deste evento estão esgotadas.", "evento_lotado", 409);

        var registration = await db.Set<EventRegistration>().FirstOrDefaultAsync(r => r.EventId == id && r.UserId == userId, ct);
        if (registration is { Status: RegistrationStatus.Registered }) throw DomainException.Conflict("Você já está inscrito(a) neste evento.");
        if (registration is null) db.Set<EventRegistration>().Add(registration = new EventRegistration { EventId = id, UserId = userId, PersonName = ctx.UserName ?? "", PersonKind = ctx.Kind!.Value });
        registration.Status = RegistrationStatus.Registered;
        registration.CancelledAt = null;
        await notifications.ToUsersAsync([userId], NotificationCategory.Event, "event.registered", "Inscrição confirmada", $"Sua inscrição no evento \"{e.Title}\" foi confirmada.", nameof(ClubEvent), id, false, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<EventDto> CancelRegistrationAsync(Guid id, CancellationToken ct = default)
    {
        var userId = ctx.RequireUser();
        var registration = await db.Set<EventRegistration>().Include(r => r.Event).Where(r => r.EventId == id && r.UserId == userId && r.Status == RegistrationStatus.Registered)
            .FirstOr404Async("Inscrição", ct);
        if ((registration.Event.CancellationDeadline ?? registration.Event.StartAt) <= DateTime.UtcNow)
            throw DomainException.Conflict("O prazo para cancelar a inscrição foi encerrado.");
        registration.Status = RegistrationStatus.Cancelled;
        registration.CancelledAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<List<EventRegistrationDto>> RegistrationsAsync(Guid id, CancellationToken ct = default)
        => await db.Set<EventRegistration>().Where(r => r.EventId == id).OrderBy(r => r.PersonName)
            .Select(r => new EventRegistrationDto(r.Id, r.UserId, r.PersonName, r.PersonKind, r.Status, r.Attended, r.CreatedAt)).ToListAsync(ct);

    public async Task SetAttendanceAsync(Guid registrationId, SetEventAttendanceRequest req, CancellationToken ct = default)
    {
        var registration = await db.Set<EventRegistration>().Where(r => r.Id == registrationId).FirstOr404Async("Inscrição", ct);
        if (registration.Status != RegistrationStatus.Registered) throw DomainException.Conflict("Inscrição cancelada.");
        registration.Attended = req.Attended;
        await db.SaveChangesAsync(ct);
    }

    private static DomainException Invalid(string message) => new(message, "evento_invalido", 400);
}
