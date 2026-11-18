using System.ComponentModel.DataAnnotations;
using Clubes.Application.Clubs;
using Clubes.Application.Common;
using Clubes.Domain;
using Microsoft.EntityFrameworkCore;

namespace Clubes.Application.Finance;

public record FinancialEntryDto(Guid Id, FinancialEntryKind Kind, string Category, string Description, string? Counterparty, decimal Amount, DateOnly DueDate,
    DateOnly? PaidOn, FinancialEntryStatus Status, bool IsOverdue, string? Notes, string? CancelReason, Guid? MaintenanceOrderId, Guid? ReceiptFileId, DateTime CreatedAt);
public record SaveEntryRequest(FinancialEntryKind Kind, [Required, StringLength(80)] string Category, [Required, StringLength(200, MinimumLength = 3)] string Description,
    [StringLength(150)] string? Counterparty, [Range(0.01, 100_000_000)] decimal Amount, DateOnly DueDate, [StringLength(1000)] string? Notes,
    Guid? MaintenanceOrderId, Guid? ReceiptFileId);
public record PayEntryRequest(DateOnly PaidOn, Guid? ReceiptFileId);
public record CashFlowPoint(string Period, decimal Inflow, decimal Outflow, decimal Net);
public record CashFlowDto(DateOnly From, DateOnly To, decimal Inflow, decimal Outflow, decimal Net, decimal Receivable, decimal OverdueReceivable, decimal Payable,
    IReadOnlyList<CashFlowPoint> Series, IReadOnlyList<CategoryTotal> InflowByCategory, IReadOnlyList<CategoryTotal> OutflowByCategory);
public record CategoryTotal(string Category, decimal Amount);
public record ClosingDto(Guid Id, string Competence, decimal ChargesIssued, decimal PaymentsReceived, decimal OtherRevenue, decimal Expenses, decimal OpenBalance,
    string? Notes, string? ClosedBy, DateTime ClosedAt);
public record CloseMonthRequest([Required, RegularExpression(@"^\d{4}-(0[1-9]|1[0-2])$", ErrorMessage = "Use o formato AAAA-MM.")] string Competence, [StringLength(1000)] string? Notes);
public record ReleaseDto(Guid Id, Guid MemberId, string MemberName, string Reason, DateOnly ValidUntil, string? GrantedBy, DateTime GrantedAt, DateTime? RevokedAt, bool IsActive);
public record GrantReleaseRequest(Guid MemberId, [Required, StringLength(500, MinimumLength = 5)] string Reason, DateOnly ValidUntil);
public record DelinquentDto(Guid MemberId, int Number, string FullName, string? Phone, string? Email, int OverdueCharges, decimal OverdueAmount, DateOnly OldestDueDate, bool HasActiveRelease);

public class EntryQuery : PageQuery
{
    public FinancialEntryKind? Kind { get; set; }
    public FinancialEntryStatus? Status { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
}

public class FinanceAdminService(IAppDb db, RequestContext ctx, ChargeService charges, ClubClock clock)
{
    // ---------- Receitas e despesas ----------

    public async Task<PagedResult<FinancialEntryDto>> EntriesAsync(EntryQuery query, CancellationToken ct = default)
    {
        var today = await clock.TodayAsync(ct);
        var q = db.Set<FinancialEntry>().AsNoTracking();
        if (query.Kind is not null) q = q.Where(e => e.Kind == query.Kind);
        if (query.Status is not null) q = q.Where(e => e.Status == query.Status);
        if (query.From is not null) q = q.Where(e => e.DueDate >= query.From);
        if (query.To is not null) q = q.Where(e => e.DueDate <= query.To);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var like = query.Search.Like();
            q = q.Where(e => EF.Functions.ILike(e.Description, like) || EF.Functions.ILike(e.Category, like) || EF.Functions.ILike(e.Counterparty!, like));
        }
        var page = await q.OrderByDescending(e => e.DueDate).ToPagedAsync(query, ct);
        return new PagedResult<FinancialEntryDto>(page.Items.Select(e => Map(e, today)).ToList(), page.Total, page.Page, page.PageSize);
    }

