using System.Collections.ObjectModel;
using Clubes.Mobile.Models;
using Clubes.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QRCoder;

namespace Clubes.Mobile.ViewModels;

public class HomeTile(string title, string icon, System.Windows.Input.ICommand command, int index)
{
    public string Title { get; } = title;
    public string Icon { get; } = icon;
    public System.Windows.Input.ICommand Command { get; } = command;
    public int Row { get; } = index / 3;
    public int Column { get; } = index % 3;
}

public partial class HomeViewModel(AuthService auth, ClubApi api, NotificationState notifications) : BaseViewModel
{
    public NotificationState Notifications => notifications;
    public string Greeting => $"Olá, {auth.FirstName}";
    public string ClubName => auth.ClubName;
    public bool IsMember => auth.IsMember;
    public bool CanReserve => auth.CanReserve;
    public bool CanRegisterGuests => auth.CanRegisterGuests;

    [ObservableProperty] public partial CardDto? Card { get; set; }
    [ObservableProperty] public partial string CardStatusText { get; set; } = "";
    [ObservableProperty] public partial ChipKind CardStatusKind { get; set; }
    [ObservableProperty] public partial string PresenceText { get; set; } = "";
    [ObservableProperty] public partial bool ShowFinancial { get; set; }
    [ObservableProperty] public partial string OpenAmountText { get; set; } = "";
    [ObservableProperty] public partial string FinancialStatusText { get; set; } = "";
    [ObservableProperty] public partial ChipKind FinancialStatusKind { get; set; }
    [ObservableProperty] public partial bool ShowBlockBanner { get; set; }
    [ObservableProperty] public partial string BlockMessage { get; set; } = "";
    [ObservableProperty] public partial bool HasReservations { get; set; }
    [ObservableProperty] public partial bool NoReservations { get; set; }
    [ObservableProperty] public partial bool HasAnnouncements { get; set; }

    private List<HomeTile>? _tiles;
    /// <summary>Atalhos visíveis conforme o perfil e as permissões informadas pela API.</summary>
    public IReadOnlyList<HomeTile> Tiles => _tiles ??= BuildTiles();

    private List<HomeTile> BuildTiles()
    {
        var tiles = new List<HomeTile>();
        void Add(string title, string icon, System.Windows.Input.ICommand command) => tiles.Add(new HomeTile(title, icon, command, tiles.Count));
        if (CanReserve) Add("Reservar", "ic_calendar.png", ReserveCommand);
        if (CanRegisterGuests) Add("Convidados", "ic_person_add.png", GuestsCommand);
        Add("Eventos", "ic_star.png", EventsCommand);
        if (IsMember) Add("Financeiro", "ic_money.png", FinanceCommand);
        Add("Manifestações", "ic_chat.png", ManifestationsCommand);
        if (IsMember) Add("Assembleias", "ic_vote.png", AssembliesCommand);
        return tiles;
    }

    public ObservableCollection<ReservationDto> Reservations { get; } = [];
    public ObservableCollection<AnnouncementDto> Announcements { get; } = [];

    protected override Task OnAppearingAsync() => Load();

