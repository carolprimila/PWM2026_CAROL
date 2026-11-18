# Dicionário de dados

Gerado a partir do esquema real. 63 tabelas.

## Plataforma e acesso

### Club

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| Name | text | não |
| LegalName | text | sim |
| Cnpj | text | sim |
| Slug | text | não |
| Email | text | sim |
| Phone | text | sim |
| ZipCode | text | sim |
| Street | text | sim |
| Number | text | sim |
| Complement | text | sim |
| District | text | sim |
| City | text | sim |
| State | text | sim |
| LogoFileId | uuid | sim |
| About | text | sim |
| TimeZoneId | text | não |
| IsActive | boolean | não |
| IsDeleted | boolean | não |
| DeletedAt | timestamp with time zone | sim |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |

Chaves estrangeiras: (LogoFileId) → StoredFile

Unicidade: `IX_Club_Cnpj` (filtrado), `IX_Club_Slug`

### ClubSettings

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| MonthlyBaseFee | numeric | sim |
| DueDay | integer | sim |
| GraceDays | integer | sim |
| LateFinePercent | numeric | sim |
| DailyInterestPercent | numeric | sim |
| DependentPricingMode | character varying(40) | não |
| DependentFixedAmount | numeric | sim |
| BlockAccessWhenDelinquent | boolean | sim |
| BlockReservationsWhenDelinquent | boolean | sim |
| DelinquentReservationPolicy | character varying(40) | sim |
| AllowAdvancePayment | boolean | sim |
| FamilyCompositionChangeRule | text | sim |
| AutoGenerateMonthlyCharges | boolean | não |
| DueReminderDays | integer | não |
| MaxDependentsPerMember | integer | não |
| MaxGuestsPerMember | integer | não |
| MemberCanToggleDependentPermissions | boolean | não |
| MaxAdvanceDays | integer | sim |
| MaxConcurrentReservations | integer | sim |
| FreeCancellationHours | integer | sim |
| LateCancellationFee | numeric | sim |
| NoShowFee | numeric | sim |
| RequireAdvancePayment | boolean | sim |
| ReservationApprovalMode | character varying(40) | sim |
| GuestFee | numeric | sim |
| GuestAdvanceRegistrationRequired | boolean | sim |
| GuestDocumentRequired | boolean | sim |
| GuestEntryWithoutMemberAllowed | boolean | sim |
| QrTokenTtlSeconds | integer | não |
| HolidayRules | text | sim |
| MinorsRules | text | sim |
| MinorsAccompanimentRules | text | sim |
| PoolPolicy | text | sim |
| EventProcedures | text | sim |
| SpaceUsagePolicy | text | sim |
| PrivacyPolicy | text | sim |
| PrivacyPolicyVersion | text | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club

Unicidade: `IX_ClubSettings_ClubId`

### ClubOpeningHour

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| DayOfWeek | character varying(40) | não |
| IsClosed | boolean | não |
| OpenMinute | integer | não |
| CloseMinute | integer | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club

Unicidade: `IX_ClubOpeningHour_ClubId_DayOfWeek`

### DependentPricingRule

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| FromCount | integer | não |
| ToCount | integer | não |
| Amount | numeric | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club

### User

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| ClubId | uuid | sim |
| Name | text | não |
| Email | text | não |
| PasswordHash | text | não |
| Kind | character varying(40) | não |
| MemberId | uuid | sim |
| DependentId | uuid | sim |
| EmployeeId | uuid | sim |
| IsBlocked | boolean | não |
| BlockReason | text | sim |
| MustChangePassword | boolean | não |
| FailedLoginCount | integer | não |
| LockoutUntil | timestamp with time zone | sim |
| LastLoginAt | timestamp with time zone | sim |
| PrivacyAcceptedAt | timestamp with time zone | sim |
| PrivacyAcceptedVersion | text | sim |
| IsDeleted | boolean | não |
| DeletedAt | timestamp with time zone | sim |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |

Chaves estrangeiras: (ClubId) → Club; (DependentId) → Dependent; (EmployeeId) → Employee; (MemberId) → Member

Unicidade: `IX_User_Email` (filtrado)

### Role

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| Name | text | não |
| Description | text | sim |
| IsSystem | boolean | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club

Unicidade: `AK_Role_ClubId_Id`, `IX_Role_ClubId_Name`

