namespace Clubes.Domain;

public class Space : TenantEntity, ISoftDelete
{
    public string Name { get; set; } = "";
    public SpaceType Type { get; set; }
    public string? Subtype { get; set; }
    public string? Description { get; set; }
    public int? Capacity { get; set; }
    public bool IsReservable { get; set; } = true;
    public BookingMode BookingMode { get; set; } = BookingMode.Hourly;
    public int SlotMinutes { get; set; } = 60;
    /// <summary>Quantidade fixa de dias para reservas MultiDay (ex.: salão de festas = 2).</summary>
    public int FixedDays { get; set; } = 1;
    public decimal? HourlyPrice { get; set; }
    public decimal? PeriodPrice { get; set; }
    public decimal? CleaningFee { get; set; }
    public SpaceChargeMode? ChargeMode { get; set; }
    public ApprovalMode? ApprovalMode { get; set; }
    public string? UsageRules { get; set; }
    public Guid? ImageFileId { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public List<SpaceSchedule> Schedules { get; set; } = [];
}

/// <summary>Janela de reserva por dia da semana, em minutos desde 00:00 (CloseMinute pode ser 1440 = meia-noite).</summary>
public class SpaceSchedule : TenantEntity
{
    public Guid SpaceId { get; set; }
    public Space Space { get; set; } = null!;
    public DayOfWeek DayOfWeek { get; set; }
    public int OpenMinute { get; set; }
    public int CloseMinute { get; set; }
}

/// <summary>Bloqueio de espaço (SpaceId nulo = todos os espaços).</summary>
public class SpaceBlock : TenantEntity
{
    public Guid? SpaceId { get; set; }
    public Space? Space { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string Reason { get; set; } = "";
    public BlockKind Kind { get; set; }
    public Guid? MaintenanceOrderId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public bool IsCancelled { get; set; }
}

public class Holiday : TenantEntity
{
    public DateOnly Date { get; set; }
    public string Name { get; set; } = "";
    public HolidayScope Scope { get; set; }
    public bool IsClosed { get; set; }
    public int? OpenMinute { get; set; }
    public int? CloseMinute { get; set; }
    public string? Notes { get; set; }
}

public class Reservation : TenantEntity
{
    public string Code { get; set; } = "";
    public Guid SpaceId { get; set; }
    public Space Space { get; set; } = null!;
    public ReservationKind Kind { get; set; } = ReservationKind.Member;
    public string? Title { get; set; }
    public Guid? MemberId { get; set; }
    public Member? Member { get; set; }
    public Guid RequestedByUserId { get; set; }
    public Guid? RequestedByDependentId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int PeopleCount { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    public decimal RentalAmount { get; set; }
    public decimal CleaningFee { get; set; }
    public decimal TotalAmount { get; set; }
    public SpaceChargeMode? ChargeMode { get; set; }
    public Guid? ChargeId { get; set; }
    public string? Notes { get; set; }
    public Guid? DecisionByUserId { get; set; }
    public DateTime? DecisionAt { get; set; }
    public string? DecisionReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public string? CancellationReason { get; set; }
    public AttendanceStatus Attendance { get; set; } = AttendanceStatus.Unknown;
    public string? AttendanceNote { get; set; }
    public Guid? PenaltyChargeId { get; set; }
    public DateTime? ReminderSentAt { get; set; }

    public List<ReservationSlot> Slots { get; set; } = [];
}

/// <summary>
/// Intervalo ocupado por uma reserva. O banco impede sobreposição de intervalos ativos no mesmo
/// espaço por restrição de exclusão (EXCLUDE USING gist), garantindo ausência de conflitos mesmo
/// sob concorrência.
/// </summary>
public class ReservationSlot : TenantEntity, INotAudited
{
    public Guid ReservationId { get; set; }
    public Reservation Reservation { get; set; } = null!;
    public Guid SpaceId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public class KeyHandover : TenantEntity
{
    public Guid ReservationId { get; set; }
    public Reservation Reservation { get; set; } = null!;
    public Guid SpaceId { get; set; }
    public Guid? MemberId { get; set; }
    public string ReceiverName { get; set; } = "";
    public Guid DeliveredByUserId { get; set; }
    public DateTime PickedUpAt { get; set; }
    public DateTime ExpectedReturnAt { get; set; }
    public string? ConditionOnDelivery { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public Guid? ReceivedByUserId { get; set; }
    public string? ConditionOnReturn { get; set; }
    public string? Pendencies { get; set; }
    public bool PendenciesResolved { get; set; }
}

public class Inspection : TenantEntity
{
    public Guid SpaceId { get; set; }
    public Space Space { get; set; } = null!;
    public Guid? ReservationId { get; set; }
    public Guid? MemberId { get; set; }
    public InspectionMoment Moment { get; set; }
    public Guid InspectorUserId { get; set; }
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public bool HasDamage { get; set; }
    public List<InspectionItem> Items { get; set; } = [];
}

public class InspectionItem : TenantEntity, INotAudited
{
    public Guid InspectionId { get; set; }
    public Inspection Inspection { get; set; } = null!;
    public InspectionCategory Category { get; set; }
    public string Description { get; set; } = "";
    public bool IsOk { get; set; }
    public bool IsDamage { get; set; }
    public string? Notes { get; set; }
}

public class Occurrence : TenantEntity
{
    public string Code { get; set; } = "";
    public OccurrenceKind Kind { get; set; }
    public string Description { get; set; } = "";
    public Guid? SpaceId { get; set; }
    public Guid? ReservationId { get; set; }
    public Guid? MemberId { get; set; }
    public Guid? InspectionId { get; set; }
    public Guid ReportedByUserId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public OccurrenceStatus Status { get; set; } = OccurrenceStatus.Open;
    public string? Resolution { get; set; }
    public decimal? ProposedChargeAmount { get; set; }
    public ChargeDecision ChargeDecision { get; set; } = ChargeDecision.None;
    public Guid? ChargeDecisionByUserId { get; set; }
    public DateTime? ChargeDecisionAt { get; set; }
    public string? ChargeDecisionReason { get; set; }
    public Guid? ChargeId { get; set; }
}

public class MaintenanceOrder : TenantEntity
{
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public MaintenanceKind Kind { get; set; }
    public Guid? SpaceId { get; set; }
    public Guid? AssetId { get; set; }
    public Guid? ResponsibleEmployeeId { get; set; }
    public string? ResponsibleName { get; set; }
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateOnly? DueDate { get; set; }
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Requested;
    public decimal? EstimatedCost { get; set; }
    public decimal? ActualCost { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid OpenedByUserId { get; set; }
}

public class Asset : TenantEntity, ISoftDelete
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Category { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public Guid? SpaceId { get; set; }
    public AssetCondition Condition { get; set; } = AssetCondition.Good;
    public DateOnly? AcquiredOn { get; set; }
    public decimal? Value { get; set; }
    public Guid? ResponsibleEmployeeId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public class Supplier : TenantEntity, ISoftDelete
{
    public string Name { get; set; } = "";
    public string? Document { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public class StockItem : TenantEntity, ISoftDelete
{
    public string Name { get; set; } = "";
    public string? Category { get; set; }
    public string Unit { get; set; } = "un";
    public decimal Quantity { get; set; }
    public decimal MinQuantity { get; set; }
    public Guid? SupplierId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public class StockMovement : TenantEntity
{
    public Guid StockItemId { get; set; }
    public StockItem StockItem { get; set; } = null!;
    public StockMovementKind Kind { get; set; }
    public decimal Quantity { get; set; }
    public decimal BalanceAfter { get; set; }
    public decimal? UnitCost { get; set; }
    public string? Reason { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid UserId { get; set; }
}
