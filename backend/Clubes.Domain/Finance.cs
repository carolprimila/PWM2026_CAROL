namespace Clubes.Domain;

/// <summary>
/// Cobrança. Preserva o retrato do momento da geração (mensalidade base, quantidade de dependentes,
/// percentuais de multa e juros). Mudanças futuras de configuração não alteram cobranças existentes.
/// </summary>
public class Charge : TenantEntity
{
    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;
    public ChargeType Type { get; set; }
    public string Description { get; set; } = "";
    /// <summary>Competência no formato AAAA-MM.</summary>
    public string Competence { get; set; } = "";
    public DateOnly DueDate { get; set; }
    /// <summary>Primeiro dia em que a cobrança em aberto caracteriza inadimplência (vencimento + tolerância + 1).</summary>
    public DateOnly DelinquentFrom { get; set; }

    public decimal BaseFee { get; set; }
    public int DependentsCount { get; set; }
    public decimal DependentsAmount { get; set; }
    public string? CompositionSnapshot { get; set; }
    public decimal OriginalAmount { get; set; }

    public decimal Additions { get; set; }
    public decimal Discounts { get; set; }
    public decimal FineAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal FeesAmount { get; set; }
    public decimal PaidAmount { get; set; }

    public int GraceDaysSnapshot { get; set; }
    public decimal? LateFinePercentSnapshot { get; set; }
    public decimal? DailyInterestPercentSnapshot { get; set; }

    public ChargeStatus Status { get; set; } = ChargeStatus.Open;
    public string? CancelReason { get; set; }
    public Guid? ReservationId { get; set; }
    public Guid? GuestId { get; set; }
    public Guid? OccurrenceId { get; set; }
    public Guid? CreatedByUserId { get; set; }

    public List<ChargeAdjustment> Adjustments { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];

    public decimal Total => OriginalAmount + Additions + FineAmount + InterestAmount + FeesAmount - Discounts;
    public decimal Balance => Math.Max(0, Total - PaidAmount);
}

public class ChargeAdjustment : TenantEntity
{
    public Guid ChargeId { get; set; }
    public Charge Charge { get; set; } = null!;
    public AdjustmentKind Kind { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = "";
    public Guid? UserId { get; set; }
    public bool IsAutomatic { get; set; }
}

public class Payment : TenantEntity
{
    public Guid ChargeId { get; set; }
    public Charge Charge { get; set; } = null!;
    public Guid MemberId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentChannel Channel { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Confirmed;
    public DateTime? PaidAt { get; set; }
    public Guid? RegisteredByUserId { get; set; }
    /// <summary>Chave de idempotência enviada pelo cliente: a mesma operação nunca é registrada duas vezes.</summary>
    public string IdempotencyKey { get; set; } = "";
    public string ReceiptNumber { get; set; } = "";
    public string? GatewayProvider { get; set; }
    public string? GatewayReference { get; set; }
    public string? GatewayPayload { get; set; }
    public Guid? ReceiptFileId { get; set; }
    public string? Notes { get; set; }
    public DateTime? RefundedAt { get; set; }
    public string? RefundReason { get; set; }
    public Guid? RefundedByUserId { get; set; }
}

/// <summary>Receitas e despesas avulsas (contas a pagar e a receber fora das cobranças de associados).</summary>
public class FinancialEntry : TenantEntity
{
    public FinancialEntryKind Kind { get; set; }
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public string? Counterparty { get; set; }
    public decimal Amount { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly? PaidOn { get; set; }
    public FinancialEntryStatus Status { get; set; } = FinancialEntryStatus.Pending;
    public string? Notes { get; set; }
    public string? CancelReason { get; set; }
    public Guid? MaintenanceOrderId { get; set; }
    public Guid? ReceiptFileId { get; set; }
    public Guid? CreatedByUserId { get; set; }
}

public class MonthlyClosing : TenantEntity
{
    public string Competence { get; set; } = "";
    public decimal ChargesIssued { get; set; }
    public decimal PaymentsReceived { get; set; }
    public decimal OtherRevenue { get; set; }
    public decimal Expenses { get; set; }
    public decimal OpenBalance { get; set; }
    public string? Notes { get; set; }
    public Guid ClosedByUserId { get; set; }
    public DateTime ClosedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Liberação excepcional de um associado inadimplente, com justificativa e validade.</summary>
public class FinancialRelease : TenantEntity
{
    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;
    public string Reason { get; set; } = "";
    public DateOnly ValidUntil { get; set; }
    public Guid GrantedByUserId { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid? RevokedByUserId { get; set; }
}
