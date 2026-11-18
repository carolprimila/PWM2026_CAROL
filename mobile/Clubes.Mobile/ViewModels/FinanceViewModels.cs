using System.Collections.ObjectModel;
using Clubes.Mobile.Models;
using Clubes.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Clubes.Mobile.ViewModels;

public partial class FinanceViewModel(ClubApi api) : BaseViewModel
{
    private List<ChargeDto> _open = [];
    private List<ChargeDto> _paid = [];

    [ObservableProperty] public partial bool ShowOpen { get; set; } = true;
    [ObservableProperty] public partial bool ShowPaid { get; set; }
    [ObservableProperty] public partial string OpenAmountText { get; set; } = "";
    [ObservableProperty] public partial string OverdueText { get; set; } = "";
    [ObservableProperty] public partial bool HasOverdue { get; set; }
    [ObservableProperty] public partial bool ShowBlockBanner { get; set; }
    [ObservableProperty] public partial string BlockMessage { get; set; } = "";
    [ObservableProperty] public partial string EmptyText { get; set; } = "";
    public ObservableCollection<ChargeDto> Items { get; } = [];

    protected override Task OnAppearingAsync() => Load();

    [RelayCommand]
    private Task Load() => LoadAsync(async () =>
    {
        var statusTask = api.MyFinancialStatusAsync();
        var openTask = api.ChargesAsync(ChargeStatus.Open);
        var paidTask = api.ChargesAsync(ChargeStatus.Paid);
        await Task.WhenAll(statusTask, openTask, paidTask);
        var f = statusTask.Result;
        OpenAmountText = Fmt.Money(f.OpenAmount);
        HasOverdue = f.IsDelinquent;
        OverdueText = f.IsDelinquent ? $"{f.OverdueCharges} cobrança(s) vencida(s): {Fmt.Money(f.OverdueAmount)}" : "Nenhuma cobrança vencida";
        var blocks = new List<string>();
        if (f.AccessBlocked) blocks.Add("o acesso ao clube");
        if (f.ReservationsBlocked) blocks.Add("novas reservas");
        ShowBlockBanner = blocks.Count > 0;
        BlockMessage = blocks.Count > 0 ? $"Por pendências financeiras, estão bloqueados: {string.Join(" e ", blocks)}." : "";
        _open = openTask.Result.Items.OrderBy(c => c.DueDate).ToList();
        _paid = paidTask.Result.Items;
        Show();
    });

    private void Show()
    {
        Items.ReplaceWith(ShowOpen ? _open : _paid);
        EmptyText = ShowOpen ? "Você não tem cobranças em aberto." : "Nenhuma cobrança paga.";
    }

    [RelayCommand] private void TabOpen() { (ShowOpen, ShowPaid) = (true, false); Show(); }
    [RelayCommand] private void TabPaid() { (ShowOpen, ShowPaid) = (false, true); Show(); }
    [RelayCommand] private Task History() => Nav.GoToAsync("pagamentos");
    [RelayCommand] private Task Open(ChargeDto? item) => item is null ? Task.CompletedTask : Nav.GoToAsync("cobranca", item.Id);
}

public record AmountRow(string Label, string Value);

public partial class ChargeDetailViewModel(ClubApi api, FileService files) : BaseViewModel
{
    private const int PollIntervalSeconds = 5;
    private const int PollAttempts = 24; // ~2 minutos
    private Guid _id;
    private string? _attemptKey;
    private PaymentMethod? _attemptMethod;
    private CancellationTokenSource? _pollCts;

    [ObservableProperty] public partial ChargeDto? Charge { get; set; }
    [ObservableProperty] public partial string TotalText { get; set; } = "";
    [ObservableProperty] public partial string BalanceText { get; set; } = "";
    [ObservableProperty] public partial bool HasComposition { get; set; }
    [ObservableProperty] public partial bool HasAdjustments { get; set; }
    [ObservableProperty] public partial bool HasPayments { get; set; }
    [ObservableProperty] public partial bool CanPay { get; set; }

    [ObservableProperty] public partial bool IsWaiting { get; set; }
    [ObservableProperty] public partial string WaitingText { get; set; } = "";
    [ObservableProperty] public partial string? PixCode { get; set; }
    [ObservableProperty] public partial bool HasPix { get; set; }
    [ObservableProperty] public partial string? CheckoutUrl { get; set; }
    [ObservableProperty] public partial bool HasCheckout { get; set; }
    [ObservableProperty] public partial string CopyText { get; set; } = "Copiar código Pix";

    public ObservableCollection<AmountRow> Rows { get; } = [];
    public ObservableCollection<string> Composition { get; } = [];
    public ObservableCollection<AdjustmentDto> Adjustments { get; } = [];
    public ObservableCollection<PaymentDto> Payments { get; } = [];

    private Guid? _pendingPaymentId;

    protected override void OnParameters(IDictionary<string, object> query) => _id = IdFrom(query);

    protected override async Task OnAppearingAsync()
    {
        await Load();
        if (_pendingPaymentId is not null) StartPolling();
    }

