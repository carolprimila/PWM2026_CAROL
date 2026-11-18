using Clubes.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Clubes.Mobile.ViewModels;

/// <summary>Base dos view models: estados de carregamento, erro e operação em andamento.</summary>
public abstract partial class BaseViewModel : ObservableObject, IQueryAttributable
{
    private bool _loading;

    /// <summary>Primeira carga em andamento (exibe o indicador de tela cheia).</summary>
    [ObservableProperty] public partial bool IsLoading { get; set; }

    /// <summary>Atualização por "puxar para atualizar".</summary>
    [ObservableProperty] public partial bool IsRefreshing { get; set; }

    /// <summary>Operação aguardando a confirmação do servidor.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    public partial bool IsBusy { get; set; }

    /// <summary>Mensagem de erro da carga (exibe o estado de erro com "Tentar novamente").</summary>
    [ObservableProperty] public partial string? ErrorMessage { get; set; }

    [ObservableProperty] public partial bool HasLoaded { get; set; }

    public bool IsNotBusy => !IsBusy;

    /// <summary>A página está visível.</summary>
    public bool IsVisible { get; private set; }

    public async Task AppearAsync()
    {
        IsVisible = true;
        await OnAppearingAsync();
    }

    public void Disappear()
    {
        IsVisible = false;
        OnDisappearing();
    }

    protected virtual Task OnAppearingAsync() => Task.CompletedTask;
    protected virtual void OnDisappearing() { }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.Count > 0) OnParameters(query);
    }

    protected virtual void OnParameters(IDictionary<string, object> query) { }

    protected static Guid IdFrom(IDictionary<string, object> query, string key = "id") =>
        query.TryGetValue(key, out var value) ? value switch { Guid g => g, string s when Guid.TryParse(s, out var g) => g, _ => Guid.Empty } : Guid.Empty;

    /// <summary>Carrega dados da tela. Na primeira vez mostra o carregamento e, em falha, o estado de erro; depois atualiza em silêncio.</summary>
    protected async Task LoadAsync(Func<Task> work)
    {
        if (_loading) { IsRefreshing = false; return; }
        _loading = true;
        var userRefresh = IsRefreshing;
        if (!HasLoaded)
        {
            ErrorMessage = null;
            if (!userRefresh) IsLoading = true;
        }
        try
        {
            await work();
            HasLoaded = true;
            ErrorMessage = null;
        }
        catch (ApiException ex)
        {
            if (ex.Code == "sessao_expirada") return;
            if (!HasLoaded) ErrorMessage = ex.Message;
            else if (userRefresh) await Dialogs.AlertAsync("Não foi possível atualizar", ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            if (!HasLoaded) ErrorMessage = "Ocorreu um erro inesperado ao carregar os dados.";
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
            _loading = false;
        }
    }

    /// <summary>Executa uma operação; nada é mostrado como concluído antes da resposta do servidor. Em falha, exibe a mensagem da API.</summary>
    protected async Task<bool> RunAsync(Func<Task> work, string errorTitle = "Não foi possível concluir")
    {
        if (IsBusy) return false;
        IsBusy = true;
        try
        {
            await work();
            return true;
        }
        catch (ApiException ex)
        {
            if (ex.Code != "sessao_expirada") await Dialogs.AlertAsync(errorTitle, ex.FullMessage);
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await Dialogs.AlertAsync(errorTitle, "Ocorreu um erro inesperado neste aparelho. Tente novamente.");
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
