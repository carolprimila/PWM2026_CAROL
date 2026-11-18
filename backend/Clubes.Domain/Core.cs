namespace Clubes.Domain;

public class Club : Entity, ISoftDelete
{
    public string Name { get; set; } = "";
    public string? LegalName { get; set; }
    public string? Cnpj { get; set; }
    public string Slug { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? ZipCode { get; set; }
    public string? Street { get; set; }
    public string? Number { get; set; }
    public string? Complement { get; set; }
    public string? District { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public Guid? LogoFileId { get; set; }
    public string? About { get; set; }
    public string TimeZoneId { get; set; } = "America/Sao_Paulo";
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

/// <summary>Horário geral de funcionamento do clube por dia da semana.</summary>
public class ClubOpeningHour : TenantEntity
{
    public DayOfWeek DayOfWeek { get; set; }
    public bool IsClosed { get; set; }
    public int OpenMinute { get; set; }
    public int CloseMinute { get; set; }
}

/// <summary>
/// Políticas configuráveis do clube. Campos nulos significam "ainda não definido pela administração":
/// o sistema nunca presume um valor — ou a regra não é aplicada (limites/penalidades) ou a operação
/// que depende dela é recusada com erro de configuração pendente.
/// </summary>
public class ClubSettings : TenantEntity
{
    // Financeiro
    public decimal? MonthlyBaseFee { get; set; }
    public int? DueDay { get; set; }
    public int? GraceDays { get; set; }
    public decimal? LateFinePercent { get; set; }
    public decimal? DailyInterestPercent { get; set; }
    public DependentPricingMode DependentPricingMode { get; set; } = DependentPricingMode.NotConfigured;
    public decimal? DependentFixedAmount { get; set; }
    public bool? BlockAccessWhenDelinquent { get; set; }
    public bool? BlockReservationsWhenDelinquent { get; set; }
    public DelinquentReservationPolicy? DelinquentReservationPolicy { get; set; }
    public bool? AllowAdvancePayment { get; set; }
    public string? FamilyCompositionChangeRule { get; set; }
    /// <summary>Gera automaticamente as mensalidades no primeiro dia de cada mês.</summary>
    public bool AutoGenerateMonthlyCharges { get; set; }
    /// <summary>Antecedência, em dias, do aviso de vencimento próximo.</summary>
    public int DueReminderDays { get; set; } = 3;

    // Cadastros
    public int MaxDependentsPerMember { get; set; } = 5;
    public int MaxGuestsPerMember { get; set; } = 3;
    public bool MemberCanToggleDependentPermissions { get; set; } = true;

    // Reservas
    public int? MaxAdvanceDays { get; set; }
    public int? MaxConcurrentReservations { get; set; }
    public int? FreeCancellationHours { get; set; }
    public decimal? LateCancellationFee { get; set; }
    public decimal? NoShowFee { get; set; }
    public bool? RequireAdvancePayment { get; set; }
    public ApprovalMode? ReservationApprovalMode { get; set; }

    // Convidados
    public decimal? GuestFee { get; set; }
    public bool? GuestAdvanceRegistrationRequired { get; set; }
    public bool? GuestDocumentRequired { get; set; }
    public bool? GuestEntryWithoutMemberAllowed { get; set; }

    // Acesso
    public int QrTokenTtlSeconds { get; set; } = 60;

    // Funcionamento (textos de política exibidos aos usuários)
    public string? HolidayRules { get; set; }
    public string? MinorsRules { get; set; }
    public string? MinorsAccompanimentRules { get; set; }
    public string? PoolPolicy { get; set; }
    public string? EventProcedures { get; set; }
    public string? SpaceUsagePolicy { get; set; }
    public string? PrivacyPolicy { get; set; }
    public string PrivacyPolicyVersion { get; set; } = "1";
}

/// <summary>
/// Regra de cobrança por dependente. Progressive: valor do N-ésimo dependente (FromCount = ToCount = N).
/// Tiers: valor adicional total quando a quantidade de dependentes está entre FromCount e ToCount.
/// </summary>
public class DependentPricingRule : TenantEntity
{
    public int FromCount { get; set; }
    public int ToCount { get; set; }
    public decimal Amount { get; set; }
}

public class User : Entity, ISoftDelete
{
    public Guid? ClubId { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public UserKind Kind { get; set; }
    public Guid? MemberId { get; set; }
    public Guid? DependentId { get; set; }
    public Guid? EmployeeId { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
    public bool MustChangePassword { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LockoutUntil { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? PrivacyAcceptedAt { get; set; }
    public string? PrivacyAcceptedVersion { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public List<UserRole> Roles { get; set; } = [];
    public List<UserPermission> Permissions { get; set; } = [];
}

public class Role : TenantEntity
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public List<RolePermission> Permissions { get; set; } = [];
}

public class RolePermission : TenantEntity
{
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public string Permission { get; set; } = "";
}

public class UserRole : TenantEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
}

/// <summary>Exceção individual: concede ou nega uma permissão a um usuário, independentemente das funções.</summary>
public class UserPermission : TenantEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Permission { get; set; } = "";
    public bool Granted { get; set; }
}

/// <summary>Sessão autenticada (refresh token com rotação). Somente o hash é armazenado.</summary>
public class RefreshToken : Entity, INotAudited
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }
    public string? ReplacedByHash { get; set; }
    public string? Device { get; set; }
    public string? Ip { get; set; }
    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
}

public class LoginHistory : Entity, INotAudited
{
    public Guid? ClubId { get; set; }
    public Guid? UserId { get; set; }
    public string Email { get; set; } = "";
    public bool Success { get; set; }
    public string? Reason { get; set; }
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
}

public class PasswordResetToken : Entity, INotAudited
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
}

public class StoredFile : TenantEntity, INotAudited
{
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Size { get; set; }
    public string StoragePath { get; set; } = "";
    public FileScope Scope { get; set; } = FileScope.Restricted;
    public Guid UploadedByUserId { get; set; }
    /// <summary>Pessoa a quem o arquivo se refere (para liberar acesso ao próprio titular).</summary>
    public Guid? OwnerMemberId { get; set; }
}

/// <summary>Anexo genérico (documentos, fotos de vistoria, manutenção, comunicados, assembleias...).</summary>
public class Attachment : TenantEntity
{
    public string OwnerType { get; set; } = "";
    public Guid OwnerId { get; set; }
    public Guid FileId { get; set; }
    public StoredFile File { get; set; } = null!;
    public string? Title { get; set; }
    public string? Category { get; set; }
    public Guid UploadedByUserId { get; set; }
}

public class AuditLog : Entity, INotAudited
{
    public Guid? ClubId { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string Operation { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string? EntityId { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string? Origin { get; set; }
    public string? Justification { get; set; }
}

/// <summary>Histórico de mudança de situação de qualquer entidade (associado, reserva, manutenção...).</summary>
public class StatusHistory : TenantEntity, INotAudited
{
    public string EntityType { get; set; } = "";
    public Guid EntityId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = "";
    public string? Note { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
}
