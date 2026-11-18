using System.ComponentModel.DataAnnotations;
using Clubes.Application.Access;
using Clubes.Domain;
using Microsoft.EntityFrameworkCore;

namespace Clubes.Application.Common;

public static class HistoryExtensions
{
    public static void AddHistory(this IAppDb db, RequestContext ctx, string entityType, Guid entityId, object? from, object to, string? note = null)
        => db.Set<StatusHistory>().Add(new StatusHistory
        {
            EntityType = entityType, EntityId = entityId, FromStatus = from?.ToString(), ToStatus = to.ToString()!, Note = note,
            UserId = ctx.UserId, UserName = ctx.UserName,
        });
}

public record HistoryDto(Guid Id, string? FromStatus, string ToStatus, string? Note, string? UserName, DateTime At);

public class HistoryService(IAppDb db)
{
    public async Task<List<HistoryDto>> ListAsync(string entityType, Guid entityId, CancellationToken ct = default)
        => await db.Set<StatusHistory>().Where(h => h.EntityType == entityType && h.EntityId == entityId).OrderByDescending(h => h.CreatedAt)
            .Select(h => new HistoryDto(h.Id, h.FromStatus, h.ToStatus, h.Note, h.UserName, h.CreatedAt)).ToListAsync(ct);
}

// ---------- Notificações ----------

public record NotificationDto(Guid Id, NotificationCategory Category, string Type, string Title, string Body, string? EntityType, Guid? EntityId,
    bool IsMandatory, DateTime CreatedAt, DateTime? ReadAt);
public record NotificationPreferenceDto(NotificationCategory Category, bool InAppEnabled, bool PushEnabled, bool EmailEnabled);
public record RegisterDeviceRequest([Required, StringLength(500)] string Token, [Required, StringLength(20)] string Platform);
public record NotificationDeliveryDto(Guid Id, string UserName, NotificationCategory Category, string Title, DateTime CreatedAt, DateTime? ReadAt,
    DeliveryStatus PushStatus, DateTime? PushSentAt, DeliveryStatus EmailStatus, string? DeliveryError);

/// <summary>
/// Serviço central de notificações. Grava as notificações internas na mesma transação da operação
/// de negócio; o envio por push e e-mail é feito depois pelo despachante em segundo plano, que
/// registra o resultado de cada entrega.
/// </summary>
public class NotificationService(IAppDb db, RequestContext ctx)
{
    public async Task ToUsersAsync(IReadOnlyCollection<Guid> userIds, NotificationCategory category, string type, string title, string body,
        string? entityType = null, Guid? entityId = null, bool mandatory = false, CancellationToken ct = default)
    {
        if (userIds.Count == 0) return;
        var prefs = await db.Set<NotificationPreference>().Where(p => userIds.Contains(p.UserId) && p.Category == category).ToListAsync(ct);
        foreach (var userId in userIds.Distinct())
        {
            var pref = prefs.FirstOrDefault(p => p.UserId == userId);
            if (!mandatory && pref is { InAppEnabled: false }) continue;
            db.Set<Notification>().Add(new Notification
            {
                UserId = userId, Category = category, Type = type, Title = title, Body = body, EntityType = entityType, EntityId = entityId,
                IsMandatory = mandatory,
                PushStatus = mandatory || pref is null or { PushEnabled: true } ? DeliveryStatus.Pending : DeliveryStatus.NotRequested,
                EmailStatus = mandatory && (pref is null or { EmailEnabled: true }) ? DeliveryStatus.Pending : DeliveryStatus.NotRequested,
            });
        }
    }

    public async Task ToMemberAsync(Guid memberId, NotificationCategory category, string type, string title, string body,
        string? entityType = null, Guid? entityId = null, bool mandatory = false, CancellationToken ct = default)
    {
        var ids = await db.Set<User>().Where(u => u.MemberId == memberId && u.Kind == UserKind.Member).Select(u => u.Id).ToListAsync(ct);
        await ToUsersAsync(ids, category, type, title, body, entityType, entityId, mandatory, ct);
    }

    public async Task ToDependentAsync(Guid dependentId, NotificationCategory category, string type, string title, string body,
        string? entityType = null, Guid? entityId = null, bool mandatory = false, CancellationToken ct = default)
    {
        var ids = await db.Set<User>().Where(u => u.DependentId == dependentId && u.Kind == UserKind.Dependent).Select(u => u.Id).ToListAsync(ct);
        await ToUsersAsync(ids, category, type, title, body, entityType, entityId, mandatory, ct);
    }