    [RelayCommand]
    private Task Load() => LoadAsync(async () =>
    {
        var cardTask = api.MyCardAsync();
        var feedTask = api.FeedAsync(1, 3);
        var financialTask = IsMember ? api.MyFinancialStatusAsync() : Task.FromResult<FinancialStatusDto>(null!);
        var reservationsTask = CanReserve ? api.UpcomingReservationsAsync(5) : Task.FromResult(new PagedResult<ReservationDto>());
        await Task.WhenAll(cardTask, feedTask, financialTask, reservationsTask, notifications.RefreshAsync());

        Card = cardTask.Result;
        CardStatusText = Labels.PersonStatus(Card.Status);
        CardStatusKind = Labels.PersonStatusKind(Card.Status);
        PresenceText = Card.IsInside ? "Você está no clube" : "Fora do clube";

        if (IsMember && financialTask.Result is { } f)
        {
            ShowFinancial = true;
            OpenAmountText = Fmt.Money(f.OpenAmount);
            FinancialStatusText = f.IsDelinquent ? $"Em atraso: {Fmt.Money(f.OverdueAmount)}" : f.OpenAmount > 0 ? "Em dia, com valores a vencer" : "Em dia";
            FinancialStatusKind = f.IsDelinquent ? ChipKind.Danger : ChipKind.Success;
            var blocks = new List<string>();
            if (f.AccessBlocked) blocks.Add("o acesso ao clube");
            if (f.ReservationsBlocked) blocks.Add("novas reservas");
            ShowBlockBanner = blocks.Count > 0;
            BlockMessage = blocks.Count > 0
                ? $"Há pendências financeiras ({Fmt.Money(f.OverdueAmount)} em atraso). Estão bloqueados: {string.Join(" e ", blocks)}. Regularize em Financeiro ou procure a secretaria."
                : "";
        }

        Replace(Reservations, reservationsTask.Result.Items);
        HasReservations = Reservations.Count > 0;
        NoReservations = CanReserve && Reservations.Count == 0;
        Replace(Announcements, feedTask.Result.Items);
        HasAnnouncements = Announcements.Count > 0;
    });

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items) target.Add(item);
    }

    [RelayCommand] private Task OpenCard() => Nav.GoToAsync("//carteirinha");
    [RelayCommand] private Task OpenNotifications() => Nav.GoToAsync("notificacoes");
    [RelayCommand] private Task Reserve() => Nav.GoToAsync("espacos");
    [RelayCommand] private Task Guests() => Nav.GoToAsync("convidados");
    [RelayCommand] private Task Events() => Nav.GoToAsync("eventos");
    [RelayCommand] private Task Finance() => Nav.GoToAsync("financeiro");
    [RelayCommand] private Task Manifestations() => Nav.GoToAsync("manifestacoes");
    [RelayCommand] private Task Assemblies() => Nav.GoToAsync("assembleias");
    [RelayCommand] private Task AllAnnouncements() => Nav.GoToAsync("//comunicados");
    [RelayCommand] private Task OpenReservation(ReservationDto? item) => item is null ? Task.CompletedTask : Nav.GoToAsync("reserva", item.Id);
    [RelayCommand] private Task OpenAnnouncement(AnnouncementDto? item) => item is null ? Task.CompletedTask : Nav.GoToAsync("comunicado", item.Id);
}

/// <summary>Carteirinha digital com QR Code dinâmico (uso único, com validade curta).</summary>
public partial class CardViewModel(ClubApi api, AuthService auth, AppRouter app) : BaseViewModel
{
    private IDispatcherTimer? _timer;
    private DateTime _expiresLocal;
    private double _ttlSeconds = 1;
    private bool _generating;
    private bool _subscribed;
    private bool _renewOnResume;

    [ObservableProperty] public partial CardDto? Card { get; set; }
    [ObservableProperty] public partial string StatusText { get; set; } = "";
    [ObservableProperty] public partial ChipKind StatusKind { get; set; }
    [ObservableProperty] public partial string PersonTypeText { get; set; } = "";
    [ObservableProperty] public partial bool HasResponsible { get; set; }
    [ObservableProperty] public partial string ResponsibleText { get; set; } = "";
    [ObservableProperty] public partial string PresenceText { get; set; } = "";

    [ObservableProperty] public partial ImageSource? QrImage { get; set; }
    [ObservableProperty] public partial bool HasQr { get; set; }
    [ObservableProperty] public partial string DisplayCode { get; set; } = "";
    [ObservableProperty] public partial string OperationText { get; set; } = "";
    [ObservableProperty] public partial double Progress { get; set; }
    [ObservableProperty] public partial string CountdownText { get; set; } = "";
    [ObservableProperty] public partial bool IsGenerating { get; set; }
    [ObservableProperty] public partial string? QrError { get; set; }
    [ObservableProperty] public partial bool HasQrError { get; set; }

    public string ClubName => auth.ClubName;

    protected override Task OnAppearingAsync()
    {
        if (!_subscribed)
        {
            _subscribed = true;
            app.Resumed += OnAppResumed;
        }
        return Load();
    }

    protected override void OnDisappearing()
    {
        // O código nunca é reaproveitado nem gerado em segundo plano: ao sair da tela ele é descartado.
        if (_subscribed)
        {
            _subscribed = false;
            app.Resumed -= OnAppResumed;
        }
        _renewOnResume = false;
        StopTimer();
        ClearQr();
    }

