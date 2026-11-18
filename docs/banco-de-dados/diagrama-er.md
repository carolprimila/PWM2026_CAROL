# Diagrama entidade-relacionamento

Gerado a partir do esquema real (`python database/scripts/gerar-doc-banco.py`). Os relacionamentos com `Club` (todas as tabelas de clube possuem `ClubId`) e com `User`/`StoredFile` foram omitidos dos diagramas para manter a leitura; constam no dicionário de dados.

## Plataforma e acesso

```mermaid
erDiagram
    Role ||--o{ RolePermission : "RoleId"
    Dependent ||--o{ User : "DependentId"
    Employee ||--o{ User : "EmployeeId"
    Member ||--o{ User : "MemberId"
    Role ||--o{ UserRole : "RoleId"
    Club {
        uuid Id
        text Name
        text LegalName
        text Cnpj
        text Slug
        text Email
        text Phone
        text ZipCode
        text Street
        text Number
        text Complement
        text District
        text City
        text State
        etc mais_8_colunas
    }
    ClubSettings {
        uuid Id
        numeric MonthlyBaseFee
        integer DueDay
        integer GraceDays
        numeric LateFinePercent
        numeric DailyInterestPercent
        varchar DependentPricingMode
        numeric DependentFixedAmount
        boolean BlockAccessWhenDelinquent
        boolean BlockReservationsWhenDelinquent
        varchar DelinquentReservationPolicy
        boolean AllowAdvancePayment
        text FamilyCompositionChangeRule
        boolean AutoGenerateMonthlyCharges
        etc mais_27_colunas
    }
    ClubOpeningHour {
        uuid Id
        varchar DayOfWeek
        boolean IsClosed
        integer OpenMinute
        integer CloseMinute
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    DependentPricingRule {
        uuid Id
        integer FromCount
        integer ToCount
        numeric Amount
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    User {
        uuid Id
        uuid ClubId
        text Name
        text Email
        text PasswordHash
        varchar Kind
        uuid MemberId
        uuid DependentId
        uuid EmployeeId
        boolean IsBlocked
        text BlockReason
        boolean MustChangePassword
        integer FailedLoginCount
        timestamptz LockoutUntil
        etc mais_7_colunas
    }
    Role {
        uuid Id
        text Name
        text Description
        boolean IsSystem
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    RolePermission {
        uuid Id
        uuid RoleId
        text Permission
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    UserRole {
        uuid Id
        uuid UserId
        uuid RoleId
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    UserPermission {
        uuid Id
        uuid UserId
        text Permission
        boolean Granted
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    RefreshToken {
        uuid Id
        uuid UserId
        text TokenHash
        timestamptz ExpiresAt
        timestamptz RevokedAt
        text RevokedReason
        text ReplacedByHash
        text Device
        text Ip
        timestamptz LastUsedAt
        timestamptz CreatedAt
        timestamptz UpdatedAt
    }
    PasswordResetToken {
        uuid Id
        uuid UserId
        text TokenHash
        timestamptz ExpiresAt
        timestamptz UsedAt
        timestamptz CreatedAt
        timestamptz UpdatedAt
    }
    LoginHistory {
        uuid Id
        uuid ClubId
        uuid UserId
        text Email
        boolean Success
        text Reason
        text Ip
        text UserAgent
        timestamptz CreatedAt
        timestamptz UpdatedAt
    }
    ClubSequence {
        uuid ClubId
        text Name
        bigint Value
    }
```

## Pessoas e portaria