    /// <summary>Notifica públicos inteiros do clube (associados, dependentes e/ou equipe).</summary>
    public async Task ToAudienceAsync(bool members, bool dependents, bool staff, NotificationCategory category, string type, string title, string body,
        string? entityType = null, Guid? entityId = null, CancellationToken ct = default)
    {
        var ids = await db.Set<User>().Where(u => !u.IsBlocked && ((members && u.Kind == UserKind.Member) || (dependents && u.Kind == UserKind.Dependent)
            || (staff && (u.Kind == UserKind.Staff || u.Kind == UserKind.ClubAdmin)))).Select(u => u.Id).ToListAsync(ct);
        await ToUsersAsync(ids, category, type, title, body, entityType, entityId, false, ct);
    }

    /// <summary>Notifica administradores do clube e funcionários que possuam a permissão informada.</summary>
    public async Task ToStaffAsync(string permission, NotificationCategory category, string type, string title, string body,
        string? entityType = null, Guid? entityId = null, CancellationToken ct = default)
    {
        var ids = await db.Set<User>().Where(u => !u.IsBlocked && (u.Kind == UserKind.ClubAdmin || (u.Kind == UserKind.Staff
                && !u.Permissions.Any(p => p.Permission == permission && !p.Granted)
                && (u.Permissions.Any(p => p.Permission == permission && p.Granted) || u.Roles.Any(r => r.Role.Permissions.Any(p => p.Permission == permission))))))
            .Select(u => u.Id).ToListAsync(ct);
        await ToUsersAsync(ids, category, type, title, body, entityType, entityId, false, ct);
    }

    public async Task<PagedResult<NotificationDto>> MineAsync(PageQuery page, bool unreadOnly, CancellationToken ct = default)
    {
        var id = ctx.RequireUser();
        var q = db.Set<Notification>().Where(n => n.UserId == id);
        if (unreadOnly) q = q.Where(n => n.ReadAt == null);
        return await q.OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto(n.Id, n.Category, n.Type, n.Title, n.Body, n.EntityType, n.EntityId, n.IsMandatory, n.CreatedAt, n.ReadAt)).ToPagedAsync(page, ct);
    }

    public async Task<int> UnreadCountAsync(CancellationToken ct = default)
    {
        var id = ctx.RequireUser();
        return await db.Set<Notification>().CountAsync(n => n.UserId == id && n.ReadAt == null, ct);
    }

    public async Task MarkReadAsync(Guid? notificationId, CancellationToken ct = default)
    {
        var id = ctx.RequireUser();
        await db.Set<Notification>().Where(n => n.UserId == id && n.ReadAt == null && (notificationId == null || n.Id == notificationId))
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, DateTime.UtcNow), ct);
    }

    public async Task RegisterDeviceAsync(RegisterDeviceRequest req, CancellationToken ct = default)
    {
        var id = ctx.RequireUser();
        var existing = await db.Set<DeviceToken>().IgnoreQueryFilters().FirstOrDefaultAsync(d => d.Token == req.Token, ct);
        if (existing is not null && existing.ClubId != ctx.RequireClub())
        {
            // O aparelho passou a ser usado por um usuário de outro clube: o registro antigo é descartado.
            await db.Set<DeviceToken>().IgnoreQueryFilters().Where(d => d.Id == existing.Id).ExecuteDeleteAsync(ct);
            existing = null;
        }
        if (existing is null) db.Set<DeviceToken>().Add(new DeviceToken { UserId = id, Token = req.Token, Platform = req.Platform });
        else { existing.UserId = id; existing.Platform = req.Platform; existing.LastSeenAt = DateTime.UtcNow; }
        await db.SaveChangesAsync(ct);
    }

    public async Task UnregisterDeviceAsync(string token, CancellationToken ct = default)
    {
        var id = ctx.RequireUser();
        await db.Set<DeviceToken>().Where(d => d.Token == token && d.UserId == id).ExecuteDeleteAsync(ct);
    }

    public async Task<List<NotificationPreferenceDto>> PreferencesAsync(CancellationToken ct = default)
    {
        var id = ctx.RequireUser();
        var saved = await db.Set<NotificationPreference>().Where(p => p.UserId == id).ToListAsync(ct);
        return Enum.GetValues<NotificationCategory>().Select(c =>
        {
            var p = saved.FirstOrDefault(x => x.Category == c);
            return new NotificationPreferenceDto(c, p?.InAppEnabled ?? true, p?.PushEnabled ?? true, p?.EmailEnabled ?? true);
        }).ToList();
    }

    public async Task<List<NotificationPreferenceDto>> SetPreferencesAsync(List<NotificationPreferenceDto> prefs, CancellationToken ct = default)
    {
        var id = ctx.RequireUser();
        var saved = await db.Set<NotificationPreference>().Where(p => p.UserId == id).ToListAsync(ct);
        foreach (var dto in prefs.DistinctBy(p => p.Category))
        {
            var row = saved.FirstOrDefault(p => p.Category == dto.Category);
            if (row is null) db.Set<NotificationPreference>().Add(row = new NotificationPreference { UserId = id, Category = dto.Category });
            (row.InAppEnabled, row.PushEnabled, row.EmailEnabled) = (dto.InAppEnabled, dto.PushEnabled, dto.EmailEnabled);
        }
        await db.SaveChangesAsync(ct);
        return await PreferencesAsync(ct);
    }

    public async Task<PagedResult<NotificationDeliveryDto>> DeliveriesAsync(PageQuery page, CancellationToken ct = default)
        => await (from n in db.Set<Notification>()
                  join u in db.Set<User>() on n.UserId equals u.Id
                  orderby n.CreatedAt descending
                  select new NotificationDeliveryDto(n.Id, u.Name, n.Category, n.Title, n.CreatedAt, n.ReadAt, n.PushStatus, n.PushSentAt, n.EmailStatus, n.DeliveryError))
            .ToPagedAsync(page, ct);
}

