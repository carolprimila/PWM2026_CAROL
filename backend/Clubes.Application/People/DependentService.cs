using System.ComponentModel.DataAnnotations;
using Clubes.Application.Access;
using Clubes.Application.Clubs;
using Clubes.Application.Common;
using Clubes.Domain;
using Microsoft.EntityFrameworkCore;

namespace Clubes.Application.People;

public record DependentPermissionDto(string Code, string Description, bool AllowedByAdmin, bool EnabledByMember, bool Effective);
public record DependentDto(Guid Id, Guid MemberId, string MemberName, string FullName, string? Cpf, string? Rg, DateOnly? BirthDate, string? Gender,
    string Relationship, Guid? PhotoFileId, string? Email, string? Phone, DependentStatus Status, string? StatusReason, DateTime CreatedAt,
    bool HasAccess, string? AccessEmail, IReadOnlyList<DependentPermissionDto> Permissions);

public class SaveDependentRequest
{
    [Required, StringLength(150, MinimumLength = 3)] public string FullName { get; set; } = "";
    public string? Cpf { get; set; }
    [StringLength(20)] public string? Rg { get; set; }
    public DateOnly? BirthDate { get; set; }
    [StringLength(30)] public string? Gender { get; set; }
    [Required, StringLength(50)] public string Relationship { get; set; } = "";
    public Guid? PhotoFileId { get; set; }
    [EmailAddress, StringLength(150)] public string? Email { get; set; }
    [StringLength(30)] public string? Phone { get; set; }
}

public record SetDependentPermissionRequest([Required] string Code, bool Value);

public class DependentQuery : PageQuery
{
    public DependentStatus? Status { get; set; }
    public Guid? MemberId { get; set; }
}

public class DependentService(IAppDb db, RequestContext ctx, SettingsService settings, UserService users, NotificationService notifications)
{
    private static readonly DependentStatus[] Counted = [DependentStatus.PendingApproval, DependentStatus.Active, DependentStatus.Suspended];