    protected override void OnDisappearing() => StopPolling();

    [RelayCommand]
    private Task Load() => LoadAsync(async () => Apply(await api.ChargeAsync(_id)));

    private void Apply(ChargeDetailDto detail)
    {
        var c = detail.Charge;
        Charge = c;
        TotalText = Fmt.Money(c.Total);
        BalanceText = Fmt.Money(c.Balance);
        var rows = new List<AmountRow>();
        if (c.Type == ChargeType.Monthly)
        {
            rows.Add(new("Mensalidade base", Fmt.Money(c.BaseFee)));
            rows.Add(new(c.DependentsCount == 1 ? "1 dependente" : $"{c.DependentsCount} dependentes", Fmt.Money(c.DependentsAmount)));
        }
        else rows.Add(new("Valor original", Fmt.Money(c.OriginalAmount)));
        if (c.Additions != 0) rows.Add(new("Acréscimos", "+ " + Fmt.Money(c.Additions)));
        if (c.Discounts != 0) rows.Add(new("Descontos", "- " + Fmt.Money(c.Discounts)));
        if (c.FineAmount != 0) rows.Add(new("Multa", "+ " + Fmt.Money(c.FineAmount)));
        if (c.InterestAmount != 0) rows.Add(new("Juros", "+ " + Fmt.Money(c.InterestAmount)));
        if (c.FeesAmount != 0) rows.Add(new("Taxas", "+ " + Fmt.Money(c.FeesAmount)));
        rows.Add(new("Total", Fmt.Money(c.Total)));
        rows.Add(new("Pago", Fmt.Money(c.PaidAmount)));
        rows.Add(new("Saldo a pagar", Fmt.Money(c.Balance)));
        Rows.ReplaceWith(rows);
        Composition.ReplaceWith(detail.Composition);
        HasComposition = Composition.Count > 0;
        Adjustments.ReplaceWith(detail.Adjustments);
        HasAdjustments = Adjustments.Count > 0;
        Payments.ReplaceWith(detail.Payments.OrderByDescending(p => p.CreatedAt));
        HasPayments = Payments.Count > 0;

        var pending = detail.Payments.Where(p => p.Status == PaymentStatus.Pending && p.Channel == PaymentChannel.App).OrderByDescending(p => p.CreatedAt).FirstOrDefault();
        if (pending is not null && c.Status == ChargeStatus.Open)
        {
            _pendingPaymentId = pending.Id;
            IsWaiting = true;
            WaitingText = $"Aguardando confirmação do pagamento de {pending.AmountText} via {Labels.Of(pending.Method)}.";
        }
        else
        {
            _pendingPaymentId = null;
            IsWaiting = false;
            ClearCheckout();
        }
        CanPay = c.Status == ChargeStatus.Open && c.Balance > 0 && !IsWaiting;
    }

    private void ClearCheckout()
    {
        PixCode = null;
        HasPix = false;
        CheckoutUrl = null;
        HasCheckout = false;
    }

    [RelayCommand]
    private async Task Pay()
    {
        if (Charge is null || !CanPay) return;
        var options = new Dictionary<string, PaymentMethod>
        {
            ["Pix"] = PaymentMethod.Pix, ["Cartão de crédito"] = PaymentMethod.CreditCard,
            ["Cartão de débito"] = PaymentMethod.DebitCard, ["Boleto"] = PaymentMethod.BankSlip,
        };
        var choice = await Dialogs.ActionSheetAsync("Forma de pagamento", options.Keys.ToArray());
        if (choice is null) return;
        var method = options[choice];
        if (!await Dialogs.ConfirmAsync("Confirmar pagamento", $"Pagar {BalanceText} ({Charge.Description}) via {choice}?", "Pagar")) return;

        // Uma chave de idempotência por tentativa: repetir a mesma tentativa (falha de rede, por exemplo) reutiliza a chave,
        // para o servidor não criar dois pagamentos. A chave só é descartada quando o servidor responde com sucesso.
        if (_attemptKey is null || _attemptMethod != method)
        {
            _attemptKey = Guid.NewGuid().ToString();
            _attemptMethod = method;
        }
        var key = _attemptKey;
        AppPaymentDto? started = null;
        if (!await RunAsync(async () => started = await api.StartPaymentAsync(_id, method, key), "Pagamento não iniciado")) return;
        _attemptKey = null;
        _attemptMethod = null;

        var result = started!;
        _pendingPaymentId = result.Payment.Id;
        IsWaiting = true;
        CanPay = false;
        WaitingText = $"Aguardando confirmação do pagamento de {result.Payment.AmountText} via {Labels.Of(result.Payment.Method)}.";
        PixCode = result.PixCopyPaste;
        HasPix = !string.IsNullOrWhiteSpace(result.PixCopyPaste);
        CheckoutUrl = result.CheckoutUrl;
        HasCheckout = !string.IsNullOrWhiteSpace(result.CheckoutUrl);
        CopyText = "Copiar código Pix";
        if (await ApplyPaymentStatusAsync(result.Payment)) return;
        if (HasCheckout) await OpenCheckout();
        StartPolling();
    }

