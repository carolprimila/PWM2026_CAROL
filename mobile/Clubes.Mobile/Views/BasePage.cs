using Clubes.Mobile.ViewModels;

namespace Clubes.Mobile.Views;

/// <summary>Página base: repassa o ciclo de vida (aparecer/desaparecer) ao view model.</summary>
public class BasePage : ContentPage
{
    public BasePage()
    {
        // Mantém o conteúdo fora de recortes, barra de status e barras do sistema (iOS e Android de ponta a ponta).
        SafeAreaEdges = SafeAreaEdges.All;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is BaseViewModel vm)
        {
            try { await vm.AppearAsync(); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        }
    }

    /// <summary>Item (BindingContext) do elemento de lista que originou o evento.</summary>
    protected static T? Ctx<T>(object? sender) where T : class => (sender as BindableObject)?.BindingContext as T;

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (BindingContext is BaseViewModel vm) vm.Disappear();
    }
}
