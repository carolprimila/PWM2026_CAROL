using System.Collections.ObjectModel;
using Clubes.Mobile.Controls;
using Clubes.Mobile.Models;
using Clubes.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Clubes.Mobile.ViewModels;

public static class CollectionExtensions
{
    public static void ReplaceWith<T>(this ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items) target.Add(item);
    }
}

public partial class ReservationsViewModel(ClubApi api, AuthService auth) : BaseViewModel
{
    public ObservableCollection<ReservationDto> Items { get; } = [];
    public bool CanReserve => auth.CanReserve;

    protected override Task OnAppearingAsync() => Load();

    [RelayCommand]
    private Task Load() => LoadAsync(async () => Items.ReplaceWith((await api.ReservationsAsync()).Items));

    [RelayCommand] private Task New() => Nav.GoToAsync("espacos");
    [RelayCommand] private Task Open(ReservationDto? item) => item is null ? Task.CompletedTask : Nav.GoToAsync("reserva", item.Id);
}

public partial class ReservationDetailViewModel(ClubApi api) : BaseViewModel
{
    private Guid _id;

    [ObservableProperty] public partial ReservationDto? Reservation { get; set; }
    [ObservableProperty] public partial string RentalText { get; set; } = "";
    [ObservableProperty] public partial string CleaningText { get; set; } = "";
    [ObservableProperty] public partial string TotalText { get; set; } = "";
    [ObservableProperty] public partial string ChargeModeText { get; set; } = "";
    [ObservableProperty] public partial string PeopleText { get; set; } = "";
    [ObservableProperty] public partial string RequestedText { get; set; } = "";
    [ObservableProperty] public partial bool HasNotes { get; set; }
    [ObservableProperty] public partial bool HasReason { get; set; }
    [ObservableProperty] public partial string ReasonText { get; set; } = "";
    [ObservableProperty] public partial bool HasGuests { get; set; }
    [ObservableProperty] public partial bool HasHistory { get; set; }
    [ObservableProperty] public partial bool CanCancel { get; set; }

    public ObservableCollection<ReservationSlotDto> Slots { get; } = [];
    public ObservableCollection<GuestDto> Guests { get; } = [];
    public ObservableCollection<HistoryDto> History { get; } = [];

    protected override void OnParameters(IDictionary<string, object> query) => _id = IdFrom(query);
    protected override Task OnAppearingAsync() => Load();

    [RelayCommand]
    private Task Load() => LoadAsync(async () => Apply(await api.ReservationAsync(_id)));

    private void Apply(ReservationDetailDto detail)
    {
        var r = detail.Reservation;
        Reservation = r;
        RentalText = Fmt.Money(r.RentalAmount);
        CleaningText = Fmt.Money(r.CleaningFee);
        TotalText = Fmt.Money(r.TotalAmount);
        ChargeModeText = Labels.Of(r.ChargeMode);
        PeopleText = r.PeopleCount == 1 ? "1 pessoa" : $"{r.PeopleCount} pessoas";
        RequestedText = $"Solicitada por {r.RequestedBy} em {Fmt.DateTime(r.CreatedAt)}";
        HasNotes = !string.IsNullOrWhiteSpace(r.Notes);
        var reason = r.Status == ReservationStatus.Cancelled ? r.CancellationReason : r.DecisionReason;
        HasReason = !string.IsNullOrWhiteSpace(reason);
        ReasonText = HasReason ? $"Motivo: {reason}" : "";
        // O botão aparece para reservas ainda vigentes; o servidor decide se o cancelamento é permitido.
        CanCancel = r.Status is ReservationStatus.Pending or ReservationStatus.Approved or ReservationStatus.Suspended;
        Slots.ReplaceWith(r.Slots);
        Guests.ReplaceWith(detail.Guests);
        History.ReplaceWith(detail.History.OrderByDescending(h => h.At));
        HasGuests = Guests.Count > 0;
        HasHistory = History.Count > 0;
    }

    [RelayCommand]
    private async Task Cancel()
    {
        if (Reservation is null) return;
        if (!await Dialogs.ConfirmAsync("Cancelar reserva", $"Deseja cancelar a reserva de {Reservation.SpaceName} ({Reservation.WhenText})? Esta ação não pode ser desfeita.", "Cancelar reserva", "Manter")) return;
        var reason = await Dialogs.PromptAsync("Motivo do cancelamento", "Informe o motivo (opcional).", "Motivo", 500);
        if (reason is null) return;
        ReservationDetailDto? updated = null;
        if (!await RunAsync(async () => updated = await api.CancelReservationAsync(_id, string.IsNullOrWhiteSpace(reason) ? null : reason.Trim()))) return;
        Apply(updated!);
        await Dialogs.AlertAsync("Reserva cancelada", "O cancelamento foi registrado.");
    }
}

