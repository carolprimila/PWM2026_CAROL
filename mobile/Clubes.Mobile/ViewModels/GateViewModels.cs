using System.Collections.ObjectModel;
using Clubes.Mobile.Models;
using Clubes.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Clubes.Mobile.ViewModels;

/// <summary>Leitor da portaria: valida o QR Code (ou o código numérico) e mostra o resultado devolvido pelo servidor.</summary>
public partial class GateScannerViewModel(ClubApi api, AuthService auth) : BaseViewModel
{
    private static readonly TimeSpan Cooldown = TimeSpan.FromMilliseconds(2500);
    private DateTime _resumeAt = DateTime.MinValue;
    private int _resultVersion;

    public string ClubName => auth.ClubName;

    [ObservableProperty] public partial bool CameraAllowed { get; set; }
    [ObservableProperty] public partial bool CameraDenied { get; set; }
    /// <summary>A câmera lê códigos somente com a tela visível, sem requisição em andamento e fora do intervalo após um resultado.</summary>
    [ObservableProperty] public partial bool IsDetecting { get; set; }
    [ObservableProperty] public partial string ManualCode { get; set; } = "";
    [ObservableProperty] public partial string? ManualError { get; set; }

    [ObservableProperty] public partial bool HasResult { get; set; }
    [ObservableProperty] public partial bool Authorized { get; set; }
    [ObservableProperty] public partial bool Denied { get; set; }
    [ObservableProperty] public partial string ResultTitle { get; set; } = "";
    [ObservableProperty] public partial string ResultMessage { get; set; } = "";
    [ObservableProperty] public partial string PersonName { get; set; } = "";
    [ObservableProperty] public partial bool HasPerson { get; set; }
    [ObservableProperty] public partial string PersonDetail { get; set; } = "";
    [ObservableProperty] public partial Guid? PhotoFileId { get; set; }

    protected override async Task OnAppearingAsync()
    {
        if (!CameraAllowed)
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted) status = await Permissions.RequestAsync<Permissions.Camera>();
            CameraAllowed = status == PermissionStatus.Granted;
            CameraDenied = !CameraAllowed;
        }
        UpdateDetecting();
    }

    protected override void OnDisappearing() => IsDetecting = false;

    private void UpdateDetecting() => IsDetecting = IsVisible && CameraAllowed && !IsBusy && DateTime.UtcNow >= _resumeAt;

    /// <summary>Chamado pela câmera a cada leitura. Leituras durante uma validação ou no intervalo de segurança são ignoradas.</summary>
    public Task OnScannedAsync(string? code)
    {
        if (string.IsNullOrWhiteSpace(code) || IsBusy || DateTime.UtcNow < _resumeAt || !IsVisible) return Task.CompletedTask;
        return ValidateAsync(code.Trim());
    }

    [RelayCommand]
    private async Task ValidateManual()
    {
        var digits = Masks.Digits(ManualCode);
        ManualError = digits.Length != 8 ? "Digite os 8 dígitos exibidos abaixo do QR Code." : null;
        if (ManualError is not null) return;
        await ValidateAsync(digits);
        ManualCode = "";
    }

    private async Task ValidateAsync(string code)
    {
        if (IsBusy) return;
        IsBusy = true;
        IsDetecting = false;
        try
        {
            var result = await api.ValidateAccessAsync(code);
            Show(result.Authorized,
                result.Authorized ? (result.Operation == AccessOperation.Exit ? "Saída registrada" : "Entrada autorizada") : "Acesso negado",
                result.Message, result.PersonName, result.PersonType, result.ResponsibleName, result.PhotoFileId);
        }
        catch (ApiException ex)
        {
            if (ex.Code != "sessao_expirada") Show(false, ex.IsNetwork ? "Sem conexão" : "Não validado", ex.FullMessage, null, null, null, null);
        }
        finally
        {
            IsBusy = false;
            // Intervalo para o mesmo QR Code não ser enviado duas vezes.
            _resumeAt = DateTime.UtcNow + Cooldown;
            _ = ResumeLaterAsync();
        }
    }

    private void Show(bool authorized, string title, string message, string? person, PersonType? type, string? responsible, Guid? photo)
    {
        _resultVersion++;
        Authorized = authorized;
        Denied = !authorized;
        ResultTitle = title;
        ResultMessage = message;
        PersonName = person ?? "";
        HasPerson = !string.IsNullOrWhiteSpace(person);
        PersonDetail = string.Join(" · ", new[] { type is { } t ? Labels.Of(t) : null, string.IsNullOrWhiteSpace(responsible) ? null : $"Responsável: {responsible}" }.Where(v => v is not null));
        PhotoFileId = photo;
        HasResult = true;
    }

    private async Task ResumeLaterAsync()
    {
        await Task.Delay(Cooldown + TimeSpan.FromMilliseconds(50));
        UpdateDetecting();
    }

    [RelayCommand]
    private void ClearResult()
    {
        HasResult = false;
        PhotoFileId = null;
    }

    [RelayCommand] private Task Guests() => Nav.GoToAsync("portaria-convidados");
    [RelayCommand] private Task Search() => Nav.GoToAsync("portaria-busca");
    [RelayCommand] private void OpenSettings() => AppInfo.Current.ShowSettingsUI();
}