    public async Task<FinancialEntryDto> SaveEntryAsync(Guid? id, SaveEntryRequest req, CancellationToken ct = default)
    {
        FinancialEntry entry;
        if (id is null)
        {
            entry = new FinancialEntry { CreatedByUserId = ctx.UserId };
            db.Set<FinancialEntry>().Add(entry);
        }
        else
        {
            entry = await db.Set<FinancialEntry>().Where(e => e.Id == id).FirstOr404Async("Lançamento", ct);
            if (entry.Status != FinancialEntryStatus.Pending) throw DomainException.Conflict("Somente lançamentos pendentes podem ser editados.");
        }
        (entry.Kind, entry.Category, entry.Description, entry.Counterparty) = (req.Kind, req.Category.Trim(), req.Description.Trim(), req.Counterparty);
        (entry.Amount, entry.DueDate, entry.Notes, entry.MaintenanceOrderId, entry.ReceiptFileId) = (decimal.Round(req.Amount, 2), req.DueDate, req.Notes, req.MaintenanceOrderId, req.ReceiptFileId);
        await db.SaveChangesAsync(ct);
        return Map(entry, await clock.TodayAsync(ct));
    }

    public async Task<FinancialEntryDto> PayEntryAsync(Guid id, PayEntryRequest req, CancellationToken ct = default)
    {
        var entry = await db.Set<FinancialEntry>().Where(e => e.Id == id).FirstOr404Async("Lançamento", ct);
        if (entry.Status != FinancialEntryStatus.Pending) throw DomainException.Conflict("Este lançamento não está pendente.");
        var today = await clock.TodayAsync(ct);
        if (req.PaidOn > today) throw new DomainException("A data de pagamento não pode ser futura.", "data_invalida", 400);
        await charges.EnsureOpenAsync(req.PaidOn.ToString("yyyy-MM"), ct);
        entry.Status = FinancialEntryStatus.Paid;
        entry.PaidOn = req.PaidOn;
        if (req.ReceiptFileId is not null) entry.ReceiptFileId = req.ReceiptFileId;
        await db.SaveChangesAsync(ct);
        return Map(entry, today);
    }

    /// <summary>Lançamentos nunca são apagados: o cancelamento preserva o registro e o motivo.</summary>
    public async Task<FinancialEntryDto> CancelEntryAsync(Guid id, CancelRequest req, CancellationToken ct = default)
    {
        var entry = await db.Set<FinancialEntry>().Where(e => e.Id == id).FirstOr404Async("Lançamento", ct);
        if (entry.Status == FinancialEntryStatus.Cancelled) throw DomainException.Conflict("Lançamento já cancelado.");
        if (entry.PaidOn is { } paid) await charges.EnsureOpenAsync(paid.ToString("yyyy-MM"), ct);
        entry.Status = FinancialEntryStatus.Cancelled;
        entry.CancelReason = req.Reason;
        ctx.Justification = req.Reason;
        await db.SaveChangesAsync(ct);
        return Map(entry, await clock.TodayAsync(ct));
    }

    // ---------- Fluxo de caixa ----------

    public async Task<CashFlowDto> CashFlowAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        if (to < from || to.DayNumber - from.DayNumber > 800) throw new DomainException("Período inválido.", "periodo_invalido", 400);
        var zone = await clock.ZoneAsync(ct);
        var fromUtc = await clock.ToUtcAsync(from, 0, ct);
        var toUtc = await clock.ToUtcAsync(to.AddDays(1), 0, ct);
        var today = await clock.TodayAsync(ct);

        var payments = await db.Set<Payment>().Where(p => p.Status == PaymentStatus.Confirmed && p.PaidAt >= fromUtc && p.PaidAt < toUtc)
            .Select(p => new { p.PaidAt, p.Amount, p.Charge.Type }).ToListAsync(ct);
        var entries = await db.Set<FinancialEntry>().Where(e => e.Status == FinancialEntryStatus.Paid && e.PaidOn >= from && e.PaidOn <= to)
            .Select(e => new { e.PaidOn, e.Amount, e.Kind, e.Category }).ToListAsync(ct);

