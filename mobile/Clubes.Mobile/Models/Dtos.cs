using System.Text.Json.Serialization;

namespace Clubes.Mobile.Models;

// DTOs do contrato da API (JSON camelCase; enums como texto). As propriedades [JsonIgnore] são apenas apresentação.

public class ApiError
{
    public string? Code { get; set; }
    public string? Message { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

// ---------- Autenticação ----------
public record LoginRequest(string Email, string Password, string? Device);
public record RefreshRequest(string RefreshToken);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string NewPassword);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public class AuthResponse
{
    public string AccessToken { get; set; } = "";
    public DateTime AccessTokenExpiresAt { get; set; }
    public string RefreshToken { get; set; } = "";
    public DateTime RefreshTokenExpiresAt { get; set; }
    public MeDto User { get; set; } = new();
}

public class MeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public UserKind Kind { get; set; }
    public Guid? ClubId { get; set; }
    public string? ClubName { get; set; }
    public Guid? ClubLogoFileId { get; set; }
    public Guid? MemberId { get; set; }
    public Guid? DependentId { get; set; }
    public List<string> Permissions { get; set; } = [];
    public List<string> DependentPermissions { get; set; } = [];
    public bool MustChangePassword { get; set; }
    public bool PrivacyAccepted { get; set; }
    public string? PrivacyPolicyVersion { get; set; }
}

public class SessionDto
{
    public Guid Id { get; set; }
    public string? Device { get; set; }
    public string? Ip { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUsedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsCurrent { get; set; }
    [JsonIgnore] public string DeviceText => string.IsNullOrWhiteSpace(Device) ? "Dispositivo não identificado" : Device!;
    [JsonIgnore] public string DetailText => $"Último uso: {Fmt.DateTime(LastUsedAt)}" + (string.IsNullOrWhiteSpace(Ip) ? "" : $" · IP {Ip}");
    [JsonIgnore] public bool CanRevoke => !IsCurrent;
}

public class PublicPoliciesDto
{
    public string? HolidayRules { get; set; }
    public string? MinorsRules { get; set; }
    public string? MinorsAccompanimentRules { get; set; }
    public string? PoolPolicy { get; set; }
    public string? EventProcedures { get; set; }
    public string? SpaceUsagePolicy { get; set; }
    public string? PrivacyPolicy { get; set; }
    public string PrivacyPolicyVersion { get; set; } = "";
    public int MaxGuestsPerMember { get; set; }
    public int MaxDependentsPerMember { get; set; }
    public decimal? GuestFee { get; set; }
    public bool MemberCanToggleDependentPermissions { get; set; }
}

// ---------- Arquivos ----------
public class FileDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Size { get; set; }
}

public class AttachmentDto
{
    public Guid Id { get; set; }
    public Guid FileId { get; set; }
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Size { get; set; }
    public string? Title { get; set; }
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; }
    [JsonIgnore] public string DisplayName => string.IsNullOrWhiteSpace(Title) ? FileName : Title!;
    [JsonIgnore] public string SizeText => Fmt.FileSize(Size);
}