```mermaid
erDiagram
    Member ||--o{ AccessLog : "ResponsibleMemberId"
    QrToken ||--o{ AccessLog : "QrTokenId"
    Member ||--o{ Dependent : "MemberId"
    Dependent ||--o{ DependentPermission : "DependentId"
    Charge ||--o{ Guest : "ChargeId"
    Member ||--o{ Guest : "MemberId"
    Reservation ||--o{ Guest : "ReservationId"
    Space ||--o{ Guest : "SpaceId"
    Member ||--o{ Presence : "ResponsibleMemberId"
    Member {
        uuid Id
        integer Number
        text FullName
        text Cpf
        text Rg
        date BirthDate
        text Gender
        text MaritalStatus
        text Email
        text Phone
        text ZipCode
        text Street
        text AddressNumber
        text Complement
        etc mais_12_colunas
    }
    Dependent {
        uuid Id
        uuid MemberId
        text FullName
        text Cpf
        text Rg
        date BirthDate
        text Gender
        text Relationship
        uuid PhotoFileId
        text Email
        text Phone
        varchar Status
        text StatusReason
        boolean IsDeleted
        etc mais_4_colunas
    }
    DependentPermission {
        uuid Id
        uuid DependentId
        text Code
        boolean AllowedByAdmin
        boolean EnabledByMember
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    Employee {
        uuid Id
        text FullName
        text Cpf
        text Phone
        text Email
        text Position
        text Department
        date HiredAt
        varchar Status
        boolean IsDeleted
        timestamptz DeletedAt
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    DigitalCard {
        uuid Id
        varchar PersonType
        uuid PersonId
        text CardNumber
        boolean IsActive
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    QrToken {
        uuid Id
        varchar PersonType
        uuid PersonId
        varchar Operation
        text TokenHash
        text DisplayCode
        timestamptz ExpiresAt
        timestamptz ConsumedAt
        uuid ConsumedByUserId
        uuid IssuedToUserId
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    AccessLog {
        uuid Id
        varchar PersonType
        uuid PersonId
        text PersonName
        uuid ResponsibleMemberId
        varchar Operation
        varchar Result
        text DenialReason
        uuid StaffUserId
        text Origin
        uuid QrTokenId
        timestamptz OccurredAt
        timestamptz CreatedAt
        timestamptz UpdatedAt
        etc mais_1_colunas
    }
    Presence {
        uuid Id
        varchar PersonType
        uuid PersonId
        text PersonName
        uuid ResponsibleMemberId
        timestamptz EnteredAt
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    Guest {
        uuid Id
        uuid MemberId
        text FullName
        text Document
        text Phone
        date VisitDate
        uuid SpaceId
        uuid ReservationId
        numeric FeeAmount
        uuid ChargeId
        varchar Authorization
        uuid AuthorizedByUserId
        text AuthorizationNote
        uuid RegisteredByUserId
        etc mais_6_colunas
    }
```

## Financeiro

```mermaid
erDiagram
    Guest ||--o{ Charge : "GuestId"
    Member ||--o{ Charge : "MemberId"
    Occurrence ||--o{ Charge : "OccurrenceId"
    Reservation ||--o{ Charge : "ReservationId"
    Charge ||--o{ ChargeAdjustment : "ChargeId"
    MaintenanceOrder ||--o{ FinancialEntry : "MaintenanceOrderId"
    Member ||--o{ FinancialRelease : "MemberId"
    Charge ||--o{ Payment : "ChargeId"
    Member ||--o{ Payment : "MemberId"
    Charge {
        uuid Id
        uuid MemberId
        varchar Type
        text Description
        text Competence
        date DueDate
        date DelinquentFrom
        numeric BaseFee
        integer DependentsCount
        numeric DependentsAmount
        jsonb CompositionSnapshot
        numeric OriginalAmount
        numeric Additions
        numeric Discounts
        etc mais_16_colunas
    }
    ChargeAdjustment {
        uuid Id
        uuid ChargeId
        varchar Kind
        numeric Amount
        text Reason
        uuid UserId
        boolean IsAutomatic
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    Payment {
        uuid Id
        uuid ChargeId
        uuid MemberId
        numeric Amount
        varchar Method
        varchar Channel
        varchar Status
        timestamptz PaidAt
        uuid RegisteredByUserId
        text IdempotencyKey
        text ReceiptNumber
        text GatewayProvider
        text GatewayReference
        text GatewayPayload
        etc mais_8_colunas
    }
    FinancialEntry {
        uuid Id
        varchar Kind
        text Category
        text Description
        text Counterparty
        numeric Amount
        date DueDate
        date PaidOn
        varchar Status
        text Notes
        text CancelReason
        uuid MaintenanceOrderId
        uuid ReceiptFileId
        uuid CreatedByUserId
        etc mais_3_colunas
    }
    MonthlyClosing {
        uuid Id
        text Competence
        numeric ChargesIssued
        numeric PaymentsReceived
        numeric OtherRevenue
        numeric Expenses
        numeric OpenBalance
        text Notes
        uuid ClosedByUserId
        timestamptz ClosedAt
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    FinancialRelease {
        uuid Id
        uuid MemberId
        text Reason
        date ValidUntil
        uuid GrantedByUserId
        timestamptz RevokedAt
        uuid RevokedByUserId
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
```

