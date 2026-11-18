using System.ComponentModel.DataAnnotations;
using Clubes.Application.Clubs;
using Clubes.Application.Common;
using Clubes.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Clubes.Application.Finance;

public record RegisterPaymentRequest(Guid ChargeId, [Range(0.01, 1_000_000)] decimal Amount, PaymentMethod Method, DateTime? PaidAt,
    [StringLength(500)] string? Notes, Guid? ReceiptFileId, [Required, StringLength(100, MinimumLength = 8)] string IdempotencyKey);
public record StartAppPaymentRequest(Guid ChargeId, PaymentMethod Method, [Required, StringLength(100, MinimumLength = 8)] string IdempotencyKey);
public record AppPaymentDto(PaymentDto Payment, string Provider, string? CheckoutUrl, string? PixCopyPaste, DateTime? ExpiresAt);
public record RefundRequest([Required, StringLength(500, MinimumLength = 3)] string Reason);

public class PaymentQuery : PageQuery
{
    public Guid? MemberId { get; set; }
    public PaymentStatus? Status { get; set; }
    public PaymentMethod? Method { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public class PaymentService(IAppDb db, RequestContext ctx, ChargeService charges, SettingsService settings, NotificationService notifications,
    IPaymentGateway gateway, ILogger<PaymentService> logger)
{
    public async Task<PagedResult<PaymentDto>> ListAsync(PaymentQuery query, CancellationToken ct = default)
    {
        var q = db.Set<Payment>().AsNoTracking().Include(p => p.Charge).ThenInclude(c => c.Member).AsQueryable();
        if (!ctx.IsStaff) q = q.Where(p => p.MemberId == ctx.RequireMember());
        else if (query.MemberId is not null) q = q.Where(p => p.MemberId == query.MemberId);
        if (query.Status is not null) q = q.Where(p => p.Status == query.Status);
        if (query.Method is not null) q = q.Where(p => p.Method == query.Method);
        if (query.From is not null) q = q.Where(p => p.PaidAt >= query.From.Value.ToUniversalTime());
        if (query.To is not null) q = q.Where(p => p.PaidAt <= query.To.Value.ToUniversalTime());
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var like = query.Search.Like();
            q = q.Where(p => EF.Functions.ILike(p.Charge.Member.FullName, like) || EF.Functions.ILike(p.ReceiptNumber, like));
        }
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(p => p.CreatedAt).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);
        var userIds = items.Select(p => p.RegisteredByUserId).Where(u => u != null).Distinct().ToList();
        var names = await db.Set<User>().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Name, ct);
        return new PagedResult<PaymentDto>(items.Select(p => Map(p, p.Charge, p.RegisteredByUserId is { } u ? names.GetValueOrDefault(u) : null)).ToList(), total, query.Page, query.PageSize);
    }

    public async Task<PaymentDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var p = await db.Set<Payment>().AsNoTracking().Include(x => x.Charge).ThenInclude(c => c.Member).Where(x => x.Id == id).FirstOr404Async("Pagamento", ct);
        if (!ctx.IsStaff && p.MemberId != ctx.MemberId) throw DomainException.NotFound("Pagamento");
        var name = p.RegisteredByUserId is { } u ? await db.Set<User>().Where(x => x.Id == u).Select(x => x.Name).FirstOrDefaultAsync(ct) : null;
        return Map(p, p.Charge, name);
    }

    /// <summary>
    /// Pagamento recebido na secretaria. A chave de idempotência impede o registro em duplicidade:
    /// repetir a mesma requisição devolve o pagamento já gravado.
    /// </summary>
    public async Task<PaymentDto> RegisterAsync(RegisterPaymentRequest req, CancellationToken ct = default)
    {
        var existing = await db.Set<Payment>().Where(p => p.IdempotencyKey == req.IdempotencyKey).Select(p => new { p.Id, p.ChargeId }).FirstOrDefaultAsync(ct);
        if (existing is not null)
        {
            if (existing.ChargeId != req.ChargeId) throw DomainException.Conflict("Chave de idempotência já utilizada em outra operação.");
            return await GetAsync(existing.Id, ct);
        }

        await charges.ApplyLateFeesAsync(req.ChargeId, ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var charge = await db.Set<Charge>().Where(c => c.Id == req.ChargeId).FirstOr404Async("Cobrança", ct);
        if (charge.Status != ChargeStatus.Open) throw DomainException.Conflict("Esta cobrança não está em aberto.");
        var amount = decimal.Round(req.Amount, 2);
        if (amount > charge.Balance) throw new DomainException($"O valor informado é maior que o saldo da cobrança ({charge.Balance:C}).", "valor_invalido", 400);
        var paidAt = req.PaidAt?.ToUniversalTime() ?? DateTime.UtcNow;
        if (paidAt > DateTime.UtcNow.AddMinutes(5)) throw new DomainException("A data do pagamento não pode ser futura.", "data_invalida", 400);

        var payment = new Payment
        {
            ChargeId = charge.Id, MemberId = charge.MemberId, Amount = amount, Method = req.Method, Channel = PaymentChannel.FrontDesk,
            Status = PaymentStatus.Confirmed, PaidAt = paidAt, RegisteredByUserId = ctx.RequireUser(), IdempotencyKey = req.IdempotencyKey,
            Notes = req.Notes, ReceiptFileId = req.ReceiptFileId, ReceiptNumber = await NextReceiptAsync(ct),
        };
        db.Set<Payment>().Add(payment);
        await ConfirmAsync(charge, payment, ct);
        await SaveAsync(ct);
        await tx.CommitAsync(ct);
        return await GetAsync(payment.Id, ct);
    }

    /// <summary>Inicia o pagamento pelo aplicativo no gateway. A confirmação chega pela notificação do provedor.</summary>
    public async Task<AppPaymentDto> StartAppPaymentAsync(StartAppPaymentRequest req, CancellationToken ct = default)
    {
        var memberId = ctx.RequireMember();
        if (!gateway.IsConfigured)
            throw new DomainException("O pagamento pelo aplicativo ainda não está disponível neste clube. Realize o pagamento na secretaria.", "gateway_nao_configurado", 503);
        if (req.Method is PaymentMethod.Cash or PaymentMethod.Transfer)
            throw new DomainException("Forma de pagamento indisponível no aplicativo.", "forma_invalida", 400);

        var existing = await db.Set<Payment>().Include(p => p.Charge).ThenInclude(c => c.Member).FirstOrDefaultAsync(p => p.IdempotencyKey == req.IdempotencyKey, ct);
        if (existing is not null)
        {
            if (existing.ChargeId != req.ChargeId || existing.MemberId != memberId) throw DomainException.Conflict("Chave de idempotência já utilizada em outra operação.");
            // Repetição da mesma tentativa: devolve os mesmos dados de checkout enquanto o pagamento estiver pendente.
            var saved = existing.Status == PaymentStatus.Pending && existing.GatewayPayload is not null
                ? System.Text.Json.JsonSerializer.Deserialize<GatewayCheckout>(existing.GatewayPayload) : null;
            return new AppPaymentDto(Map(existing, existing.Charge, null), existing.GatewayProvider ?? gateway.Provider, saved?.CheckoutUrl, saved?.PixCopyPaste, saved?.ExpiresAt);
        }

        await charges.ApplyLateFeesAsync(req.ChargeId, ct);
        var charge = await db.Set<Charge>().Include(c => c.Member).Where(c => c.Id == req.ChargeId && c.MemberId == memberId).FirstOr404Async("Cobrança", ct);
        if (charge.Status != ChargeStatus.Open || charge.Balance <= 0) throw DomainException.Conflict("Esta cobrança não está em aberto.");
        var s = await settings.LoadAsync(ct);
        if (s.AllowAdvancePayment == false && charge.DueDate > DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(1))
            throw new DomainException("O pagamento antecipado desta cobrança não é permitido pela política do clube.", "antecipacao_nao_permitida");
        if (await db.Set<Payment>().AnyAsync(p => p.ChargeId == charge.Id && p.Status == PaymentStatus.Pending && p.CreatedAt > DateTime.UtcNow.AddMinutes(-30), ct))
            throw DomainException.Conflict("Já existe um pagamento em processamento para esta cobrança. Aguarde a confirmação.");

        var payment = new Payment
        {
            ChargeId = charge.Id, MemberId = memberId, Amount = charge.Balance, Method = req.Method, Channel = PaymentChannel.App,
            Status = PaymentStatus.Pending, IdempotencyKey = req.IdempotencyKey, ReceiptNumber = await NextReceiptAsync(ct), GatewayProvider = gateway.Provider,
        };
        var checkout = await gateway.CreateCheckoutAsync(payment.Id, payment.Amount, charge.Description, req.Method, charge.Member.FullName, charge.Member.Cpf, ct);
        payment.GatewayReference = checkout.Reference;
        payment.GatewayPayload = System.Text.Json.JsonSerializer.Serialize(checkout);
        db.Set<Payment>().Add(payment);
        await db.SaveChangesAsync(ct);
        return new AppPaymentDto(Map(payment, charge, null), checkout.Provider, checkout.CheckoutUrl, checkout.PixCopyPaste, checkout.ExpiresAt);
    }

    /// <summary>Processa a notificação do gateway. Idempotente: notificações repetidas não geram novo crédito.</summary>
    public async Task<bool> HandleGatewayNotificationAsync(string body, IDictionary<string, string> headers, CancellationToken ct = default)
    {
        var notification = gateway.ParseNotification(body, headers);
        if (notification is null)
        {
            logger.LogWarning("Notificação de gateway rejeitada (assinatura ou formato inválido).");
            return false;
        }
        var payment = await db.Set<Payment>().IgnoreQueryFilters().FirstOrDefaultAsync(p => p.GatewayReference == notification.Reference && p.GatewayProvider == gateway.Provider, ct);
        if (payment is null) return false;

        ctx.ClubId = payment.ClubId;
        ctx.Origin = "gateway:" + gateway.Provider;
        if (payment.Status != PaymentStatus.Pending) return true;

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var charge = await db.Set<Charge>().FirstAsync(c => c.Id == payment.ChargeId, ct);
        payment.GatewayPayload = notification.RawPayload;
        if (!notification.Paid) payment.Status = PaymentStatus.Failed;
        else
        {
            payment.Status = PaymentStatus.Confirmed;
            payment.PaidAt = DateTime.UtcNow;
            await ConfirmAsync(charge, payment, ct);
        }
        await SaveAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    private async Task ConfirmAsync(Charge charge, Payment payment, CancellationToken ct)
    {
        charge.PaidAmount += payment.Amount;
        if (charge.Balance == 0) charge.Status = ChargeStatus.Paid;
        await notifications.ToMemberAsync(charge.MemberId, NotificationCategory.Finance, "payment.confirmed", "Pagamento confirmado",
            $"Recebemos {payment.Amount:C} referente a \"{charge.Description}\". Recibo nº {payment.ReceiptNumber}.", nameof(Payment), payment.Id, true, ct);
    }

    /// <summary>Estorno: o pagamento permanece no histórico com situação "Estornado" e a cobrança volta a ficar em aberto.</summary>
    public async Task<PaymentDto> RefundAsync(Guid id, RefundRequest req, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var payment = await db.Set<Payment>().Include(p => p.Charge).Where(p => p.Id == id).FirstOr404Async("Pagamento", ct);
        if (payment.Status != PaymentStatus.Confirmed) throw DomainException.Conflict("Somente pagamentos confirmados podem ser estornados.");
        await charges.EnsureOpenAsync((payment.PaidAt ?? payment.CreatedAt).ToString("yyyy-MM"), ct);

        payment.Status = PaymentStatus.Refunded;
        payment.RefundedAt = DateTime.UtcNow;
        payment.RefundReason = req.Reason;
        payment.RefundedByUserId = ctx.RequireUser();
        payment.Charge.PaidAmount -= payment.Amount;
        if (payment.Charge.Status == ChargeStatus.Paid) payment.Charge.Status = ChargeStatus.Open;
        ctx.Justification = req.Reason;
        await notifications.ToMemberAsync(payment.MemberId, NotificationCategory.Finance, "payment.refunded", "Pagamento estornado",
            $"O pagamento de {payment.Amount:C} (recibo nº {payment.ReceiptNumber}) foi estornado. Motivo: {req.Reason}", nameof(Payment), id, true, ct);
        await SaveAsync(ct);
        await tx.CommitAsync(ct);
        return await GetAsync(id, ct);
    }

    private async Task<string> NextReceiptAsync(CancellationToken ct) => $"{DateTime.UtcNow:yyyy}-{await db.NextSequenceAsync("receipt", ct):D6}";

    private async Task SaveAsync(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw DomainException.Conflict("A cobrança foi alterada por outra operação. Atualize e tente novamente."); }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IdempotencyKey") == true)
        {
            throw DomainException.Conflict("Este pagamento já foi registrado.");
        }
    }

    internal static PaymentDto Map(Payment p, Charge c, string? registeredBy) => new(p.Id, p.ChargeId, p.MemberId, c.Member?.FullName ?? "", c.Description, p.Amount,
        p.Method, p.Channel, p.Status, p.PaidAt, p.ReceiptNumber, p.Notes, registeredBy, p.RefundedAt, p.RefundReason, p.ReceiptFileId, p.CreatedAt);
}
