using System.ComponentModel.DataAnnotations;
using Clubes.Application.Common;
using Clubes.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Clubes.Application.Access;

public class AuthOptions
{
    public int MaxFailedAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
    public int PasswordResetMinutes { get; set; } = 30;
    public string? PasswordResetUrl { get; set; }
}

public record LoginRequest([Required, EmailAddress] string Email, [Required] string Password, string? Device);
public record RefreshRequest([Required] string RefreshToken);
public record ForgotPasswordRequest([Required, EmailAddress] string Email);
public record ResetPasswordRequest([Required] string Token, [Required, MinLength(8)] string NewPassword);
public record ChangePasswordRequest([Required] string CurrentPassword, [Required, MinLength(8)] string NewPassword);
public record MeDto(Guid Id, string Name, string Email, UserKind Kind, Guid? ClubId, string? ClubName, Guid? ClubLogoFileId,
    Guid? MemberId, Guid? DependentId, IReadOnlyCollection<string> Permissions, IReadOnlyCollection<string> DependentPermissions,
    bool MustChangePassword, bool PrivacyAccepted, string? PrivacyPolicyVersion);
public record AuthResponse(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken, DateTime RefreshTokenExpiresAt, MeDto User);
public record SessionDto(Guid Id, string? Device, string? Ip, DateTime CreatedAt, DateTime LastUsedAt, DateTime ExpiresAt, bool IsCurrent);

public class PermissionService(IAppDb db, RequestContext ctx)
{
    private HashSet<string>? _cache;

    public async Task<IReadOnlySet<string>> GetForCurrentUserAsync(CancellationToken ct = default)
    {
        if (_cache is not null) return _cache;
        _cache = ctx.UserId is { } id && ctx.Kind is { } kind ? await GetAsync(id, kind, ct) : [];
        return _cache;
    }

    public async Task<HashSet<string>> GetAsync(Guid userId, UserKind kind, CancellationToken ct = default)
    {
        if (kind is UserKind.SuperAdmin or UserKind.ClubAdmin) return [.. Permissions.Codes];
        if (kind != UserKind.Staff) return [];

        var fromRoles = await db.Set<UserRole>().IgnoreQueryFilters().Where(ur => ur.UserId == userId)
            .SelectMany(ur => ur.Role.Permissions.Select(p => p.Permission)).Distinct().ToListAsync(ct);
        var overrides = await db.Set<UserPermission>().IgnoreQueryFilters().Where(up => up.UserId == userId).ToListAsync(ct);
        var set = fromRoles.ToHashSet();
        foreach (var o in overrides)
            if (o.Granted) set.Add(o.Permission); else set.Remove(o.Permission);
        set.IntersectWith(Permissions.Codes);
        return set;
    }

    public async Task<bool> HasAsync(string permission, CancellationToken ct = default)
        => (await GetForCurrentUserAsync(ct)).Contains(permission);

    public async Task EnsureAsync(string permission, CancellationToken ct = default)
    {
        if (!await HasAsync(permission, ct)) throw DomainException.Forbidden();
    }
}

