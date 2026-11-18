using Clubes.Mobile.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;

namespace Clubes.Mobile.Services;

/// <summary>Estado das notificações no aparelho: contador de não lidas e aviso local quando chega uma notificação em tempo real.</summary>
public partial class NotificationState : ObservableObject
{
    private readonly ClubApi _api;
    private readonly RealtimeService _realtime;
    private int _localId = 1000;
    private bool _attached;

    public NotificationState(ClubApi api, RealtimeService realtime)
    {
        _api = api;
        _realtime = realtime;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUnread), nameof(UnreadText))]
    public partial int UnreadCount { get; set; }

    public bool HasUnread => UnreadCount > 0;
    public string UnreadText => UnreadCount > 99 ? "99+" : UnreadCount.ToString();

    /// <summary>Uma notificação chegou em tempo real (as listas abertas podem se atualizar).</summary>
    public event Action<RealtimeNotification>? Received;

    public void Attach()
    {
        if (_attached) return;
        _attached = true;
        _realtime.NotificationReceived += OnReceived;
    }

    public void Reset() => UnreadCount = 0;

    public async Task RefreshAsync()
    {
        try { UnreadCount = (await _api.UnreadCountAsync()).Count; }
        catch (ApiException) { /* mantém o último valor conhecido */ }
    }

    /// <summary>Pede a permissão de notificações do sistema (Android 13+ / iOS). Sem permissão, o aviso fica apenas dentro do app.</summary>
    public async Task EnsurePermissionAsync()
    {
        try
        {
            if (!await LocalNotificationCenter.Current.AreNotificationsEnabled())
                await LocalNotificationCenter.Current.RequestNotificationPermission();
        }
        catch (Exception) { /* plataforma sem suporte: segue sem aviso do sistema */ }
    }

    private void OnReceived(RealtimeNotification notification)
    {
        UnreadCount++;
        Received?.Invoke(notification);
        _ = ShowLocalAsync(notification);
        _ = RefreshAsync();
    }

    private async Task ShowLocalAsync(RealtimeNotification notification)
    {
        try
        {
            await LocalNotificationCenter.Current.Show(new NotificationRequest
            {
                NotificationId = Interlocked.Increment(ref _localId),
                Title = notification.Title,
                Description = notification.Body,
            });
        }
        catch (Exception) { /* sem permissão ou sem suporte: o contador dentro do app já foi atualizado */ }
    }
}
