namespace Clubes.Domain;

public enum UserKind { SuperAdmin, ClubAdmin, Staff, Member, Dependent }
public enum PersonType { Member, Dependent, Guest }

public enum MemberStatus { PendingApproval, Active, Suspended, Cancelled }
public enum DependentStatus { PendingApproval, Active, Suspended, Rejected, Removed }
public enum EmployeeStatus { Active, Inactive }

public enum AccessOperation { Entry, Exit }
public enum AccessResult { Authorized, Denied }

public enum DependentPricingMode { NotConfigured, None, FixedPerDependent, Progressive, Tiers }
public enum DelinquentReservationPolicy { Keep, Suspend, Cancel }
public enum ChargeType { Monthly, Fine, Fee, Reservation, Guest, Damage, Penalty, Other }
public enum ChargeStatus { Open, Paid, Cancelled }
public enum AdjustmentKind { Discount, Addition, Fine, Interest, Fee }
public enum PaymentMethod { Cash, Pix, CreditCard, DebitCard, BankSlip, Transfer }
public enum PaymentChannel { App, FrontDesk }
public enum PaymentStatus { Pending, Confirmed, Failed, Refunded }
public enum FinancialEntryKind { Revenue, Expense }
public enum FinancialEntryStatus { Pending, Paid, Cancelled }

public enum SpaceType { Court, Kiosk, PartyHall, Pool, Other }
public enum BookingMode { Hourly, MultiDay }
public enum SpaceChargeMode { NextMonthlyBill, Advance }
public enum ApprovalMode { Manual, Automatic }
public enum ReservationKind { Member, Administrative, InternalEvent }
public enum ReservationStatus { Pending, Approved, Rejected, Cancelled, Suspended, Completed }
public enum AttendanceStatus { Unknown, Attended, NoShow }
public enum BlockKind { Administrative, Maintenance, Holiday }
public enum HolidayScope { National, State, Municipal, Commemorative, ExceptionalClosure, SpecialHours }

public enum GuestAuthorization { Pending, Authorized, Denied }

public enum EventAudience { Public, MembersOnly, Specific }
public enum EventStatus { Scheduled, Cancelled, Finished }
public enum RegistrationStatus { Registered, Cancelled }

public enum AnnouncementCategory { Communication, Notice, News, Important, Operation, Event, Financial }
public enum Priority { Normal, High, Urgent }
public enum NotificationCategory { General, Finance, Reservation, Dependent, Support, Assembly, Event }
public enum DeliveryStatus { NotRequested, Pending, Sent, Failed, NoDestination }

public enum ManifestationStatus { Received, UnderReview, InProgress, Accepted, ForwardedToAssembly, Resolved, Refused }

public enum AssemblyStatus { Scheduled, InProgress, Finished, Cancelled }
public enum PollStatus { Draft, Open, Closed }
public enum PollCriterion { ActiveMembers, NonDelinquentMembers, PresentAtAssembly }

public enum AssetCondition { New, Good, Regular, Bad, Unusable }
public enum StockMovementKind { In, Out, Adjustment }
public enum MaintenanceKind { Preventive, Corrective }
public enum MaintenanceStatus { Requested, UnderReview, Approved, InProgress, Completed, Cancelled }
public enum InspectionMoment { Before, After }
public enum InspectionCategory { Conservation, Cleaning, Equipment, Furniture, Utensils }
public enum OccurrenceKind { Damage, Gate, Other }
public enum OccurrenceStatus { Open, UnderReview, Resolved, Dismissed }
public enum ChargeDecision { None, Pending, Approved, Rejected }
/// <summary>ClubPublic: qualquer usuário do clube. Staff: equipe e o titular. Restricted: equipe autorizada a ver documentos e o titular.</summary>
public enum FileScope { ClubPublic, Staff, Restricted }