### RolePermission

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| RoleId | uuid | não |
| Permission | text | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (ClubId,RoleId) → Role

Unicidade: `IX_RolePermission_RoleId_Permission`

### UserRole

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| UserId | uuid | não |
| RoleId | uuid | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (ClubId,RoleId) → Role; (UserId) → User

Unicidade: `IX_UserRole_UserId_RoleId`

### UserPermission

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| UserId | uuid | não |
| Permission | text | não |
| Granted | boolean | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (UserId) → User

Unicidade: `IX_UserPermission_UserId_Permission`

### RefreshToken

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| UserId | uuid | não |
| TokenHash | text | não |
| ExpiresAt | timestamp with time zone | não |
| RevokedAt | timestamp with time zone | sim |
| RevokedReason | text | sim |
| ReplacedByHash | text | sim |
| Device | text | sim |
| Ip | text | sim |
| LastUsedAt | timestamp with time zone | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |

Chaves estrangeiras: (UserId) → User

Unicidade: `IX_RefreshToken_TokenHash`

### PasswordResetToken

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| UserId | uuid | não |
| TokenHash | text | não |
| ExpiresAt | timestamp with time zone | não |
| UsedAt | timestamp with time zone | sim |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |

Chaves estrangeiras: (UserId) → User

Unicidade: `IX_PasswordResetToken_TokenHash`

### LoginHistory

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| ClubId | uuid | sim |
| UserId | uuid | sim |
| Email | text | não |
| Success | boolean | não |
| Reason | text | sim |
| Ip | text | sim |
| UserAgent | text | sim |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |

Chaves estrangeiras: (ClubId) → Club; (UserId) → User

### ClubSequence

| Coluna | Tipo | Nulo |
|---|---|---|
| ClubId | uuid | não |
| Name | text | não |
| Value | bigint | não |

Chaves estrangeiras: (ClubId) → Club

## Pessoas e portaria

### Member

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| Number | integer | não |
| FullName | text | não |
| Cpf | text | não |
| Rg | text | sim |
| BirthDate | date | sim |
| Gender | text | sim |
| MaritalStatus | text | sim |
| Email | text | sim |
| Phone | text | sim |
| ZipCode | text | sim |
| Street | text | sim |
| AddressNumber | text | sim |
| Complement | text | sim |
| District | text | sim |
| City | text | sim |
| State | text | sim |
| PhotoFileId | uuid | sim |
| JoinedAt | date | sim |
| Status | character varying(40) | não |
| Notes | text | sim |
| IsDeleted | boolean | não |
| DeletedAt | timestamp with time zone | sim |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (ClubId,PhotoFileId) → StoredFile

Unicidade: `AK_Member_ClubId_Id`, `IX_Member_ClubId_Cpf` (filtrado), `IX_Member_ClubId_Number`

### Dependent

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| MemberId | uuid | não |
| FullName | text | não |
| Cpf | text | sim |
| Rg | text | sim |
| BirthDate | date | sim |
| Gender | text | sim |
| Relationship | text | não |
| PhotoFileId | uuid | sim |
| Email | text | sim |
| Phone | text | sim |
| Status | character varying(40) | não |
| StatusReason | text | sim |
| IsDeleted | boolean | não |
| DeletedAt | timestamp with time zone | sim |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (ClubId,MemberId) → Member; (ClubId,PhotoFileId) → StoredFile

Unicidade: `AK_Dependent_ClubId_Id`, `IX_Dependent_ClubId_Cpf` (filtrado)

### DependentPermission

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| DependentId | uuid | não |
| Code | text | não |
| AllowedByAdmin | boolean | não |
| EnabledByMember | boolean | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (ClubId,DependentId) → Dependent

Unicidade: `IX_DependentPermission_DependentId_Code`

### Employee

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| FullName | text | não |
| Cpf | text | não |
| Phone | text | sim |
| Email | text | sim |
| Position | text | sim |
| Department | text | sim |
| HiredAt | date | sim |
| Status | character varying(40) | não |
| IsDeleted | boolean | não |
| DeletedAt | timestamp with time zone | sim |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club

Unicidade: `AK_Employee_ClubId_Id`, `IX_Employee_ClubId_Cpf` (filtrado)

### DigitalCard

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| PersonType | character varying(40) | não |
| PersonId | uuid | não |
| CardNumber | text | não |
| IsActive | boolean | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club

