using Clubes.Mobile.Services;
using Clubes.Mobile.Views;

namespace Clubes.Mobile;

public partial class App : Application
{
    private readonly AppRouter _router;

    public App(AppRouter router)
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Light;
        _router = router;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new StartupPage(_router)) { Title = "Clubes" };
        window.Resumed += async (_, _) =>
        {
            try { await _router.OnResumedAsync(); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        };
        window.Stopped += (_, _) => _router.OnStopped();
        return window;
    }
}
