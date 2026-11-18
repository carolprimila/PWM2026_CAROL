using Clubes.Mobile.ViewModels;

namespace Clubes.Mobile.Views;

public partial class LoginPage : BasePage
{
    public LoginPage(LoginViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
#if DEBUG
        AddDevLogins(vm);
#endif
    }

#if DEBUG
    /// <summary>Entrada rápida com contas de teste. Existe apenas em builds Debug e só aparece se o arquivo local de contas foi gerado.</summary>
    private void AddDevLogins(LoginViewModel vm)
    {
        if (Services.DevLogins.All.Count == 0 || Content is not ScrollView { Content: VerticalStackLayout root }) return;
        var section = new VerticalStackLayout { Spacing = 8, Margin = new Thickness(0, 8, 0, 0) };
        section.Children.Add(new Label { Text = "Entrada rápida · ambiente de desenvolvimento", FontSize = 12, TextColor = Colors.Gray, HorizontalTextAlignment = TextAlignment.Center });
        foreach (var login in Services.DevLogins.All)
        {
            var button = new Button { Text = $"Entrar como {login.Label}" };
            if (Application.Current?.Resources.TryGetValue("SecondaryButton", out var style) == true && style is Style secondary) button.Style = secondary;
            button.Clicked += (_, _) =>
            {
                vm.Email = login.Email;
                vm.Password = login.Password;
                if (vm.LoginCommand.CanExecute(null)) vm.LoginCommand.Execute(null);
            };
            section.Children.Add(button);
        }
        root.Children.Insert(Math.Max(0, root.Children.Count - 1), section);
    }
#endif
}

public partial class ForgotPasswordPage : BasePage
{
    public ForgotPasswordPage(ForgotPasswordViewModel vm) { InitializeComponent(); BindingContext = vm; }
}

public partial class ResetPasswordPage : BasePage
{
    public ResetPasswordPage(ResetPasswordViewModel vm) { InitializeComponent(); BindingContext = vm; }
}

public partial class ChangePasswordPage : BasePage
{
    public ChangePasswordPage(ChangePasswordViewModel vm) { InitializeComponent(); BindingContext = vm; }
}

public partial class PrivacyPage : BasePage
{
    public PrivacyPage(PrivacyViewModel vm) { InitializeComponent(); BindingContext = vm; }
}

public partial class UnsupportedPage : BasePage
{
    public UnsupportedPage(UnsupportedViewModel vm) { InitializeComponent(); BindingContext = vm; }
}
