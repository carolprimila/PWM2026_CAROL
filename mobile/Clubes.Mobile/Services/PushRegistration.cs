namespace Clubes.Mobile.Services;

public enum PushRegistrationStatus { Registered, NotConfigured, Failed }

/// <summary>Obtém o token de push do aparelho (FCM no Android, APNs no iOS).</summary>
public interface IPushTokenProvider
{
    /// <summary>"android" ou "ios".</summary>
    string Platform { get; }
    /// <summary>Token do aparelho, ou null quando o push não está configurado neste build.</summary>
    Task<string?> GetTokenAsync();
}

/// <summary>Registra o aparelho para receber push (POST /api/notificacoes/dispositivos).</summary>
public interface IPushRegistration
{
    Task<PushRegistrationStatus> RegisterAsync();
    Task UnregisterAsync();
}

/// <summary>
/// Implementação padrão enquanto o clube não possui credenciais Firebase/APNs: não há token, portanto nada é registrado.
/// Para habilitar o push, substitua este provedor (veja mobile/README.md, seção "Push").
/// </summary>
public class NotConfiguredPushTokenProvider : IPushTokenProvider
{
    public string Platform => DeviceInfo.Current.Platform == DevicePlatform.iOS ? "ios" : DeviceInfo.Current.Platform == DevicePlatform.Android ? "android" : "outro";
    public Task<string?> GetTokenAsync() => Task.FromResult<string?>(null);
}

public class PushRegistration(ClubApi api, IPushTokenProvider provider) : IPushRegistration
{
    private string? _registeredToken;

    public async Task<PushRegistrationStatus> RegisterAsync()
    {
        try
        {
            var token = await provider.GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token)) return PushRegistrationStatus.NotConfigured;
            await api.RegisterDeviceAsync(token, provider.Platform);
            _registeredToken = token;
            return PushRegistrationStatus.Registered;
        }
        catch (Exception)
        {
            return PushRegistrationStatus.Failed;
        }
    }

    public async Task UnregisterAsync()
    {
        var token = _registeredToken;
        _registeredToken = null;
        if (string.IsNullOrWhiteSpace(token)) return;
        try { await api.UnregisterDeviceAsync(token); }
        catch (Exception) { /* o servidor descarta tokens inválidos depois */ }
    }
}
