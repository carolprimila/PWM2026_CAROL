namespace Clubes.Domain;

public class Member : TenantEntity, ISoftDelete
{
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
    public MemberStatus Status { get; set; } = MemberStatus.PendingApproval;
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public List<Dependent> Dependents { get; set; } = [];
}

public class Dependent : TenantEntity, ISoftDelete
{
    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;
    public string FullName { get; set; } = "";
    public string? Cpf { get; set; }
    public string? Rg { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string Relationship { get; set; } = "";
    public Guid? PhotoFileId { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DependentStatus Status { get; set; } = DependentStatus.PendingApproval;
    public string? StatusReason { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public List<DependentPermission> Permissions { get; set; } = [];
}

/// <summary>
/// Permissão individual do dependente. Só é efetiva quando liberada pela administração
/// (AllowedByAdmin) e habilitada pelo associado responsável (EnabledByMember).
/// </summary>
public class DependentPermission : TenantEntity
{
    public Guid DependentId { get; set; }
    public Dependent Dependent { get; set; } = null!;
    public string Code { get; set; } = "";
    public bool AllowedByAdmin { get; set; }
    public bool EnabledByMember { get; set; }
    public bool IsEffective => AllowedByAdmin && EnabledByMember;
}

public class Employee : TenantEntity, ISoftDelete
{
    public string FullName { get; set; } = "";
    public string Cpf { get; set; } = "";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Position { get; set; }
    public string? Department { get; set; }
    public DateOnly? HiredAt { get; set; }
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public class DigitalCard : TenantEntity
{
    public PersonType PersonType { get; set; }
    public Guid PersonId { get; set; }
    public string CardNumber { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

/// <summary>Token de uso único para uma operação (entrada OU saída). Somente o hash é armazenado.</summary>
public class QrToken : TenantEntity, INotAudited
{
    public PersonType PersonType { get; set; }
    public Guid PersonId { get; set; }
    public AccessOperation Operation { get; set; }
    public string TokenHash { get; set; } = "";
    public string DisplayCode { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public Guid? ConsumedByUserId { get; set; }
    public Guid IssuedToUserId { get; set; }
}

public class AccessLog : TenantEntity, INotAudited
{
    public PersonType? PersonType { get; set; }
    public Guid? PersonId { get; set; }
    public string? PersonName { get; set; }
    public Guid? ResponsibleMemberId { get; set; }
    public AccessOperation Operation { get; set; }
    public AccessResult Result { get; set; }
    public string? DenialReason { get; set; }
    public Guid? StaffUserId { get; set; }
    public string? Origin { get; set; }
    public Guid? QrTokenId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Pessoa atualmente dentro do clube. Inserido na entrada, removido na saída.</summary>
public class Presence : TenantEntity, INotAudited
{
    public PersonType PersonType { get; set; }
    public Guid PersonId { get; set; }
    public string PersonName { get; set; } = "";
    public Guid? ResponsibleMemberId { get; set; }
    public DateTime EnteredAt { get; set; } = DateTime.UtcNow;
}

public class Guest : TenantEntity
{
    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;
    public string FullName { get; set; } = "";
    public string? Document { get; set; }
    public string? Phone { get; set; }
    public DateOnly VisitDate { get; set; }
    public Guid? SpaceId { get; set; }
    public Guid? ReservationId { get; set; }
    public decimal FeeAmount { get; set; }
    public Guid? ChargeId { get; set; }
    public GuestAuthorization Authorization { get; set; } = GuestAuthorization.Pending;
    public Guid? AuthorizedByUserId { get; set; }
    public string? AuthorizationNote { get; set; }
    public Guid RegisteredByUserId { get; set; }
    public DateTime? EnteredAt { get; set; }
    public DateTime? ExitedAt { get; set; }
    public bool IsCancelled { get; set; }
}
