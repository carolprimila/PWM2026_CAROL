using Clubes.Mobile.Services;
using Clubes.Mobile.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Clubes.Mobile.ViewModels;

public partial class LoginViewModel(AuthService auth, AppRouter router, AppConfig config) : BaseViewModel
{
    [ObservableProperty] public partial string Email { get; set; } = "";
    [ObservableProperty] public partial string Password { get; set; } = "";
    [ObservableProperty] public partial string? EmailError { get; set; }
    [ObservableProperty] public partial string? PasswordError { get; set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFormError))]
    public partial string? FormError { get; set; }
    public bool HasFormError => !string.IsNullOrWhiteSpace(FormError);
    [ObservableProperty] public partial bool ShowServer { get; set; }
    [ObservableProperty] public partial string ServerUrl { get; set; } = config.BaseUrl;
    [ObservableProperty] public partial string? ServerError { get; set; }

    public string VersionText => $"Clubes · versão {AppInfo.Current.VersionString}";

    [RelayCommand]
    private void ToggleServer() => ShowServer = !ShowServer;

    [RelayCommand]
    private void ResetServer()
    {
        ServerUrl = AppConfig.DefaultBaseUrl;
        ServerError = null;
    }

    [RelayCommand]
    private async Task Login()
    {
        EmailError = string.IsNullOrWhiteSpace(Email) || !Email.Contains('@') ? "Informe um e-mail válido." : null;
        PasswordError = string.IsNullOrEmpty(Password) ? "Informe sua senha." : null;
        ServerError = AppConfig.IsValid(ServerUrl) ? null : "Endereço inválido. Exemplo: http://192.168.0.10:5080";
        FormError = null;
        if (ServerError is not null) ShowServer = true;
        if (EmailError is not null || PasswordError is not null || ServerError is not null) return;
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            config.BaseUrl = ServerUrl;
            await auth.LoginAsync(Email, Password);
            Password = "";
            await router.RouteAsync();
        }
        catch (ApiException ex)
        {
            FormError = ex.FullMessage;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task Forgot()
    {
        if (AppConfig.IsValid(ServerUrl)) config.BaseUrl = ServerUrl;
        return router.PushAsync<ForgotPasswordPage>();
    }
}

public partial class ForgotPasswordViewModel(AuthService auth, AppRouter router) : BaseViewModel
{
    [ObservableProperty] public partial string Email { get; set; } = "";
    [ObservableProperty] public partial string? EmailError { get; set; }

    [RelayCommand]
    private async Task Send()
    {
        EmailError = string.IsNullOrWhiteSpace(Email) || !Email.Contains('@') ? "Informe um e-mail válido." : null;
        if (EmailError is not null) return;
        if (!await RunAsync(() => auth.ForgotPasswordAsync(Email))) return;
        await Dialogs.AlertAsync("Verifique seu e-mail", "Se o e-mail estiver cadastrado, você receberá um código para redefinir a senha.");
        await router.PushAsync<ResetPasswordPage>();
    }

    [RelayCommand]
    private Task HaveCode() => router.PushAsync<ResetPasswordPage>();
}

public partial class ResetPasswordViewModel(AuthService auth, AppRouter router) : BaseViewModel
{
    [ObservableProperty] public partial string Token { get; set; } = "";
    [ObservableProperty] public partial string NewPassword { get; set; } = "";
    [ObservableProperty] public partial string Confirm { get; set; } = "";
    [ObservableProperty] public partial string? TokenError { get; set; }
    [ObservableProperty] public partial string? PasswordError { get; set; }
    [ObservableProperty] public partial string? ConfirmError { get; set; }

    [RelayCommand]
    private async Task Save()
    {
        TokenError = string.IsNullOrWhiteSpace(Token) ? "Informe o código recebido por e-mail." : null;
        PasswordError = NewPassword.Length < 8 ? "A senha deve ter ao menos 8 caracteres." : null;
        ConfirmError = Confirm != NewPassword ? "As senhas não conferem." : null;
        if (TokenError is not null || PasswordError is not null || ConfirmError is not null) return;
        if (!await RunAsync(() => auth.ResetPasswordAsync(Token, NewPassword))) return;
        await Dialogs.AlertAsync("Senha redefinida", "Entre com a sua nova senha.");
        await router.PopToRootAsync();
    }
}

public partial class ChangePasswordViewModel(AuthService auth, AppRouter router) : BaseViewModel
{
    /// <summary>Troca obrigatória (senha temporária): nada mais é acessível até concluir.</summary>
    [ObservableProperty] public partial bool Forced { get; set; }
    [ObservableProperty] public partial string Current { get; set; } = "";
    [ObservableProperty] public partial string NewPassword { get; set; } = "";
    [ObservableProperty] public partial string Confirm { get; set; } = "";
    [ObservableProperty] public partial string? CurrentError { get; set; }
    [ObservableProperty] public partial string? PasswordError { get; set; }
    [ObservableProperty] public partial string? ConfirmError { get; set; }

    [RelayCommand]
    private async Task Save()
    {
        CurrentError = string.IsNullOrEmpty(Current) ? (Forced ? "Informe a senha temporária." : "Informe a senha atual.") : null;
        PasswordError = NewPassword.Length < 8 ? "A senha deve ter ao menos 8 caracteres, com letras e números." : null;
        ConfirmError = Confirm != NewPassword ? "As senhas não conferem." : null;
        if (CurrentError is not null || PasswordError is not null || ConfirmError is not null) return;
        if (!await RunAsync(() => auth.ChangePasswordAsync(Current, NewPassword))) return;
        Current = NewPassword = Confirm = "";
        if (Forced)
        {
            await router.RouteAsync();
        }
        else
        {
            await Dialogs.AlertAsync("Senha alterada", "Sua senha foi alterada. As sessões em outros aparelhos foram encerradas.");
            await Nav.BackAsync();
        }
    }

    [RelayCommand]
    private Task Logout() => router.LogoutAsync();
}

public partial class PrivacyViewModel(AuthService auth, ClubApi api, AppRouter router) : BaseViewModel
{
    [ObservableProperty] public partial string PolicyText { get; set; } = "";
    [ObservableProperty] public partial bool HasPolicy { get; set; }
    [ObservableProperty] public partial bool NoPolicy { get; set; }
    [ObservableProperty] public partial string VersionText { get; set; } = "";
    public string ClubName => auth.ClubName;

    protected override Task OnAppearingAsync() => HasLoaded ? Task.CompletedTask : Load();

    [RelayCommand]
    private Task Load() => LoadAsync(async () =>
    {
        var policies = await api.PoliciesAsync();
        PolicyText = policies.PrivacyPolicy?.Trim() ?? "";
        HasPolicy = PolicyText.Length > 0;
        NoPolicy = !HasPolicy;
        VersionText = string.IsNullOrWhiteSpace(policies.PrivacyPolicyVersion) ? "" : $"Versão {policies.PrivacyPolicyVersion}";
    });

    [RelayCommand]
    private async Task Accept()
    {
        if (await RunAsync(auth.AcceptPrivacyAsync)) await router.RouteAsync();
    }

    [RelayCommand]
    private Task Logout() => router.LogoutAsync();
}

public partial class UnsupportedViewModel(AuthService auth, AppRouter router) : BaseViewModel
{
    public string Greeting => $"Olá, {auth.FirstName}";
    public string ClubName => auth.ClubName;

    [RelayCommand]
    private Task Logout() => router.LogoutAsync();
}