Unicidade: `IX_DigitalCard_ClubId_CardNumber`, `IX_DigitalCard_ClubId_PersonType_PersonId`

### QrToken

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| PersonType | character varying(40) | não |
| PersonId | uuid | não |
| Operation | character varying(40) | não |
| TokenHash | text | não |
| DisplayCode | text | não |
| ExpiresAt | timestamp with time zone | não |
| ConsumedAt | timestamp with time zone | sim |
| ConsumedByUserId | uuid | sim |
| IssuedToUserId | uuid | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (ConsumedByUserId) → User; (IssuedToUserId) → User

Unicidade: `AK_QrToken_ClubId_Id`, `IX_QrToken_TokenHash`

### AccessLog

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| PersonType | character varying(40) | sim |
| PersonId | uuid | sim |
| PersonName | text | sim |
| ResponsibleMemberId | uuid | sim |
| Operation | character varying(40) | não |
| Result | character varying(40) | não |
| DenialReason | text | sim |
| StaffUserId | uuid | sim |
| Origin | text | sim |
| QrTokenId | uuid | sim |
| OccurredAt | timestamp with time zone | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (ClubId,ResponsibleMemberId) → Member; (ClubId,QrTokenId) → QrToken; (StaffUserId) → User

### Presence

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| PersonType | character varying(40) | não |
| PersonId | uuid | não |
| PersonName | text | não |
| ResponsibleMemberId | uuid | sim |
| EnteredAt | timestamp with time zone | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (ClubId,ResponsibleMemberId) → Member

Unicidade: `IX_Presence_ClubId_PersonType_PersonId`

### Guest

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| MemberId | uuid | não |
| FullName | text | não |
| Document | text | sim |
| Phone | text | sim |
| VisitDate | date | não |
| SpaceId | uuid | sim |
| ReservationId | uuid | sim |
| FeeAmount | numeric | não |
| ChargeId | uuid | sim |
| Authorization | character varying(40) | não |
| AuthorizedByUserId | uuid | sim |
| AuthorizationNote | text | sim |
| RegisteredByUserId | uuid | não |
| EnteredAt | timestamp with time zone | sim |
| ExitedAt | timestamp with time zone | sim |
| IsCancelled | boolean | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId,ChargeId) → Charge; (ClubId) → Club; (ClubId,MemberId) → Member; (ClubId,ReservationId) → Reservation; (ClubId,SpaceId) → Space; (AuthorizedByUserId) → User; (RegisteredByUserId) → User

Unicidade: `AK_Guest_ClubId_Id`

## Financeiro

### Charge

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| MemberId | uuid | não |
| Type | character varying(40) | não |
| Description | text | não |
| Competence | text | não |
| DueDate | date | não |
| DelinquentFrom | date | não |
| BaseFee | numeric | não |
| DependentsCount | integer | não |
| DependentsAmount | numeric | não |
| CompositionSnapshot | jsonb | sim |
| OriginalAmount | numeric | não |
| Additions | numeric | não |
| Discounts | numeric | não |
| FineAmount | numeric | não |
| InterestAmount | numeric | não |
| FeesAmount | numeric | não |
| PaidAmount | numeric | não |
| GraceDaysSnapshot | integer | não |
| LateFinePercentSnapshot | numeric | sim |
| DailyInterestPercentSnapshot | numeric | sim |
| Status | character varying(40) | não |
| CancelReason | text | sim |
| ReservationId | uuid | sim |
| GuestId | uuid | sim |
| OccurrenceId | uuid | sim |
| CreatedByUserId | uuid | sim |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (ClubId,GuestId) → Guest; (ClubId,MemberId) → Member; (ClubId,OccurrenceId) → Occurrence; (ClubId,ReservationId) → Reservation; (CreatedByUserId) → User

Unicidade: `AK_Charge_ClubId_Id`, `IX_Charge_ClubId_MemberId_Competence` (filtrado)

Restrições: `CK_Charge_Valores`: CHECK ((("OriginalAmount" >= (0)::numeric) AND ("Additions" >= (0)::numeric) AND ("Discounts" >= (0)::numeric) AND ("FineAmount" >= (0)::numeric) AND ("InterestAmount" >= (0)::numeric) AND ("FeesAmount" >= (0)::numeric) AND ("PaidAmount" >= (0)::numeric) AND ("PaidAmount" <= ((((("OriginalAmount" + "Additions") + "FineAmount") + "InterestAmount") + "FeesAmount") - "Discounts"))))

