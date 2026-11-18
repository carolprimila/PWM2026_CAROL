using System.ComponentModel.DataAnnotations;
using Clubes.Application.Common;
using Clubes.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Clubes.Application.Access;

public record UserDto(Guid Id, string Name, string Email, UserKind Kind, bool IsBlocked, string? BlockReason, DateTime? LastLoginAt,
    Guid? MemberId, Guid? DependentId, Guid? EmployeeId, IReadOnlyList<RoleRef> Roles, IReadOnlyList<PermissionOverride> Overrides);
public record RoleRef(Guid Id, string Name);
public record PermissionOverride([Required] string Permission, bool Granted);
public record CreateUserRequest([Required, StringLength(150)] string Name, [Required, EmailAddress] string Email, UserKind Kind, Guid? EmployeeId, List<Guid>? RoleIds);
public record UpdateUserRequest([Required, StringLength(150)] string Name, [Required, EmailAddress] string Email);
public record SetRolesRequest([Required] List<Guid> RoleIds);
public record SetOverridesRequest([Required] List<PermissionOverride> Overrides);
public record BlockRequest([Required, StringLength(300)] string Reason);
public record TemporaryPasswordDto(Guid UserId, string Email, string TemporaryPassword, bool EmailSent);
public record LoginHistoryDto(Guid Id, string Email, Guid? UserId, bool Success, string? Reason, string? Ip, string? UserAgent, DateTime At);
public record RoleDto(Guid Id, string Name, string? Description, bool IsSystem, IReadOnlyList<string> Permissions, int Users);
public record SaveRoleRequest([Required, StringLength(80)] string Name, [StringLength(300)] string? Description, [Required] List<string> Permissions);
public record PermissionInfoDto(string Code, string Group, string Description);

public class UserService(IAppDb db, RequestContext ctx, IPasswordHasher<User> hasher, AuthService auth, IEmailSender email, PermissionService permissions)
{
    public async Task<PagedResult<UserDto>> ListAsync(PageQuery page, UserKind? kind, CancellationToken ct = default)
    {
        var q = db.Set<User>().AsNoTracking();
        if (kind is not null) q = q.Where(u => u.Kind == kind);
        if (!string.IsNullOrWhiteSpace(page.Search))
        {
            var like = page.Search.Like();
            q = q.Where(u => EF.Functions.ILike(u.Name, like) || EF.Functions.ILike(u.Email, like));
        }
        var total = await q.CountAsync(ct);
        var users = await q.OrderBy(u => u.Name).Skip((page.Page - 1) * page.PageSize).Take(page.PageSize)
            .Include(u => u.Roles).ThenInclude(r => r.Role).Include(u => u.Permissions).AsSplitQuery().ToListAsync(ct);
        return new PagedResult<UserDto>(users.Select(Map).ToList(), total, page.Page, page.PageSize);
    }

    public async Task<UserDto> GetAsync(Guid id, CancellationToken ct = default) => Map(await LoadAsync(id, ct));

    private async Task<User> LoadAsync(Guid id, CancellationToken ct)
        => await db.Set<User>().Include(u => u.Roles).ThenInclude(r => r.Role).Include(u => u.Permissions).AsSplitQuery()
            .Where(u => u.Id == id).FirstOr404Async("Usuário", ct);

    private static UserDto Map(User u) => new(u.Id, u.Name, u.Email, u.Kind, u.IsBlocked, u.BlockReason, u.LastLoginAt, u.MemberId, u.DependentId, u.EmployeeId,
        u.Roles.Select(r => new RoleRef(r.RoleId, r.Role.Name)).OrderBy(r => r.Name).ToList(),
        u.Permissions.Select(p => new PermissionOverride(p.Permission, p.Granted)).ToList());

    public async Task<TemporaryPasswordDto> CreateStaffAsync(CreateUserRequest req, CancellationToken ct = default)
    {
        if (req.Kind is not (UserKind.ClubAdmin or UserKind.Staff))
            throw new DomainException("Por aqui só é possível criar usuários administradores ou funcionários.", "tipo_invalido", 400);
        if (req.Kind == UserKind.ClubAdmin && !ctx.IsAdmin) throw DomainException.Forbidden("Somente administradores podem criar outros administradores.");
        if (req.EmployeeId is { } employeeId)
        {
            await db.Set<Employee>().Where(e => e.Id == employeeId).FirstOr404Async("Funcionário", ct);
            if (await db.Set<User>().AnyAsync(u => u.EmployeeId == employeeId, ct)) throw DomainException.Conflict("Este funcionário já possui usuário de acesso.");
        }
        // Atribuir funções exige a mesma permissão na criação e na edição.
        if (req.RoleIds is { Count: > 0 }) await permissions.EnsureAsync(Permissions.RolesManage, ct);
        var result = await CreateAccessAsync(req.Kind, req.Name, req.Email, null, null, req.EmployeeId, ct);
        if (req.RoleIds is { Count: > 0 }) await SetRolesAsync(result.UserId, new SetRolesRequest(req.RoleIds), ct);
        return result;
    }

