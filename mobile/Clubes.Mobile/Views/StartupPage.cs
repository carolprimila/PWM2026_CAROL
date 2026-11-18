using Clubes.Mobile.Controls;
using Clubes.Mobile.Services;

namespace Clubes.Mobile.Views;

/// <summary>Tela inicial: restaura a sessão salva e encaminha para o login ou para o app.</summary>
public class StartupPage : ContentPage
{
    private readonly AppRouter _router;
    private readonly ActivityIndicator _spinner = new() { IsRunning = true, Color = Colors.White, HorizontalOptions = LayoutOptions.Center };
    private readonly Label _message = new() { TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center, IsVisible = false };
    private readonly Button _retry = new() { Text = "Tentar novamente", IsVisible = false, BackgroundColor = Colors.White, TextColor = Palette.Primary, HorizontalOptions = LayoutOptions.Center };
    private readonly Button _logout = new() { Text = "Sair desta conta", IsVisible = false, BackgroundColor = Colors.Transparent, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center };
    private bool _started;

    public StartupPage(AppRouter router)
    {
        _router = router;
        BackgroundColor = Palette.Primary;
        _retry.Clicked += async (_, _) => await StartAsync();
        _logout.Clicked += async (_, _) => await _router.LogoutAsync();
        Content = new VerticalStackLayout
        {
            Spacing = 16, Padding = 32, VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label { Text = "Clubes", TextColor = Colors.White, FontFamily = "OpenSansSemibold", FontSize = 30, HorizontalTextAlignment = TextAlignment.Center },
                _spinner, _message, _retry, _logout,
            },
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_started) return;
        _started = true;
        await StartAsync();
    }

    private async Task StartAsync()
    {
        _spinner.IsVisible = _spinner.IsRunning = true;
        _message.IsVisible = _retry.IsVisible = _logout.IsVisible = false;
        try
        {
            await _router.StartAsync();
        }
        catch (ApiException ex)
        {
            _spinner.IsVisible = _spinner.IsRunning = false;
            _message.Text = ex.Message;
            _message.IsVisible = _retry.IsVisible = _logout.IsVisible = true;
        }
    }
}
