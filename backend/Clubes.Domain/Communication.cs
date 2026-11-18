namespace Clubes.Domain;

public class ClubEvent : TenantEntity
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public Guid? ImageFileId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string? Location { get; set; }
    public Guid? SpaceId { get; set; }
    public int? Capacity { get; set; }
    public EventAudience Audience { get; set; } = EventAudience.MembersOnly;
    /// <summary>Públicos aceitos quando Audience = Specific.</summary>
    public bool AllowMembers { get; set; } = true;
    public bool AllowDependents { get; set; } = true;
    public bool AllowStaff { get; set; }
    public bool RegistrationRequired { get; set; } = true;
    public DateTime? RegistrationDeadline { get; set; }
    public DateTime? CancellationDeadline { get; set; }
    public EventStatus Status { get; set; } = EventStatus.Scheduled;
    public string? CancelReason { get; set; }
    public Guid CreatedByUserId { get; set; }
    public List<EventRegistration> Registrations { get; set; } = [];
}

public class EventRegistration : TenantEntity
{
    public Guid EventId { get; set; }
    public ClubEvent Event { get; set; } = null!;
    public Guid UserId { get; set; }
    public string PersonName { get; set; } = "";
    public UserKind PersonKind { get; set; }
    public RegistrationStatus Status { get; set; } = RegistrationStatus.Registered;
    public bool? Attended { get; set; }
    public DateTime? CancelledAt { get; set; }
}

public class Announcement : TenantEntity, ISoftDelete
{
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public AnnouncementCategory Category { get; set; }
    public Priority Priority { get; set; }
    public bool AudienceMembers { get; set; }
    public bool AudienceDependents { get; set; }
    public bool AudienceStaff { get; set; }
    /// <summary>Quando verdadeiro, somente os usuários listados em Targets recebem.</summary>
    public bool IsSpecific { get; set; }
    public Guid? ImageFileId { get; set; }
    public Guid? EventId { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public Guid AuthorUserId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public List<AnnouncementTarget> Targets { get; set; } = [];
}

public class AnnouncementTarget : TenantEntity, INotAudited
{
    public Guid AnnouncementId { get; set; }
    public Announcement Announcement { get; set; } = null!;
    public Guid UserId { get; set; }
}

public class AnnouncementRead : TenantEntity, INotAudited
{
    public Guid AnnouncementId { get; set; }
    public Announcement Announcement { get; set; } = null!;
    public Guid UserId { get; set; }
}

public class Notification : TenantEntity, INotAudited
{
    public Guid UserId { get; set; }
    public NotificationCategory Category { get; set; }
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public bool IsMandatory { get; set; }
    public DateTime? ReadAt { get; set; }
    public DeliveryStatus PushStatus { get; set; } = DeliveryStatus.NotRequested;
    public DateTime? PushSentAt { get; set; }
    public DeliveryStatus EmailStatus { get; set; } = DeliveryStatus.NotRequested;
    public DateTime? EmailSentAt { get; set; }
    public string? DeliveryError { get; set; }
    /// <summary>Momento em que o despachante processou a notificação (tempo real, push e e-mail).</summary>
    public DateTime? DispatchedAt { get; set; }
}

public class DeviceToken : TenantEntity, INotAudited
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = "";
    public string Platform { get; set; } = "";
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
}

public class NotificationPreference : TenantEntity, INotAudited
{
    public Guid UserId { get; set; }
    public NotificationCategory Category { get; set; }
    public bool InAppEnabled { get; set; } = true;
    public bool PushEnabled { get; set; } = true;
    public bool EmailEnabled { get; set; } = true;
}

public class ManifestationType : TenantEntity
{
    public string Name { get; set; } = "";
    public bool IsSuggestion { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Manifestation : TenantEntity
{
    public string Protocol { get; set; } = "";
    public Guid TypeId { get; set; }
    public ManifestationType Type { get; set; } = null!;
    public string Description { get; set; } = "";
    public Guid? PhotoFileId { get; set; }
    public Guid UserId { get; set; }
    public string AuthorName { get; set; } = "";
    public UserKind AuthorKind { get; set; }
    public Guid? MemberId { get; set; }
    public ManifestationStatus Status { get; set; } = ManifestationStatus.Received;
    public string? Classification { get; set; }
    public Guid? AssemblyId { get; set; }
    public DateTime? ClosedAt { get; set; }
    public List<ManifestationUpdate> Updates { get; set; } = [];
}

public class ManifestationUpdate : TenantEntity, INotAudited
{
    public Guid ManifestationId { get; set; }
    public Manifestation Manifestation { get; set; } = null!;
    public ManifestationStatus Status { get; set; }
    public string? Message { get; set; }
    public bool IsVisibleToAuthor { get; set; } = true;
    public Guid UserId { get; set; }
    public string UserName { get; set; } = "";
}

public class Assembly : TenantEntity
{
    public string Title { get; set; } = "";
    public DateTime ScheduledAt { get; set; }
    public string? Location { get; set; }
    public string? Agenda { get; set; }
    public AssemblyStatus Status { get; set; } = AssemblyStatus.Scheduled;
    public string? Minutes { get; set; }
    public Guid? MinutesFileId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public List<Poll> Polls { get; set; } = [];
    public List<AssemblyAttendance> Attendances { get; set; } = [];
}

public class AssemblyAttendance : TenantEntity
{
    public Guid AssemblyId { get; set; }
    public Assembly Assembly { get; set; } = null!;
    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;
    public Guid RegisteredByUserId { get; set; }
}

public class Poll : TenantEntity
{
    public Guid? AssemblyId { get; set; }
    public Assembly? Assembly { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateTime? OpensAt { get; set; }
    public DateTime? ClosesAt { get; set; }
    public PollStatus Status { get; set; } = PollStatus.Draft;
    public PollCriterion Criterion { get; set; } = PollCriterion.ActiveMembers;
    public bool ResultsPublished { get; set; }
    public Guid CreatedByUserId { get; set; }
    public List<PollOption> Options { get; set; } = [];
    public List<Vote> Votes { get; set; } = [];
}

public class PollOption : TenantEntity
{
    public Guid PollId { get; set; }
    public Poll Poll { get; set; } = null!;
    public string Text { get; set; } = "";
    public int SortOrder { get; set; }
}

/// <summary>Voto. Unicidade (PollId, MemberId) no banco impede voto duplicado. Somente associados votam.</summary>
public class Vote : TenantEntity
{
    public Guid PollId { get; set; }
    public Poll Poll { get; set; } = null!;
    public Guid PollOptionId { get; set; }
    public PollOption PollOption { get; set; } = null!;
    public Guid MemberId { get; set; }
    public Guid UserId { get; set; }
}
