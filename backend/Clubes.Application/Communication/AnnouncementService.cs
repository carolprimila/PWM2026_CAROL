using System.ComponentModel.DataAnnotations;
using Clubes.Application.Common;
using Clubes.Domain;
using Microsoft.EntityFrameworkCore;

namespace Clubes.Application.Communication;

public record AnnouncementDto(Guid Id, string Title, string Body, AnnouncementCategory Category, Priority Priority, bool AudienceMembers, bool AudienceDependents,
    bool AudienceStaff, bool IsSpecific, IReadOnlyList<Guid> TargetUserIds, Guid? ImageFileId, Guid? EventId, DateTime? PublishedAt, DateTime? ExpiresAt,
    string AuthorName, bool IsRead, int ReadCount, int Attachments, DateTime CreatedAt);

public class SaveAnnouncementRequest
{
    [Required, StringLength(150, MinimumLength = 3)] public string Title { get; set; } = "";
    [Required, StringLength(20000)] public string Body { get; set; } = "";
    public AnnouncementCategory Category { get; set; }
    public Priority Priority { get; set; }
    public bool AudienceMembers { get; set; }
    public bool AudienceDependents { get; set; }
    public bool AudienceStaff { get; set; }
    public bool IsSpecific { get; set; }
    public List<Guid> TargetUserIds { get; set; } = [];
    public Guid? ImageFileId { get; set; }
    public Guid? EventId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool Publish { get; set; }
}

public record AnnouncementReaderDto(Guid UserId, string Name, UserKind Kind, DateTime ReadAt);

public static class AnnouncementVisibility
{
    /// <summary>
    /// Comunicados publicados que o usuário pode ver. Dependentes só enxergam o que foi explicitamente
    /// destinado a dependentes (ou a eles individualmente) — nunca os comunicados dos associados.
    /// </summary>
    public static IQueryable<Announcement> VisibleTo(this IQueryable<Announcement> q, RequestContext ctx)
    {
        var now = DateTime.UtcNow;
        var userId = ctx.UserId;
        var kind = ctx.Kind;
        return q.Where(a => a.PublishedAt != null && a.PublishedAt <= now && (a.ExpiresAt == null || a.ExpiresAt > now)
            && (a.IsSpecific
                ? a.Targets.Any(t => t.UserId == userId)
                : (kind == UserKind.Member && a.AudienceMembers) || (kind == UserKind.Dependent && a.AudienceDependents)
                    || ((kind == UserKind.Staff || kind == UserKind.ClubAdmin || kind == UserKind.SuperAdmin) && a.AudienceStaff)));
    }
}

public class AnnouncementService(IAppDb db, RequestContext ctx, NotificationService notifications)
{
    public async Task<PagedResult<AnnouncementDto>> ListAsync(PageQuery page, AnnouncementCategory? category, CancellationToken ct = default)
    {
        var q = db.Set<Announcement>().AsNoTracking();
        if (category is not null) q = q.Where(a => a.Category == category);
        if (!string.IsNullOrWhiteSpace(page.Search)) { var like = page.Search.Like(); q = q.Where(a => EF.Functions.ILike(a.Title, like)); }
        return await Project(q.OrderByDescending(a => a.PublishedAt ?? a.CreatedAt)).ToPagedAsync(page, ct);
    }

    public async Task<PagedResult<AnnouncementDto>> FeedAsync(PageQuery page, AnnouncementCategory? category, CancellationToken ct = default)
    {
        var q = db.Set<Announcement>().AsNoTracking().VisibleTo(ctx);
        if (category is not null) q = q.Where(a => a.Category == category);
        return await Project(q.OrderByDescending(a => a.Priority == Priority.Urgent).ThenByDescending(a => a.PublishedAt)).ToPagedAsync(page, ct);
    }