public class SpaceGroup(string title, IEnumerable<SpaceDto> items) : List<SpaceDto>(items)
{
    public string Title { get; } = title;
}

public partial class SpacePickerViewModel(ClubApi api) : BaseViewModel
{
    public ObservableCollection<SpaceGroup> Groups { get; } = [];

    protected override Task OnAppearingAsync() => HasLoaded ? Task.CompletedTask : Load();

    [RelayCommand]
    private Task Load() => LoadAsync(async () =>
    {
        var spaces = (await api.SpacesAsync()).Where(s => s.IsReservable && s.IsActive).ToList();
        Groups.ReplaceWith(spaces.GroupBy(s => s.Type).OrderBy(g => g.Key)
            .Select(g => new SpaceGroup(Labels.Of(g.Key), g.OrderBy(s => s.SortOrder).ThenBy(s => s.Name))));
    });

    [RelayCommand] private Task Open(SpaceDto? space) => space is null ? Task.CompletedTask : Nav.GoToAsync("nova-reserva", space.Id);
}

/// <summary>Horário de um dia (espaços reservados por hora).</summary>
public partial class SlotItem(SlotDto dto) : ObservableObject
{
    public int StartMinute => dto.StartMinute;
    public string Label => Fmt.Minutes(dto.StartMinute);
    public bool IsAvailable => dto.State == SlotState.Available;
    public ChoiceTone Tone => dto.Mine ? ChoiceTone.Mine : IsAvailable ? ChoiceTone.Normal : ChoiceTone.Unavailable;
    public string? Caption => dto.Mine ? "sua reserva" : dto.State switch
    {
        SlotState.Reserved => "reservado", SlotState.Blocked => "bloqueado", SlotState.Past => "encerrado", SlotState.Closed => "fechado", _ => $"até {Fmt.Minutes(dto.EndMinute)}",
    };
    [ObservableProperty] public partial bool IsSelected { get; set; }
    public int Column { get; set; }
}

public class SlotRow(IEnumerable<SlotItem> items) : List<SlotItem>(items);
public class DayRow(IEnumerable<DayItem> items) : List<DayItem>(items);

/// <summary>Data de início possível (espaços reservados por período, como o salão de festas).</summary>
public partial class DayItem(PeriodDayDto dto) : ObservableObject
{
    public DateOnly Date => dto.Date;
    public string Label => dto.Date.ToString("dd/MM", Fmt.Br);
    public bool IsAvailable => dto.State == SlotState.Available;
    public ChoiceTone Tone => IsAvailable ? ChoiceTone.Normal : ChoiceTone.Unavailable;
    public string Caption => IsAvailable ? dto.Date.ToString("ddd", Fmt.Br) : dto.State switch
    {
        SlotState.Reserved => "reservado", SlotState.Blocked => "bloqueado", SlotState.Past => "encerrado", _ => "fechado",
    };
    [ObservableProperty] public partial bool IsSelected { get; set; }
    public int Column { get; set; }
}

/// <summary>Nova reserva: horários ou período, pessoas, convidados, simulação de valores e confirmação.</summary>
public partial class NewReservationViewModel(ClubApi api, RealtimeService realtime) : BaseViewModel
{
    private const int PeriodWindowDays = 60;
    private Guid _spaceId;
    private bool _subscribed;
    private bool _reloading;
    private int _periodDays = PeriodWindowDays;

    [ObservableProperty] public partial SpaceDto? Space { get; set; }
    [ObservableProperty] public partial bool IsHourly { get; set; }
    [ObservableProperty] public partial bool IsMultiDay { get; set; }
    [ObservableProperty] public partial string PeriodExplanation { get; set; } = "";
    [ObservableProperty] public partial bool HasUsageRules { get; set; }

    [ObservableProperty] public partial DateTime? Date { get; set; } = DateTime.Today;
    private DateOnly SelectedDate => DateOnly.FromDateTime(Date ?? DateTime.Today);
    public DateTime MinDate { get; } = DateTime.Today;
    [ObservableProperty] public partial bool IsLoadingAvailability { get; set; }
    [ObservableProperty] public partial string? DayMessage { get; set; }
    [ObservableProperty] public partial bool HasDayMessage { get; set; }
    [ObservableProperty] public partial string SelectionText { get; set; } = "";

    [ObservableProperty] public partial string PeopleCount { get; set; } = "1";
    [ObservableProperty] public partial string? PeopleError { get; set; }
    [ObservableProperty] public partial string Notes { get; set; } = "";