    [RelayCommand]
    private async Task OpenCheckout()
    {
        if (string.IsNullOrWhiteSpace(CheckoutUrl) || !Uri.TryCreate(CheckoutUrl, UriKind.Absolute, out var uri)) return;
        try { await Browser.Default.OpenAsync(uri, BrowserLaunchMode.SystemPreferred); }
        catch (Exception) { await Dialogs.AlertAsync("Pagamento", "Não foi possível abrir a página de pagamento neste aparelho."); }
    }

    [RelayCommand]
    private async Task CopyPix()
    {
        if (string.IsNullOrWhiteSpace(PixCode)) return;
        await Clipboard.Default.SetTextAsync(PixCode);
        CopyText = "Código copiado";
    }

    /// <summary>"Puxar para atualizar": recarrega a cobrança e, se ainda houver pagamento pendente, volta a consultá-lo.</summary>
    [RelayCommand]
    private async Task Refresh()
    {
        await Load();
        if (_pendingPaymentId is not null) StartPolling();
    }

    private void StartPolling()
    {
        StopPolling();
        if (_pendingPaymentId is not { } id || !IsVisible) return;
        var cts = _pollCts = new CancellationTokenSource();
        _ = PollAsync(id, cts.Token);
    }

    private void StopPolling()
    {
        _pollCts?.Cancel();
        _pollCts = null;
    }

    /// <summary>Consulta o pagamento a cada 5 s por cerca de 2 minutos, enquanto a tela estiver visível.</summary>
    private async Task PollAsync(Guid paymentId, CancellationToken ct)
    {
        for (var attempt = 0; attempt < PollAttempts && !ct.IsCancellationRequested; attempt++)
        {
            try { await Task.Delay(TimeSpan.FromSeconds(PollIntervalSeconds), ct); }
            catch (OperationCanceledException) { return; }
            try
            {
                var payment = await api.PaymentAsync(paymentId);
                if (ct.IsCancellationRequested) return;
                if (await ApplyPaymentStatusAsync(payment)) return;
            }
            catch (ApiException) { /* falha momentânea: tenta de novo no próximo ciclo */ }
        }
        if (!ct.IsCancellationRequested)
            WaitingText = "O pagamento ainda não foi confirmado. Puxe a tela para baixo para verificar novamente.";
    }

    /// <summary>Devolve true quando o pagamento chegou a um estado final.</summary>
    private async Task<bool> ApplyPaymentStatusAsync(PaymentDto payment)
    {
        if (payment.Status == PaymentStatus.Pending) return false;
        StopPolling();
        _pendingPaymentId = null;
        IsWaiting = false;
        ClearCheckout();
        try { Apply(await api.ChargeAsync(_id)); }
        catch (ApiException) { /* a tela será atualizada na próxima carga */ }
        if (payment.Status == PaymentStatus.Confirmed)
            await Dialogs.AlertAsync("Pagamento confirmado", $"Recebemos o pagamento de {payment.AmountText}. {payment.ReceiptText}.");
        else
            await Dialogs.AlertAsync("Pagamento não concluído", $"O pagamento não foi confirmado ({payment.StatusText}). Você pode tentar novamente.");
        return true;
    }

    [RelayCommand]
    private Task Receipt(PaymentDto? payment) => payment is null ? Task.CompletedTask
        : RunAsync(async () => await files.OpenBytesAsync(await api.ReceiptAsync(payment.Id), $"comprovante-{payment.ReceiptNumber}.pdf"), "Não foi possível abrir o comprovante");
}

public partial class PaymentsViewModel(ClubApi api, FileService files) : BaseViewModel
{
    private int _page;
    private int _total;
    private bool _loadingMore;
    public ObservableCollection<PaymentDto> Items { get; } = [];

    protected override Task OnAppearingAsync() => HasLoaded ? Task.CompletedTask : Load();

    [RelayCommand]
    private Task Load() => LoadAsync(async () =>
    {
        var result = await api.PaymentsAsync(1);
        _page = 1;
        _total = result.Total;
        Items.ReplaceWith(result.Items);
    });

    [RelayCommand]
    private async Task LoadMore()
    {
        if (_loadingMore || !HasLoaded || Items.Count >= _total) return;
        _loadingMore = true;
        try
        {
            var result = await api.PaymentsAsync(_page + 1);
            _page++;
            _total = result.Total;
            foreach (var item in result.Items.Where(i => Items.All(x => x.Id != i.Id))) Items.Add(item);
        }
        catch (ApiException) { /* tenta de novo na próxima rolagem */ }
        finally { _loadingMore = false; }
    }

    [RelayCommand]
    private Task Receipt(PaymentDto? payment) => payment is null ? Task.CompletedTask
        : RunAsync(async () => await files.OpenBytesAsync(await api.ReceiptAsync(payment.Id), $"comprovante-{payment.ReceiptNumber}.pdf"), "Não foi possível abrir o comprovante");
}