        var byDay = to.DayNumber - from.DayNumber <= 62;
        string Key(DateOnly d) => byDay ? d.ToString("yyyy-MM-dd") : d.ToString("yyyy-MM");
        var flows = payments.Select(p => (Period: Key(DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(p.PaidAt!.Value, zone))), In: p.Amount, Out: 0m))
            .Concat(entries.Select(e => (Period: Key(e.PaidOn!.Value), In: e.Kind == FinancialEntryKind.Revenue ? e.Amount : 0m, Out: e.Kind == FinancialEntryKind.Expense ? e.Amount : 0m)));
        var series = flows.GroupBy(f => f.Period).OrderBy(g => g.Key)
            .Select(g => new CashFlowPoint(g.Key, g.Sum(f => f.In), g.Sum(f => f.Out), g.Sum(f => f.In) - g.Sum(f => f.Out))).ToList();

        var inflowByCategory = payments.GroupBy(p => ChargeTypeLabel(p.Type)).Select(g => new CategoryTotal(g.Key, g.Sum(p => p.Amount)))
            .Concat(entries.Where(e => e.Kind == FinancialEntryKind.Revenue).GroupBy(e => e.Category).Select(g => new CategoryTotal(g.Key, g.Sum(e => e.Amount))))
            .OrderByDescending(c => c.Amount).ToList();
        var outflowByCategory = entries.Where(e => e.Kind == FinancialEntryKind.Expense).GroupBy(e => e.Category)
            .Select(g => new CategoryTotal(g.Key, g.Sum(e => e.Amount))).OrderByDescending(c => c.Amount).ToList();

        var open = db.Set<Charge>().Where(c => c.Status == ChargeStatus.Open);
        var receivable = await open.SumAsync(ChargeService.BalanceExpr, ct)
            + await db.Set<FinancialEntry>().Where(e => e.Kind == FinancialEntryKind.Revenue && e.Status == FinancialEntryStatus.Pending).SumAsync(e => e.Amount, ct);
        var overdue = await open.Where(c => c.DueDate < today).SumAsync(ChargeService.BalanceExpr, ct);
        var payable = await db.Set<FinancialEntry>().Where(e => e.Kind == FinancialEntryKind.Expense && e.Status == FinancialEntryStatus.Pending).SumAsync(e => e.Amount, ct);

        var inflow = series.Sum(s => s.Inflow);
        var outflow = series.Sum(s => s.Outflow);
        return new CashFlowDto(from, to, inflow, outflow, inflow - outflow, receivable, overdue, payable, series, inflowByCategory, outflowByCategory);
    }

    public static string ChargeTypeLabel(ChargeType type) => type switch
    {
        ChargeType.Monthly => "Mensalidades", ChargeType.Fine => "Multas", ChargeType.Fee => "Taxas", ChargeType.Reservation => "Reservas",
        ChargeType.Guest => "Convidados", ChargeType.Damage => "Danos", ChargeType.Penalty => "Penalidades", _ => "Outras cobranças",
    };

    // ---------- Fechamento mensal ----------

    public async Task<List<ClosingDto>> ClosingsAsync(CancellationToken ct = default)
        => await (from c in db.Set<MonthlyClosing>()
                  join u in db.Set<User>() on c.ClosedByUserId equals u.Id
                  orderby c.Competence descending
                  select new ClosingDto(c.Id, c.Competence, c.ChargesIssued, c.PaymentsReceived, c.OtherRevenue, c.Expenses, c.OpenBalance, c.Notes, u.Name, c.ClosedAt)).ToListAsync(ct);

    public async Task<ClosingDto> CloseMonthAsync(CloseMonthRequest req, CancellationToken ct = default)
    {
        var year = int.Parse(req.Competence[..4]);
        var month = int.Parse(req.Competence[5..]);
        var first = new DateOnly(year, month, 1);
        var last = first.AddMonths(1).AddDays(-1);
        if (last >= await clock.TodayAsync(ct)) throw DomainException.Conflict("Só é possível fechar competências já encerradas.");
        await charges.EnsureOpenAsync(req.Competence, ct);

        var fromUtc = await clock.ToUtcAsync(first, 0, ct);
        var toUtc = await clock.ToUtcAsync(last.AddDays(1), 0, ct);
        var monthCharges = db.Set<Charge>().Where(c => c.Competence == req.Competence && c.Status != ChargeStatus.Cancelled);
        var closing = new MonthlyClosing
        {
            Competence = req.Competence, Notes = req.Notes, ClosedByUserId = ctx.RequireUser(),
            ChargesIssued = await monthCharges.SumAsync(c => c.OriginalAmount + c.Additions + c.FineAmount + c.InterestAmount + c.FeesAmount - c.Discounts, ct),
            PaymentsReceived = await db.Set<Payment>().Where(p => p.Status == PaymentStatus.Confirmed && p.PaidAt >= fromUtc && p.PaidAt < toUtc).SumAsync(p => p.Amount, ct),
            OtherRevenue = await db.Set<FinancialEntry>().Where(e => e.Kind == FinancialEntryKind.Revenue && e.Status == FinancialEntryStatus.Paid && e.PaidOn >= first && e.PaidOn <= last).SumAsync(e => e.Amount, ct),
            Expenses = await db.Set<FinancialEntry>().Where(e => e.Kind == FinancialEntryKind.Expense && e.Status == FinancialEntryStatus.Paid && e.PaidOn >= first && e.PaidOn <= last).SumAsync(e => e.Amount, ct),
            OpenBalance = await monthCharges.Where(c => c.Status == ChargeStatus.Open).SumAsync(ChargeService.BalanceExpr, ct),
        };
        db.Set<MonthlyClosing>().Add(closing);
        await db.SaveChangesAsync(ct);
        return (await ClosingsAsync(ct)).First(c => c.Id == closing.Id);
    }

    public async Task ReopenMonthAsync(Guid id, CancelRequest req, CancellationToken ct = default)
    {
        var closing = await db.Set<MonthlyClosing>().Where(c => c.Id == id).FirstOr404Async("Fechamento", ct);
        ctx.Justification = req.Reason;
        db.Set<MonthlyClosing>().Remove(closing);
        await db.SaveChangesAsync(ct);
    }

    // ---------- Inadimplência ----------

    public async Task<List<DelinquentDto>> DelinquentsAsync(CancellationToken ct = default)
    {
        var today = await clock.TodayAsync(ct);
        var rows = await db.Set<Charge>().Where(c => c.Status == ChargeStatus.Open && c.DelinquentFrom <= today)
            .GroupBy(c => new { c.MemberId, c.Member.Number, c.Member.FullName, c.Member.Phone, c.Member.Email })
            .Select(g => new
            {
                g.Key, Count = g.Count(), Oldest = g.Min(c => c.DueDate),
                Amount = g.Sum(c => c.OriginalAmount + c.Additions + c.FineAmount + c.InterestAmount + c.FeesAmount - c.Discounts - c.PaidAmount),
            }).ToListAsync(ct);
        var released = (await db.Set<FinancialRelease>().Where(r => r.RevokedAt == null && r.ValidUntil >= today).Select(r => r.MemberId).ToListAsync(ct)).ToHashSet();
        return rows.OrderByDescending(r => r.Amount)
            .Select(r => new DelinquentDto(r.Key.MemberId, r.Key.Number, r.Key.FullName, r.Key.Phone, r.Key.Email, r.Count, r.Amount, r.Oldest, released.Contains(r.Key.MemberId))).ToList();
    }

    public async Task<List<ReleaseDto>> ReleasesAsync(Guid? memberId, CancellationToken ct = default)
    {
        var today = await clock.TodayAsync(ct);
        return await (from r in db.Set<FinancialRelease>()
                      join u in db.Set<User>() on r.GrantedByUserId equals u.Id
                      where memberId == null || r.MemberId == memberId
                      orderby r.CreatedAt descending
                      select new ReleaseDto(r.Id, r.MemberId, r.Member.FullName, r.Reason, r.ValidUntil, u.Name, r.CreatedAt, r.RevokedAt, r.RevokedAt == null && r.ValidUntil >= today))
            .Take(200).ToListAsync(ct);
    }

    public async Task<ReleaseDto> GrantReleaseAsync(GrantReleaseRequest req, CancellationToken ct = default)
    {
        await db.Set<Member>().Where(m => m.Id == req.MemberId).FirstOr404Async("Associado", ct);
        if (req.ValidUntil < await clock.TodayAsync(ct)) throw new DomainException("A validade da liberação deve ser futura.", "data_invalida", 400);
        var release = new FinancialRelease { MemberId = req.MemberId, Reason = req.Reason, ValidUntil = req.ValidUntil, GrantedByUserId = ctx.RequireUser() };
        ctx.Justification = req.Reason;
        db.Set<FinancialRelease>().Add(release);
        await db.SaveChangesAsync(ct);
        return (await ReleasesAsync(req.MemberId, ct)).First(r => r.Id == release.Id);
    }

    public async Task RevokeReleaseAsync(Guid id, CancellationToken ct = default)
    {
        var release = await db.Set<FinancialRelease>().Where(r => r.Id == id).FirstOr404Async("Liberação", ct);
        if (release.RevokedAt is not null) return;
        release.RevokedAt = DateTime.UtcNow;
        release.RevokedByUserId = ctx.RequireUser();
        await db.SaveChangesAsync(ct);
    }

    private static FinancialEntryDto Map(FinancialEntry e, DateOnly today) => new(e.Id, e.Kind, e.Category, e.Description, e.Counterparty, e.Amount, e.DueDate, e.PaidOn,
        e.Status, e.Status == FinancialEntryStatus.Pending && e.DueDate < today, e.Notes, e.CancelReason, e.MaintenanceOrderId, e.ReceiptFileId, e.CreatedAt);
}