    [ObservableProperty] public partial int MaxGuests { get; set; }
    [ObservableProperty] public partial string GuestsHint { get; set; } = "";
    [ObservableProperty] public partial bool CanAddGuest { get; set; }
    [ObservableProperty] public partial string GuestName { get; set; } = "";
    [ObservableProperty] public partial string GuestDocument { get; set; } = "";
    [ObservableProperty] public partial string GuestPhone { get; set; } = "";
    [ObservableProperty] public partial string? GuestError { get; set; }

    [ObservableProperty] public partial QuoteDto? Quote { get; set; }
    [ObservableProperty] public partial bool HasQuote { get; set; }
    [ObservableProperty] public partial bool NoQuote { get; set; } = true;
    [ObservableProperty] public partial string QuoteRentalText { get; set; } = "";
    [ObservableProperty] public partial string QuoteCleaningText { get; set; } = "";
    [ObservableProperty] public partial string QuoteTotalText { get; set; } = "";
    [ObservableProperty] public partial string QuoteChargeText { get; set; } = "";
    [ObservableProperty] public partial string QuoteApprovalText { get; set; } = "";
    [ObservableProperty] public partial string QuotePeriodText { get; set; } = "";

    private const int Columns = 4;
    public List<SlotItem> Slots { get; private set; } = [];
    public List<DayItem> Days { get; private set; } = [];
    /// <summary>Grade exibida: linhas de até 4 chips.</summary>
    public ObservableCollection<SlotRow> SlotRows { get; } = [];
    public ObservableCollection<DayRow> DayRows { get; } = [];

    private void SetSlots(List<SlotItem> items)
    {
        for (var i = 0; i < items.Count; i++) items[i].Column = i % Columns;
        Slots = items;
        SlotRows.ReplaceWith(items.Chunk(Columns).Select(c => new SlotRow(c)));
    }

    private void SetDays(List<DayItem> items)
    {
        for (var i = 0; i < items.Count; i++) items[i].Column = i % Columns;
        Days = items;
        DayRows.ReplaceWith(items.Chunk(Columns).Select(c => new DayRow(c)));
    }
    public ObservableCollection<GuestInput> Guests { get; } = [];
    public ObservableCollection<string> QuoteNotes { get; } = [];

    protected override void OnParameters(IDictionary<string, object> query) => _spaceId = IdFrom(query);

    protected override async Task OnAppearingAsync()
    {
        if (!_subscribed)
        {
            _subscribed = true;
            realtime.AvailabilityChanged += OnAvailabilityChanged;
        }
        if (!HasLoaded) await Load();
        else await ReloadAvailabilityAsync(keepSelection: true);
    }

    protected override void OnDisappearing()
    {
        if (!_subscribed) return;
        _subscribed = false;
        realtime.AvailabilityChanged -= OnAvailabilityChanged;
    }

    [RelayCommand]
    private Task Load() => LoadAsync(async () =>
    {
        var spaceTask = api.SpaceAsync(_spaceId);
        var policiesTask = api.PoliciesAsync();
        await Task.WhenAll(spaceTask, policiesTask);
        var space = spaceTask.Result;
        Space = space;
        IsMultiDay = space.IsMultiDay;
        IsHourly = !space.IsMultiDay;
        HasUsageRules = !string.IsNullOrWhiteSpace(space.UsageRules);
        PeriodExplanation = $"A reserva deste espaço ocupa {space.FixedDays} dias consecutivos a partir da data escolhida, cobrindo a montagem, o evento e a limpeza. Escolha a data de início entre as disponíveis.";
        MaxGuests = policiesTask.Result.MaxGuestsPerMember;
        UpdateGuestsHint();
        await FetchAvailabilityAsync(keepSelection: false);
    });

    partial void OnDateChanged(DateTime? value)
    {
        if (HasLoaded && IsHourly) _ = ReloadAvailabilityAsync(keepSelection: false);
    }

    partial void OnPeopleCountChanged(string value) => InvalidateQuote();
    partial void OnNotesChanged(string value) => InvalidateQuote();

    private void OnAvailabilityChanged(Guid? spaceId)
    {
        // Outro usuário reservou ou a administração bloqueou: recarrega a grade enquanto a tela está aberta.
        if (HasLoaded && IsVisible && (spaceId is null || spaceId == _spaceId)) _ = ReloadAvailabilityAsync(keepSelection: true);
    }