### ChargeAdjustment

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| ChargeId | uuid | não |
| Kind | character varying(40) | não |
| Amount | numeric | não |
| Reason | text | não |
| UserId | uuid | sim |
| IsAutomatic | boolean | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId,ChargeId) → Charge; (ClubId) → Club; (UserId) → User

### Payment

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| ChargeId | uuid | não |
| MemberId | uuid | não |
| Amount | numeric | não |
| Method | character varying(40) | não |
| Channel | character varying(40) | não |
| Status | character varying(40) | não |
| PaidAt | timestamp with time zone | sim |
| RegisteredByUserId | uuid | sim |
| IdempotencyKey | text | não |
| ReceiptNumber | text | não |
| GatewayProvider | text | sim |
| GatewayReference | text | sim |
| GatewayPayload | text | sim |
| ReceiptFileId | uuid | sim |
| Notes | text | sim |
| RefundedAt | timestamp with time zone | sim |
| RefundReason | text | sim |
| RefundedByUserId | uuid | sim |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId,ChargeId) → Charge; (ClubId) → Club; (ClubId,MemberId) → Member; (ClubId,ReceiptFileId) → StoredFile; (RegisteredByUserId) → User; (RefundedByUserId) → User

Unicidade: `IX_Payment_ClubId_IdempotencyKey`, `IX_Payment_ClubId_ReceiptNumber`

Restrições: `CK_Payment_Valor`: CHECK (("Amount" > (0)::numeric))

### FinancialEntry

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| Kind | character varying(40) | não |
| Category | text | não |
| Description | text | não |
| Counterparty | text | sim |
| Amount | numeric | não |
| DueDate | date | não |
| PaidOn | date | sim |
| Status | character varying(40) | não |
| Notes | text | sim |
| CancelReason | text | sim |
| MaintenanceOrderId | uuid | sim |
| ReceiptFileId | uuid | sim |
| CreatedByUserId | uuid | sim |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (ClubId,MaintenanceOrderId) → MaintenanceOrder; (ClubId,ReceiptFileId) → StoredFile; (CreatedByUserId) → User

Restrições: `CK_FinancialEntry_Valor`: CHECK (("Amount" > (0)::numeric))

### MonthlyClosing

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| Competence | text | não |
| ChargesIssued | numeric | não |
| PaymentsReceived | numeric | não |
| OtherRevenue | numeric | não |
| Expenses | numeric | não |
| OpenBalance | numeric | não |
| Notes | text | sim |
| ClosedByUserId | uuid | não |
| ClosedAt | timestamp with time zone | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (ClosedByUserId) → User

Unicidade: `IX_MonthlyClosing_ClubId_Competence`

### FinancialRelease

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| MemberId | uuid | não |
| Reason | text | não |
| ValidUntil | date | não |
| GrantedByUserId | uuid | não |
| RevokedAt | timestamp with time zone | sim |
| RevokedByUserId | uuid | sim |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (ClubId,MemberId) → Member; (GrantedByUserId) → User; (RevokedByUserId) → User

## Espaços e reservas

### Space

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| Name | text | não |
| Type | character varying(40) | não |
| Subtype | text | sim |
| Description | text | sim |
| Capacity | integer | sim |
| IsReservable | boolean | não |
| BookingMode | character varying(40) | não |
| SlotMinutes | integer | não |
| FixedDays | integer | não |
| HourlyPrice | numeric | sim |
| PeriodPrice | numeric | sim |
| CleaningFee | numeric | sim |
| ChargeMode | character varying(40) | sim |
| ApprovalMode | character varying(40) | sim |
| UsageRules | text | sim |
| ImageFileId | uuid | sim |
| IsActive | boolean | não |
| SortOrder | integer | não |
| IsDeleted | boolean | não |
| DeletedAt | timestamp with time zone | sim |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (ClubId,ImageFileId) → StoredFile

Unicidade: `AK_Space_ClubId_Id`

### SpaceSchedule

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| SpaceId | uuid | não |
| DayOfWeek | character varying(40) | não |
| OpenMinute | integer | não |
| CloseMinute | integer | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (ClubId,SpaceId) → Space

