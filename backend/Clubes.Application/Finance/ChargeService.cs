using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Clubes.Application.Clubs;
using Clubes.Application.Common;
using Clubes.Domain;
using Microsoft.EntityFrameworkCore;

namespace Clubes.Application.Finance;

public record AdjustmentDto(Guid Id, AdjustmentKind Kind, decimal Amount, string Reason, bool IsAutomatic, DateTime At);
public record PaymentDto(Guid Id, Guid ChargeId, Guid MemberId, string MemberName, string ChargeDescription, decimal Amount, PaymentMethod Method,
    PaymentChannel Channel, PaymentStatus Status, DateTime? PaidAt, string ReceiptNumber, string? Notes, string? RegisteredBy, DateTime? RefundedAt,
    string? RefundReason, Guid? ReceiptFileId, DateTime CreatedAt);
public record ChargeDto(Guid Id, Guid MemberId, string MemberName, int MemberNumber, ChargeType Type, string Description, string Competence, DateOnly DueDate,
    decimal BaseFee, int DependentsCount, decimal DependentsAmount, decimal OriginalAmount, decimal Additions, decimal Discounts, decimal FineAmount,
    decimal InterestAmount, decimal FeesAmount, decimal Total, decimal PaidAmount, decimal Balance, ChargeStatus Status, bool IsOverdue, string? CancelReason,
    Guid? ReservationId, DateTime CreatedAt);
public record ChargeDetailDto(ChargeDto Charge, IReadOnlyList<string> Composition, IReadOnlyList<AdjustmentDto> Adjustments, IReadOnlyList<PaymentDto> Payments);
public record GenerateMonthlyRequest([Required, RegularExpression(@"^\d{4}-(0[1-9]|1[0-2])$", ErrorMessage = "Use o formato AAAA-MM.")] string Competence, List<Guid>? MemberIds);
public record GenerateMonthlyResult(string Competence, int Created, int AlreadyExisting, decimal TotalAmount);
public record CreateChargeRequest(Guid MemberId, ChargeType Type, [Required, StringLength(200, MinimumLength = 3)] string Description,
    [Range(0.01, 1_000_000)] decimal Amount, DateOnly DueDate);
public record AdjustChargeRequest(AdjustmentKind Kind, [Range(0.01, 1_000_000)] decimal Amount, [Required, StringLength(500, MinimumLength = 3)] string Reason);
public record CancelRequest([Required, StringLength(500, MinimumLength = 3)] string Reason);

public class ChargeQuery : PageQuery
{
    public Guid? MemberId { get; set; }
    public ChargeStatus? Status { get; set; }
    public ChargeType? Type { get; set; }
    public string? Competence { get; set; }
    public bool? Overdue { get; set; }
}

/// <summary>Cálculo do acréscimo por dependentes conforme a política configurada. Sem valores embutidos no código.</summary>
public static class DependentPricing
{
    public static decimal Calculate(DependentPricingMode mode, decimal? fixedAmount, IReadOnlyCollection<DependentPricingRule> rules, int dependents)
    {
        if (mode == DependentPricingMode.NotConfigured) throw DomainException.NotConfigured("política de cobrança por dependente");
        if (dependents <= 0 || mode == DependentPricingMode.None) return 0;
        switch (mode)
        {
            case DependentPricingMode.FixedPerDependent:
                return dependents * (fixedAmount ?? throw DomainException.NotConfigured("valor fixo por dependente"));
            case DependentPricingMode.Progressive:
                var total = 0m;
                for (var position = 1; position <= dependents; position++)
                    total += rules.FirstOrDefault(r => r.FromCount == position)?.Amount ?? throw DomainException.NotConfigured($"valor do {position}º dependente");
                return total;
            case DependentPricingMode.Tiers:
                return rules.FirstOrDefault(r => dependents >= r.FromCount && dependents <= r.ToCount)?.Amount
                    ?? throw DomainException.NotConfigured($"faixa de cobrança para {dependents} dependente(s)");
            default:
                throw DomainException.NotConfigured("política de cobrança por dependente");
        }
    }
}

public class ChargeService(IAppDb db, RequestContext ctx, SettingsService settings, ClubClock clock, NotificationService notifications)
{
    public static readonly System.Linq.Expressions.Expression<Func<Charge, decimal>> BalanceExpr =
        c => c.OriginalAmount + c.Additions + c.FineAmount + c.InterestAmount + c.FeesAmount - c.Discounts - c.PaidAmount;