// ---------- Arquivos e anexos ----------

public record FileDto(Guid Id, string FileName, string ContentType, long Size, FileScope Scope);
public record AttachmentDto(Guid Id, Guid FileId, string FileName, string ContentType, long Size, string? Title, string? Category, DateTime CreatedAt);
public record AddAttachmentRequest([Required] string OwnerType, Guid OwnerId, Guid FileId, [StringLength(150)] string? Title, [StringLength(80)] string? Category);

public class FileService(IAppDb db, RequestContext ctx, IFileStorage storage, PermissionService permissions)
{
    private const long MaxSize = 10 * 1024 * 1024;
    private static readonly Dictionary<string, string[]> Allowed = new()
    {
        ["image/jpeg"] = [".jpg", ".jpeg"], ["image/png"] = [".png"], ["image/webp"] = [".webp"], ["application/pdf"] = [".pdf"],
    };

    public async Task<FileDto> UploadAsync(Stream content, string fileName, string contentType, long size, FileScope scope, Guid? ownerMemberId, CancellationToken ct = default)
    {
        var userId = ctx.RequireUser();
        var clubId = ctx.RequireClub();
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (!Allowed.TryGetValue(contentType.ToLowerInvariant(), out var exts) || !exts.Contains(ext))
            throw new DomainException("Tipo de arquivo não permitido. Envie imagens (JPG, PNG, WEBP) ou PDF.", "arquivo_invalido", 400);
        if (size is <= 0 or > MaxSize) throw new DomainException("O arquivo deve ter até 10 MB.", "arquivo_invalido", 400);

        // Quem não é da equipe só envia arquivos vinculados ao próprio cadastro.
        if (!ctx.IsStaff)
        {
            if (scope == FileScope.ClubPublic) scope = FileScope.Staff;
            ownerMemberId = ctx.MemberId ?? await db.Set<Dependent>().Where(d => d.Id == ctx.DependentId).Select(d => (Guid?)d.MemberId).FirstOrDefaultAsync(ct);
        }
        else if (ownerMemberId is { } owner && !await db.Set<Member>().AnyAsync(m => m.Id == owner, ct)) throw DomainException.NotFound("Associado");

        var safeName = Path.GetFileName(fileName);
        var path = await storage.SaveAsync(clubId, safeName, content, ct);
        var file = new StoredFile { FileName = safeName, ContentType = contentType.ToLowerInvariant(), Size = size, StoragePath = path, Scope = scope, UploadedByUserId = userId, OwnerMemberId = ownerMemberId };
        db.Set<StoredFile>().Add(file);
        await db.SaveChangesAsync(ct);
        return new FileDto(file.Id, file.FileName, file.ContentType, file.Size, file.Scope);
    }

    public async Task<(StoredFile File, Stream Content)> OpenAsync(Guid id, CancellationToken ct = default)
    {
        var userId = ctx.RequireUser();
        var file = await db.Set<StoredFile>().AsNoTracking().Where(f => f.Id == id).FirstOr404Async("Arquivo", ct);
        var allowed = file.Scope == FileScope.ClubPublic || file.UploadedByUserId == userId;
        if (!allowed && ctx.IsStaff) allowed = file.Scope == FileScope.Staff || await permissions.HasAsync(Permissions.DocumentsView, ct);
        if (!allowed && file.OwnerMemberId is { } owner)
        {
            var myMember = ctx.MemberId ?? (file.Scope == FileScope.Staff
                ? await db.Set<Dependent>().Where(d => d.Id == ctx.DependentId).Select(d => (Guid?)d.MemberId).FirstOrDefaultAsync(ct)
                : null);
            allowed = myMember == owner;
        }
        // Responde 404 (e não 403) para não confirmar a existência do arquivo.
        if (!allowed) throw DomainException.NotFound("Arquivo");
        return (file, await storage.OpenAsync(file.StoragePath, ct));
    }
}

