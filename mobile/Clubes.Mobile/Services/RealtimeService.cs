using Clubes.Mobile.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace Clubes.Mobile.Services;

/// <summary>Canal em tempo real (SignalR) do clube: notificações, disponibilidade de espaços e ocupação.</summary>
public class RealtimeService(ApiClient api, AppConfig config)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private HubConnection? _hub;
    private CancellationTokenSource? _startCts;

    public event Action<RealtimeNotification>? NotificationReceived;
    /// <summary>Espaço cuja disponibilidade mudou (nulo: pode afetar qualquer espaço).</summary>
    public event Action<Guid?>? AvailabilityChanged;
    public event Action? OccupancyChanged;

    public async Task StartAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (_hub is not null) return;
            var hub = new HubConnectionBuilder()
                .WithUrl(config.BaseUrl + "/hubs/clube", options => options.AccessTokenProvider = () => api.GetValidAccessTokenAsync())
                .WithAutomaticReconnect(new ForeverRetryPolicy())
                .Build();
            hub.On<RealtimeNotification>("notification", n => Dispatch(() => NotificationReceived?.Invoke(n)));
            hub.On<AvailabilityChanged>("availabilityChanged", e => Dispatch(() => AvailabilityChanged?.Invoke(e.SpaceId)));
            hub.On<object>("occupancyChanged", _ => Dispatch(() => OccupancyChanged?.Invoke()));
            _hub = hub;
            _startCts = new CancellationTokenSource();
            _ = ConnectLoopAsync(hub, _startCts.Token);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StopAsync()
    {
        HubConnection? hub;
        await _gate.WaitAsync();
        try
        {
            hub = _hub;
            _hub = null;
            _startCts?.Cancel();
            _startCts = null;
        }
        finally
        {
            _gate.Release();
        }
        if (hub is null) return;
        try { await hub.DisposeAsync(); }
        catch (Exception) { /* conexão já encerrada */ }
    }

    /// <summary>A primeira conexão não é coberta pela reconexão automática: tenta até conseguir ou ser cancelada.</summary>
    private static async Task ConnectLoopAsync(HubConnection hub, CancellationToken ct)
    {
        var delay = TimeSpan.FromSeconds(2);
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (hub.State == HubConnectionState.Disconnected) await hub.StartAsync(ct);
                return;
            }
            catch (OperationCanceledException) { return; }
            catch (ObjectDisposedException) { return; }
            catch (Exception)
            {
                try { await Task.Delay(delay, ct); } catch (OperationCanceledException) { return; }
                delay = TimeSpan.FromSeconds(Math.Min(30, delay.TotalSeconds * 2));
            }
        }
    }

    private static void Dispatch(Action action) => MainThread.BeginInvokeOnMainThread(action);

    private sealed class ForeverRetryPolicy : IRetryPolicy
    {
        public TimeSpan? NextRetryDelay(RetryContext retryContext) =>
            TimeSpan.FromSeconds(Math.Min(30, 2 * Math.Pow(2, Math.Min(4, retryContext.PreviousRetryCount))));
    }
}
