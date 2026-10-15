using CleanVander.Client.Views;
namespace CleanVander.Client;
public partial class App : Application
{
    public App() => InitializeComponent();
    protected override Window CreateWindow(IActivationState? activationState) => new(new NavigationPage(new FrmInicio()));
}