public class AuthService(IAppDb db, RequestContext ctx, ITokenService tokens, IPasswordHasher<User> hasher,
    PermissionService permissions, IEmailSender email, IOptions<AuthOptions> options, ILogger<AuthService> logger)
{
    private readonly AuthOptions _opt = options.Value;
    private const string InvalidCredentials = "E-mail ou senha inválidos.";

    public async Task<AuthResponse> LoginAsync(LoginRequest req, string? ip, string? userAgent, CancellationToken ct = default)
    {
        var emailAddress = req.Email.Trim().ToLowerInvariant();
        var user = await db.Set<User>().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == emailAddress && !u.IsDeleted, ct);

        async Task<DomainException> Fail(string reason, string message, int status = 401)
        {
            db.Set<LoginHistory>().Add(new LoginHistory { ClubId = user?.ClubId, UserId = user?.Id, Email = emailAddress, Success = false, Reason = reason, Ip = ip, UserAgent = userAgent });
            await db.SaveChangesAsync(ct);
            logger.LogWarning("Falha de login para {Email}: {Reason} (IP {Ip})", emailAddress, reason, ip);
            return new DomainException(message, "login_invalido", status);
        }

        if (user is null) throw await Fail("usuario_inexistente", InvalidCredentials);
        if (user.LockoutUntil > DateTime.UtcNow)
            throw await Fail("bloqueio_temporario", "Muitas tentativas inválidas. Aguarde alguns minutos e tente novamente.", 429);

        if (hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password) == PasswordVerificationResult.Failed)
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= _opt.MaxFailedAttempts)
            {
                user.LockoutUntil = DateTime.UtcNow.AddMinutes(_opt.LockoutMinutes);
                user.FailedLoginCount = 0;
            }
            throw await Fail("senha_invalida", InvalidCredentials);
        }

        if (user.IsBlocked) throw await Fail("usuario_bloqueado", "Seu acesso está bloqueado. Procure a administração do clube.", 403);
        if (user.ClubId is { } clubId)
        {
            var club = await db.Set<Club>().FirstOrDefaultAsync(c => c.Id == clubId, ct);
            if (club is not { IsActive: true }) throw await Fail("clube_inativo", "O clube está inativo na plataforma.", 403);
        }
        await EnsurePersonCanAccessAsync(user, async (r, m) => await Fail(r, m, 403), ct);

        user.FailedLoginCount = 0;
        user.LockoutUntil = null;
        user.LastLoginAt = DateTime.UtcNow;
        db.Set<LoginHistory>().Add(new LoginHistory { ClubId = user.ClubId, UserId = user.Id, Email = emailAddress, Success = true, Ip = ip, UserAgent = userAgent });
        return await IssueAsync(user, req.Device ?? userAgent, ip, ct);
    }

    private async Task EnsurePersonCanAccessAsync(User user, Func<string, string, Task<DomainException>> fail, CancellationToken ct)
    {
        if (user is { Kind: UserKind.Member, MemberId: { } memberId })
        {
            var status = await db.Set<Member>().IgnoreQueryFilters().Where(m => m.Id == memberId && !m.IsDeleted).Select(m => (MemberStatus?)m.Status).FirstOrDefaultAsync(ct);
            if (status is null or MemberStatus.Cancelled or MemberStatus.PendingApproval)
                throw await fail("associado_sem_acesso", "Seu cadastro não está ativo. Procure a secretaria do clube.");
        }
        if (user is { Kind: UserKind.Dependent, DependentId: { } dependentId })
        {
            var status = await db.Set<Dependent>().IgnoreQueryFilters().Where(d => d.Id == dependentId && !d.IsDeleted).Select(d => (DependentStatus?)d.Status).FirstOrDefaultAsync(ct);
            if (status is not (DependentStatus.Active or DependentStatus.Suspended))
                throw await fail("dependente_sem_acesso", "Seu cadastro de dependente não está ativo. Procure o associado responsável ou a secretaria.");
        }
        if (user is { Kind: UserKind.Staff, EmployeeId: { } employeeId })
        {
            var status = await db.Set<Employee>().IgnoreQueryFilters().Where(e => e.Id == employeeId && !e.IsDeleted).Select(e => (EmployeeStatus?)e.Status).FirstOrDefaultAsync(ct);
            if (status is not EmployeeStatus.Active) throw await fail("funcionario_inativo", "Seu cadastro de funcionário está inativo.");
        }
    }

    private async Task<AuthResponse> IssueAsync(User user, string? device, string? ip, CancellationToken ct)
    {
        var (access, accessExp) = tokens.CreateAccessToken(user);
        var refresh = tokens.CreateOpaqueToken();
        var refreshExp = DateTime.UtcNow.Add(tokens.RefreshTokenLifetime);
        db.Set<RefreshToken>().Add(new RefreshToken { UserId = user.Id, TokenHash = tokens.Hash(refresh), ExpiresAt = refreshExp, Device = Truncate(device, 200), Ip = ip });
        await db.SaveChangesAsync(ct);
        return new AuthResponse(access, accessExp, refresh, refreshExp, await BuildMeAsync(user, ct));
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest req, string? ip, CancellationToken ct = default)
    {
        var hash = tokens.Hash(req.RefreshToken);
        var stored = await db.Set<RefreshToken>().Include(t => t.User).IgnoreQueryFilters().FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        var invalid = new DomainException("Sessão expirada. Entre novamente.", "sessao_expirada", 401);
        if (stored is null) throw invalid;

        if (stored.RevokedAt is not null)
        {
            // Reutilização de um token já rotacionado indica vazamento: encerra todas as sessões do usuário.
            await db.Set<RefreshToken>().Where(t => t.UserId == stored.UserId && t.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow).SetProperty(t => t.RevokedReason, "reuso_detectado"), ct);
            logger.LogWarning("Reuso de refresh token detectado para o usuário {UserId}", stored.UserId);
            throw invalid;
        }
        if (stored.ExpiresAt <= DateTime.UtcNow || stored.User.IsBlocked || stored.User.IsDeleted) throw invalid;
        await EnsurePersonCanAccessAsync(stored.User, (_, _) => Task.FromResult(invalid), ct);

        // Rotação atômica: somente uma requisição concorrente consegue consumir o token.
        var newToken = tokens.CreateOpaqueToken();
        var newHash = tokens.Hash(newToken);
        var consumed = await db.Set<RefreshToken>().Where(t => t.Id == stored.Id && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow).SetProperty(t => t.RevokedReason, "rotacionado").SetProperty(t => t.ReplacedByHash, newHash), ct);
        if (consumed == 0) throw invalid;

        var (access, accessExp) = tokens.CreateAccessToken(stored.User);
        var refreshExp = DateTime.UtcNow.Add(tokens.RefreshTokenLifetime);
        db.Set<RefreshToken>().Add(new RefreshToken { UserId = stored.UserId, TokenHash = newHash, ExpiresAt = refreshExp, Device = stored.Device, Ip = ip });
        await db.SaveChangesAsync(ct);
        return new AuthResponse(access, accessExp, newToken, refreshExp, await BuildMeAsync(stored.User, ct));
    }

    public async Task LogoutAsync(RefreshRequest req, CancellationToken ct = default)
    {
        var hash = tokens.Hash(req.RefreshToken);
        await db.Set<RefreshToken>().Where(t => t.TokenHash == hash && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow).SetProperty(t => t.RevokedReason, "logout"), ct);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest req, CancellationToken ct = default)
    {
        // Resposta sempre igual, exista ou não o e-mail, para não revelar cadastros.
        var emailAddress = req.Email.Trim().ToLowerInvariant();
        var user = await db.Set<User>().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == emailAddress && !u.IsDeleted && !u.IsBlocked, ct);
        if (user is null) return;

        var token = tokens.CreateOpaqueToken();
        db.Set<PasswordResetToken>().Add(new PasswordResetToken { UserId = user.Id, TokenHash = tokens.Hash(token), ExpiresAt = DateTime.UtcNow.AddMinutes(_opt.PasswordResetMinutes) });
        await db.SaveChangesAsync(ct);

        if (!email.IsConfigured)
        {
            logger.LogError("Recuperação de senha solicitada para o usuário {UserId}, mas o envio de e-mail não está configurado (Email:Smtp).", user.Id);
            return;
        }
        var link = string.IsNullOrWhiteSpace(_opt.PasswordResetUrl) ? null : $"{_opt.PasswordResetUrl}?token={Uri.EscapeDataString(token)}";
        var body = $"Olá, {user.Name}.\n\nRecebemos um pedido para redefinir sua senha. " +
                   (link is null ? $"Use o código abaixo no aplicativo:\n\n{token}" : $"Acesse o link para criar uma nova senha:\n\n{link}\n\nOu informe o código: {token}") +
                   $"\n\nO código expira em {_opt.PasswordResetMinutes} minutos. Se você não fez este pedido, ignore este e-mail.";
        await email.SendAsync(new EmailMessage(user.Email, "Redefinição de senha", body), ct);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest req, CancellationToken ct = default)
    {
        PasswordPolicy.Ensure(req.NewPassword);
        var hash = tokens.Hash(req.Token.Trim());
        var stored = await db.Set<PasswordResetToken>().Include(t => t.User).IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (stored is null || stored.UsedAt is not null || stored.ExpiresAt <= DateTime.UtcNow)
            throw new DomainException("Código inválido ou expirado. Solicite uma nova redefinição de senha.", "token_invalido", 400);

        stored.UsedAt = DateTime.UtcNow;
        await SetPasswordAsync(stored.User, req.NewPassword, mustChange: false, ct);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest req, CancellationToken ct = default)
    {
        PasswordPolicy.Ensure(req.NewPassword);
        var id = ctx.RequireUser();
        var user = await db.Set<User>().IgnoreQueryFilters().FirstAsync(u => u.Id == id, ct);
        if (hasher.VerifyHashedPassword(user, user.PasswordHash, req.CurrentPassword) == PasswordVerificationResult.Failed)
            throw new DomainException("A senha atual não confere.", "senha_invalida", 400);
        await SetPasswordAsync(user, req.NewPassword, mustChange: false, ct);
    }

    /// <summary>Define a senha e encerra todas as sessões existentes do usuário.</summary>
    public async Task SetPasswordAsync(User user, string password, bool mustChange, CancellationToken ct = default)
    {
        user.PasswordHash = hasher.HashPassword(user, password);
        user.MustChangePassword = mustChange;
        user.FailedLoginCount = 0;
        user.LockoutUntil = null;
        await db.SaveChangesAsync(ct);
        await db.Set<RefreshToken>().Where(t => t.UserId == user.Id && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow).SetProperty(t => t.RevokedReason, "senha_alterada"), ct);
    }

    public async Task<MeDto> MeAsync(CancellationToken ct = default)
    {
        var id = ctx.RequireUser();
        var user = await db.Set<User>().IgnoreQueryFilters().FirstAsync(u => u.Id == id, ct);
        return await BuildMeAsync(user, ct);
    }

    public async Task AcceptPrivacyAsync(CancellationToken ct = default)
    {
        var id = ctx.RequireUser();
        var user = await db.Set<User>().IgnoreQueryFilters().FirstAsync(u => u.Id == id, ct);
        var version = user.ClubId is { } clubId
            ? await db.Set<ClubSettings>().IgnoreQueryFilters().Where(s => s.ClubId == clubId).Select(s => s.PrivacyPolicyVersion).FirstOrDefaultAsync(ct) ?? "1"
            : "1";
        user.PrivacyAcceptedAt = DateTime.UtcNow;
        user.PrivacyAcceptedVersion = version;
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<SessionDto>> SessionsAsync(Guid? userId, string? currentRefreshToken, CancellationToken ct = default)
    {
        var id = userId ?? ctx.RequireUser();
        var currentHash = currentRefreshToken is null ? null : tokens.Hash(currentRefreshToken);
        return await db.Set<RefreshToken>().Where(t => t.UserId == id && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(t => t.LastUsedAt)
            .Select(t => new SessionDto(t.Id, t.Device, t.Ip, t.CreatedAt, t.LastUsedAt, t.ExpiresAt, t.TokenHash == currentHash)).ToListAsync(ct);
    }

    public async Task RevokeSessionAsync(Guid sessionId, Guid? userId, CancellationToken ct = default)
    {
        var id = userId ?? ctx.RequireUser();
        await db.Set<RefreshToken>().Where(t => t.Id == sessionId && t.UserId == id && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow).SetProperty(t => t.RevokedReason, "encerrada"), ct);
    }

    private async Task<MeDto> BuildMeAsync(User user, CancellationToken ct)
    {
        string? clubName = null, policyVersion = null;
        Guid? logo = null;
        if (user.ClubId is { } clubId)
        {
            var club = await db.Set<Club>().Where(c => c.Id == clubId).Select(c => new { c.Name, c.LogoFileId }).FirstOrDefaultAsync(ct);
            clubName = club?.Name;
            logo = club?.LogoFileId;
            policyVersion = await db.Set<ClubSettings>().IgnoreQueryFilters().Where(s => s.ClubId == clubId).Select(s => s.PrivacyPolicyVersion).FirstOrDefaultAsync(ct);
        }
        var perms = await permissions.GetAsync(user.Id, user.Kind, ct);
        var depPerms = user.DependentId is { } depId
            ? await db.Set<DependentPermission>().IgnoreQueryFilters().Where(p => p.DependentId == depId && p.AllowedByAdmin && p.EnabledByMember).Select(p => p.Code).ToListAsync(ct)
            : [];
        var accepted = user.PrivacyAcceptedAt is not null && (policyVersion is null || user.PrivacyAcceptedVersion == policyVersion);
        return new MeDto(user.Id, user.Name, user.Email, user.Kind, user.ClubId, clubName, logo, user.MemberId, user.DependentId,
            perms.Order().ToList(), depPerms, user.MustChangePassword, accepted, policyVersion);
    }

    private static string? Truncate(string? value, int max) => value is { Length: > 0 } && value.Length > max ? value[..max] : value;
}

public static class PasswordPolicy
{
    public static void Ensure(string password)
    {
        if (password.Length < 8 || !password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            throw new DomainException("A senha deve ter ao menos 8 caracteres, com letras e números.", "senha_fraca", 400);
    }

    /// <summary>Senha temporária aleatória (o usuário é obrigado a trocá-la no primeiro acesso).</summary>
    public static string GenerateTemporary()
    {
        const string letters = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ";
        const string digits = "23456789";
        var chars = new char[10];
        for (var i = 0; i < chars.Length; i++)
        {
            var source = i % 3 == 2 ? digits : letters;
            chars[i] = source[System.Security.Cryptography.RandomNumberGenerator.GetInt32(source.Length)];
        }
        return new string(chars);
    }
}