## Espaços e reservas

```mermaid
erDiagram
    Member ||--o{ Inspection : "MemberId"
    Reservation ||--o{ Inspection : "ReservationId"
    Space ||--o{ Inspection : "SpaceId"
    Inspection ||--o{ InspectionItem : "InspectionId"
    Member ||--o{ KeyHandover : "MemberId"
    Reservation ||--o{ KeyHandover : "ReservationId"
    Space ||--o{ KeyHandover : "SpaceId"
    Charge ||--o{ Occurrence : "ChargeId"
    Inspection ||--o{ Occurrence : "InspectionId"
    Member ||--o{ Occurrence : "MemberId"
    Reservation ||--o{ Occurrence : "ReservationId"
    Space ||--o{ Occurrence : "SpaceId"
    Charge ||--o{ Reservation : "PenaltyChargeId"
    Dependent ||--o{ Reservation : "RequestedByDependentId"
    Member ||--o{ Reservation : "MemberId"
    Space ||--o{ Reservation : "SpaceId"
    Reservation ||--o{ ReservationSlot : "ReservationId"
    Space ||--o{ ReservationSlot : "SpaceId"
    MaintenanceOrder ||--o{ SpaceBlock : "MaintenanceOrderId"
    Space ||--o{ SpaceBlock : "SpaceId"
    Space ||--o{ SpaceSchedule : "SpaceId"
    Space {
        uuid Id
        text Name
        varchar Type
        text Subtype
        text Description
        integer Capacity
        boolean IsReservable
        varchar BookingMode
        integer SlotMinutes
        integer FixedDays
        numeric HourlyPrice
        numeric PeriodPrice
        numeric CleaningFee
        varchar ChargeMode
        etc mais_10_colunas
    }
    SpaceSchedule {
        uuid Id
        uuid SpaceId
        varchar DayOfWeek
        integer OpenMinute
        integer CloseMinute
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    SpaceBlock {
        uuid Id
        uuid SpaceId
        timestamptz StartAt
        timestamptz EndAt
        text Reason
        varchar Kind
        uuid MaintenanceOrderId
        uuid CreatedByUserId
        boolean IsCancelled
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    Holiday {
        uuid Id
        date Date
        text Name
        varchar Scope
        boolean IsClosed
        integer OpenMinute
        integer CloseMinute
        text Notes
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    Reservation {
        uuid Id
        text Code
        uuid SpaceId
        varchar Kind
        text Title
        uuid MemberId
        uuid RequestedByUserId
        uuid RequestedByDependentId
        timestamptz StartAt
        timestamptz EndAt
        integer PeopleCount
        varchar Status
        numeric RentalAmount
        numeric CleaningFee
        etc mais_17_colunas
    }
    ReservationSlot {
        uuid Id
        uuid ReservationId
        uuid SpaceId
        timestamptz StartAt
        timestamptz EndAt
        boolean IsActive
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    KeyHandover {
        uuid Id
        uuid ReservationId
        uuid SpaceId
        uuid MemberId
        text ReceiverName
        uuid DeliveredByUserId
        timestamptz PickedUpAt
        timestamptz ExpectedReturnAt
        text ConditionOnDelivery
        timestamptz ReturnedAt
        uuid ReceivedByUserId
        text ConditionOnReturn
        text Pendencies
        boolean PendenciesResolved
        etc mais_3_colunas
    }
    Inspection {
        uuid Id
        uuid SpaceId
        uuid ReservationId
        uuid MemberId
        varchar Moment
        uuid InspectorUserId
        timestamptz PerformedAt
        text Notes
        boolean HasDamage
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    InspectionItem {
        uuid Id
        uuid InspectionId
        varchar Category
        text Description
        boolean IsOk
        boolean IsDamage
        text Notes
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    Occurrence {
        uuid Id
        text Code
        varchar Kind
        text Description
        uuid SpaceId
        uuid ReservationId
        uuid MemberId
        uuid InspectionId
        uuid ReportedByUserId
        timestamptz OccurredAt
        varchar Status
        text Resolution
        numeric ProposedChargeAmount
        varchar ChargeDecision
        etc mais_7_colunas
    }
```