    private async Task ReloadAvailabilityAsync(bool keepSelection)
    {
        if (_reloading) return;
        _reloading = true;
        IsLoadingAvailability = true;
        try
        {
            await FetchAvailabilityAsync(keepSelection);
        }
        catch (ApiException ex)
        {
            if (ex.Code != "sessao_expirada")
            {
                SetSlots([]);
                SetDays([]);
                DayMessage = ex.Message;
                HasDayMessage = true;
                UpdateSelectionText();
            }
        }
        finally
        {
            IsLoadingAvailability = false;
            _reloading = false;
        }
    }

    private async Task FetchAvailabilityAsync(bool keepSelection)
    {
        if (IsHourly)
        {
            var requestedDate = SelectedDate;
            var day = await api.DayAvailabilityAsync(_spaceId, requestedDate);
            if (requestedDate != SelectedDate) return; // a data mudou enquanto carregava
            // A seleção é lida depois da resposta, para não perder toques feitos durante a atualização.
            var selected = keepSelection ? Slots.Where(s => s.IsSelected).Select(s => s.StartMinute).ToHashSet() : [];
            var items = day.Slots.Select(s => new SlotItem(s)).ToList();
            var lost = false;
            foreach (var item in items)
            {
                if (!selected.Contains(item.StartMinute)) continue;
                if (item.IsAvailable) item.IsSelected = true; else lost = true;
            }
            SetSlots(items);
            DayMessage = !day.IsOpen ? (string.IsNullOrWhiteSpace(day.ClosedReason) ? "O espaço não abre nesta data." : day.ClosedReason)
                : items.Count == 0 ? "Não há horários para esta data." : null;
            HasDayMessage = DayMessage is not null;
            if (!keepSelection || lost || selected.Count != items.Count(i => i.IsSelected)) InvalidateQuote();
            if (lost) await Dialogs.AlertAsync("Horário indisponível", "Um dos horários escolhidos acabou de ser reservado. Selecione outro horário.");
        }
        else
        {
            var from = DateOnly.FromDateTime(DateTime.Today);
            var days = await api.PeriodAvailabilityAsync(_spaceId, from, from.AddDays(_periodDays));
            var selected = keepSelection ? Days.FirstOrDefault(d => d.IsSelected)?.Date : null;
            var items = days.Select(d => new DayItem(d)).ToList();
            var lost = false;
            if (selected is { } date)
            {
                var match = items.FirstOrDefault(i => i.Date == date);
                if (match is { IsAvailable: true }) match.IsSelected = true; else lost = true;
            }
            SetDays(items);
            DayMessage = items.Any(i => i.IsAvailable) ? null : "Não há datas de início disponíveis neste período.";
            HasDayMessage = DayMessage is not null;
            if (!keepSelection || lost) InvalidateQuote();
            if (lost) await Dialogs.AlertAsync("Data indisponível", "A data escolhida acabou de ser reservada. Selecione outra data.");
        }
        UpdateSelectionText();
    }

    [RelayCommand]
    private async Task MoreDays()
    {
        _periodDays += PeriodWindowDays;
        await ReloadAvailabilityAsync(keepSelection: true);
    }

    public void ToggleSlot(SlotItem? slot)
    {
        if (slot is null || !slot.IsAvailable || IsBusy) return;
        slot.IsSelected = !slot.IsSelected;
        InvalidateQuote();
        UpdateSelectionText();
    }

    public void SelectDay(DayItem? day)
    {
        if (day is null || !day.IsAvailable || IsBusy) return;
        foreach (var d in Days) d.IsSelected = ReferenceEquals(d, day);
        InvalidateQuote();
        UpdateSelectionText();
    }

    private void UpdateSelectionText()
    {
        if (IsHourly)
        {
            var selected = Slots.Where(s => s.IsSelected).OrderBy(s => s.StartMinute).ToList();
            SelectionText = selected.Count == 0 ? "Toque nos horários disponíveis para selecionar."
                : $"{selected.Count} horário(s): {string.Join(", ", selected.Select(s => s.Label))}";
        }
        else
        {
            var day = Days.FirstOrDefault(d => d.IsSelected);
            SelectionText = day is null || Space is null ? "Toque em uma data disponível para selecionar o início."
                : $"De {Fmt.Date(day.Date)} a {Fmt.Date(day.Date.AddDays(Math.Max(1, Space.FixedDays) - 1))}";
        }
    }

    // ----- Convidados -----
    [RelayCommand]
    private void AddGuest()
    {
        var name = GuestName.Trim();
        GuestError = name.Length < 3 ? "Informe o nome completo do convidado." : null;
        if (GuestError is not null || !CanAddGuest) return;
        Guests.Add(new GuestInput
        {
            FullName = name,
            Document = string.IsNullOrWhiteSpace(GuestDocument) ? null : GuestDocument.Trim(),
            Phone = string.IsNullOrWhiteSpace(GuestPhone) ? null : GuestPhone.Trim(),
        });
        GuestName = GuestDocument = GuestPhone = "";
        UpdateGuestsHint();
        InvalidateQuote();
    }