Unicidade: `IX_SpaceSchedule_SpaceId_DayOfWeek`

### SpaceBlock

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| SpaceId | uuid | sim |
| StartAt | timestamp with time zone | não |
| EndAt | timestamp with time zone | não |
| Reason | text | não |
| Kind | character varying(40) | não |
| MaintenanceOrderId | uuid | sim |
| CreatedByUserId | uuid | não |
| IsCancelled | boolean | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (ClubId,MaintenanceOrderId) → MaintenanceOrder; (ClubId,SpaceId) → Space; (CreatedByUserId) → User

Restrições: `CK_SpaceBlock_Periodo`: CHECK (("EndAt" > "StartAt"))

### Holiday

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| Date | date | não |
| Name | text | não |
| Scope | character varying(40) | não |
| IsClosed | boolean | não |
| OpenMinute | integer | sim |
| CloseMinute | integer | sim |
| Notes | text | sim |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club

### Reservation

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| Code | text | não |
| SpaceId | uuid | não |
| Kind | character varying(40) | não |
| Title | text | sim |
| MemberId | uuid | sim |
| RequestedByUserId | uuid | não |
| RequestedByDependentId | uuid | sim |
| StartAt | timestamp with time zone | não |
| EndAt | timestamp with time zone | não |
| PeopleCount | integer | não |
| Status | character varying(40) | não |
| RentalAmount | numeric | não |
| CleaningFee | numeric | não |
| TotalAmount | numeric | não |
| ChargeMode | character varying(40) | sim |
| ChargeId | uuid | sim |
| Notes | text | sim |
| DecisionByUserId | uuid | sim |
| DecisionAt | timestamp with time zone | sim |
| DecisionReason | text | sim |
| CancelledAt | timestamp with time zone | sim |
| CancelledByUserId | uuid | sim |
| CancellationReason | text | sim |
| Attendance | character varying(40) | não |
| AttendanceNote | text | sim |
| PenaltyChargeId | uuid | sim |
| ReminderSentAt | timestamp with time zone | sim |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId,PenaltyChargeId) → Charge; (ClubId,ChargeId) → Charge; (ClubId) → Club; (ClubId,RequestedByDependentId) → Dependent; (ClubId,MemberId) → Member; (ClubId,SpaceId) → Space; (CancelledByUserId) → User; (RequestedByUserId) → User; (DecisionByUserId) → User

Unicidade: `AK_Reservation_ClubId_Id`, `IX_Reservation_ClubId_Code`

Restrições: `CK_Reservation_Pessoas`: CHECK (("PeopleCount" >= 1))

### ReservationSlot

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| ReservationId | uuid | não |
| SpaceId | uuid | não |
| StartAt | timestamp with time zone | não |
| EndAt | timestamp with time zone | não |
| IsActive | boolean | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (ClubId,ReservationId) → Reservation; (ClubId,SpaceId) → Space

Restrições: `CK_ReservationSlot_Periodo`: CHECK (("EndAt" > "StartAt")); `EX_ReservationSlot_NoOverlap`: EXCLUDE USING gist ("SpaceId" WITH =, tstzrange("StartAt", "EndAt", '[)'::text) WITH &&) WHERE ("IsActive")

### KeyHandover

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| ReservationId | uuid | não |
| SpaceId | uuid | não |
| MemberId | uuid | sim |
| ReceiverName | text | não |
| DeliveredByUserId | uuid | não |
| PickedUpAt | timestamp with time zone | não |
| ExpectedReturnAt | timestamp with time zone | não |
| ConditionOnDelivery | text | sim |
| ReturnedAt | timestamp with time zone | sim |
| ReceivedByUserId | uuid | sim |
| ConditionOnReturn | text | sim |
| Pendencies | text | sim |
| PendenciesResolved | boolean | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (ClubId,MemberId) → Member; (ClubId,ReservationId) → Reservation; (ClubId,SpaceId) → Space; (DeliveredByUserId) → User; (ReceivedByUserId) → User

### Inspection

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| SpaceId | uuid | não |
| ReservationId | uuid | sim |
| MemberId | uuid | sim |
| Moment | character varying(40) | não |
| InspectorUserId | uuid | não |
| PerformedAt | timestamp with time zone | não |
| Notes | text | sim |
| HasDamage | boolean | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (ClubId,MemberId) → Member; (ClubId,ReservationId) → Reservation; (ClubId,SpaceId) → Space; (InspectorUserId) → User