## Comunicação

```mermaid
erDiagram
    ClubEvent ||--o{ Announcement : "EventId"
    Announcement ||--o{ AnnouncementRead : "AnnouncementId"
    Announcement ||--o{ AnnouncementTarget : "AnnouncementId"
    Assembly ||--o{ AssemblyAttendance : "AssemblyId"
    Member ||--o{ AssemblyAttendance : "MemberId"
    Space ||--o{ ClubEvent : "SpaceId"
    ClubEvent ||--o{ EventRegistration : "EventId"
    Assembly ||--o{ Manifestation : "AssemblyId"
    ManifestationType ||--o{ Manifestation : "TypeId"
    Member ||--o{ Manifestation : "MemberId"
    Manifestation ||--o{ ManifestationUpdate : "ManifestationId"
    Assembly ||--o{ Poll : "AssemblyId"
    Poll ||--o{ PollOption : "PollId"
    Member ||--o{ Vote : "MemberId"
    Poll ||--o{ Vote : "PollId"
    PollOption ||--o{ Vote : "PollOptionId"
    ClubEvent {
        uuid Id
        text Title
        text Description
        uuid ImageFileId
        timestamptz StartAt
        timestamptz EndAt
        text Location
        uuid SpaceId
        integer Capacity
        varchar Audience
        boolean AllowMembers
        boolean AllowDependents
        boolean AllowStaff
        boolean RegistrationRequired
        etc mais_8_colunas
    }
    EventRegistration {
        uuid Id
        uuid EventId
        uuid UserId
        text PersonName
        varchar PersonKind
        varchar Status
        boolean Attended
        timestamptz CancelledAt
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    Announcement {
        uuid Id
        text Title
        text Body
        varchar Category
        varchar Priority
        boolean AudienceMembers
        boolean AudienceDependents
        boolean AudienceStaff
        boolean IsSpecific
        uuid ImageFileId
        uuid EventId
        timestamptz PublishedAt
        timestamptz ExpiresAt
        uuid AuthorUserId
        etc mais_5_colunas
    }
    AnnouncementTarget {
        uuid Id
        uuid AnnouncementId
        uuid UserId
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    AnnouncementRead {
        uuid Id
        uuid AnnouncementId
        uuid UserId
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    Notification {
        uuid Id
        uuid UserId
        varchar Category
        text Type
        text Title
        text Body
        text EntityType
        uuid EntityId
        boolean IsMandatory
        timestamptz ReadAt
        varchar PushStatus
        timestamptz PushSentAt
        varchar EmailStatus
        timestamptz EmailSentAt
        etc mais_5_colunas
    }
    DeviceToken {
        uuid Id
        uuid UserId
        text Token
        text Platform
        timestamptz LastSeenAt
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    NotificationPreference {
        uuid Id
        uuid UserId
        varchar Category
        boolean InAppEnabled
        boolean PushEnabled
        boolean EmailEnabled
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    ManifestationType {
        uuid Id
        text Name
        boolean IsSuggestion
        boolean IsActive
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    Manifestation {
        uuid Id
        text Protocol
        uuid TypeId
        text Description
        uuid PhotoFileId
        uuid UserId
        text AuthorName
        varchar AuthorKind
        uuid MemberId
        varchar Status
        text Classification
        uuid AssemblyId
        timestamptz ClosedAt
        timestamptz CreatedAt
        etc mais_2_colunas
    }
    ManifestationUpdate {
        uuid Id
        uuid ManifestationId
        varchar Status
        text Message
        boolean IsVisibleToAuthor
        uuid UserId
        text UserName
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    Assembly {
        uuid Id
        text Title
        timestamptz ScheduledAt
        text Location
        text Agenda
        varchar Status
        text Minutes
        uuid MinutesFileId
        uuid CreatedByUserId
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    AssemblyAttendance {
        uuid Id
        uuid AssemblyId
        uuid MemberId
        uuid RegisteredByUserId
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    Poll {
        uuid Id
        uuid AssemblyId
        text Title
        text Description
        timestamptz OpensAt
        timestamptz ClosesAt
        varchar Status
        varchar Criterion
        boolean ResultsPublished
        uuid CreatedByUserId
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    PollOption {
        uuid Id
        uuid PollId
        text Text
        integer SortOrder
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    Vote {
        uuid Id
        uuid PollId
        uuid PollOptionId
        uuid MemberId
        uuid UserId
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
```

