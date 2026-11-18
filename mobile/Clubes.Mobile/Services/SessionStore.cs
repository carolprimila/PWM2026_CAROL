using Clubes.Mobile.Models;

namespace Clubes.Mobile.Services;

/// <summary>Endereço da API. Não é segredo: fica em Preferences e pode ser alterado no campo "Servidor" da tela de login.</summary>
public class AppConfig
{
    private const string Key = "api_base_url";

    public static string DefaultBaseUrl =>
#if ANDROID
        "http://10.0.2.2:5080"; // host da máquina visto pelo emulador Android
#else
        "http://localhost:5080";
#endif

    public string BaseUrl
    {
        get => Normalize(Preferences.Default.Get(Key, DefaultBaseUrl));
        set
        {
            var normalized = Normalize(value);
            if (normalized.Length == 0 || normalized == DefaultBaseUrl) Preferences.Default.Remove(Key);
            else Preferences.Default.Set(Key, normalized);
        }
    }

    public static bool IsValid(string? url) =>
        Uri.TryCreate(Normalize(url), UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static string Normalize(string? url) => (url ?? "").Trim().TrimEnd('/');
}

/// <summary>Sessão do usuário. Os tokens ficam somente no SecureStorage (nunca em Preferences ou arquivos).</summary>
public class SessionStore
{
    private const string AccessKey = "clubes.access_token";
    private const string AccessExpiresKey = "clubes.access_expires";
    private const string RefreshKey = "clubes.refresh_token";

    public string? AccessToken { get; private set; }
    public DateTime AccessTokenExpiresAt { get; private set; }
    public string? RefreshToken { get; private set; }
    public MeDto? User { get; set; }

    public bool HasSession => !string.IsNullOrEmpty(RefreshToken);

    public async Task LoadAsync()
    {
        try
        {
            AccessToken = await SecureStorage.Default.GetAsync(AccessKey);
            RefreshToken = await SecureStorage.Default.GetAsync(RefreshKey);
            var expires = await SecureStorage.Default.GetAsync(AccessExpiresKey);
            AccessTokenExpiresAt = long.TryParse(expires, out var ticks) ? new DateTime(ticks, DateTimeKind.Utc) : DateTime.MinValue;
        }
        catch (Exception)
        {
            // Armazenamento seguro indisponível ou corrompido: trata como sessão inexistente.
            AccessToken = RefreshToken = null;
        }
    }

    public async Task SaveAsync(AuthResponse auth)
    {
        AccessToken = auth.AccessToken;
        AccessTokenExpiresAt = DateTime.SpecifyKind(auth.AccessTokenExpiresAt, DateTimeKind.Utc);
        RefreshToken = auth.RefreshToken;
        User = auth.User;
        try
        {
            await SecureStorage.Default.SetAsync(AccessKey, auth.AccessToken);
            await SecureStorage.Default.SetAsync(AccessExpiresKey, AccessTokenExpiresAt.Ticks.ToString());
            await SecureStorage.Default.SetAsync(RefreshKey, auth.RefreshToken);
        }
        catch (Exception)
        {
            // Armazenamento seguro indisponível: a sessão vale apenas enquanto o app estiver aberto (nunca é gravada em outro lugar).
        }
    }

    public void Clear()
    {
        AccessToken = RefreshToken = null;
        AccessTokenExpiresAt = DateTime.MinValue;
        User = null;
        try
        {
            SecureStorage.Default.Remove(AccessKey);
            SecureStorage.Default.Remove(AccessExpiresKey);
            SecureStorage.Default.Remove(RefreshKey);
        }
        catch (Exception) { /* nada a limpar */ }
    }
}