Unicidade: `AK_Inspection_ClubId_Id`

### InspectionItem

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| InspectionId | uuid | não |
| Category | character varying(40) | não |
| Description | text | não |
| IsOk | boolean | não |
| IsDamage | boolean | não |
| Notes | text | sim |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (ClubId,InspectionId) → Inspection

### Occurrence

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| Code | text | não |
| Kind | character varying(40) | não |
| Description | text | não |
| SpaceId | uuid | sim |
| ReservationId | uuid | sim |
| MemberId | uuid | sim |
| InspectionId | uuid | sim |
| ReportedByUserId | uuid | não |
| OccurredAt | timestamp with time zone | não |
| Status | character varying(40) | não |
| Resolution | text | sim |
| ProposedChargeAmount | numeric | sim |
| ChargeDecision | character varying(40) | não |
| ChargeDecisionByUserId | uuid | sim |
| ChargeDecisionAt | timestamp with time zone | sim |
| ChargeDecisionReason | text | sim |
| ChargeId | uuid | sim |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId,ChargeId) → Charge; (ClubId) → Club; (ClubId,InspectionId) → Inspection; (ClubId,MemberId) → Member; (ClubId,ReservationId) → Reservation; (ClubId,SpaceId) → Space; (ReportedByUserId) → User; (ChargeDecisionByUserId) → User

Unicidade: `AK_Occurrence_ClubId_Id`, `IX_Occurrence_ClubId_Code`

## Comunicação

### ClubEvent

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| Title | text | não |
| Description | text | sim |
| ImageFileId | uuid | sim |
| StartAt | timestamp with time zone | não |
| EndAt | timestamp with time zone | não |
| Location | text | sim |
| SpaceId | uuid | sim |
| Capacity | integer | sim |
| Audience | character varying(40) | não |
| AllowMembers | boolean | não |
| AllowDependents | boolean | não |
| AllowStaff | boolean | não |
| RegistrationRequired | boolean | não |
| RegistrationDeadline | timestamp with time zone | sim |
| CancellationDeadline | timestamp with time zone | sim |
| Status | character varying(40) | não |
| CancelReason | text | sim |
| CreatedByUserId | uuid | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (ClubId,SpaceId) → Space; (ClubId,ImageFileId) → StoredFile; (CreatedByUserId) → User

Unicidade: `AK_ClubEvent_ClubId_Id`

### EventRegistration

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| EventId | uuid | não |
| UserId | uuid | não |
| PersonName | text | não |
| PersonKind | character varying(40) | não |
| Status | character varying(40) | não |
| Attended | boolean | sim |
| CancelledAt | timestamp with time zone | sim |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (ClubId,EventId) → ClubEvent; (UserId) → User

Unicidade: `IX_EventRegistration_EventId_UserId`

### Announcement

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| Title | text | não |
| Body | text | não |
| Category | character varying(40) | não |
| Priority | character varying(40) | não |
| AudienceMembers | boolean | não |
| AudienceDependents | boolean | não |
| AudienceStaff | boolean | não |
| IsSpecific | boolean | não |
| ImageFileId | uuid | sim |
| EventId | uuid | sim |
| PublishedAt | timestamp with time zone | sim |
| ExpiresAt | timestamp with time zone | sim |
| AuthorUserId | uuid | não |
| IsDeleted | boolean | não |
| DeletedAt | timestamp with time zone | sim |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (ClubId,EventId) → ClubEvent; (ClubId,ImageFileId) → StoredFile; (AuthorUserId) → User

Unicidade: `AK_Announcement_ClubId_Id`

### AnnouncementTarget

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| AnnouncementId | uuid | não |
| UserId | uuid | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId,AnnouncementId) → Announcement; (ClubId) → Club; (UserId) → User

Unicidade: `IX_AnnouncementTarget_AnnouncementId_UserId`

### AnnouncementRead

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| AnnouncementId | uuid | não |
| UserId | uuid | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId,AnnouncementId) → Announcement; (ClubId) → Club; (UserId) → User

Unicidade: `IX_AnnouncementRead_AnnouncementId_UserId`