public class AttachmentService(IAppDb db, RequestContext ctx, PermissionService permissions)
{
    /// <summary>Tipo de dono → (permissão para ler, permissão para alterar, leitura liberada a associados).</summary>
    private static readonly Dictionary<string, (string Read, string Write, bool OpenToMembers)> Policies = new()
    {
        ["Member"] = (Permissions.DocumentsView, Permissions.MembersManage, false),
        ["Dependent"] = (Permissions.DocumentsView, Permissions.DependentsManage, false),
        ["Employee"] = (Permissions.EmployeesView, Permissions.EmployeesManage, false),
        ["Assembly"] = (Permissions.AssembliesView, Permissions.AssembliesManage, true),
        ["Announcement"] = (Permissions.AnnouncementsManage, Permissions.AnnouncementsManage, true),
        ["MaintenanceOrder"] = (Permissions.MaintenanceView, Permissions.MaintenanceManage, false),
        ["Inspection"] = (Permissions.InspectionsView, Permissions.InspectionsManage, false),
        ["Occurrence"] = (Permissions.OccurrencesView, Permissions.OccurrencesManage, false),
        ["Asset"] = (Permissions.AssetsView, Permissions.AssetsManage, false),
        ["FinancialEntry"] = (Permissions.FinanceView, Permissions.FinanceEntries, false),
    };

    private async Task EnsureAsync(string ownerType, Guid ownerId, bool write, CancellationToken ct)
    {
        if (!Policies.TryGetValue(ownerType, out var policy)) throw new DomainException("Tipo de anexo desconhecido.", "anexo_invalido", 400);
        if (ctx.IsStaff)
        {
            await permissions.EnsureAsync(write ? policy.Write : policy.Read, ct);
            return;
        }
        // Associado: lê e envia os próprios documentos e os de seus dependentes; lê anexos públicos (assembleias).
        var mine = ownerType switch
        {
            "Member" => ctx.MemberId == ownerId,
            "Dependent" => ctx.MemberId is { } m && await db.Set<Dependent>().AnyAsync(d => d.Id == ownerId && d.MemberId == m, ct),
            _ => false,
        };
        if (mine) return;
        if (!write && policy.OpenToMembers)
        {
            // Anexos de comunicados seguem a segmentação do comunicado; documentos de assembleia são restritos a associados.
            if (ownerType == "Announcement" && await Communication.AnnouncementVisibility.VisibleTo(db.Set<Announcement>(), ctx).AnyAsync(a => a.Id == ownerId, ct)) return;
            if (ownerType == "Assembly" && ctx.Kind == UserKind.Member) return;
        }
        throw DomainException.Forbidden();
    }

    public async Task<List<AttachmentDto>> ListAsync(string ownerType, Guid ownerId, CancellationToken ct = default)
    {
        await EnsureAsync(ownerType, ownerId, false, ct);
        return await db.Set<Attachment>().Where(a => a.OwnerType == ownerType && a.OwnerId == ownerId).OrderBy(a => a.CreatedAt)
            .Select(a => new AttachmentDto(a.Id, a.FileId, a.File.FileName, a.File.ContentType, a.File.Size, a.Title, a.Category, a.CreatedAt)).ToListAsync(ct);
    }

    public async Task<AttachmentDto> AddAsync(AddAttachmentRequest req, CancellationToken ct = default)
    {
        await EnsureAsync(req.OwnerType, req.OwnerId, true, ct);
        var file = await db.Set<StoredFile>().Where(f => f.Id == req.FileId).FirstOr404Async("Arquivo", ct);
        var attachment = new Attachment { OwnerType = req.OwnerType, OwnerId = req.OwnerId, FileId = file.Id, Title = req.Title, Category = req.Category, UploadedByUserId = ctx.RequireUser() };
        db.Set<Attachment>().Add(attachment);
        await db.SaveChangesAsync(ct);
        return new AttachmentDto(attachment.Id, file.Id, file.FileName, file.ContentType, file.Size, attachment.Title, attachment.Category, attachment.CreatedAt);
    }

    public async Task RemoveAsync(Guid id, CancellationToken ct = default)
    {
        var attachment = await db.Set<Attachment>().Where(a => a.Id == id).FirstOr404Async("Anexo", ct);
        await EnsureAsync(attachment.OwnerType, attachment.OwnerId, true, ct);
        db.Set<Attachment>().Remove(attachment);
        await db.SaveChangesAsync(ct);
    }
}
