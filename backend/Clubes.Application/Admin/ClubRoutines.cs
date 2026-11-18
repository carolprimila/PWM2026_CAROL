using Clubes.Application.Clubs;
using Clubes.Application.Common;
using Clubes.Application.Finance;
using Clubes.Application.Operations;
using Clubes.Application.Spaces;
using Clubes.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Clubes.Application.Admin;

/// <summary>
/// Rotinas automáticas executadas periodicamente para cada clube pelo serviço em segundo plano:
/// multas e juros, avisos financeiros, política de inadimplência em reservas, lembretes e limpeza.
/// Todas são idempotentes — podem ser executadas várias vezes sem duplicar efeitos.
/// </summary>
public class ClubRoutines(IAppDb db, ChargeService charges, ReservationService reservations, KeyHandoverService keys, NotificationService notifications,
    SettingsService settings, ClubClock clock, ILogger<ClubRoutines> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        await StepAsync("mensalidades automáticas", () => GenerateMonthlyIfDueAsync(ct));
        await StepAsync("multas e juros", () => charges.ApplyLateFeesAsync(null, ct));
        await StepAsync("avisos financeiros", () => FinanceNoticesAsync(ct));
        await StepAsync("inadimplência em reservas", () => reservations.ApplyDelinquencyPolicyAsync(ct));
        await StepAsync("lembretes de reservas", () => reservations.SendRemindersAndCompleteAsync(ct));
        await StepAsync("devolução de chaves", () => keys.NotifyOverdueAsync(ct));
        await StepAsync("limpeza de tokens", () => CleanupAsync(ct));
    }

    private async Task StepAsync(string name, Func<Task> step)
    {
        // Uma rotina com falha não impede as demais; o erro fica registrado para monitoramento.
        try { await step(); }
        catch (DomainException ex) when (ex.Code == "configuracao_pendente") { logger.LogDebug("Rotina {Routine} ignorada: {Message}", name, ex.Message); }
        catch (Exception ex) { logger.LogError(ex, "Falha na rotina automática {Routine}", name); }
    }

    private async Task GenerateMonthlyIfDueAsync(CancellationToken ct)
    {
        var s = await settings.LoadAsync(ct);
        if (!s.AutoGenerateMonthlyCharges) return;
        var competence = (await clock.TodayAsync(ct)).ToString("yyyy-MM");
        if (await db.Set<MonthlyClosing>().AnyAsync(c => c.Competence == competence, ct)) return;
        var result = await charges.GenerateMonthlyAsync(new GenerateMonthlyRequest(competence, null), ct);
        if (result.Created > 0) logger.LogInformation("Mensalidades de {Competence} geradas automaticamente: {Count}", competence, result.Created);
    }

    private async Task FinanceNoticesAsync(CancellationToken ct)
    {
        var s = await settings.LoadAsync(ct);
        var today = await clock.TodayAsync(ct);
        var limit = today.AddDays(s.DueReminderDays);
        var candidates = await db.Set<Charge>().Where(c => c.Status == ChargeStatus.Open && c.DueDate <= limit)
            .Select(c => new { c.Id, c.MemberId, c.Description, c.DueDate, c.DelinquentFrom, Balance = c.OriginalAmount + c.Additions + c.FineAmount + c.InterestAmount + c.FeesAmount - c.Discounts - c.PaidAmount })
            .ToListAsync(ct);
        if (candidates.Count == 0) return;
        var ids = candidates.Select(c => c.Id).ToList();
        var sent = (await db.Set<Notification>().Where(n => n.EntityId != null && ids.Contains(n.EntityId.Value) && (n.Type == "charge.due-soon" || n.Type == "charge.overdue" || n.Type == "charge.blocked"))
            .Select(n => new { n.EntityId, n.Type }).Distinct().ToListAsync(ct)).Select(n => (n.EntityId!.Value, n.Type)).ToHashSet();

        foreach (var c in candidates)
        {
            if (c.DueDate >= today)
            {
                if (s.DueReminderDays > 0 && !sent.Contains((c.Id, "charge.due-soon")))
                    await notifications.ToMemberAsync(c.MemberId, NotificationCategory.Finance, "charge.due-soon", "Vencimento próximo",
                        $"{c.Description}: {c.Balance:C} vence em {c.DueDate:dd/MM/yyyy}.", nameof(Charge), c.Id, false, ct);
                continue;
            }
            if (!sent.Contains((c.Id, "charge.overdue")))
                await notifications.ToMemberAsync(c.MemberId, NotificationCategory.Finance, "charge.overdue", "Cobrança vencida",
                    $"{c.Description} venceu em {c.DueDate:dd/MM/yyyy}. Saldo: {c.Balance:C}.", nameof(Charge), c.Id, true, ct);
            if (c.DelinquentFrom <= today && (s.BlockAccessWhenDelinquent == true || s.BlockReservationsWhenDelinquent == true) && !sent.Contains((c.Id, "charge.blocked")))
                await notifications.ToMemberAsync(c.MemberId, NotificationCategory.Finance, "charge.blocked", "Bloqueio financeiro",
                    "Há cobranças em atraso além do prazo de tolerância. Algumas funcionalidades e acessos estão bloqueados até a regularização.", nameof(Charge), c.Id, true, ct);
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddDays(-7);
        // Tokens de QR só são descartados se nunca foram usados; os consumidos permanecem vinculados ao registro de acesso.
        await db.Set<QrToken>().Where(t => t.ExpiresAt < cutoff && t.ConsumedAt == null && !db.Set<AccessLog>().Any(l => l.QrTokenId == t.Id)).ExecuteDeleteAsync(ct);
    }
}