### Notification

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| UserId | uuid | não |
| Category | character varying(40) | não |
| Type | text | não |
| Title | text | não |
| Body | text | não |
| EntityType | text | sim |
| EntityId | uuid | sim |
| IsMandatory | boolean | não |
| ReadAt | timestamp with time zone | sim |
| PushStatus | character varying(40) | não |
| PushSentAt | timestamp with time zone | sim |
| EmailStatus | character varying(40) | não |
| EmailSentAt | timestamp with time zone | sim |
| DeliveryError | text | sim |
| DispatchedAt | timestamp with time zone | sim |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (UserId) → User

### DeviceToken

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| UserId | uuid | não |
| Token | text | não |
| Platform | text | não |
| LastSeenAt | timestamp with time zone | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (UserId) → User

Unicidade: `IX_DeviceToken_Token`

### NotificationPreference

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| UserId | uuid | não |
| Category | character varying(40) | não |
| InAppEnabled | boolean | não |
| PushEnabled | boolean | não |
| EmailEnabled | boolean | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (UserId) → User

Unicidade: `IX_NotificationPreference_UserId_Category`

### ManifestationType

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| Name | text | não |
| IsSuggestion | boolean | não |
| IsActive | boolean | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club

Unicidade: `AK_ManifestationType_ClubId_Id`

### Manifestation

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| Protocol | text | não |
| TypeId | uuid | não |
| Description | text | não |
| PhotoFileId | uuid | sim |
| UserId | uuid | não |
| AuthorName | text | não |
| AuthorKind | character varying(40) | não |
| MemberId | uuid | sim |
| Status | character varying(40) | não |
| Classification | text | sim |
| AssemblyId | uuid | sim |
| ClosedAt | timestamp with time zone | sim |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId,AssemblyId) → Assembly; (ClubId) → Club; (ClubId,TypeId) → ManifestationType; (ClubId,MemberId) → Member; (ClubId,PhotoFileId) → StoredFile; (UserId) → User

Unicidade: `AK_Manifestation_ClubId_Id`, `IX_Manifestation_ClubId_Protocol`

### ManifestationUpdate

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| ManifestationId | uuid | não |
| Status | character varying(40) | não |
| Message | text | sim |
| IsVisibleToAuthor | boolean | não |
| UserId | uuid | não |
| UserName | text | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (ClubId,ManifestationId) → Manifestation; (UserId) → User

### Assembly

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| Title | text | não |
| ScheduledAt | timestamp with time zone | não |
| Location | text | sim |
| Agenda | text | sim |
| Status | character varying(40) | não |
| Minutes | text | sim |
| MinutesFileId | uuid | sim |
| CreatedByUserId | uuid | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (ClubId,MinutesFileId) → StoredFile; (CreatedByUserId) → User

Unicidade: `AK_Assembly_ClubId_Id`

### AssemblyAttendance

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| AssemblyId | uuid | não |
| MemberId | uuid | não |
| RegisteredByUserId | uuid | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId,AssemblyId) → Assembly; (ClubId) → Club; (ClubId,MemberId) → Member; (RegisteredByUserId) → User

Unicidade: `IX_AssemblyAttendance_AssemblyId_MemberId`

### Poll

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| AssemblyId | uuid | sim |
| Title | text | não |
| Description | text | sim |
| OpensAt | timestamp with time zone | sim |
| ClosesAt | timestamp with time zone | sim |
| Status | character varying(40) | não |
| Criterion | character varying(40) | não |
| ResultsPublished | boolean | não |
| CreatedByUserId | uuid | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId,AssemblyId) → Assembly; (ClubId) → Club; (CreatedByUserId) → User

Unicidade: `AK_Poll_ClubId_Id`

### PollOption

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| PollId | uuid | não |
| Text | text | não |
| SortOrder | integer | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (ClubId,PollId) → Poll

Unicidade: `AK_PollOption_ClubId_Id`

### Vote

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| PollId | uuid | não |
| PollOptionId | uuid | não |
| MemberId | uuid | não |
| UserId | uuid | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (ClubId,MemberId) → Member; (ClubId,PollId) → Poll; (ClubId,PollOptionId) → PollOption; (UserId) → User

Unicidade: `IX_Vote_PollId_MemberId`

## Operação e auditoria