    public void RemoveGuest(GuestInput? guest)
    {
        if (guest is null || IsBusy) return;
        Guests.Remove(guest);
        UpdateGuestsHint();
        InvalidateQuote();
    }

    private void UpdateGuestsHint()
    {
        CanAddGuest = Guests.Count < MaxGuests;
        GuestsHint = MaxGuests <= 0 ? "O clube não permite convidados nas reservas."
            : $"{Guests.Count} de {MaxGuests} convidados permitidos por associado.";
    }

    // ----- Simulação e confirmação -----
    private void InvalidateQuote()
    {
        if (Quote is null && !HasQuote) return;
        Quote = null;
        HasQuote = false;
        NoQuote = true;
        QuoteNotes.Clear();
    }

    private CreateReservationRequest? BuildRequest()
    {
        if (Space is null) return null;
        var request = new CreateReservationRequest
        {
            SpaceId = Space.Id,
            Guests = Guests.ToList(),
            Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
        };
        if (IsHourly)
        {
            request.Date = SelectedDate;
            request.StartMinutes = Slots.Where(s => s.IsSelected).Select(s => s.StartMinute).OrderBy(m => m).ToList();
            if (request.StartMinutes.Count == 0)
            {
                _ = Dialogs.AlertAsync("Selecione os horários", "Escolha ao menos um horário disponível.");
                return null;
            }
        }
        else
        {
            var day = Days.FirstOrDefault(d => d.IsSelected);
            if (day is null)
            {
                _ = Dialogs.AlertAsync("Selecione a data", "Escolha a data de início do período.");
                return null;
            }
            request.Date = day.Date;
        }
        if (!int.TryParse(PeopleCount, out var people) || people < 1) PeopleError = "Informe a quantidade de pessoas.";
        else if (Space.Capacity is { } capacity && people > capacity) PeopleError = $"A capacidade do espaço é de {capacity} pessoas.";
        else PeopleError = null;
        if (PeopleError is not null) return null;
        request.PeopleCount = people;
        return request;
    }

    [RelayCommand]
    private async Task Simulate()
    {
        var request = BuildRequest();
        if (request is null) return;
        QuoteDto? quote = null;
        if (!await SubmitAsync(async () => quote = await api.QuoteReservationAsync(request))) return;
        Quote = quote;
        QuoteRentalText = Fmt.Money(quote!.RentalAmount);
        QuoteCleaningText = Fmt.Money(quote.CleaningFee);
        QuoteTotalText = Fmt.Money(quote.TotalAmount);
        QuoteChargeText = Labels.Of(quote.ChargeMode);
        QuoteApprovalText = quote.RequiresApproval ? "A reserva ficará aguardando a aprovação da administração." : "A reserva é confirmada automaticamente.";
        QuotePeriodText = Fmt.Period(quote.StartAt, quote.EndAt);
        QuoteNotes.ReplaceWith(quote.Notes);
        HasQuote = true;
        NoQuote = false;
    }

    [RelayCommand]
    private async Task Confirm()
    {
        var request = BuildRequest();
        if (request is null || Quote is null || Space is null) return;
        if (!await Dialogs.ConfirmAsync("Confirmar reserva", $"{Space.Name}\n{QuotePeriodText}\nTotal: {QuoteTotalText}\n\nDeseja enviar a solicitação?", "Reservar")) return;
        ReservationDetailDto? created = null;
        if (!await SubmitAsync(async () => created = await api.CreateReservationAsync(request))) return;
        var r = created!.Reservation;
        await Dialogs.AlertAsync(r.Status == ReservationStatus.Approved ? "Reserva confirmada" : "Solicitação enviada",
            $"Reserva {r.Code} — {r.StatusText}.");
        await Nav.ToRootAsync();
        await Nav.GoToAsync("reserva", r.Id);
    }

    /// <summary>Envia ao servidor; em conflito (409: horário recém-ocupado) mostra a mensagem e recarrega a disponibilidade.</summary>
    private async Task<bool> SubmitAsync(Func<Task> work)
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
            if (ex.Code == "sessao_expirada") return false;
            await Dialogs.AlertAsync("Não foi possível reservar", ex.FullMessage);
            if (ex.Status == 409)
            {
                InvalidateQuote();
                await ReloadAvailabilityAsync(keepSelection: false);
            }
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
