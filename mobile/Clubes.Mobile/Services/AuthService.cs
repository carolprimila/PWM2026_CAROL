using Clubes.Mobile.Models;

namespace Clubes.Mobile.Services;

/// <summary>Perfil de uso do aplicativo, derivado apenas do que a API informa sobre o usuário.</summary>
public enum AppProfile { None, Member, Dependent, Gate, Unsupported }

public class AuthService(ApiClient api, SessionStore session)
{
    public const string GateOperate = "gate.operate";
    public const string DepReservations = "reservations.request";
    public const string DepEvents = "events.register";
    public const string DepGuests = "guests.register";

    public MeDto? User => session.User;
    public bool IsAuthenticated => session.HasSession && session.User is not null;

    public AppProfile Profile => User switch
    {
        null => AppProfile.None,
        { Kind: UserKind.Member } => AppProfile.Member,
        { Kind: UserKind.Dependent } => AppProfile.Dependent,
        { Kind: UserKind.Staff or UserKind.ClubAdmin } u when u.Permissions.Contains(GateOperate) => AppProfile.Gate,
        _ => AppProfile.Unsupported,
    };

    public bool IsMember => Profile == AppProfile.Member;
    public bool IsDependent => Profile == AppProfile.Dependent;
    public bool CanReserve => IsMember || HasDependentPermission(DepReservations);
    public bool CanRegisterGuests => IsMember || HasDependentPermission(DepGuests);
    public bool CanRegisterEvents => IsMember || HasDependentPermission(DepEvents);
    public string ClubName => string.IsNullOrWhiteSpace(User?.ClubName) ? "Clube" : User!.ClubName!;
    public string FirstName => (User?.Name ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";

    private bool HasDependentPermission(string code) => IsDependent && User!.DependentPermissions.Contains(code);

    public static string DeviceName
    {
        get
        {
            var name = $"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model} ({DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString})".Trim();
            return name.Length > 100 ? name[..100] : name;
        }
    }

    public async Task LoginAsync(string email, string password)
    {
        var auth = await api.PostAnonymousAsync<AuthResponse>("/api/auth/login", new LoginRequest(email.Trim(), password, DeviceName));
        await session.SaveAsync(auth);
    }

    /// <summary>Restaura a sessão salva. Devolve false quando não há sessão (ou ela expirou); lança ApiException sem conexão.</summary>
    public async Task<bool> RestoreAsync()
    {
        await session.LoadAsync();
        if (!session.HasSession) return false;
        try
        {
            session.User = await api.GetAsync<MeDto>("/api/auth/me");
            return true;
        }
        catch (ApiException ex) when (ex.Status == 401)
        {
            return false;
        }
    }

    public async Task RefreshUserAsync() => session.User = await api.GetAsync<MeDto>("/api/auth/me");

    /// <summary>
    /// Altera a senha. O servidor encerra todas as sessões ao trocar a senha (inclusive a atual), por isso o app
    /// entra novamente com a nova senha — que o usuário acabou de digitar e não é armazenada.
    /// </summary>
    public async Task ChangePasswordAsync(string current, string next)
    {
        var email = session.User?.Email ?? "";
        await api.PostAsync("/api/auth/alterar-senha", new ChangePasswordRequest(current, next));
        await LoginAsync(email, next);
    }

    public async Task AcceptPrivacyAsync()
    {
        await api.PostAsync("/api/auth/aceitar-privacidade");
        if (session.User is { } u) u.PrivacyAccepted = true;
    }

    public Task ForgotPasswordAsync(string email) => api.PostAnonymousAsync("/api/auth/esqueci-senha", new ForgotPasswordRequest(email.Trim()));
    public Task ResetPasswordAsync(string token, string newPassword) => api.PostAnonymousAsync("/api/auth/redefinir-senha", new ResetPasswordRequest(token.Trim(), newPassword));

    public Task<List<SessionDto>> SessionsAsync() =>
        api.GetAsync<List<SessionDto>>("/api/auth/sessoes", new Dictionary<string, string> { ["X-Refresh-Token"] = session.RefreshToken ?? "" });
    public Task RevokeSessionAsync(Guid id) => api.DeleteAsync($"/api/auth/sessoes/{id}");

    /// <summary>Encerra a sessão no servidor (melhor esforço) e apaga os tokens do aparelho.</summary>
    public async Task LogoutAsync()
    {
        var refresh = session.RefreshToken;
        if (!string.IsNullOrEmpty(refresh))
        {
            try { await api.PostAnonymousAsync("/api/auth/logout", new RefreshRequest(refresh)); }
            catch (ApiException) { /* sem conexão: a sessão local é apagada de qualquer forma */ }
        }
        session.Clear();
    }
}
