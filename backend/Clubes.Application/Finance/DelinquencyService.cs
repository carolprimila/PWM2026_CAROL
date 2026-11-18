using Clubes.Application.Clubs;
using Clubes.Application.Common;
using Clubes.Domain;
using Microsoft.EntityFrameworkCore;

namespace Clubes.Application.Finance;

public record FinancialStatusDto(bool IsDelinquent, int OverdueCharges, decimal OverdueAmount, decimal OpenAmount, DateOnly? OldestDueDate,
    bool HasActiveRelease, DateOnly? ReleaseValidUntil, bool AccessBlocked, bool ReservationsBlocked);

/// <summary>
/// Fonte única da regra de inadimplência, usada pela portaria, reservas, convidados e votações.
/// Um associado é inadimplente quando possui cobrança em aberto cujo vencimento + tolerância já passou.
/// Os bloqueios só são aplicados quando a política correspondente foi habilitada pela administração.
/// </summary>
public class DelinquencyService(IAppDb db, ClubClock clock, SettingsService settings)
{
    public async Task<FinancialStatusDto> GetAsync(Guid memberId, CancellationToken ct = default)
    {
        var today = await clock.TodayAsync(ct);
        var open = await db.Set<Charge>().Where(c => c.MemberId == memberId && c.Status == ChargeStatus.Open)
            .Select(c => new { c.DueDate, c.DelinquentFrom, Balance = c.OriginalAmount + c.Additions + c.FineAmount + c.InterestAmount + c.FeesAmount - c.Discounts - c.PaidAmount })
            .ToListAsync(ct);
        var delinquent = open.Where(c => c.DelinquentFrom <= today).ToList();
        var release = await db.Set<FinancialRelease>().Where(r => r.MemberId == memberId && r.RevokedAt == null && r.ValidUntil >= today)
            .OrderByDescending(r => r.ValidUntil).Select(r => (DateOnly?)r.ValidUntil).FirstOrDefaultAsync(ct);
        var s = await settings.LoadAsync(ct);
        var blocked = delinquent.Count > 0 && release is null;
        return new FinancialStatusDto(delinquent.Count > 0, delinquent.Count, delinquent.Sum(c => c.Balance), open.Sum(c => c.Balance),
            delinquent.Count > 0 ? delinquent.Min(c => c.DueDate) : null, release is not null, release,
            blocked && s.BlockAccessWhenDelinquent == true, blocked && s.BlockReservationsWhenDelinquent == true);
    }

    /// <summary>Associados inadimplentes do clube (sem considerar liberações excepcionais).</summary>
    public async Task<IQueryable<Guid>> DelinquentMemberIdsAsync(CancellationToken ct = default)
    {
        var today = await clock.TodayAsync(ct);
        return db.Set<Charge>().Where(c => c.Status == ChargeStatus.Open && c.DelinquentFrom <= today).Select(c => c.MemberId).Distinct();
    }

    public async Task EnsureCanReserveAsync(Guid memberId, CancellationToken ct = default)
    {
        if ((await GetAsync(memberId, ct)).ReservationsBlocked)
            throw new DomainException("Há pendências financeiras em aberto. Regularize sua situação para solicitar reservas.", "bloqueio_financeiro", 403);
    }
}
