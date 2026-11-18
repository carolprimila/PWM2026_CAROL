namespace Clubes.Mobile.Models;

// Espelham os enums do servidor (serializados como texto).
public enum UserKind { SuperAdmin, ClubAdmin, Staff, Member, Dependent }
public enum PersonType { Member, Dependent, Guest }
public enum MemberStatus { PendingApproval, Active, Suspended, Cancelled }
public enum DependentStatus { PendingApproval, Active, Suspended, Rejected, Removed }
public enum AccessOperation { Entry, Exit }
public enum ChargeType { Monthly, Fine, Fee, Reservation, Guest, Damage, Penalty, Other }
public enum ChargeStatus { Open, Paid, Cancelled }
public enum AdjustmentKind { Discount, Addition, Fine, Interest, Fee }
public enum PaymentMethod { Cash, Pix, CreditCard, DebitCard, BankSlip, Transfer }
public enum PaymentChannel { App, FrontDesk }
public enum PaymentStatus { Pending, Confirmed, Failed, Refunded }
public enum SpaceType { Court, Kiosk, PartyHall, Pool, Other }
public enum BookingMode { Hourly, MultiDay }
public enum SpaceChargeMode { NextMonthlyBill, Advance }
public enum ApprovalMode { Manual, Automatic }
public enum ReservationKind { Member, Administrative, InternalEvent }
public enum ReservationStatus { Pending, Approved, Rejected, Cancelled, Suspended, Completed }
public enum AttendanceStatus { Unknown, Attended, NoShow }
public enum SlotState { Available, Reserved, Blocked, Past, Closed }
public enum GuestAuthorization { Pending, Authorized, Denied }
public enum EventAudience { Public, MembersOnly, Specific }
public enum EventStatus { Scheduled, Cancelled, Finished }
public enum AnnouncementCategory { Communication, Notice, News, Important, Operation, Event, Financial }
public enum Priority { Normal, High, Urgent }
public enum NotificationCategory { General, Finance, Reservation, Dependent, Support, Assembly, Event }
public enum ManifestationStatus { Received, UnderReview, InProgress, Accepted, ForwardedToAssembly, Resolved, Refused }
public enum AssemblyStatus { Scheduled, InProgress, Finished, Cancelled }
public enum PollStatus { Draft, Open, Closed }
public enum PollCriterion { ActiveMembers, NonDelinquentMembers, PresentAtAssembly }
public enum FileScope { ClubPublic, Staff, Restricted }

/// <summary>Tom visual de um indicador de situação.</summary>
public enum ChipKind { Neutral, Success, Warning, Danger, Info }
