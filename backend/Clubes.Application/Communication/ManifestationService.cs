using System.ComponentModel.DataAnnotations;
using Clubes.Application.Common;
using Clubes.Domain;
using Microsoft.EntityFrameworkCore;

namespace Clubes.Application.Communication;

public record ManifestationTypeDto(Guid Id, string Name, bool IsSuggestion, bool IsActive);
public record SaveManifestationTypeRequest([Required, StringLength(60, MinimumLength = 2)] string Name, bool IsSuggestion, bool IsActive);
public record ManifestationUpdateDto(Guid Id, ManifestationStatus Status, string? Message, bool IsVisibleToAuthor, string UserName, DateTime At);
public record ManifestationDto(Guid Id, string Protocol, Guid TypeId, string TypeName, bool IsSuggestion, string Description, Guid? PhotoFileId, string AuthorName,
    UserKind AuthorKind, Guid? MemberId, ManifestationStatus Status, string? Classification, Guid? AssemblyId, DateTime CreatedAt, DateTime? ClosedAt);
public record ManifestationDetailDto(ManifestationDto Manifestation, IReadOnlyList<ManifestationUpdateDto> Updates);
public record CreateManifestationRequest(Guid TypeId, [Required, StringLength(4000, MinimumLength = 10)] string Description, Guid? PhotoFileId);
public record UpdateManifestationRequest(ManifestationStatus Status, [StringLength(2000)] string? Message, [StringLength(80)] string? Classification, Guid? AssemblyId);
public record RespondManifestationRequest([Required, StringLength(2000, MinimumLength = 2)] string Message, bool IsVisibleToAuthor = true);

public class ManifestationQuery : PageQuery
{
    public ManifestationStatus? Status { get; set; }
    public Guid? TypeId { get; set; }
    public bool? OpenOnly { get; set; }
}

public class ManifestationService(IAppDb db, RequestContext ctx, NotificationService notifications)
{
    private static readonly ManifestationStatus[] Final = [ManifestationStatus.Resolved, ManifestationStatus.Refused];

    public async Task<List<ManifestationTypeDto>> TypesAsync(bool includeInactive, CancellationToken ct = default)
        => await db.Set<ManifestationType>().Where(t => includeInactive || t.IsActive).OrderBy(t => t.Name)
            .Select(t => new ManifestationTypeDto(t.Id, t.Name, t.IsSuggestion, t.IsActive)).ToListAsync(ct);

    public async Task<ManifestationTypeDto> SaveTypeAsync(Guid? id, SaveManifestationTypeRequest req, CancellationToken ct = default)
    {
        ManifestationType type;
        if (id is null) { type = new ManifestationType(); db.Set<ManifestationType>().Add(type); }
        else type = await db.Set<ManifestationType>().Where(t => t.Id == id).FirstOr404Async("Tipo de manifestação", ct);
        if (await db.Set<ManifestationType>().AnyAsync(t => t.Name == req.Name.Trim() && t.Id != type.Id, ct)) throw DomainException.Conflict("Já existe um tipo com este nome.");
        (type.Name, type.IsSuggestion, type.IsActive) = (req.Name.Trim(), req.IsSuggestion, req.IsActive);
        await db.SaveChangesAsync(ct);
        return new ManifestationTypeDto(type.Id, type.Name, type.IsSuggestion, type.IsActive);
    }

