using Clubes.Mobile.Models;
using Clubes.Mobile.ViewModels;
using ZXing.Net.Maui;

namespace Clubes.Mobile.Views;

public partial class GateScannerPage : BasePage
{
    private readonly GateScannerViewModel _vm;

    public GateScannerPage(GateScannerViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        Camera.Options = new BarcodeReaderOptions { Formats = BarcodeFormat.QrCode, AutoRotate = true, Multiple = false };
    }

    // A câmera entrega as leituras fora da thread de interface; o view model descarta leituras durante uma validação
    // e por ~2,5 s após cada resultado, para o mesmo QR Code não ser enviado duas vezes.
    private void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        var code = e.Results?.FirstOrDefault()?.Value;
        if (string.IsNullOrWhiteSpace(code)) return;
        MainThread.BeginInvokeOnMainThread(async () => await _vm.OnScannedAsync(code));
    }
}

public partial class GatePresentPage : BasePage
{
    public GatePresentPage(GatePresentViewModel vm) { InitializeComponent(); BindingContext = vm; }
}

public partial class GateGuestsPage : BasePage
{
    private readonly GateGuestsViewModel _vm;
    public GateGuestsPage(GateGuestsViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    private void OnEntry(object? sender, EventArgs e) => _vm.EntryCommand.Execute(Ctx<GuestDto>(sender));
    private void OnExit(object? sender, EventArgs e) => _vm.ExitCommand.Execute(Ctx<GuestDto>(sender));
}

public partial class GateSearchPage : BasePage
{
    private readonly GateSearchViewModel _vm;
    public GateSearchPage(GateSearchViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    private void OnRegister(object? sender, EventArgs e) => _vm.RegisterCommand.Execute(Ctx<GatePersonDto>(sender));
}