public class HistoryDto
{
    public Guid Id { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = "";
    public string? Note { get; set; }
    public string? UserName { get; set; }
    public DateTime At { get; set; }
    [JsonIgnore] public string StatusText => Enum.TryParse<ReservationStatus>(ToStatus, out var s) ? Labels.Of(s) : ToStatus;
    [JsonIgnore] public string AtText => Fmt.DateTime(At);
    [JsonIgnore] public bool HasNote => !string.IsNullOrWhiteSpace(Note);
}

// ---------- Cartão e portaria ----------
public class CardDto
{
    public PersonType PersonType { get; set; }
    public Guid PersonId { get; set; }
    public string Name { get; set; } = "";
    public Guid? PhotoFileId { get; set; }
    public string CardNumber { get; set; } = "";
    public string ClubName { get; set; } = "";
    public Guid? ClubLogoFileId { get; set; }
    public string Status { get; set; } = "";
    public bool IsActive { get; set; }
    public string? ResponsibleName { get; set; }
    public bool IsInside { get; set; }
}

public class QrTokenDto
{
    public string Token { get; set; } = "";
    public string DisplayCode { get; set; } = "";
    public AccessOperation Operation { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int TtlSeconds { get; set; }
}

public record ValidateRequest(string Code, string? Device);
public record ManualAccessRequest(PersonType PersonType, Guid PersonId, AccessOperation Operation, string Justification);

public class AccessResultDto
{
    public bool Authorized { get; set; }
    public string Message { get; set; } = "";
    public AccessOperation? Operation { get; set; }
    public PersonType? PersonType { get; set; }
    public Guid? PersonId { get; set; }
    public string? PersonName { get; set; }
    public Guid? PhotoFileId { get; set; }
    public string? ResponsibleName { get; set; }
    public DateTime At { get; set; }
}

public class PresenceDto
{
    public Guid Id { get; set; }
    public PersonType PersonType { get; set; }
    public Guid PersonId { get; set; }
    public string PersonName { get; set; } = "";
    public Guid? ResponsibleMemberId { get; set; }
    public string? ResponsibleName { get; set; }
    public DateTime EnteredAt { get; set; }
    [JsonIgnore] public string TypeText => Labels.Of(PersonType);
    [JsonIgnore] public string DetailText => $"Entrada às {Fmt.Time(EnteredAt)}" + (string.IsNullOrWhiteSpace(ResponsibleName) ? "" : $" · Responsável: {ResponsibleName}");
}

public class OccupancyDto
{
    public int Members { get; set; }
    public int Dependents { get; set; }
    public int Guests { get; set; }
    public int Total { get; set; }
}

public class GatePersonDto
{
    public PersonType PersonType { get; set; }
    public Guid PersonId { get; set; }
    public string Name { get; set; } = "";
    public string? Document { get; set; }
    public Guid? PhotoFileId { get; set; }
    public string Status { get; set; } = "";
    public string? ResponsibleName { get; set; }
    public bool IsInside { get; set; }
    [JsonIgnore] public string DetailText => Labels.Of(PersonType) + (string.IsNullOrWhiteSpace(Document) ? "" : $" · {Document}")
        + (string.IsNullOrWhiteSpace(ResponsibleName) ? "" : $" · Resp.: {ResponsibleName}");
    [JsonIgnore] public string StatusText => Labels.PersonStatus(Status);
    [JsonIgnore] public ChipKind StatusKind => Labels.PersonStatusKind(Status);
    [JsonIgnore] public string PresenceText => IsInside ? "No clube" : "Fora do clube";
    [JsonIgnore] public string ActionText => IsInside ? "Registrar saída" : "Registrar entrada";
}

// ---------- Financeiro ----------
public class FinancialStatusDto
{
    public bool IsDelinquent { get; set; }
    public int OverdueCharges { get; set; }
    public decimal OverdueAmount { get; set; }
    public decimal OpenAmount { get; set; }
    public DateOnly? OldestDueDate { get; set; }
    public bool HasActiveRelease { get; set; }
    public DateOnly? ReleaseValidUntil { get; set; }
    public bool AccessBlocked { get; set; }
    public bool ReservationsBlocked { get; set; }
}

public class ChargeDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public ChargeType Type { get; set; }
    public string Description { get; set; } = "";
    public string Competence { get; set; } = "";
    public DateOnly DueDate { get; set; }
    public decimal BaseFee { get; set; }
    public int DependentsCount { get; set; }
    public decimal DependentsAmount { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal Additions { get; set; }
    public decimal Discounts { get; set; }
    public decimal FineAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal FeesAmount { get; set; }
    public decimal Total { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }
    public ChargeStatus Status { get; set; }
    public bool IsOverdue { get; set; }
    public string? CancelReason { get; set; }
    public Guid? ReservationId { get; set; }
    public DateTime CreatedAt { get; set; }
    [JsonIgnore] public string TypeText => Labels.Of(Type);
    [JsonIgnore] public string DueText => $"Vencimento: {Fmt.Date(DueDate)}";
    [JsonIgnore] public string AmountText => Fmt.Money(Status == ChargeStatus.Open ? Balance : Total);
    [JsonIgnore] public string StatusText => Status == ChargeStatus.Open && IsOverdue ? "Vencida" : Labels.Of(Status);
    [JsonIgnore] public ChipKind StatusKind => Status switch
    {
        ChargeStatus.Paid => ChipKind.Success, ChargeStatus.Cancelled => ChipKind.Neutral, _ => IsOverdue ? ChipKind.Danger : ChipKind.Warning,
    };
}

public class AdjustmentDto
{
    public Guid Id { get; set; }
    public AdjustmentKind Kind { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = "";
    public bool IsAutomatic { get; set; }
    public DateTime At { get; set; }
    [JsonIgnore] public string Title => $"{Labels.Of(Kind)} · {Fmt.Date(At)}";
    [JsonIgnore] public string AmountText => (Kind == AdjustmentKind.Discount ? "- " : "+ ") + Fmt.Money(Amount);
}

public class PaymentDto
{
    public Guid Id { get; set; }
    public Guid ChargeId { get; set; }
    public Guid MemberId { get; set; }
    public string ChargeDescription { get; set; } = "";
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentChannel Channel { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime? PaidAt { get; set; }
    public string ReceiptNumber { get; set; } = "";
    public string? Notes { get; set; }
    public DateTime? RefundedAt { get; set; }
    public string? RefundReason { get; set; }
    public DateTime CreatedAt { get; set; }
    [JsonIgnore] public string AmountText => Fmt.Money(Amount);
    [JsonIgnore] public string StatusText => Labels.Of(Status);
    [JsonIgnore] public ChipKind StatusKind => Labels.Kind(Status);
    [JsonIgnore] public string DetailText => $"{Labels.Of(Method)} · {Labels.Of(Channel)} · {Fmt.DateTime(PaidAt ?? CreatedAt)}";
    [JsonIgnore] public string ReceiptText => $"Recibo nº {ReceiptNumber}";
    [JsonIgnore] public bool HasReceipt => Status is PaymentStatus.Confirmed or PaymentStatus.Refunded;
}

public class ChargeDetailDto
{
    public ChargeDto Charge { get; set; } = new();
    public List<string> Composition { get; set; } = [];
    public List<AdjustmentDto> Adjustments { get; set; } = [];
    public List<PaymentDto> Payments { get; set; } = [];
}

public record StartAppPaymentRequest(Guid ChargeId, PaymentMethod Method, string IdempotencyKey);

public class AppPaymentDto
{
    public PaymentDto Payment { get; set; } = new();
    public string Provider { get; set; } = "";
    public string? CheckoutUrl { get; set; }
    public string? PixCopyPaste { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

// ---------- Espaços e reservas ----------
public class ScheduleDto
{
    public DayOfWeek DayOfWeek { get; set; }
    public int OpenMinute { get; set; }
    public int CloseMinute { get; set; }
}

public class SpaceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public SpaceType Type { get; set; }
    public string? Subtype { get; set; }
    public string? Description { get; set; }
    public int? Capacity { get; set; }
    public bool IsReservable { get; set; }
    public BookingMode BookingMode { get; set; }
    public int SlotMinutes { get; set; }
    public int FixedDays { get; set; }
    public decimal? HourlyPrice { get; set; }
    public decimal? PeriodPrice { get; set; }
    public decimal? CleaningFee { get; set; }
    public SpaceChargeMode? ChargeMode { get; set; }
    public ApprovalMode? ApprovalMode { get; set; }
    public string? UsageRules { get; set; }
    public Guid? ImageFileId { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public List<ScheduleDto> Schedules { get; set; } = [];
    [JsonIgnore] public bool IsMultiDay => BookingMode == BookingMode.MultiDay;
    [JsonIgnore] public string CapacityText => Capacity is { } c ? $"Até {c} pessoas" : "Capacidade não informada";
    [JsonIgnore] public string PriceText
    {
        get
        {
            var parts = new List<string>();
            if (IsMultiDay) { if (PeriodPrice is > 0) parts.Add($"{Fmt.Money(PeriodPrice)} pelo período de {FixedDays} dias"); }
            else if (HourlyPrice is > 0) parts.Add($"{Fmt.Money(HourlyPrice)} por horário");
            if (CleaningFee is > 0) parts.Add($"limpeza {Fmt.Money(CleaningFee)}");
            return parts.Count == 0 ? "Sem custo informado" : string.Join(" + ", parts);
        }
    }
    [JsonIgnore] public string Subtitle => string.IsNullOrWhiteSpace(Subtype) ? CapacityText : $"{Subtype} · {CapacityText}";
}

public class SlotDto
{
    public int StartMinute { get; set; }
    public int EndMinute { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public SlotState State { get; set; }
    public string? Reason { get; set; }
    public bool Mine { get; set; }
}

public class DayAvailabilityDto
{
    public Guid SpaceId { get; set; }
    public DateOnly Date { get; set; }
    public bool IsOpen { get; set; }
    public string? ClosedReason { get; set; }
    public int? OpenMinute { get; set; }
    public int? CloseMinute { get; set; }
    public List<SlotDto> Slots { get; set; } = [];
}

public class PeriodDayDto
{
    public DateOnly Date { get; set; }
    public SlotState State { get; set; }
    public string? Reason { get; set; }
}

public class GuestInput
{
    public string FullName { get; set; } = "";
    public string? Document { get; set; }
    public string? Phone { get; set; }
    [JsonIgnore] public string DetailText => string.Join(" · ", new[] { Document, Phone }.Where(v => !string.IsNullOrWhiteSpace(v)));
}

public class CreateReservationRequest
{
    public Guid SpaceId { get; set; }
    public DateOnly Date { get; set; }
    public List<int> StartMinutes { get; set; } = [];
    public int PeopleCount { get; set; }
    public List<GuestInput> Guests { get; set; } = [];
    public string? Notes { get; set; }
}

public class QuoteDto
{
    public decimal RentalAmount { get; set; }
    public decimal CleaningFee { get; set; }
    public decimal TotalAmount { get; set; }
    public SpaceChargeMode? ChargeMode { get; set; }
    public bool RequiresApproval { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public List<string> Notes { get; set; } = [];
}

public class ReservationSlotDto
{
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    [JsonIgnore] public string Text => Fmt.Period(StartAt, EndAt);
}

public class ReservationDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public Guid SpaceId { get; set; }
    public string SpaceName { get; set; } = "";
    public SpaceType SpaceType { get; set; }
    public ReservationKind Kind { get; set; }
    public string? Title { get; set; }
    public Guid? MemberId { get; set; }
    public string? MemberName { get; set; }
    public string RequestedBy { get; set; } = "";
    public bool RequestedByDependent { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public List<ReservationSlotDto> Slots { get; set; } = [];
    public int PeopleCount { get; set; }
    public ReservationStatus Status { get; set; }
    public decimal RentalAmount { get; set; }
    public decimal CleaningFee { get; set; }
    public decimal TotalAmount { get; set; }
    public SpaceChargeMode? ChargeMode { get; set; }
    public Guid? ChargeId { get; set; }
    public string? Notes { get; set; }
    public string? DecisionReason { get; set; }
    public string? CancellationReason { get; set; }
    public int Guests { get; set; }
    public DateTime CreatedAt { get; set; }
    [JsonIgnore] public string WhenText => Fmt.Period(StartAt, EndAt);
    [JsonIgnore] public string StatusText => Labels.Of(Status);
    [JsonIgnore] public ChipKind StatusKind => Labels.Kind(Status);
    [JsonIgnore] public string CodeText => $"Reserva {Code}";
    [JsonIgnore] public string TotalText => Fmt.Money(TotalAmount);
}

public class ReservationDetailDto
{
    public ReservationDto Reservation { get; set; } = new();
    public List<GuestDto> Guests { get; set; } = [];
    public List<HistoryDto> History { get; set; } = [];
}

public record DecisionRequest(string? Reason);
public record StatusChangeRequest(string Reason);

// ---------- Convidados ----------
public class GuestDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? Document { get; set; }
    public string? Phone { get; set; }
    public DateOnly VisitDate { get; set; }
    public Guid? SpaceId { get; set; }
    public string? SpaceName { get; set; }
    public Guid? ReservationId { get; set; }
    public decimal FeeAmount { get; set; }
    public Guid? ChargeId { get; set; }
    public string PaymentStatus { get; set; } = "";
    public GuestAuthorization Authorization { get; set; }
    public string? AuthorizationNote { get; set; }
    public DateTime? EnteredAt { get; set; }
    public DateTime? ExitedAt { get; set; }
    public bool IsCancelled { get; set; }
    public DateTime CreatedAt { get; set; }
    [JsonIgnore] public string VisitText => $"Visita em {Fmt.Date(VisitDate)}" + (string.IsNullOrWhiteSpace(SpaceName) ? "" : $" · {SpaceName}");
    [JsonIgnore] public string StatusText => IsCancelled ? "Cancelado" : Labels.Of(Authorization);
    [JsonIgnore] public ChipKind StatusKind => IsCancelled ? ChipKind.Neutral : Labels.Kind(Authorization);
    [JsonIgnore] public string FeeText => FeeAmount > 0 ? $"Taxa: {Fmt.Money(FeeAmount)} · {PaymentStatus}" : $"Taxa: {PaymentStatus}";
    [JsonIgnore] public bool HasNote => !string.IsNullOrWhiteSpace(AuthorizationNote);
    [JsonIgnore] public bool CanCancel => !IsCancelled && EnteredAt is null;
    [JsonIgnore] public string ResponsibleText => $"Responsável: {MemberName}";
    [JsonIgnore] public string GateText => EnteredAt is null ? "Ainda não entrou" : ExitedAt is null ? $"Entrou às {Fmt.Time(EnteredAt.Value)}" : $"Entrou às {Fmt.Time(EnteredAt.Value)} · saiu às {Fmt.Time(ExitedAt.Value)}";
    [JsonIgnore] public bool CanEnter => !IsCancelled && EnteredAt is null;
    [JsonIgnore] public bool CanExit => !IsCancelled && EnteredAt is not null && ExitedAt is null;
}

public class CreateGuestRequest
{
    public string FullName { get; set; } = "";
    public string? Document { get; set; }
    public string? Phone { get; set; }
    public DateOnly VisitDate { get; set; }
    public Guid? SpaceId { get; set; }
}

// ---------- Associado e dependentes ----------
public class MemberDto
{
    public Guid Id { get; set; }
    public int Number { get; set; }
    public string FullName { get; set; } = "";
    public string Cpf { get; set; } = "";
    public string? Rg { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? MaritalStatus { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? ZipCode { get; set; }
    public string? Street { get; set; }
    public string? AddressNumber { get; set; }
    public string? Complement { get; set; }
    public string? District { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public Guid? PhotoFileId { get; set; }
    public DateOnly? JoinedAt { get; set; }
    public MemberStatus Status { get; set; }
    public bool HasAccess { get; set; }
    public string? AccessEmail { get; set; }
    public int Dependents { get; set; }
}

public class UpdateMyProfileRequest
{
    public string? Phone { get; set; }
    public string? ZipCode { get; set; }
    public string? Street { get; set; }
    public string? AddressNumber { get; set; }
    public string? Complement { get; set; }
    public string? District { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public Guid? PhotoFileId { get; set; }
}

public class DependentPermissionDto
{
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
    public bool AllowedByAdmin { get; set; }
    public bool EnabledByMember { get; set; }
    public bool Effective { get; set; }
    [JsonIgnore] public string Title => string.IsNullOrWhiteSpace(Description) ? Labels.DependentPermission(Code) : Description;
    [JsonIgnore] public string EffectiveText => Effective ? "Liberado" : "Não liberado";
    [JsonIgnore] public ChipKind EffectiveKind => Effective ? ChipKind.Success : ChipKind.Neutral;
}

public class DependentDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? Cpf { get; set; }
    public string? Rg { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string Relationship { get; set; } = "";
    public Guid? PhotoFileId { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DependentStatus Status { get; set; }
    public string? StatusReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool HasAccess { get; set; }
    public string? AccessEmail { get; set; }
    public List<DependentPermissionDto> Permissions { get; set; } = [];
    [JsonIgnore] public string StatusText => Labels.Of(Status);
    [JsonIgnore] public ChipKind StatusKind => Labels.Kind(Status);
    [JsonIgnore] public string Subtitle => Relationship + (BirthDate is { } b ? $" · nasc. {Fmt.Date(b)}" : "");
}

public class SaveDependentRequest
{
    public string FullName { get; set; } = "";
    public string? Cpf { get; set; }
    public string? Rg { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string Relationship { get; set; } = "";
    public Guid? PhotoFileId { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

public record SetDependentPermissionRequest(string Code, bool Value);
public record CreateAccessRequest(string? Email);

public class TemporaryPasswordDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = "";
    public string TemporaryPassword { get; set; } = "";
    public bool EmailSent { get; set; }
}

// ---------- Comunicação ----------
public class AnnouncementDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public AnnouncementCategory Category { get; set; }
    public Priority Priority { get; set; }
    public Guid? ImageFileId { get; set; }
    public Guid? EventId { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string AuthorName { get; set; } = "";
    public bool IsRead { get; set; }
    public int Attachments { get; set; }
    public DateTime CreatedAt { get; set; }
    [JsonIgnore] public string CategoryText => Labels.Of(Category);
    [JsonIgnore] public string PriorityText => Labels.Of(Priority);
    [JsonIgnore] public bool HasPriority => Priority != Priority.Normal;
    [JsonIgnore] public ChipKind PriorityKind => Priority == Priority.Urgent ? ChipKind.Danger : ChipKind.Warning;
    [JsonIgnore] public string DateText => Fmt.DateTime(PublishedAt ?? CreatedAt);
    [JsonIgnore] public bool IsUnread => !IsRead;
    [JsonIgnore] public bool HasImage => ImageFileId is not null;
    [JsonIgnore] public string Preview => Body.Length > 160 ? Body[..160].TrimEnd() + "…" : Body;
}

public class EventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public Guid? ImageFileId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string? Location { get; set; }
    public int? Capacity { get; set; }
    public EventAudience Audience { get; set; }
    public bool AllowMembers { get; set; }
    public bool AllowDependents { get; set; }
    public bool RegistrationRequired { get; set; }
    public DateTime? RegistrationDeadline { get; set; }
    public DateTime? CancellationDeadline { get; set; }
    public EventStatus Status { get; set; }
    public string? CancelReason { get; set; }
    public int Registered { get; set; }
    public bool IsRegistered { get; set; }
    [JsonIgnore] public string WhenText => Fmt.Period(StartAt, EndAt);
    [JsonIgnore] public string StatusText => IsRegistered ? "Inscrito" : Labels.Of(Status);
    [JsonIgnore] public ChipKind StatusKind => IsRegistered ? ChipKind.Success : Labels.Kind(Status);
    [JsonIgnore] public bool HasLocation => !string.IsNullOrWhiteSpace(Location);
    [JsonIgnore] public bool HasImage => ImageFileId is not null;
    [JsonIgnore] public string SeatsText => Capacity is { } c ? $"{Registered} de {c} vagas preenchidas" : $"{Registered} inscritos";
}

public class ManifestationTypeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsSuggestion { get; set; }
    public bool IsActive { get; set; }
    public override string ToString() => Name;
}

public class ManifestationUpdateDto
{
    public Guid Id { get; set; }
    public ManifestationStatus Status { get; set; }
    public string? Message { get; set; }
    public bool IsVisibleToAuthor { get; set; }
    public string UserName { get; set; } = "";
    public DateTime At { get; set; }
    [JsonIgnore] public string StatusText => Labels.Of(Status);
    [JsonIgnore] public string MetaText => $"{Fmt.DateTime(At)} · {UserName}";
    [JsonIgnore] public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
}

public class ManifestationDto
{
    public Guid Id { get; set; }
    public string Protocol { get; set; } = "";
    public Guid TypeId { get; set; }
    public string TypeName { get; set; } = "";
    public bool IsSuggestion { get; set; }
    public string Description { get; set; } = "";
    public Guid? PhotoFileId { get; set; }
    public string AuthorName { get; set; } = "";
    public ManifestationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    [JsonIgnore] public string ProtocolText => $"Protocolo {Protocol}";
    [JsonIgnore] public string StatusText => Labels.Of(Status);
    [JsonIgnore] public ChipKind StatusKind => Labels.Kind(Status);
    [JsonIgnore] public string MetaText => $"{TypeName} · {Fmt.DateTime(CreatedAt)}";
    [JsonIgnore] public string Preview => Description.Length > 120 ? Description[..120].TrimEnd() + "…" : Description;
    [JsonIgnore] public bool HasPhoto => PhotoFileId is not null;
}

public class ManifestationDetailDto
{
    public ManifestationDto Manifestation { get; set; } = new();
    public List<ManifestationUpdateDto> Updates { get; set; } = [];
}

public record CreateManifestationRequest(Guid TypeId, string Description, Guid? PhotoFileId);

public class AssemblyDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public DateTime ScheduledAt { get; set; }
    public string? Location { get; set; }
    public string? Agenda { get; set; }
    public AssemblyStatus Status { get; set; }
    public string? Minutes { get; set; }
    public Guid? MinutesFileId { get; set; }
    public int Attendances { get; set; }
    public int Polls { get; set; }
    [JsonIgnore] public string WhenText => Fmt.DateTime(ScheduledAt) + (string.IsNullOrWhiteSpace(Location) ? "" : $" · {Location}");
    [JsonIgnore] public string StatusText => Labels.Of(Status);
    [JsonIgnore] public ChipKind StatusKind => Labels.Kind(Status);
}

public class PollOptionDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = "";
    public int SortOrder { get; set; }
    public int? Votes { get; set; }
}

public class PollDto
{
    public Guid Id { get; set; }
    public Guid? AssemblyId { get; set; }
    public string? AssemblyTitle { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateTime? OpensAt { get; set; }
    public DateTime? ClosesAt { get; set; }
    public PollStatus Status { get; set; }
    public PollCriterion Criterion { get; set; }
    public bool ResultsPublished { get; set; }
    public List<PollOptionDto> Options { get; set; } = [];
    public int? TotalVotes { get; set; }
    public bool HasVoted { get; set; }
    public bool IsOpenNow { get; set; }
    [JsonIgnore] public string StatusText => HasVoted ? "Voto registrado" : IsOpenNow ? "Aberta" : Status == PollStatus.Closed ? "Encerrada" : "Aguardando abertura";
    [JsonIgnore] public ChipKind StatusKind => HasVoted ? ChipKind.Success : IsOpenNow ? ChipKind.Warning : ChipKind.Neutral;
    [JsonIgnore] public string PeriodText => ClosesAt is { } c ? (IsOpenNow ? $"Encerra em {Fmt.DateTime(c)}" : $"Encerrada em {Fmt.DateTime(c)}") : OpensAt is { } o ? $"Abertura: {Fmt.DateTime(o)}" : "";
    [JsonIgnore] public bool HasAssembly => !string.IsNullOrWhiteSpace(AssemblyTitle);
}

public record VoteRequest(Guid OptionId);

// ---------- Notificações ----------
public class NotificationDto
{
    public Guid Id { get; set; }
    public NotificationCategory Category { get; set; }
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public bool IsMandatory { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    [JsonIgnore] public bool IsUnread => ReadAt is null;
    [JsonIgnore] public string MetaText => $"{Labels.Of(Category)} · {Fmt.DateTime(CreatedAt)}";
}

public class NotificationPreferenceDto
{
    public NotificationCategory Category { get; set; }
    public bool InAppEnabled { get; set; }
    public bool PushEnabled { get; set; }
    public bool EmailEnabled { get; set; }
    [JsonIgnore] public string CategoryText => Labels.Of(Category);
}

public class UnreadCountDto { public int Count { get; set; } }
public record RegisterDeviceRequest(string Token, string Platform);

/// <summary>Evento "notification" recebido em tempo real.</summary>
public class RealtimeNotification
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string? Category { get; set; }
    public string? Type { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
}

public class AvailabilityChanged
{
    public Guid? SpaceId { get; set; }
}