    /// <summary>Cria o login de uma pessoa (funcionário, associado ou dependente) com senha temporária de troca obrigatória.</summary>
    public async Task<TemporaryPasswordDto> CreateAccessAsync(UserKind kind, string name, string emailAddress, Guid? memberId, Guid? dependentId, Guid? employeeId, CancellationToken ct = default)
    {
        var clubId = ctx.RequireClub();
        var normalized = emailAddress.Trim().ToLowerInvariant();
        if (await db.Set<User>().IgnoreQueryFilters().AnyAsync(u => u.Email == normalized && !u.IsDeleted, ct))
            throw DomainException.Conflict("Já existe um usuário com este e-mail.");

        var password = PasswordPolicy.GenerateTemporary();
        var user = new User { ClubId = clubId, Name = name.Trim(), Email = normalized, Kind = kind, MemberId = memberId, DependentId = dependentId, EmployeeId = employeeId, MustChangePassword = true };
        user.PasswordHash = hasher.HashPassword(user, password);
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(ct);
        return new TemporaryPasswordDto(user.Id, user.Email, password, await SendCredentialsAsync(user, password, ct));
    }

    private async Task<bool> SendCredentialsAsync(User user, string password, CancellationToken ct)
    {
        if (!email.IsConfigured) return false;
        try
        {
            await email.SendAsync(new EmailMessage(user.Email, "Seu acesso ao clube",
                $"Olá, {user.Name}.\n\nSeu acesso foi criado.\nLogin: {user.Email}\nSenha temporária: {password}\n\nVocê deverá definir uma nova senha no primeiro acesso."), ct);
            return true;
        }
        catch { return false; }
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest req, CancellationToken ct = default)
    {
        var user = await LoadAsync(id, ct);
        EnsureCanManage(user);
        var normalized = req.Email.Trim().ToLowerInvariant();
        if (normalized != user.Email && await db.Set<User>().IgnoreQueryFilters().AnyAsync(u => u.Email == normalized && !u.IsDeleted, ct))
            throw DomainException.Conflict("Já existe um usuário com este e-mail.");
        user.Name = req.Name.Trim();
        user.Email = normalized;
        await db.SaveChangesAsync(ct);
        return Map(user);
    }

    public async Task<UserDto> SetRolesAsync(Guid id, SetRolesRequest req, CancellationToken ct = default)
    {
        var user = await LoadAsync(id, ct);
        EnsureCanManage(user);
        if (user.Kind != UserKind.Staff) throw new DomainException("Funções só se aplicam a usuários funcionários.", "tipo_invalido", 400);
        var roles = await db.Set<Role>().Where(r => req.RoleIds.Contains(r.Id)).ToListAsync(ct);
        if (roles.Count != req.RoleIds.Distinct().Count()) throw DomainException.NotFound("Função");
        db.Set<UserRole>().RemoveRange(user.Roles.Where(r => !req.RoleIds.Contains(r.RoleId)));
        foreach (var role in roles.Where(r => user.Roles.All(ur => ur.RoleId != r.Id)))
            db.Set<UserRole>().Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await db.SaveChangesAsync(ct);
        return Map(await LoadAsync(id, ct));
    }

    public async Task<UserDto> SetOverridesAsync(Guid id, SetOverridesRequest req, CancellationToken ct = default)
    {
        var user = await LoadAsync(id, ct);
        EnsureCanManage(user);
        if (user.Kind != UserKind.Staff) throw new DomainException("Permissões individuais só se aplicam a usuários funcionários.", "tipo_invalido", 400);
        var invalid = req.Overrides.FirstOrDefault(o => !Permissions.Codes.Contains(o.Permission));
        if (invalid is not null) throw new DomainException($"Permissão desconhecida: {invalid.Permission}.", "permissao_invalida", 400);
        db.Set<UserPermission>().RemoveRange(user.Permissions);
        foreach (var o in req.Overrides.DistinctBy(o => o.Permission))
            db.Set<UserPermission>().Add(new UserPermission { UserId = user.Id, Permission = o.Permission, Granted = o.Granted });
        await db.SaveChangesAsync(ct);
        return Map(await LoadAsync(id, ct));
    }