    /// <summary>O app voltou ao primeiro plano com a carteirinha aberta: se o código expirou enquanto estava fora, gera outro.</summary>
    private void OnAppResumed()
    {
        if (!IsVisible) return;
        if (_renewOnResume || !HasQr)
        {
            _renewOnResume = false;
            _ = NewCode();
        }
    }

    [RelayCommand]
    private Task Load() => LoadAsync(async () =>
    {
        await LoadCardAsync();
        await GenerateAsync();
    });

    [RelayCommand]
    private async Task NewCode()
    {
        try { await LoadCardAsync(); }
        catch (ApiException) { /* o erro relevante aparece na geração do código */ }
        await GenerateAsync();
    }

    private async Task LoadCardAsync()
    {
        var card = await api.MyCardAsync();
        Card = card;
        StatusText = Labels.PersonStatus(card.Status);
        StatusKind = card.IsActive ? ChipKind.Success : Labels.PersonStatusKind(card.Status) == ChipKind.Success ? ChipKind.Warning : Labels.PersonStatusKind(card.Status);
        PersonTypeText = Labels.Of(card.PersonType);
        HasResponsible = !string.IsNullOrWhiteSpace(card.ResponsibleName);
        ResponsibleText = HasResponsible ? $"Responsável: {card.ResponsibleName}" : "";
        PresenceText = card.IsInside ? "Você está no clube" : "Você está fora do clube";
    }

    /// <summary>Pede um novo token ao servidor. Em recusa (cadastro inativo, 403) mostra a mensagem no lugar do QR Code.</summary>
    private async Task GenerateAsync()
    {
        if (_generating || !IsVisible) return;
        if (!app.IsForeground) { _renewOnResume = true; return; }
        _generating = true;
        IsGenerating = true;
        StopTimer();
        try
        {
            var token = await api.IssueQrAsync();
            if (!IsVisible) return;
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(token.Token, QRCodeGenerator.ECCLevel.M);
            var png = new PngByteQRCode(data).GetGraphic(12);
            QrImage = ImageSource.FromStream(() => new MemoryStream(png));
            var code = token.DisplayCode ?? "";
            DisplayCode = code.Length == 8 ? $"{code[..4]} {code[4..]}" : code;
            OperationText = Labels.Of(token.Operation);
            _ttlSeconds = Math.Max(1, token.TtlSeconds);
            // Usa a validade relativa (ttl) para não depender do relógio do aparelho estar igual ao do servidor.
            _expiresLocal = DateTime.UtcNow.AddSeconds(_ttlSeconds);
            QrError = null;
            HasQrError = false;
            HasQr = true;
            Tick();
            StartTimer();
        }
        catch (ApiException ex)
        {
            ClearQr();
            if (ex.Code != "sessao_expirada")
            {
                QrError = ex.Message;
                HasQrError = true;
            }
        }
        finally
        {
            _generating = false;
            IsGenerating = false;
        }
    }

    private void ClearQr()
    {
        QrImage = null;
        HasQr = false;
        DisplayCode = "";
        CountdownText = "";
        Progress = 0;
    }

    private void StartTimer()
    {
        _timer ??= Application.Current?.Dispatcher.CreateTimer();
        if (_timer is null) return;
        _timer.Interval = TimeSpan.FromMilliseconds(500);
        _timer.Tick -= OnTick;
        _timer.Tick += OnTick;
        _timer.Start();
    }

    private void StopTimer() => _timer?.Stop();

    private void OnTick(object? sender, EventArgs e) => Tick();

    private void Tick()
    {
        var remaining = (_expiresLocal - DateTime.UtcNow).TotalSeconds;
        if (remaining <= 0)
        {
            StopTimer();
            ClearQr();
            // Expirou com a tela visível: atualiza o cartão (a portaria pode ter registrado a passagem) e gera outro código.
            // Com o app em segundo plano nada é gerado: o novo código é pedido quando o usuário voltar.
            if (IsVisible && app.IsForeground) _ = NewCode();
            else if (IsVisible) _renewOnResume = true;
            return;
        }
        Progress = Math.Clamp(remaining / _ttlSeconds, 0, 1);
        CountdownText = $"Expira em {Math.Ceiling(remaining):0} s";
    }
}
