using Clubes.Mobile.ViewModels;
using Clubes.Mobile.Views;

namespace Clubes.Mobile.Services;

/// <summary>Decide a tela raiz conforme o estado da sessão: login, troca de senha obrigatória, privacidade ou o app do perfil.</summary>
public class AppRouter(IServiceProvider services, AuthService auth, ApiClient api, RealtimeService realtime, NotificationState notifications,
    IPushRegistration push, FileService files)
{
    private bool _initialized;

    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        notifications.Attach();
        // Sessão não pôde ser renovada: encerra os serviços e volta ao login.
        api.SessionExpired += () => MainThread.BeginInvokeOnMainThread(async () =>
        {
            await StopSessionServicesAsync();
            SetRoot(new NavigationPage(services.GetRequiredService<LoginPage>()));
            await Dialogs.AlertAsync("Sessão expirada", "Entre novamente para continuar.");
        });
    }

    /// <summary>Restaura a sessão salva e abre a tela adequada. Lança ApiException se não houver conexão.</summary>
    public async Task StartAsync()
    {
        Initialize();
        await auth.RestoreAsync();
        await RouteAsync();
    }

    public async Task RouteAsync()
    {
        var user = auth.User;
        if (!auth.IsAuthenticated || user is null)
        {
            SetRoot(new NavigationPage(services.GetRequiredService<LoginPage>()));
            return;
        }
        if (user.MustChangePassword)
        {
            var page = services.GetRequiredService<ChangePasswordPage>();
            ((ChangePasswordViewModel)page.BindingContext).Forced = true;
            SetRoot(new NavigationPage(page));
            return;
        }
        if (!user.PrivacyAccepted)
        {
            SetRoot(new NavigationPage(services.GetRequiredService<PrivacyPage>()));
            return;
        }
        if (auth.Profile == AppProfile.Unsupported)
        {
            SetRoot(services.GetRequiredService<UnsupportedPage>());
            return;
        }
        SetRoot(new AppShell(services, auth));
        await StartSessionServicesAsync();
    }

    public async Task LogoutAsync()
    {
        await push.UnregisterAsync();
        await StopSessionServicesAsync();
        await auth.LogoutAsync();
        SetRoot(new NavigationPage(services.GetRequiredService<LoginPage>()));
    }

    /// <summary>O app está em primeiro plano (telas não devem gerar códigos nem consultar o servidor em segundo plano).</summary>
    public bool IsForeground { get; private set; } = true;

    /// <summary>O app voltou ao primeiro plano.</summary>
    public event Action? Resumed;

    public void OnStopped() => IsForeground = false;

    /// <summary>Ao voltar para o app: atualiza o contador de não lidas e avisa as telas visíveis.</summary>
    public async Task OnResumedAsync()
    {
        IsForeground = true;
        Resumed?.Invoke();
        if (auth.IsAuthenticated && auth.Profile is AppProfile.Member or AppProfile.Dependent)
            await notifications.RefreshAsync();
    }

    public Task PushAsync<TPage>() where TPage : Page =>
        Root?.Navigation.PushAsync(services.GetRequiredService<TPage>()) ?? Task.CompletedTask;

    public Task PopToRootAsync() => Root?.Navigation.PopToRootAsync() ?? Task.CompletedTask;

    private static Page? Root => Application.Current?.Windows.FirstOrDefault()?.Page;

    private static void SetRoot(Page page)
    {
        var window = Application.Current?.Windows.FirstOrDefault();
        if (window is not null) window.Page = page;
    }

    private async Task StartSessionServicesAsync()
    {
        await realtime.StartAsync();
        if (auth.Profile is AppProfile.Member or AppProfile.Dependent)
        {
            _ = notifications.RefreshAsync();
            _ = notifications.EnsurePermissionAsync();
            _ = push.RegisterAsync();
        }
    }

    private async Task StopSessionServicesAsync()
    {
        await realtime.StopAsync();
        notifications.Reset();
        files.ClearCache();
    }
}