    public async Task BlockAsync(Guid id, BlockRequest req, CancellationToken ct = default)
    {
        var user = await LoadAsync(id, ct);
        EnsureCanManage(user);
        if (user.Id == ctx.UserId) throw new DomainException("Você não pode bloquear o próprio usuário.", "operacao_invalida", 400);
        user.IsBlocked = true;
        user.BlockReason = req.Reason;
        ctx.Justification = req.Reason;
        await db.SaveChangesAsync(ct);
        await db.Set<RefreshToken>().Where(t => t.UserId == id && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow).SetProperty(t => t.RevokedReason, "usuario_bloqueado"), ct);
    }

    public async Task UnblockAsync(Guid id, CancellationToken ct = default)
    {
        var user = await LoadAsync(id, ct);
        EnsureCanManage(user);
        user.IsBlocked = false;
        user.BlockReason = null;
        user.FailedLoginCount = 0;
        user.LockoutUntil = null;
        await db.SaveChangesAsync(ct);
    }

    public async Task<TemporaryPasswordDto> ResetPasswordAsync(Guid id, CancellationToken ct = default)
    {
        var user = await LoadAsync(id, ct);
        EnsureCanManage(user);
        var password = PasswordPolicy.GenerateTemporary();
        await auth.SetPasswordAsync(user, password, mustChange: true, ct);
        return new TemporaryPasswordDto(user.Id, user.Email, password, await SendCredentialsAsync(user, password, ct));
    }

    public async Task<PagedResult<LoginHistoryDto>> LoginHistoryAsync(PageQuery page, Guid? userId, bool? success, CancellationToken ct = default)
    {
        var q = db.Set<LoginHistory>().AsNoTracking();
        if (userId is not null) q = q.Where(h => h.UserId == userId);
        if (success is not null) q = q.Where(h => h.Success == success);
        if (!string.IsNullOrWhiteSpace(page.Search)) { var like = page.Search.Like(); q = q.Where(h => EF.Functions.ILike(h.Email, like)); }
        return await q.OrderByDescending(h => h.CreatedAt)
            .Select(h => new LoginHistoryDto(h.Id, h.Email, h.UserId, h.Success, h.Reason, h.Ip, h.UserAgent, h.CreatedAt)).ToPagedAsync(page, ct);
    }

    private void EnsureCanManage(User target)
    {
        if (target.Kind is UserKind.ClubAdmin or UserKind.SuperAdmin && !ctx.IsAdmin)
            throw DomainException.Forbidden("Somente administradores podem alterar usuários administradores.");
    }
}

public class RoleService(IAppDb db)
{
    public IReadOnlyList<PermissionInfoDto> Catalog() => Permissions.All.Select(p => new PermissionInfoDto(p.Code, p.Group, p.Description)).ToList();

    public async Task<List<RoleDto>> ListAsync(CancellationToken ct = default)
    {
        var roles = await db.Set<Role>().AsNoTracking().Include(r => r.Permissions).OrderBy(r => r.Name).ToListAsync(ct);
        var counts = await db.Set<UserRole>().GroupBy(ur => ur.RoleId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        return roles.Select(r => new RoleDto(r.Id, r.Name, r.Description, r.IsSystem, r.Permissions.Select(p => p.Permission).Order().ToList(), counts.GetValueOrDefault(r.Id))).ToList();
    }

    public async Task<RoleDto> SaveAsync(Guid? id, SaveRoleRequest req, CancellationToken ct = default)
    {
        var invalid = req.Permissions.FirstOrDefault(p => !Permissions.Codes.Contains(p));
        if (invalid is not null) throw new DomainException($"Permissão desconhecida: {invalid}.", "permissao_invalida", 400);
        var name = req.Name.Trim();
        if (await db.Set<Role>().AnyAsync(r => r.Name == name && r.Id != id, ct)) throw DomainException.Conflict("Já existe uma função com este nome.");

        Role role;
        if (id is null)
        {
            role = new Role { Name = name, Description = req.Description };
            db.Set<Role>().Add(role);
        }
        else
        {
            role = await db.Set<Role>().Include(r => r.Permissions).Where(r => r.Id == id).FirstOr404Async("Função", ct);
            role.Name = name;
            role.Description = req.Description;
            db.Set<RolePermission>().RemoveRange(role.Permissions.Where(p => !req.Permissions.Contains(p.Permission)));
        }
        foreach (var code in req.Permissions.Distinct().Where(c => role.Permissions.All(p => p.Permission != c)))
            db.Set<RolePermission>().Add(new RolePermission { Role = role, Permission = code });
        await db.SaveChangesAsync(ct);
        return (await ListAsync(ct)).First(r => r.Id == role.Id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var role = await db.Set<Role>().Include(r => r.Permissions).Where(r => r.Id == id).FirstOr404Async("Função", ct);
        if (await db.Set<UserRole>().AnyAsync(ur => ur.RoleId == id, ct))
            throw DomainException.Conflict("Há usuários vinculados a esta função. Remova os vínculos antes de excluí-la.");
        db.Set<RolePermission>().RemoveRange(role.Permissions);
        db.Set<Role>().Remove(role);
        await db.SaveChangesAsync(ct);
    }
}