    public async Task<PagedResult<DependentDto>> ListAsync(DependentQuery query, CancellationToken ct = default)
    {
        var q = db.Set<Dependent>().AsNoTracking().Include(d => d.Member).Include(d => d.Permissions).AsQueryable();
        if (query.Status is not null) q = q.Where(d => d.Status == query.Status);
        if (query.MemberId is not null) q = q.Where(d => d.MemberId == query.MemberId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var like = query.Search.Like();
            q = q.Where(d => EF.Functions.ILike(d.FullName, like) || EF.Functions.ILike(d.Member.FullName, like));
        }
        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(d => d.FullName).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);
        return new PagedResult<DependentDto>(await MapAsync(items, ct), total, query.Page, query.PageSize);
    }

    public async Task<List<DependentDto>> ListMineAsync(CancellationToken ct = default)
    {
        var memberId = ctx.RequireMember();
        var items = await db.Set<Dependent>().AsNoTracking().Include(d => d.Member).Include(d => d.Permissions)
            .Where(d => d.MemberId == memberId && d.Status != DependentStatus.Removed).OrderBy(d => d.FullName).ToListAsync(ct);
        return await MapAsync(items, ct);
    }

    public async Task<DependentDto> GetAsync(Guid id, CancellationToken ct = default) => (await MapAsync([await LoadAsync(id, ct)], ct))[0];

    /// <summary>Carrega o dependente garantindo que quem consulta é da equipe, o associado responsável ou o próprio dependente.</summary>
    private async Task<Dependent> LoadAsync(Guid id, CancellationToken ct)
    {
        var dependent = await db.Set<Dependent>().Include(d => d.Member).Include(d => d.Permissions).Where(d => d.Id == id).FirstOr404Async("Dependente", ct);
        var allowed = ctx.IsStaff || ctx.MemberId == dependent.MemberId || ctx.DependentId == dependent.Id;
        if (!allowed) throw DomainException.NotFound("Dependente");
        return dependent;
    }

    public async Task<DependentDto> CreateAsync(Guid? memberId, SaveDependentRequest req, CancellationToken ct = default)
    {
        var owner = ctx.IsStaff ? memberId ?? throw new DomainException("Informe o associado responsável.", "associado_obrigatorio", 400) : ctx.RequireMember();
        var s = await settings.LoadAsync(ct);

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // Bloqueia a linha do associado: o limite de dependentes é respeitado mesmo com cadastros simultâneos.
        await db.Database.ExecuteSqlAsync($"""SELECT 1 FROM "Member" WHERE "Id" = {owner} FOR UPDATE""", ct);
        var locked = await db.Set<Member>().AsNoTracking().Where(m => m.Id == owner).FirstOr404Async("Associado", ct);
        if (locked.Status is MemberStatus.Cancelled) throw DomainException.Conflict("Não é possível cadastrar dependentes para um associado cancelado.");
        var current = await db.Set<Dependent>().CountAsync(d => d.MemberId == owner && Counted.Contains(d.Status), ct);
        if (current >= s.MaxDependentsPerMember)
            throw new DomainException($"Limite de {s.MaxDependentsPerMember} dependentes por associado atingido.", "limite_dependentes");

        var dependent = new Dependent { MemberId = owner, Status = ctx.IsStaff ? DependentStatus.Active : DependentStatus.PendingApproval };
        await ApplyAsync(dependent, req, ct);
        dependent.Permissions.AddRange(DependentPermissions.All.Select(p => new DependentPermission { Code = p.Code }));
        db.Set<Dependent>().Add(dependent);
        db.AddHistory(ctx, nameof(Dependent), dependent.Id, null, dependent.Status, ctx.IsStaff ? "Cadastrado pela administração" : "Cadastro enviado pelo associado");
        if (!ctx.IsStaff)
        {
            await notifications.ToStaffAsync(Permissions.DependentsManage, NotificationCategory.Dependent, "dependent.received", "Novo dependente para aprovação",
                $"{locked.FullName} cadastrou o dependente {dependent.FullName}.", nameof(Dependent), dependent.Id, ct);
            await notifications.ToMemberAsync(owner, NotificationCategory.Dependent, "dependent.received", "Cadastro de dependente recebido",
                $"O cadastro de {dependent.FullName} foi recebido e aguarda aprovação da administração.", nameof(Dependent), dependent.Id, false, ct);
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return await GetAsync(dependent.Id, ct);
    }

    public async Task<DependentDto> UpdateAsync(Guid id, SaveDependentRequest req, CancellationToken ct = default)
    {
        var dependent = await LoadAsync(id, ct);
        if (ctx.DependentId is not null) throw DomainException.Forbidden();
        if (dependent.Status is DependentStatus.Removed) throw DomainException.Conflict("Dependente removido não pode ser alterado.");
        await ApplyAsync(dependent, req, ct);
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public Task<DependentDto> ApproveAsync(Guid id, CancellationToken ct = default)
        => ChangeStatusAsync(id, DependentStatus.Active, null, [DependentStatus.PendingApproval], "dependent.approved", "Dependente aprovado", "O cadastro de {0} foi aprovado.", ct);

    public Task<DependentDto> RejectAsync(Guid id, StatusChangeRequest req, CancellationToken ct = default)
        => ChangeStatusAsync(id, DependentStatus.Rejected, req.Reason, [DependentStatus.PendingApproval], "dependent.rejected", "Dependente não aprovado", "O cadastro de {0} não foi aprovado. Motivo: " + req.Reason, ct);

    public Task<DependentDto> SuspendAsync(Guid id, StatusChangeRequest req, CancellationToken ct = default)
        => ChangeStatusAsync(id, DependentStatus.Suspended, req.Reason, [DependentStatus.Active], "dependent.suspended", "Dependente suspenso", "O cadastro de {0} foi suspenso pela administração.", ct);

    public Task<DependentDto> ReactivateAsync(Guid id, StatusChangeRequest req, CancellationToken ct = default)
        => ChangeStatusAsync(id, DependentStatus.Active, req.Reason, [DependentStatus.Suspended], "dependent.reactivated", "Dependente reativado", "O cadastro de {0} está ativo novamente.", ct);

    /// <summary>Remoção do vínculo (pela administração ou pelo associado responsável). Cobranças futuras passam a desconsiderá-lo.</summary>
    public async Task RemoveAsync(Guid id, StatusChangeRequest req, CancellationToken ct = default)
    {
        if (ctx.DependentId is not null) throw DomainException.Forbidden();
        await ChangeStatusAsync(id, DependentStatus.Removed, req.Reason, [DependentStatus.PendingApproval, DependentStatus.Active, DependentStatus.Suspended, DependentStatus.Rejected], null, null, null, ct);
        // O login do dependente removido é excluído (exclusão lógica) e suas sessões são encerradas.
        var logins = await db.Set<User>().Where(u => u.DependentId == id).ToListAsync(ct);
        var userIds = logins.Select(u => u.Id).ToList();
        db.Set<User>().RemoveRange(logins);
        await db.SaveChangesAsync(ct);
        await db.Set<RefreshToken>().Where(t => userIds.Contains(t.UserId) && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow).SetProperty(t => t.RevokedReason, "dependente_removido"), ct);
    }

    private async Task<DependentDto> ChangeStatusAsync(Guid id, DependentStatus to, string? reason, DependentStatus[] allowedFrom, string? type, string? title, string? body, CancellationToken ct)
    {
        var dependent = await LoadAsync(id, ct);
        if (!allowedFrom.Contains(dependent.Status)) throw DomainException.Conflict("A situação atual do dependente não permite esta operação.");
        if (to == DependentStatus.Active)
        {
            var s = await settings.LoadAsync(ct);
            var others = await db.Set<Dependent>().CountAsync(d => d.MemberId == dependent.MemberId && d.Id != id && Counted.Contains(d.Status), ct);
            if (others >= s.MaxDependentsPerMember) throw new DomainException($"Limite de {s.MaxDependentsPerMember} dependentes por associado atingido.", "limite_dependentes");
        }
        var from = dependent.Status;
        dependent.Status = to;
        dependent.StatusReason = reason;
        ctx.Justification = reason;
        db.AddHistory(ctx, nameof(Dependent), id, from, to, reason);
        if (type is not null)
        {
            var text = string.Format(body!, dependent.FullName);
            await notifications.ToMemberAsync(dependent.MemberId, NotificationCategory.Dependent, type, title!, text, nameof(Dependent), id, false, ct);
            await notifications.ToDependentAsync(id, NotificationCategory.Dependent, type, title!, text, nameof(Dependent), id, false, ct);
        }
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    /// <summary>
    /// Administração libera (AllowedByAdmin); o associado responsável habilita (EnabledByMember) quando a
    /// política do clube permite. A permissão só vale com as duas marcações.
    /// </summary>
    public async Task<DependentDto> SetPermissionAsync(Guid id, SetDependentPermissionRequest req, CancellationToken ct = default)
    {
        if (DependentPermissions.All.All(p => p.Code != req.Code)) throw new DomainException("Permissão desconhecida.", "permissao_invalida", 400);
        var dependent = await LoadAsync(id, ct);
        var permission = dependent.Permissions.FirstOrDefault(p => p.Code == req.Code);
        if (permission is null) db.Set<DependentPermission>().Add(permission = new DependentPermission { DependentId = id, Code = req.Code });

        if (ctx.IsStaff)
        {
            permission.AllowedByAdmin = req.Value;
            // Sem a etapa do associado (política do clube), a liberação da administração já torna a permissão efetiva.
            if (!(await settings.LoadAsync(ct)).MemberCanToggleDependentPermissions) permission.EnabledByMember = req.Value;
        }
        else if (ctx.MemberId == dependent.MemberId)
        {
            if (!(await settings.LoadAsync(ct)).MemberCanToggleDependentPermissions)
                throw DomainException.Forbidden("As permissões de dependentes são definidas somente pela administração do clube.");
            if (req.Value && !permission.AllowedByAdmin)
                throw DomainException.Forbidden("Esta permissão ainda não foi liberada pela administração para este dependente.");
            permission.EnabledByMember = req.Value;
        }
        else throw DomainException.Forbidden();

        var description = DependentPermissions.All.First(p => p.Code == req.Code).Description;
        await notifications.ToDependentAsync(id, NotificationCategory.Dependent, "dependent.permissions", "Permissões atualizadas",
            $"Sua permissão \"{description}\" foi {(permission.IsEffective ? "habilitada" : "desabilitada")}.", nameof(Dependent), id, false, ct);
        if (ctx.IsStaff)
            await notifications.ToMemberAsync(dependent.MemberId, NotificationCategory.Dependent, "dependent.permissions", "Permissões de dependente alteradas",
                $"A administração {(req.Value ? "liberou" : "retirou")} a permissão \"{description}\" de {dependent.FullName}.", nameof(Dependent), id, false, ct);
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<TemporaryPasswordDto> CreateAccessAsync(Guid id, CreateAccessRequest req, CancellationToken ct = default)
    {
        var dependent = await LoadAsync(id, ct);
        if (ctx.DependentId is not null) throw DomainException.Forbidden();
        if (dependent.Status != DependentStatus.Active) throw DomainException.Conflict("O acesso só pode ser criado para dependentes ativos.");
        if (await db.Set<User>().AnyAsync(u => u.DependentId == id, ct)) throw DomainException.Conflict("Este dependente já possui acesso.");
        var emailAddress = req.Email ?? dependent.Email ?? throw new DomainException("Informe um e-mail para criar o acesso do dependente.", "email_obrigatorio", 400);
        dependent.Email ??= emailAddress.Trim().ToLowerInvariant();
        return await users.CreateAccessAsync(UserKind.Dependent, dependent.FullName, emailAddress, null, dependent.Id, null, ct);
    }

    /// <summary>Validação no servidor de uma permissão individual do dependente autenticado.</summary>
    public async Task EnsureDependentCanAsync(string code, CancellationToken ct = default)
    {
        if (ctx.DependentId is not { } id) return;
        var ok = await db.Set<DependentPermission>().AnyAsync(p => p.DependentId == id && p.Code == code && p.AllowedByAdmin && p.EnabledByMember, ct);
        if (!ok) throw DomainException.Forbidden("Você não tem permissão para esta funcionalidade. Solicite ao associado responsável.");
    }

    private async Task ApplyAsync(Dependent d, SaveDependentRequest req, CancellationToken ct)
    {
        var cpf = req.Cpf.OnlyDigits();
        if (cpf.Length > 0)
        {
            if (!Cpf.IsValid(cpf)) throw new DomainException("CPF inválido.", "cpf_invalido", 400);
            if (await db.Set<Dependent>().AnyAsync(x => x.Cpf == cpf && x.Id != d.Id, ct)) throw DomainException.Conflict("Já existe um dependente com este CPF.");
        }
        if (req.BirthDate > DateOnly.FromDateTime(DateTime.UtcNow)) throw new DomainException("Data de nascimento inválida.", "data_invalida", 400);
        d.FullName = req.FullName.Trim();
        d.Cpf = cpf.Length > 0 ? cpf : null;
        d.Rg = req.Rg;
        d.BirthDate = req.BirthDate;
        d.Gender = req.Gender;
        d.Relationship = req.Relationship.Trim();
        d.PhotoFileId = req.PhotoFileId;
        d.Email = req.Email?.Trim().ToLowerInvariant();
        d.Phone = req.Phone;
    }

    private async Task<List<DependentDto>> MapAsync(List<Dependent> items, CancellationToken ct)
    {
        var ids = items.Select(d => d.Id).ToList();
        var access = await db.Set<User>().Where(u => u.DependentId != null && ids.Contains(u.DependentId.Value)).Select(u => new { u.DependentId, u.Email }).ToListAsync(ct);
        return items.Select(d =>
        {
            var email = access.FirstOrDefault(a => a.DependentId == d.Id)?.Email;
            var perms = DependentPermissions.All.Select(p =>
            {
                var row = d.Permissions.FirstOrDefault(x => x.Code == p.Code);
                return new DependentPermissionDto(p.Code, p.Description, row?.AllowedByAdmin ?? false, row?.EnabledByMember ?? false, row?.IsEffective ?? false);
            }).ToList();
            return new DependentDto(d.Id, d.MemberId, d.Member.FullName, d.FullName, d.Cpf, d.Rg, d.BirthDate, d.Gender, d.Relationship, d.PhotoFileId, d.Email, d.Phone,
                d.Status, d.StatusReason, d.CreatedAt, email is not null, email, perms);
        }).ToList();
    }
}
