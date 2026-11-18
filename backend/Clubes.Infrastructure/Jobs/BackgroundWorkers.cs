using Clubes.Application.Admin;
using Clubes.Application.Common;
using Clubes.Domain;
using Clubes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Clubes.Infrastructure.Jobs;

public class RoutineOptions
{
    public bool Enabled { get; set; } = true;
    public int IntervalMinutes { get; set; } = 15;
    public int DispatchIntervalSeconds { get; set; } = 10;
}

/// <summary>Executa periodicamente as rotinas automáticas de cada clube ativo, cada uma em seu próprio escopo e contexto de clube.</summary>
public class RoutineWorker(IServiceScopeFactory scopes, IOptions<RoutineOptions> options, ILogger<RoutineWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled) return;
        await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken).ContinueWith(_ => { }, CancellationToken.None);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                List<Guid> clubs;
                await using (var scope = scopes.CreateAsyncScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    clubs = await db.Set<Club>().Where(c => c.IsActive).Select(c => c.Id).ToListAsync(stoppingToken);
                    var cutoff = DateTime.UtcNow.AddDays(-30);
                    await db.Set<RefreshToken>().Where(t => t.ExpiresAt < cutoff).ExecuteDeleteAsync(stoppingToken);
                    await db.Set<PasswordResetToken>().Where(t => t.ExpiresAt < cutoff).ExecuteDeleteAsync(stoppingToken);
                }
                foreach (var clubId in clubs)
                {
                    await using var scope = scopes.CreateAsyncScope();
                    var ctx = scope.ServiceProvider.GetRequiredService<RequestContext>();
                    (ctx.ClubId, ctx.UserName, ctx.Origin) = (clubId, "Sistema", "rotina-automatica");
                    await scope.ServiceProvider.GetRequiredService<ClubRoutines>().RunAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Falha no ciclo de rotinas automáticas"); }

            try { await Task.Delay(TimeSpan.FromMinutes(Math.Max(1, options.Value.IntervalMinutes)), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}

/// <summary>
/// Despachante de notificações: entrega em tempo real, push e e-mail o que os serviços gravaram,
/// registrando o resultado de cada canal. Roda fora da transação de negócio — falha de entrega nunca
/// desfaz a operação que originou a notificação.
/// </summary>
public class NotificationDispatcher(IServiceScopeFactory scopes, IOptions<RoutineOptions> options, ILogger<NotificationDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled) return;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await DispatchAsync(scope.ServiceProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Falha no despacho de notificações"); }

            try { await Task.Delay(TimeSpan.FromSeconds(Math.Max(2, options.Value.DispatchIntervalSeconds)), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    public static async Task<int> DispatchAsync(IServiceProvider services, CancellationToken ct)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var push = services.GetRequiredService<IPushSender>();
        var email = services.GetRequiredService<IEmailSender>();
        var realtime = services.GetRequiredService<IRealtimeNotifier>();

        var batch = await db.Set<Notification>().IgnoreQueryFilters().Where(n => n.DispatchedAt == null).OrderBy(n => n.CreatedAt).Take(200).ToListAsync(ct);
        if (batch.Count == 0) return 0;
        var userIds = batch.Select(n => n.UserId).Distinct().ToList();
        var tokens = await db.Set<DeviceToken>().IgnoreQueryFilters().Where(d => userIds.Contains(d.UserId)).Select(d => new { d.UserId, d.Token }).ToListAsync(ct);
        var emails = await db.Set<User>().IgnoreQueryFilters().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Email, ct);

        foreach (var n in batch)
        {
            var errors = new List<string>();
            try { await realtime.PublishToUserAsync(n.UserId, "notification", new { n.Id, n.Title, n.Body, n.Category, n.Type, n.EntityType, n.EntityId }, ct); }
            catch (Exception ex) { errors.Add("tempo real: " + ex.Message); }

            if (n.PushStatus == DeliveryStatus.Pending)
            {
                var userTokens = tokens.Where(t => t.UserId == n.UserId).Select(t => t.Token).ToList();
                if (!push.IsConfigured || userTokens.Count == 0)
                {
                    n.PushStatus = DeliveryStatus.NoDestination;
                    if (!push.IsConfigured) errors.Add("push: provedor não configurado");
                }
                else
                {
                    try
                    {
                        await push.SendAsync(userTokens, n.Title, n.Body, new Dictionary<string, string> { ["notificationId"] = n.Id.ToString(), ["type"] = n.Type }, ct);
                        (n.PushStatus, n.PushSentAt) = (DeliveryStatus.Sent, DateTime.UtcNow);
                    }
                    catch (Exception ex) { n.PushStatus = DeliveryStatus.Failed; errors.Add("push: " + ex.Message); }
                }
            }

            if (n.EmailStatus == DeliveryStatus.Pending)
            {
                if (!email.IsConfigured || !emails.TryGetValue(n.UserId, out var address)) n.EmailStatus = DeliveryStatus.NoDestination;
                else
                {
                    try
                    {
                        await email.SendAsync(new EmailMessage(address, n.Title, n.Body), ct);
                        (n.EmailStatus, n.EmailSentAt) = (DeliveryStatus.Sent, DateTime.UtcNow);
                    }
                    catch (Exception ex) { n.EmailStatus = DeliveryStatus.Failed; errors.Add("e-mail: " + ex.Message); }
                }
            }
            n.DispatchedAt = DateTime.UtcNow;
            if (errors.Count > 0) n.DeliveryError = string.Join(" | ", errors) is { Length: > 500 } joined ? joined[..500] : string.Join(" | ", errors);
        }
        await db.SaveChangesAsync(ct);
        return batch.Count;
    }
}