    public async Task<AnnouncementDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var q = db.Set<Announcement>().AsNoTracking().Where(a => a.Id == id);
        if (!ctx.IsStaff) q = q.VisibleTo(ctx);
        return await Project(q).FirstOr404Async("Comunicado", ct);
    }

    private IQueryable<AnnouncementDto> Project(IQueryable<Announcement> q)
    {
        var userId = ctx.UserId;
        return q.Select(a => new AnnouncementDto(a.Id, a.Title, a.Body, a.Category, a.Priority, a.AudienceMembers, a.AudienceDependents, a.AudienceStaff, a.IsSpecific,
            a.Targets.Select(t => t.UserId).ToList(), a.ImageFileId, a.EventId, a.PublishedAt, a.ExpiresAt,
            db.Set<User>().Where(u => u.Id == a.AuthorUserId).Select(u => u.Name).FirstOrDefault() ?? "",
            db.Set<AnnouncementRead>().Any(r => r.AnnouncementId == a.Id && r.UserId == userId),
            db.Set<AnnouncementRead>().Count(r => r.AnnouncementId == a.Id),
            db.Set<Attachment>().Count(x => x.OwnerType == "Announcement" && x.OwnerId == a.Id), a.CreatedAt));
    }

    public async Task<AnnouncementDto> SaveAsync(Guid? id, SaveAnnouncementRequest req, CancellationToken ct = default)
    {
        if (req.IsSpecific && req.TargetUserIds.Count == 0) throw Invalid("Selecione ao menos um destinatário para o comunicado específico.");
        if (!req.IsSpecific && !(req.AudienceMembers || req.AudienceDependents || req.AudienceStaff)) throw Invalid("Selecione o público do comunicado.");
        if (req.ExpiresAt is { } expires && expires.ToUniversalTime() <= DateTime.UtcNow) throw Invalid("A data de expiração deve ser futura.");

        Announcement a;
        if (id is null) { a = new Announcement { AuthorUserId = ctx.RequireUser() }; db.Set<Announcement>().Add(a); }
        else a = await db.Set<Announcement>().Include(x => x.Targets).Where(x => x.Id == id).FirstOr404Async("Comunicado", ct);
        var wasPublished = a.PublishedAt is not null;

        (a.Title, a.Body, a.Category, a.Priority) = (req.Title.Trim(), req.Body, req.Category, req.Priority);
        (a.AudienceMembers, a.AudienceDependents, a.AudienceStaff, a.IsSpecific) = (req.AudienceMembers, req.AudienceDependents, req.AudienceStaff, req.IsSpecific);
        (a.ImageFileId, a.EventId, a.ExpiresAt) = (req.ImageFileId, req.EventId, req.ExpiresAt?.ToUniversalTime());

        var targets = req.IsSpecific ? req.TargetUserIds.Distinct().ToList() : [];
        if (targets.Count > 0 && await db.Set<User>().CountAsync(u => targets.Contains(u.Id), ct) != targets.Count) throw DomainException.NotFound("Destinatário");
        db.Set<AnnouncementTarget>().RemoveRange(a.Targets.Where(t => !targets.Contains(t.UserId)).ToList());
        foreach (var userId in targets.Where(t => a.Targets.All(x => x.UserId != t)))
            db.Set<AnnouncementTarget>().Add(new AnnouncementTarget { Announcement = a, UserId = userId });

        if (req.Publish && !wasPublished)
        {
            a.PublishedAt = DateTime.UtcNow;
            var category = a.Category == AnnouncementCategory.Financial ? NotificationCategory.Finance : a.Category == AnnouncementCategory.Event ? NotificationCategory.Event : NotificationCategory.General;
            var summary = a.Body.Length > 140 ? a.Body[..140] + "…" : a.Body;
            if (a.IsSpecific) await notifications.ToUsersAsync(targets, category, "announcement.published", a.Title, summary, nameof(Announcement), a.Id, false, ct);
            else await notifications.ToAudienceAsync(a.AudienceMembers, a.AudienceDependents, a.AudienceStaff, category, "announcement.published", a.Title, summary, nameof(Announcement), a.Id, ct);
        }
        await db.SaveChangesAsync(ct);
        return await GetAsync(a.Id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var a = await db.Set<Announcement>().Where(x => x.Id == id).FirstOr404Async("Comunicado", ct);
        db.Set<Announcement>().Remove(a);
        await db.SaveChangesAsync(ct);
    }

    public async Task MarkReadAsync(Guid id, CancellationToken ct = default)
    {
        var userId = ctx.RequireUser();
        if (!await db.Set<Announcement>().VisibleTo(ctx).AnyAsync(a => a.Id == id, ct)) throw DomainException.NotFound("Comunicado");
        if (await db.Set<AnnouncementRead>().AnyAsync(r => r.AnnouncementId == id && r.UserId == userId, ct)) return;
        db.Set<AnnouncementRead>().Add(new AnnouncementRead { AnnouncementId = id, UserId = userId });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { /* leitura já registrada por requisição concorrente */ }
    }

    public async Task<List<AnnouncementReaderDto>> ReadersAsync(Guid id, CancellationToken ct = default)
        => await (from r in db.Set<AnnouncementRead>()
                  join u in db.Set<User>() on r.UserId equals u.Id
                  where r.AnnouncementId == id
                  orderby r.CreatedAt descending
                  select new AnnouncementReaderDto(u.Id, u.Name, u.Kind, r.CreatedAt)).ToListAsync(ct);

    private static DomainException Invalid(string message) => new(message, "comunicado_invalido", 400);
}