    public async Task<PagedResult<ManifestationDto>> ListAsync(ManifestationQuery query, CancellationToken ct = default)
    {
        var q = db.Set<Manifestation>().AsNoTracking();
        if (!ctx.IsStaff) q = q.Where(m => m.UserId == ctx.UserId);
        if (query.Status is not null) q = q.Where(m => m.Status == query.Status);
        if (query.TypeId is not null) q = q.Where(m => m.TypeId == query.TypeId);
        if (query.OpenOnly is true) q = q.Where(m => !Final.Contains(m.Status));
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var like = query.Search.Like();
            q = q.Where(m => EF.Functions.ILike(m.Protocol, like) || EF.Functions.ILike(m.AuthorName, like) || EF.Functions.ILike(m.Description, like));
        }
        return await Project(q.OrderByDescending(m => m.CreatedAt)).ToPagedAsync(query, ct);
    }

    public async Task<ManifestationDetailDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var q = db.Set<Manifestation>().AsNoTracking().Where(m => m.Id == id);
        if (!ctx.IsStaff) q = q.Where(m => m.UserId == ctx.UserId);
        var dto = await Project(q).FirstOr404Async("Manifestação", ct);
        var updates = await db.Set<ManifestationUpdate>().Where(u => u.ManifestationId == id && (ctx.IsStaff || u.IsVisibleToAuthor)).OrderBy(u => u.CreatedAt)
            .Select(u => new ManifestationUpdateDto(u.Id, u.Status, u.Message, u.IsVisibleToAuthor, u.UserName, u.CreatedAt)).ToListAsync(ct);
        return new ManifestationDetailDto(dto, updates);
    }

    private static IQueryable<ManifestationDto> Project(IQueryable<Manifestation> q)
        => q.Select(m => new ManifestationDto(m.Id, m.Protocol, m.TypeId, m.Type.Name, m.Type.IsSuggestion, m.Description, m.PhotoFileId, m.AuthorName, m.AuthorKind,
            m.MemberId, m.Status, m.Classification, m.AssemblyId, m.CreatedAt, m.ClosedAt));

    public async Task<ManifestationDetailDto> CreateAsync(CreateManifestationRequest req, CancellationToken ct = default)
    {
        var userId = ctx.RequireUser();
        if (ctx.Kind is not (UserKind.Member or UserKind.Dependent)) throw DomainException.Forbidden("Manifestações são enviadas por associados e dependentes.");
        var type = await db.Set<ManifestationType>().Where(t => t.Id == req.TypeId && t.IsActive).FirstOr404Async("Tipo de manifestação", ct);
        var memberId = ctx.MemberId ?? await db.Set<Dependent>().Where(d => d.Id == ctx.DependentId).Select(d => (Guid?)d.MemberId).FirstOrDefaultAsync(ct);

        var manifestation = new Manifestation
        {
            Protocol = $"{DateTime.UtcNow:yyyy}{await db.NextSequenceAsync("manifestation", ct):D6}", TypeId = type.Id, Description = req.Description.Trim(),
            PhotoFileId = req.PhotoFileId, UserId = userId, AuthorName = ctx.UserName ?? "", AuthorKind = ctx.Kind.Value, MemberId = memberId,
        };
        manifestation.Updates.Add(new ManifestationUpdate { Status = ManifestationStatus.Received, Message = "Manifestação registrada.", UserId = userId, UserName = ctx.UserName ?? "" });
        db.Set<Manifestation>().Add(manifestation);
        await notifications.ToUsersAsync([userId], NotificationCategory.Support, "manifestation.received", "Manifestação recebida",
            $"Sua {type.Name.ToLowerInvariant()} foi registrada com o protocolo {manifestation.Protocol}.", nameof(Manifestation), manifestation.Id, false, ct);
        await notifications.ToStaffAsync(Permissions.ManifestationsManage, NotificationCategory.Support, "manifestation.new", $"Nova manifestação: {type.Name}",
            $"Protocolo {manifestation.Protocol}, enviada por {manifestation.AuthorName}.", nameof(Manifestation), manifestation.Id, ct);
        await db.SaveChangesAsync(ct);
        return await GetAsync(manifestation.Id, ct);
    }

    /// <summary>Atualiza o andamento e notifica o autor conforme a nova situação.</summary>
    public async Task<ManifestationDetailDto> UpdateStatusAsync(Guid id, UpdateManifestationRequest req, CancellationToken ct = default)
    {
        var m = await db.Set<Manifestation>().Include(x => x.Type).Where(x => x.Id == id).FirstOr404Async("Manifestação", ct);
        if (Final.Contains(m.Status)) throw DomainException.Conflict("Esta manifestação já foi encerrada.");
        if (req.Status == ManifestationStatus.Received) throw Invalid("Situação inválida.");
        if (req.Status is ManifestationStatus.Accepted or ManifestationStatus.ForwardedToAssembly && !m.Type.IsSuggestion)
            throw Invalid("Somente sugestões podem ser aceitas e encaminhadas à assembleia.");
        if (req.Status is ManifestationStatus.Refused or ManifestationStatus.Resolved && string.IsNullOrWhiteSpace(req.Message))
            throw Invalid("Informe a resposta ao autor da manifestação.");
        if (req.AssemblyId is { } assemblyId) await db.Set<Assembly>().Where(a => a.Id == assemblyId).FirstOr404Async("Assembleia", ct);

        m.Status = req.Status;
        if (req.Classification is not null) m.Classification = req.Classification;
        if (req.AssemblyId is not null) m.AssemblyId = req.AssemblyId;
        if (Final.Contains(req.Status)) m.ClosedAt = DateTime.UtcNow;
        db.Set<ManifestationUpdate>().Add(new ManifestationUpdate { ManifestationId = id, Status = req.Status, Message = req.Message, UserId = ctx.RequireUser(), UserName = ctx.UserName ?? "" });

        var (type, title, body) = req.Status switch
        {
            ManifestationStatus.UnderReview => ("manifestation.review", "Manifestação em análise", $"Sua manifestação {m.Protocol} está sendo analisada pela administração."),
            ManifestationStatus.InProgress => ("manifestation.progress", "Manifestação em andamento", $"Sua manifestação {m.Protocol} está sendo resolvida."),
            ManifestationStatus.Accepted => ("manifestation.accepted", "Sugestão aceita", $"Sua sugestão {m.Protocol} foi aceita e será levada à assembleia."),
            ManifestationStatus.ForwardedToAssembly => ("manifestation.assembly", "Sugestão encaminhada à assembleia", $"Sua sugestão {m.Protocol} foi encaminhada à assembleia."),
            ManifestationStatus.Resolved => ("manifestation.resolved", "Manifestação resolvida", $"Sua manifestação {m.Protocol} foi resolvida."),
            _ => ("manifestation.refused", "Manifestação não acolhida", $"Sua manifestação {m.Protocol} não foi acolhida."),
        };
        if (!string.IsNullOrWhiteSpace(req.Message)) body += $" Resposta: {req.Message}";
        await notifications.ToUsersAsync([m.UserId], NotificationCategory.Support, type, title, body, nameof(Manifestation), id, false, ct);
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<ManifestationDetailDto> RespondAsync(Guid id, RespondManifestationRequest req, CancellationToken ct = default)
    {
        var m = await db.Set<Manifestation>().Where(x => x.Id == id).FirstOr404Async("Manifestação", ct);
        db.Set<ManifestationUpdate>().Add(new ManifestationUpdate
        {
            ManifestationId = id, Status = m.Status, Message = req.Message, IsVisibleToAuthor = req.IsVisibleToAuthor, UserId = ctx.RequireUser(), UserName = ctx.UserName ?? "",
        });
        if (req.IsVisibleToAuthor)
            await notifications.ToUsersAsync([m.UserId], NotificationCategory.Support, "manifestation.reply", "Nova resposta da administração",
                $"Manifestação {m.Protocol}: {req.Message}", nameof(Manifestation), id, false, ct);
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    private static DomainException Invalid(string message) => new(message, "manifestacao_invalida", 400);
}