## Operação e auditoria

```mermaid
erDiagram
    Employee ||--o{ Asset : "ResponsibleEmployeeId"
    Space ||--o{ Asset : "SpaceId"
    Asset ||--o{ MaintenanceOrder : "AssetId"
    Employee ||--o{ MaintenanceOrder : "ResponsibleEmployeeId"
    Space ||--o{ MaintenanceOrder : "SpaceId"
    Supplier ||--o{ StockItem : "SupplierId"
    StockItem ||--o{ StockMovement : "StockItemId"
    Supplier ||--o{ StockMovement : "SupplierId"
    Member ||--o{ StoredFile : "OwnerMemberId"
    MaintenanceOrder {
        uuid Id
        text Code
        text Title
        text Description
        varchar Kind
        uuid SpaceId
        uuid AssetId
        uuid ResponsibleEmployeeId
        text ResponsibleName
        timestamptz OpenedAt
        date DueDate
        varchar Status
        numeric EstimatedCost
        numeric ActualCost
        etc mais_5_colunas
    }
    Asset {
        uuid Id
        text Code
        text Name
        text Category
        text Description
        text Location
        uuid SpaceId
        varchar Condition
        date AcquiredOn
        numeric Value
        uuid ResponsibleEmployeeId
        boolean IsDeleted
        timestamptz DeletedAt
        timestamptz CreatedAt
        etc mais_2_colunas
    }
    Supplier {
        uuid Id
        text Name
        text Document
        text Phone
        text Email
        text Notes
        boolean IsDeleted
        timestamptz DeletedAt
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    StockItem {
        uuid Id
        text Name
        text Category
        text Unit
        numeric Quantity
        numeric MinQuantity
        uuid SupplierId
        boolean IsDeleted
        timestamptz DeletedAt
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    StockMovement {
        uuid Id
        uuid StockItemId
        varchar Kind
        numeric Quantity
        numeric BalanceAfter
        numeric UnitCost
        text Reason
        uuid SupplierId
        uuid UserId
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    StoredFile {
        uuid Id
        text FileName
        text ContentType
        bigint Size
        text StoragePath
        varchar Scope
        uuid UploadedByUserId
        uuid OwnerMemberId
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    Attachment {
        uuid Id
        text OwnerType
        uuid OwnerId
        uuid FileId
        text Title
        text Category
        uuid UploadedByUserId
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
    AuditLog {
        uuid Id
        uuid ClubId
        uuid UserId
        text UserName
        text Operation
        text EntityType
        text EntityId
        jsonb BeforeJson
        jsonb AfterJson
        text Origin
        text Justification
        timestamptz CreatedAt
        timestamptz UpdatedAt
    }
    StatusHistory {
        uuid Id
        text EntityType
        uuid EntityId
        text FromStatus
        text ToStatus
        text Note
        uuid UserId
        text UserName
        timestamptz CreatedAt
        timestamptz UpdatedAt
        uuid ClubId
    }
```