### MaintenanceOrder

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| Code | text | não |
| Title | text | não |
| Description | text | sim |
| Kind | character varying(40) | não |
| SpaceId | uuid | sim |
| AssetId | uuid | sim |
| ResponsibleEmployeeId | uuid | sim |
| ResponsibleName | text | sim |
| OpenedAt | timestamp with time zone | não |
| DueDate | date | sim |
| Status | character varying(40) | não |
| EstimatedCost | numeric | sim |
| ActualCost | numeric | sim |
| CompletedAt | timestamp with time zone | sim |
| OpenedByUserId | uuid | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId,AssetId) → Asset; (ClubId) → Club; (ClubId,ResponsibleEmployeeId) → Employee; (ClubId,SpaceId) → Space; (OpenedByUserId) → User

Unicidade: `AK_MaintenanceOrder_ClubId_Id`, `IX_MaintenanceOrder_ClubId_Code`

### Asset

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| Code | text | não |
| Name | text | não |
| Category | text | sim |
| Description | text | sim |
| Location | text | sim |
| SpaceId | uuid | sim |
| Condition | character varying(40) | não |
| AcquiredOn | date | sim |
| Value | numeric | sim |
| ResponsibleEmployeeId | uuid | sim |
| IsDeleted | boolean | não |
| DeletedAt | timestamp with time zone | sim |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (ClubId,ResponsibleEmployeeId) → Employee; (ClubId,SpaceId) → Space

Unicidade: `AK_Asset_ClubId_Id`, `IX_Asset_ClubId_Code` (filtrado)

### Supplier

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| Name | text | não |
| Document | text | sim |
| Phone | text | sim |
| Email | text | sim |
| Notes | text | sim |
| IsDeleted | boolean | não |
| DeletedAt | timestamp with time zone | sim |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club

Unicidade: `AK_Supplier_ClubId_Id`

### StockItem

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| Name | text | não |
| Category | text | sim |
| Unit | text | não |
| Quantity | numeric | não |
| MinQuantity | numeric | não |
| SupplierId | uuid | sim |
| IsDeleted | boolean | não |
| DeletedAt | timestamp with time zone | sim |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (ClubId,SupplierId) → Supplier

Unicidade: `AK_StockItem_ClubId_Id`

Restrições: `CK_StockItem_Saldo`: CHECK (("Quantity" >= (0)::numeric))

### StockMovement

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| StockItemId | uuid | não |
| Kind | character varying(40) | não |
| Quantity | numeric | não |
| BalanceAfter | numeric | não |
| UnitCost | numeric | sim |
| Reason | text | sim |
| SupplierId | uuid | sim |
| UserId | uuid | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (ClubId,StockItemId) → StockItem; (ClubId,SupplierId) → Supplier; (UserId) → User

### StoredFile

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| FileName | text | não |
| ContentType | text | não |
| Size | bigint | não |
| StoragePath | text | não |
| Scope | character varying(40) | não |
| UploadedByUserId | uuid | não |
| OwnerMemberId | uuid | sim |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (ClubId,OwnerMemberId) → Member; (UploadedByUserId) → User

Unicidade: `AK_StoredFile_ClubId_Id`

### Attachment

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| OwnerType | text | não |
| OwnerId | uuid | não |
| FileId | uuid | não |
| Title | text | sim |
| Category | text | sim |
| UploadedByUserId | uuid | não |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (ClubId,FileId) → StoredFile; (UploadedByUserId) → User

### AuditLog

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| ClubId | uuid | sim |
| UserId | uuid | sim |
| UserName | text | sim |
| Operation | text | não |
| EntityType | text | não |
| EntityId | text | sim |
| BeforeJson | jsonb | sim |
| AfterJson | jsonb | sim |
| Origin | text | sim |
| Justification | text | sim |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |

Chaves estrangeiras: (ClubId) → Club; (UserId) → User

### StatusHistory

| Coluna | Tipo | Nulo |
|---|---|---|
| Id | uuid | não |
| EntityType | text | não |
| EntityId | uuid | não |
| FromStatus | text | sim |
| ToStatus | text | não |
| Note | text | sim |
| UserId | uuid | sim |
| UserName | text | sim |
| CreatedAt | timestamp with time zone | não |
| UpdatedAt | timestamp with time zone | sim |
| ClubId | uuid | não |

Chaves estrangeiras: (ClubId) → Club; (UserId) → User