    public async Task<PagedResult<ChargeDto>> ListAsync(ChargeQuery query, CancellationToken ct = default)
    {
        var today = await clock.TodayAsync(ct);
        var q = db.Set<Charge>().AsNoTracking().Include(c => c.Member).AsQueryable();
        if (!ctx.IsStaff) q = q.Where(c => c.MemberId == ctx.RequireMember());
        else if (query.MemberId is not null) q = q.Where(c => c.MemberId == query.MemberId);
        if (query.Status is not null) q = q.Where(c => c.Status == query.Status);
        if (query.Type is not null) q = q.Where(c => c.Type == query.Type);
        if (!string.IsNullOrWhiteSpace(query.Competence)) q = q.Where(c => c.Competence == query.Competence);
        if (query.Overdue is true) q = q.Where(c => c.Status == ChargeStatus.Open && c.DueDate < today);
        if (query.Overdue is false) q = q.Where(c => !(c.Status == ChargeStatus.Open && c.DueDate < today));
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var like = query.Search.Like();
            q = q.Where(c => EF.Functions.ILike(c.Member.FullName, like) || EF.Functions.ILike(c.Description, like));
        }
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(c => c.DueDate).ThenBy(c => c.Member.FullName).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);
        return new PagedResult<ChargeDto>(items.Select(c => Map(c, today)).ToList(), total, query.Page, query.PageSize);
    }

    public async Task<ChargeDetailDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var charge = await db.Set<Charge>().AsNoTracking().Include(c => c.Member).Include(c => c.Adjustments).Include(c => c.Payments).AsSplitQuery()
            .Where(c => c.Id == id).FirstOr404Async("Cobrança", ct);
        if (!ctx.IsStaff && charge.MemberId != ctx.MemberId) throw DomainException.NotFound("Cobrança");
        var userIds = charge.Payments.Select(p => p.RegisteredByUserId).Where(u => u != null).Distinct().ToList();
        var names = await db.Set<User>().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Name, ct);
        var composition = charge.CompositionSnapshot is null ? [] : JsonSerializer.Deserialize<List<string>>(charge.CompositionSnapshot) ?? [];
        return new ChargeDetailDto(Map(charge, await clock.TodayAsync(ct)), composition,
            charge.Adjustments.OrderBy(a => a.CreatedAt).Select(a => new AdjustmentDto(a.Id, a.Kind, a.Amount, a.Reason, a.IsAutomatic, a.CreatedAt)).ToList(),
            charge.Payments.OrderBy(p => p.CreatedAt).Select(p => PaymentService.Map(p, charge, p.RegisteredByUserId is { } u ? names.GetValueOrDefault(u) : null)).ToList());
    }

    /// <summary>
    /// Gera as mensalidades de uma competência para os associados ativos. É idempotente: associados que
    /// já possuem mensalidade ativa na competência são ignorados (garantido também por índice único).
    /// </summary>
    public async Task<GenerateMonthlyResult> GenerateMonthlyAsync(GenerateMonthlyRequest req, CancellationToken ct = default)
    {
        var s = await settings.LoadAsync(ct);
        var baseFee = s.MonthlyBaseFee ?? throw DomainException.NotConfigured("valor da mensalidade base");
        var dueDay = s.DueDay ?? throw DomainException.NotConfigured("dia de vencimento");
        if (s.DependentPricingMode == DependentPricingMode.NotConfigured) throw DomainException.NotConfigured("política de cobrança por dependente");
        await EnsureOpenAsync(req.Competence, ct);

        var rules = await db.Set<DependentPricingRule>().AsNoTracking().ToListAsync(ct);
        var year = int.Parse(req.Competence[..4]);
        var month = int.Parse(req.Competence[5..]);
        var dueDate = new DateOnly(year, month, Math.Min(dueDay, DateTime.DaysInMonth(year, month)));

        var members = await db.Set<Member>().Where(m => m.Status == MemberStatus.Active && (req.MemberIds == null || req.MemberIds.Contains(m.Id)))
            .Select(m => new
            {
                m.Id, m.FullName,
                Dependents = m.Dependents.Where(d => d.Status == DependentStatus.Active || d.Status == DependentStatus.Suspended).Select(d => d.FullName).ToList(),
            }).ToListAsync(ct);
        var existing = (await db.Set<Charge>().Where(c => c.Type == ChargeType.Monthly && c.Competence == req.Competence && c.Status != ChargeStatus.Cancelled)
            .Select(c => c.MemberId).ToListAsync(ct)).ToHashSet();

        int created = 0; decimal total = 0;
        foreach (var m in members.Where(m => !existing.Contains(m.Id)))
        {
            var dependentsAmount = DependentPricing.Calculate(s.DependentPricingMode, s.DependentFixedAmount, rules, m.Dependents.Count);
            var charge = New(s, m.Id, ChargeType.Monthly, $"Mensalidade {month:00}/{year}", baseFee + dependentsAmount, dueDate, req.Competence);
            charge.BaseFee = baseFee;
            charge.DependentsCount = m.Dependents.Count;
            charge.DependentsAmount = dependentsAmount;
            charge.CompositionSnapshot = JsonSerializer.Serialize(m.Dependents);
            await notifications.ToMemberAsync(m.Id, NotificationCategory.Finance, "charge.created", "Nova mensalidade",
                $"Sua mensalidade de {month:00}/{year} no valor de {charge.OriginalAmount:C} vence em {dueDate:dd/MM/yyyy}.", nameof(Charge), charge.Id, false, ct);
            created++;
            total += charge.OriginalAmount;
        }
        await db.SaveChangesAsync(ct);
        return new GenerateMonthlyResult(req.Competence, created, members.Count - created, total);
    }

    public async Task<ChargeDetailDto> CreateAsync(CreateChargeRequest req, CancellationToken ct = default)
    {
        if (req.Type is ChargeType.Monthly) throw new DomainException("Mensalidades são geradas pela rotina de competência.", "tipo_invalido", 400);
        await db.Set<Member>().Where(m => m.Id == req.MemberId).FirstOr404Async("Associado", ct);
        var s = await settings.LoadAsync(ct);
        var charge = New(s, req.MemberId, req.Type, req.Description.Trim(), req.Amount, req.DueDate);
        await EnsureOpenAsync(charge.Competence, ct);
        await NotifyNewAsync(charge, ct);
        await db.SaveChangesAsync(ct);
        return await GetAsync(charge.Id, ct);
    }

    /// <summary>Cria uma cobrança no contexto (sem salvar), com o retrato das regras de atraso vigentes.</summary>
    public Charge New(ClubSettings s, Guid memberId, ChargeType type, string description, decimal amount, DateOnly dueDate, string? competence = null)
    {
        var grace = s.GraceDays ?? 0;
        var charge = new Charge
        {
            MemberId = memberId, Type = type, Description = description, OriginalAmount = decimal.Round(amount, 2), DueDate = dueDate,
            DelinquentFrom = dueDate.AddDays(grace + 1), Competence = competence ?? dueDate.ToString("yyyy-MM"), GraceDaysSnapshot = grace,
            LateFinePercentSnapshot = s.LateFinePercent, DailyInterestPercentSnapshot = s.DailyInterestPercent, CreatedByUserId = ctx.UserId,
        };
        db.Set<Charge>().Add(charge);
        return charge;
    }

    public Task NotifyNewAsync(Charge charge, CancellationToken ct = default)
        => notifications.ToMemberAsync(charge.MemberId, NotificationCategory.Finance, charge.Type == ChargeType.Fine ? "charge.fine" : "charge.additional",
            charge.Type == ChargeType.Fine ? "Multa registrada" : "Nova cobrança",
            $"{charge.Description}: {charge.OriginalAmount:C}, vencimento em {charge.DueDate:dd/MM/yyyy}.", nameof(Charge), charge.Id, true, ct);

    public async Task<ChargeDetailDto> AdjustAsync(Guid id, AdjustChargeRequest req, CancellationToken ct = default)
    {
        var charge = await db.Set<Charge>().Where(c => c.Id == id).FirstOr404Async("Cobrança", ct);
        if (charge.Status != ChargeStatus.Open) throw DomainException.Conflict("Somente cobranças em aberto podem ser ajustadas.");
        var amount = decimal.Round(req.Amount, 2);
        if (req.Kind == AdjustmentKind.Discount && amount > charge.Balance)
            throw new DomainException("O desconto não pode ser maior que o saldo da cobrança.", "valor_invalido", 400);

        switch (req.Kind)
        {
            case AdjustmentKind.Discount: charge.Discounts += amount; break;
            case AdjustmentKind.Addition: charge.Additions += amount; break;
            case AdjustmentKind.Fine: charge.FineAmount += amount; break;
            case AdjustmentKind.Interest: charge.InterestAmount += amount; break;
            case AdjustmentKind.Fee: charge.FeesAmount += amount; break;
        }
        db.Set<ChargeAdjustment>().Add(new ChargeAdjustment { ChargeId = id, Kind = req.Kind, Amount = amount, Reason = req.Reason, UserId = ctx.UserId });
        if (charge.Balance == 0) charge.Status = ChargeStatus.Paid;
        ctx.Justification = req.Reason;
        if (req.Kind is AdjustmentKind.Fine or AdjustmentKind.Fee or AdjustmentKind.Addition)
            await notifications.ToMemberAsync(charge.MemberId, NotificationCategory.Finance, "charge.adjusted", "Cobrança atualizada",
                $"Foi lançado um acréscimo de {amount:C} em \"{charge.Description}\". Motivo: {req.Reason}", nameof(Charge), id, true, ct);
        await SaveWithConcurrencyAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<ChargeDetailDto> CancelAsync(Guid id, CancelRequest req, CancellationToken ct = default)
    {
        var charge = await db.Set<Charge>().Where(c => c.Id == id).FirstOr404Async("Cobrança", ct);
        if (charge.Status != ChargeStatus.Open) throw DomainException.Conflict("Somente cobranças em aberto podem ser canceladas.");
        if (charge.PaidAmount > 0) throw DomainException.Conflict("A cobrança possui pagamentos. Estorne os pagamentos antes de cancelar.");
        await EnsureOpenAsync(charge.Competence, ct);
        charge.Status = ChargeStatus.Cancelled;
        charge.CancelReason = req.Reason;
        ctx.Justification = req.Reason;
        await SaveWithConcurrencyAsync(ct);
        return await GetAsync(id, ct);
    }

    /// <summary>
    /// Aplica multa (uma vez) e juros diários sobre cobranças vencidas além da tolerância, usando os
    /// percentuais registrados na própria cobrança no momento da geração.
    /// </summary>
    public async Task<int> ApplyLateFeesAsync(Guid? chargeId = null, CancellationToken ct = default)
    {
        var today = await clock.TodayAsync(ct);
        var charges = await db.Set<Charge>().Include(c => c.Adjustments)
            .Where(c => c.Status == ChargeStatus.Open && c.DelinquentFrom <= today && (chargeId == null || c.Id == chargeId)
                && (c.LateFinePercentSnapshot != null || c.DailyInterestPercentSnapshot != null)).ToListAsync(ct);
        var changed = 0;
        foreach (var c in charges)
        {
            var basis = c.OriginalAmount + c.Additions - c.Discounts;
            if (basis <= 0) continue;
            var before = (c.FineAmount, c.InterestAmount);

            if (c.LateFinePercentSnapshot is > 0 && !c.Adjustments.Any(a => a is { IsAutomatic: true, Kind: AdjustmentKind.Fine }))
            {
                var fine = decimal.Round(basis * c.LateFinePercentSnapshot.Value / 100, 2);
                c.FineAmount += fine;
                db.Set<ChargeAdjustment>().Add(new ChargeAdjustment { ChargeId = c.Id, Kind = AdjustmentKind.Fine, Amount = fine, IsAutomatic = true, Reason = $"Multa por atraso ({c.LateFinePercentSnapshot:0.####}%)" });
            }
            if (c.DailyInterestPercentSnapshot is > 0)
            {
                var days = today.DayNumber - c.DueDate.DayNumber;
                var interest = decimal.Round(basis * c.DailyInterestPercentSnapshot.Value / 100 * days, 2);
                var auto = c.Adjustments.FirstOrDefault(a => a is { IsAutomatic: true, Kind: AdjustmentKind.Interest });
                var previous = auto?.Amount ?? 0;
                if (interest > previous)
                {
                    c.InterestAmount += interest - previous;
                    var reason = $"Juros por atraso ({c.DailyInterestPercentSnapshot:0.####}% ao dia, {days} dia(s))";
                    if (auto is null) db.Set<ChargeAdjustment>().Add(new ChargeAdjustment { ChargeId = c.Id, Kind = AdjustmentKind.Interest, Amount = interest, IsAutomatic = true, Reason = reason });
                    else { auto.Amount = interest; auto.Reason = reason; }
                }
            }
            if (before != (c.FineAmount, c.InterestAmount)) changed++;
        }
        if (changed > 0) await db.SaveChangesAsync(ct);
        return changed;
    }

    public async Task EnsureOpenAsync(string competence, CancellationToken ct = default)
    {
        if (await db.Set<MonthlyClosing>().AnyAsync(c => c.Competence == competence, ct))
            throw DomainException.Conflict($"A competência {competence} já foi fechada. Reabra o fechamento para lançar ou alterar movimentações.");
    }

    private async Task SaveWithConcurrencyAsync(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw DomainException.Conflict("A cobrança foi alterada por outra operação. Atualize e tente novamente."); }
    }

    internal static ChargeDto Map(Charge c, DateOnly today) => new(c.Id, c.MemberId, c.Member.FullName, c.Member.Number, c.Type, c.Description, c.Competence, c.DueDate,
        c.BaseFee, c.DependentsCount, c.DependentsAmount, c.OriginalAmount, c.Additions, c.Discounts, c.FineAmount, c.InterestAmount, c.FeesAmount,
        c.Total, c.PaidAmount, c.Balance, c.Status, c.Status == ChargeStatus.Open && c.DueDate < today, c.CancelReason, c.ReservationId, c.CreatedAt);
}