public partial class GateGuestsViewModel(ClubApi api, RealtimeService realtime) : BaseViewModel
{
    private bool _subscribed;
    public string DateText { get; } = $"Convidados de hoje · {DateTime.Today.ToString("dd/MM/yyyy", Fmt.Br)}";
    public ObservableCollection<GuestDto> Items { get; } = [];

    protected override Task OnAppearingAsync()
    {
        if (!_subscribed) { _subscribed = true; realtime.OccupancyChanged += OnOccupancyChanged; }
        return Load();
    }

    protected override void OnDisappearing()
    {
        if (_subscribed) { _subscribed = false; realtime.OccupancyChanged -= OnOccupancyChanged; }
    }

    private void OnOccupancyChanged()
    {
        if (IsVisible && !IsBusy) _ = Load();
    }

    [RelayCommand]
    private Task Load() => LoadAsync(async () =>
        Items.ReplaceWith((await api.GuestsOfDayAsync(DateOnly.FromDateTime(DateTime.Today))).Items.Where(g => !g.IsCancelled).OrderBy(g => g.FullName)));

    [RelayCommand] private Task Entry(GuestDto? guest) => RegisterAsync(guest, true);
    [RelayCommand] private Task Exit(GuestDto? guest) => RegisterAsync(guest, false);

    private async Task RegisterAsync(GuestDto? guest, bool entry)
    {
        if (guest is null) return;
        AccessResultDto? result = null;
        if (!await RunAsync(async () => result = entry ? await api.GuestEntryAsync(guest.Id) : await api.GuestExitAsync(guest.Id))) return;
        await Dialogs.AlertAsync(result!.Authorized ? (entry ? "Entrada registrada" : "Saída registrada") : "Acesso negado", $"{guest.FullName}\n{result.Message}");
        await Load();
    }
}

public partial class GatePresentViewModel(ClubApi api, RealtimeService realtime) : BaseViewModel
{
    private bool _subscribed;

    [ObservableProperty] public partial int Members { get; set; }
    [ObservableProperty] public partial int Dependents { get; set; }
    [ObservableProperty] public partial int Guests { get; set; }
    [ObservableProperty] public partial int Total { get; set; }
    public ObservableCollection<PresenceDto> Items { get; } = [];

    protected override Task OnAppearingAsync()
    {
        if (!_subscribed) { _subscribed = true; realtime.OccupancyChanged += OnOccupancyChanged; }
        return Load();
    }

    protected override void OnDisappearing()
    {
        if (_subscribed) { _subscribed = false; realtime.OccupancyChanged -= OnOccupancyChanged; }
    }

    private void OnOccupancyChanged()
    {
        if (IsVisible) _ = Load();
    }

    [RelayCommand]
    private Task Load() => LoadAsync(async () =>
    {
        var presentTask = api.PresentAsync();
        var occupancyTask = api.OccupancyAsync();
        await Task.WhenAll(presentTask, occupancyTask);
        var o = occupancyTask.Result;
        (Members, Dependents, Guests, Total) = (o.Members, o.Dependents, o.Guests, o.Total);
        Items.ReplaceWith(presentTask.Result);
    });
}

public partial class GateSearchViewModel(ClubApi api) : BaseViewModel
{
    [ObservableProperty] public partial string Term { get; set; } = "";
    [ObservableProperty] public partial string? TermError { get; set; }
    [ObservableProperty] public partial bool Searched { get; set; }
    public ObservableCollection<GatePersonDto> Items { get; } = [];

    [RelayCommand]
    private async Task Search()
    {
        var term = Term.Trim();
        TermError = term.Length < 2 ? "Digite ao menos 2 caracteres do nome ou do documento." : null;
        if (TermError is not null) return;
        List<GatePersonDto>? result = null;
        if (!await RunAsync(async () => result = await api.SearchPeopleAsync(term), "Busca não realizada")) return;
        Items.ReplaceWith(result!);
        Searched = true;
    }

    /// <summary>Registro manual de entrada ou saída, com justificativa obrigatória.</summary>
    [RelayCommand]
    private async Task Register(GatePersonDto? person)
    {
        if (person is null) return;
        var operation = person.IsInside ? AccessOperation.Exit : AccessOperation.Entry;
        var label = Labels.Of(operation).ToLowerInvariant();
        var justification = await Dialogs.PromptAsync($"Registrar {label} manual", $"{person.Name}\nInforme a justificativa do registro manual (obrigatória).", "Justificativa", 300);
        if (justification is null) return;
        if (justification.Trim().Length < 5)
        {
            await Dialogs.AlertAsync("Justificativa obrigatória", "Descreva a justificativa com ao menos 5 caracteres.");
            return;
        }
        AccessResultDto? result = null;
        if (!await RunAsync(async () => result = await api.ManualAccessAsync(new ManualAccessRequest(person.PersonType, person.PersonId, operation, justification.Trim())))) return;
        await Dialogs.AlertAsync(result!.Authorized ? $"{Labels.Of(operation)} registrada" : "Acesso negado", $"{person.Name}\n{result.Message}");
        if (Term.Trim().Length >= 2) await Search();
    }
}
