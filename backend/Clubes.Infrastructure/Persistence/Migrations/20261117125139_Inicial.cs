using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clubes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:btree_gist", ",,");

            migrationBuilder.CreateTable(
                name: "AccessLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    PersonName = table.Column<string>(type: "text", nullable: true),
                    ResponsibleMemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    Operation = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Result = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DenialReason = table.Column<string>(type: "text", nullable: true),
                    StaffUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Origin = table.Column<string>(type: "text", nullable: true),
                    QrTokenId = table.Column<Guid>(type: "uuid", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Announcement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Priority = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AudienceMembers = table.Column<bool>(type: "boolean", nullable: false),
                    AudienceDependents = table.Column<bool>(type: "boolean", nullable: false),
                    AudienceStaff = table.Column<bool>(type: "boolean", nullable: false),
                    IsSpecific = table.Column<bool>(type: "boolean", nullable: false),
                    ImageFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventId = table.Column<Guid>(type: "uuid", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AuthorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcement", x => x.Id);
                    table.UniqueConstraint("AK_Announcement_ClubId_Id", x => new { x.ClubId, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "AnnouncementRead",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnnouncementId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnnouncementRead", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnnouncementRead_Announcement_ClubId_AnnouncementId",
                        columns: x => new { x.ClubId, x.AnnouncementId },
                        principalTable: "Announcement",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AnnouncementTarget",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnnouncementId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnnouncementTarget", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnnouncementTarget_Announcement_ClubId_AnnouncementId",
                        columns: x => new { x.ClubId, x.AnnouncementId },
                        principalTable: "Announcement",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Assembly",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: true),
                    Agenda = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Minutes = table.Column<string>(type: "text", nullable: true),
                    MinutesFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assembly", x => x.Id);
                    table.UniqueConstraint("AK_Assembly_ClubId_Id", x => new { x.ClubId, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "AssemblyAttendance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssemblyId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegisteredByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssemblyAttendance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssemblyAttendance_Assembly_ClubId_AssemblyId",
                        columns: x => new { x.ClubId, x.AssemblyId },
                        principalTable: "Assembly",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Asset",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Location = table.Column<string>(type: "text", nullable: true),
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Condition = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AcquiredOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Value = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    ResponsibleEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asset", x => x.Id);
                    table.UniqueConstraint("AK_Asset_ClubId_Id", x => new { x.ClubId, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "Attachment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerType = table.Column<string>(type: "text", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: true),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    Operation = table.Column<string>(type: "text", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<string>(type: "text", nullable: true),
                    BeforeJson = table.Column<string>(type: "jsonb", nullable: true),
                    AfterJson = table.Column<string>(type: "jsonb", nullable: true),
                    Origin = table.Column<string>(type: "text", nullable: true),
                    Justification = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Charge",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Competence = table.Column<string>(type: "text", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DelinquentFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    BaseFee = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    DependentsCount = table.Column<int>(type: "integer", nullable: false),
                    DependentsAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    CompositionSnapshot = table.Column<string>(type: "jsonb", nullable: true),
                    OriginalAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    Additions = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    Discounts = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    FineAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    InterestAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    FeesAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    GraceDaysSnapshot = table.Column<int>(type: "integer", nullable: false),
                    LateFinePercentSnapshot = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: true),
                    DailyInterestPercentSnapshot = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CancelReason = table.Column<string>(type: "text", nullable: true),
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: true),
                    GuestId = table.Column<Guid>(type: "uuid", nullable: true),
                    OccurrenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Charge", x => x.Id);
                    table.UniqueConstraint("AK_Charge_ClubId_Id", x => new { x.ClubId, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "ChargeAdjustment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChargeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsAutomatic = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChargeAdjustment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChargeAdjustment_Charge_ClubId_ChargeId",
                        columns: x => new { x.ClubId, x.ChargeId },
                        principalTable: "Charge",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Club",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    LegalName = table.Column<string>(type: "text", nullable: true),
                    Cnpj = table.Column<string>(type: "text", nullable: true),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    ZipCode = table.Column<string>(type: "text", nullable: true),
                    Street = table.Column<string>(type: "text", nullable: true),
                    Number = table.Column<string>(type: "text", nullable: true),
                    Complement = table.Column<string>(type: "text", nullable: true),
                    District = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    State = table.Column<string>(type: "text", nullable: true),
                    LogoFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    About = table.Column<string>(type: "text", nullable: true),
                    TimeZoneId = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Club", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClubOpeningHour",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false),
                    OpenMinute = table.Column<int>(type: "integer", nullable: false),
                    CloseMinute = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubOpeningHour", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClubOpeningHour_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClubSequence",
                columns: table => new
                {
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubSequence", x => new { x.ClubId, x.Name });
                    table.ForeignKey(
                        name: "FK_ClubSequence_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClubSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MonthlyBaseFee = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    DueDay = table.Column<int>(type: "integer", nullable: true),
                    GraceDays = table.Column<int>(type: "integer", nullable: true),
                    LateFinePercent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: true),
                    DailyInterestPercent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: true),
                    DependentPricingMode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DependentFixedAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    BlockAccessWhenDelinquent = table.Column<bool>(type: "boolean", nullable: true),
                    BlockReservationsWhenDelinquent = table.Column<bool>(type: "boolean", nullable: true),
                    DelinquentReservationPolicy = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    AllowAdvancePayment = table.Column<bool>(type: "boolean", nullable: true),
                    FamilyCompositionChangeRule = table.Column<string>(type: "text", nullable: true),
                    AutoGenerateMonthlyCharges = table.Column<bool>(type: "boolean", nullable: false),
                    DueReminderDays = table.Column<int>(type: "integer", nullable: false),
                    MaxDependentsPerMember = table.Column<int>(type: "integer", nullable: false),
                    MaxGuestsPerMember = table.Column<int>(type: "integer", nullable: false),
                    MemberCanToggleDependentPermissions = table.Column<bool>(type: "boolean", nullable: false),
                    MaxAdvanceDays = table.Column<int>(type: "integer", nullable: true),
                    MaxConcurrentReservations = table.Column<int>(type: "integer", nullable: true),
                    FreeCancellationHours = table.Column<int>(type: "integer", nullable: true),
                    LateCancellationFee = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    NoShowFee = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    RequireAdvancePayment = table.Column<bool>(type: "boolean", nullable: true),
                    ReservationApprovalMode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    GuestFee = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    GuestAdvanceRegistrationRequired = table.Column<bool>(type: "boolean", nullable: true),
                    GuestDocumentRequired = table.Column<bool>(type: "boolean", nullable: true),
                    GuestEntryWithoutMemberAllowed = table.Column<bool>(type: "boolean", nullable: true),
                    QrTokenTtlSeconds = table.Column<int>(type: "integer", nullable: false),
                    HolidayRules = table.Column<string>(type: "text", nullable: true),
                    MinorsRules = table.Column<string>(type: "text", nullable: true),
                    MinorsAccompanimentRules = table.Column<string>(type: "text", nullable: true),
                    PoolPolicy = table.Column<string>(type: "text", nullable: true),
                    EventProcedures = table.Column<string>(type: "text", nullable: true),
                    SpaceUsagePolicy = table.Column<string>(type: "text", nullable: true),
                    PrivacyPolicy = table.Column<string>(type: "text", nullable: true),
                    PrivacyPolicyVersion = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClubSettings_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DependentPricingRule",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FromCount = table.Column<int>(type: "integer", nullable: false),
                    ToCount = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DependentPricingRule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DependentPricingRule_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DigitalCard",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    CardNumber = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigitalCard", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DigitalCard_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Employee",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Cpf = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Position = table.Column<string>(type: "text", nullable: true),
                    Department = table.Column<string>(type: "text", nullable: true),
                    HiredAt = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employee", x => x.Id);
                    table.UniqueConstraint("AK_Employee_ClubId_Id", x => new { x.ClubId, x.Id });
                    table.ForeignKey(
                        name: "FK_Employee_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Holiday",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Scope = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false),
                    OpenMinute = table.Column<int>(type: "integer", nullable: true),
                    CloseMinute = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Holiday", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Holiday_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ManifestationType",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsSuggestion = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManifestationType", x => x.Id);
                    table.UniqueConstraint("AK_ManifestationType_ClubId_Id", x => new { x.ClubId, x.Id });
                    table.ForeignKey(
                        name: "FK_ManifestationType_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.Id);
                    table.UniqueConstraint("AK_Role_ClubId_Id", x => new { x.ClubId, x.Id });
                    table.ForeignKey(
                        name: "FK_Role_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Supplier",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Document = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Supplier", x => x.Id);
                    table.UniqueConstraint("AK_Supplier_ClubId_Id", x => new { x.ClubId, x.Id });
                    table.ForeignKey(
                        name: "FK_Supplier_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolePermission",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Permission = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermission_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolePermission_Role_ClubId_RoleId",
                        columns: x => new { x.ClubId, x.RoleId },
                        principalTable: "Role",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: true),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    MinQuantity = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockItem", x => x.Id);
                    table.UniqueConstraint("AK_StockItem_ClubId_Id", x => new { x.ClubId, x.Id });
                    table.ForeignKey(
                        name: "FK_StockItem_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockItem_Supplier_ClubId_SupplierId",
                        columns: x => new { x.ClubId, x.SupplierId },
                        principalTable: "Supplier",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClubEvent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ImageFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: true),
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Capacity = table.Column<int>(type: "integer", nullable: true),
                    Audience = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AllowMembers = table.Column<bool>(type: "boolean", nullable: false),
                    AllowDependents = table.Column<bool>(type: "boolean", nullable: false),
                    AllowStaff = table.Column<bool>(type: "boolean", nullable: false),
                    RegistrationRequired = table.Column<bool>(type: "boolean", nullable: false),
                    RegistrationDeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationDeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CancelReason = table.Column<string>(type: "text", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubEvent", x => x.Id);
                    table.UniqueConstraint("AK_ClubEvent_ClubId_Id", x => new { x.ClubId, x.Id });
                    table.ForeignKey(
                        name: "FK_ClubEvent_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Dependent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Cpf = table.Column<string>(type: "text", nullable: true),
                    Rg = table.Column<string>(type: "text", nullable: true),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<string>(type: "text", nullable: true),
                    Relationship = table.Column<string>(type: "text", nullable: false),
                    PhotoFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    StatusReason = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dependent", x => x.Id);
                    table.UniqueConstraint("AK_Dependent_ClubId_Id", x => new { x.ClubId, x.Id });
                    table.ForeignKey(
                        name: "FK_Dependent_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DependentPermission",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DependentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    AllowedByAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    EnabledByMember = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DependentPermission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DependentPermission_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DependentPermission_Dependent_ClubId_DependentId",
                        columns: x => new { x.ClubId, x.DependentId },
                        principalTable: "Dependent",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeviceToken",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    Platform = table.Column<string>(type: "text", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceToken", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceToken_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventRegistration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonName = table.Column<string>(type: "text", nullable: false),
                    PersonKind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Attended = table.Column<bool>(type: "boolean", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventRegistration", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventRegistration_ClubEvent_ClubId_EventId",
                        columns: x => new { x.ClubId, x.EventId },
                        principalTable: "ClubEvent",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventRegistration_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancialEntry",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Counterparty = table.Column<string>(type: "text", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PaidOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CancelReason = table.Column<string>(type: "text", nullable: true),
                    MaintenanceOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReceiptFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialEntry_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancialRelease",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    ValidUntil = table.Column<DateOnly>(type: "date", nullable: false),
                    GrantedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialRelease", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialRelease_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Guest",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Document = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    VisitDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: true),
                    FeeAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    ChargeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Authorization = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AuthorizedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorizationNote = table.Column<string>(type: "text", nullable: true),
                    RegisteredByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExitedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsCancelled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guest", x => x.Id);
                    table.UniqueConstraint("AK_Guest_ClubId_Id", x => new { x.ClubId, x.Id });
                    table.ForeignKey(
                        name: "FK_Guest_Charge_ClubId_ChargeId",
                        columns: x => new { x.ClubId, x.ChargeId },
                        principalTable: "Charge",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Guest_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Inspection",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: true),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    Moment = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    InspectorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    HasDamage = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inspection", x => x.Id);
                    table.UniqueConstraint("AK_Inspection_ClubId_Id", x => new { x.ClubId, x.Id });
                    table.ForeignKey(
                        name: "FK_Inspection_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InspectionItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IsOk = table.Column<bool>(type: "boolean", nullable: false),
                    IsDamage = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InspectionItem_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InspectionItem_Inspection_ClubId_InspectionId",
                        columns: x => new { x.ClubId, x.InspectionId },
                        principalTable: "Inspection",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KeyHandover",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReceiverName = table.Column<string>(type: "text", nullable: false),
                    DeliveredByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PickedUpAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpectedReturnAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConditionOnDelivery = table.Column<string>(type: "text", nullable: true),
                    ReturnedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReceivedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConditionOnReturn = table.Column<string>(type: "text", nullable: true),
                    Pendencies = table.Column<string>(type: "text", nullable: true),
                    PendenciesResolved = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyHandover", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KeyHandover_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoginHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    Ip = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoginHistory_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceOrder",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResponsibleEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResponsibleName = table.Column<string>(type: "text", nullable: true),
                    OpenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    ActualCost = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OpenedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceOrder", x => x.Id);
                    table.UniqueConstraint("AK_MaintenanceOrder_ClubId_Id", x => new { x.ClubId, x.Id });
                    table.ForeignKey(
                        name: "FK_MaintenanceOrder_Asset_ClubId_AssetId",
                        columns: x => new { x.ClubId, x.AssetId },
                        principalTable: "Asset",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenanceOrder_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenanceOrder_Employee_ClubId_ResponsibleEmployeeId",
                        columns: x => new { x.ClubId, x.ResponsibleEmployeeId },
                        principalTable: "Employee",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Manifestation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Protocol = table.Column<string>(type: "text", nullable: false),
                    TypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    PhotoFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorName = table.Column<string>(type: "text", nullable: false),
                    AuthorKind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Classification = table.Column<string>(type: "text", nullable: true),
                    AssemblyId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Manifestation", x => x.Id);
                    table.UniqueConstraint("AK_Manifestation_ClubId_Id", x => new { x.ClubId, x.Id });
                    table.ForeignKey(
                        name: "FK_Manifestation_Assembly_ClubId_AssemblyId",
                        columns: x => new { x.ClubId, x.AssemblyId },
                        principalTable: "Assembly",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Manifestation_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Manifestation_ManifestationType_ClubId_TypeId",
                        columns: x => new { x.ClubId, x.TypeId },
                        principalTable: "ManifestationType",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ManifestationUpdate",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ManifestationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: true),
                    IsVisibleToAuthor = table.Column<bool>(type: "boolean", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManifestationUpdate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManifestationUpdate_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManifestationUpdate_Manifestation_ClubId_ManifestationId",
                        columns: x => new { x.ClubId, x.ManifestationId },
                        principalTable: "Manifestation",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Member",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Cpf = table.Column<string>(type: "text", nullable: false),
                    Rg = table.Column<string>(type: "text", nullable: true),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<string>(type: "text", nullable: true),
                    MaritalStatus = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    ZipCode = table.Column<string>(type: "text", nullable: true),
                    Street = table.Column<string>(type: "text", nullable: true),
                    AddressNumber = table.Column<string>(type: "text", nullable: true),
                    Complement = table.Column<string>(type: "text", nullable: true),
                    District = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    State = table.Column<string>(type: "text", nullable: true),
                    PhotoFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    JoinedAt = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Member", x => x.Id);
                    table.UniqueConstraint("AK_Member_ClubId_Id", x => new { x.ClubId, x.Id });
                    table.ForeignKey(
                        name: "FK_Member_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Presence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonName = table.Column<string>(type: "text", nullable: false),
                    ResponsibleMemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    EnteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Presence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Presence_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Presence_Member_ClubId_ResponsibleMemberId",
                        columns: x => new { x.ClubId, x.ResponsibleMemberId },
                        principalTable: "Member",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    DependentId = table.Column<Guid>(type: "uuid", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    BlockReason = table.Column<string>(type: "text", nullable: true),
                    MustChangePassword = table.Column<bool>(type: "boolean", nullable: false),
                    FailedLoginCount = table.Column<int>(type: "integer", nullable: false),
                    LockoutUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PrivacyAcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PrivacyAcceptedVersion = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                    table.ForeignKey(
                        name: "FK_User_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_User_Dependent_DependentId",
                        column: x => x.DependentId,
                        principalTable: "Dependent",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_User_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_User_Member_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Member",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyClosing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Competence = table.Column<string>(type: "text", nullable: false),
                    ChargesIssued = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    PaymentsReceived = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    OtherRevenue = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    Expenses = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    OpenBalance = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    ClosedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyClosing", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyClosing_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonthlyClosing_User_ClosedByUserId",
                        column: x => x.ClosedByUserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: true),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsMandatory = table.Column<bool>(type: "boolean", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PushStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PushSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmailStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    EmailSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveryError = table.Column<string>(type: "text", nullable: true),
                    DispatchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notification_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notification_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationPreference",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    InAppEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    PushEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreference", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationPreference_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificationPreference_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetToken",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetToken", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetToken_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Poll",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssemblyId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    OpensAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosesAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Criterion = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ResultsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Poll", x => x.Id);
                    table.UniqueConstraint("AK_Poll_ClubId_Id", x => new { x.ClubId, x.Id });
                    table.ForeignKey(
                        name: "FK_Poll_Assembly_ClubId_AssemblyId",
                        columns: x => new { x.ClubId, x.AssemblyId },
                        principalTable: "Assembly",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Poll_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Poll_User_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QrToken",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Operation = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    DisplayCode = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConsumedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IssuedToUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QrToken", x => x.Id);
                    table.UniqueConstraint("AK_QrToken_ClubId_Id", x => new { x.ClubId, x.Id });
                    table.ForeignKey(
                        name: "FK_QrToken_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QrToken_User_ConsumedByUserId",
                        column: x => x.ConsumedByUserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QrToken_User_IssuedToUserId",
                        column: x => x.IssuedToUserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefreshToken",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedReason = table.Column<string>(type: "text", nullable: true),
                    ReplacedByHash = table.Column<string>(type: "text", nullable: true),
                    Device = table.Column<string>(type: "text", nullable: true),
                    Ip = table.Column<string>(type: "text", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshToken", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshToken_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StatusHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<string>(type: "text", nullable: true),
                    ToStatus = table.Column<string>(type: "text", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StatusHistory_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StatusHistory_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockMovement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovement_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovement_StockItem_ClubId_StockItemId",
                        columns: x => new { x.ClubId, x.StockItemId },
                        principalTable: "StockItem",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovement_Supplier_ClubId_SupplierId",
                        columns: x => new { x.ClubId, x.SupplierId },
                        principalTable: "Supplier",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovement_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoredFile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    StoragePath = table.Column<string>(type: "text", nullable: false),
                    Scope = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerMemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredFile", x => x.Id);
                    table.UniqueConstraint("AK_StoredFile_ClubId_Id", x => new { x.ClubId, x.Id });
                    table.ForeignKey(
                        name: "FK_StoredFile_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoredFile_Member_ClubId_OwnerMemberId",
                        columns: x => new { x.ClubId, x.OwnerMemberId },
                        principalTable: "Member",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoredFile_User_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserPermission",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Permission = table.Column<string>(type: "text", nullable: false),
                    Granted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPermission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPermission_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserPermission_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserRole",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRole", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRole_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRole_Role_ClubId_RoleId",
                        columns: x => new { x.ClubId, x.RoleId },
                        principalTable: "Role",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRole_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PollOption",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PollId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PollOption", x => x.Id);
                    table.UniqueConstraint("AK_PollOption_ClubId_Id", x => new { x.ClubId, x.Id });
                    table.ForeignKey(
                        name: "FK_PollOption_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PollOption_Poll_ClubId_PollId",
                        columns: x => new { x.ClubId, x.PollId },
                        principalTable: "Poll",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChargeId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    Method = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Channel = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RegisteredByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "text", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "text", nullable: false),
                    GatewayProvider = table.Column<string>(type: "text", nullable: true),
                    GatewayReference = table.Column<string>(type: "text", nullable: true),
                    GatewayPayload = table.Column<string>(type: "text", nullable: true),
                    ReceiptFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    RefundedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RefundReason = table.Column<string>(type: "text", nullable: true),
                    RefundedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payment_Charge_ClubId_ChargeId",
                        columns: x => new { x.ClubId, x.ChargeId },
                        principalTable: "Charge",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payment_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payment_Member_ClubId_MemberId",
                        columns: x => new { x.ClubId, x.MemberId },
                        principalTable: "Member",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payment_StoredFile_ClubId_ReceiptFileId",
                        columns: x => new { x.ClubId, x.ReceiptFileId },
                        principalTable: "StoredFile",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payment_User_RefundedByUserId",
                        column: x => x.RefundedByUserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payment_User_RegisteredByUserId",
                        column: x => x.RegisteredByUserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Space",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Subtype = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Capacity = table.Column<int>(type: "integer", nullable: true),
                    IsReservable = table.Column<bool>(type: "boolean", nullable: false),
                    BookingMode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SlotMinutes = table.Column<int>(type: "integer", nullable: false),
                    FixedDays = table.Column<int>(type: "integer", nullable: false),
                    HourlyPrice = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    PeriodPrice = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    CleaningFee = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    ChargeMode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ApprovalMode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    UsageRules = table.Column<string>(type: "text", nullable: true),
                    ImageFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Space", x => x.Id);
                    table.UniqueConstraint("AK_Space_ClubId_Id", x => new { x.ClubId, x.Id });
                    table.ForeignKey(
                        name: "FK_Space_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Space_StoredFile_ClubId_ImageFileId",
                        columns: x => new { x.ClubId, x.ImageFileId },
                        principalTable: "StoredFile",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vote",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PollId = table.Column<Guid>(type: "uuid", nullable: false),
                    PollOptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vote", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vote_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vote_Member_ClubId_MemberId",
                        columns: x => new { x.ClubId, x.MemberId },
                        principalTable: "Member",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vote_PollOption_ClubId_PollOptionId",
                        columns: x => new { x.ClubId, x.PollOptionId },
                        principalTable: "PollOption",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vote_Poll_ClubId_PollId",
                        columns: x => new { x.ClubId, x.PollId },
                        principalTable: "Poll",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vote_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reservation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByDependentId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeopleCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RentalAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    CleaningFee = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    ChargeMode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ChargeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    DecisionByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DecisionAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DecisionReason = table.Column<string>(type: "text", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CancellationReason = table.Column<string>(type: "text", nullable: true),
                    Attendance = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AttendanceNote = table.Column<string>(type: "text", nullable: true),
                    PenaltyChargeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReminderSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservation", x => x.Id);
                    table.UniqueConstraint("AK_Reservation_ClubId_Id", x => new { x.ClubId, x.Id });
                    table.ForeignKey(
                        name: "FK_Reservation_Charge_ClubId_ChargeId",
                        columns: x => new { x.ClubId, x.ChargeId },
                        principalTable: "Charge",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservation_Charge_ClubId_PenaltyChargeId",
                        columns: x => new { x.ClubId, x.PenaltyChargeId },
                        principalTable: "Charge",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservation_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservation_Dependent_ClubId_RequestedByDependentId",
                        columns: x => new { x.ClubId, x.RequestedByDependentId },
                        principalTable: "Dependent",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservation_Member_ClubId_MemberId",
                        columns: x => new { x.ClubId, x.MemberId },
                        principalTable: "Member",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservation_Space_ClubId_SpaceId",
                        columns: x => new { x.ClubId, x.SpaceId },
                        principalTable: "Space",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservation_User_CancelledByUserId",
                        column: x => x.CancelledByUserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservation_User_DecisionByUserId",
                        column: x => x.DecisionByUserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservation_User_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SpaceBlock",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MaintenanceOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsCancelled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpaceBlock", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpaceBlock_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpaceBlock_MaintenanceOrder_ClubId_MaintenanceOrderId",
                        columns: x => new { x.ClubId, x.MaintenanceOrderId },
                        principalTable: "MaintenanceOrder",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpaceBlock_Space_ClubId_SpaceId",
                        columns: x => new { x.ClubId, x.SpaceId },
                        principalTable: "Space",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpaceBlock_User_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SpaceSchedule",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    OpenMinute = table.Column<int>(type: "integer", nullable: false),
                    CloseMinute = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpaceSchedule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpaceSchedule_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpaceSchedule_Space_ClubId_SpaceId",
                        columns: x => new { x.ClubId, x.SpaceId },
                        principalTable: "Space",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Occurrence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: true),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    InspectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReportedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Resolution = table.Column<string>(type: "text", nullable: true),
                    ProposedChargeAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    ChargeDecision = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ChargeDecisionByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChargeDecisionAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ChargeDecisionReason = table.Column<string>(type: "text", nullable: true),
                    ChargeId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Occurrence", x => x.Id);
                    table.UniqueConstraint("AK_Occurrence_ClubId_Id", x => new { x.ClubId, x.Id });
                    table.ForeignKey(
                        name: "FK_Occurrence_Charge_ClubId_ChargeId",
                        columns: x => new { x.ClubId, x.ChargeId },
                        principalTable: "Charge",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Occurrence_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Occurrence_Inspection_ClubId_InspectionId",
                        columns: x => new { x.ClubId, x.InspectionId },
                        principalTable: "Inspection",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Occurrence_Member_ClubId_MemberId",
                        columns: x => new { x.ClubId, x.MemberId },
                        principalTable: "Member",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Occurrence_Reservation_ClubId_ReservationId",
                        columns: x => new { x.ClubId, x.ReservationId },
                        principalTable: "Reservation",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Occurrence_Space_ClubId_SpaceId",
                        columns: x => new { x.ClubId, x.SpaceId },
                        principalTable: "Space",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Occurrence_User_ChargeDecisionByUserId",
                        column: x => x.ChargeDecisionByUserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Occurrence_User_ReportedByUserId",
                        column: x => x.ReportedByUserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReservationSlot",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationSlot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservationSlot_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservationSlot_Reservation_ClubId_ReservationId",
                        columns: x => new { x.ClubId, x.ReservationId },
                        principalTable: "Reservation",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservationSlot_Space_ClubId_SpaceId",
                        columns: x => new { x.ClubId, x.SpaceId },
                        principalTable: "Space",
                        principalColumns: new[] { "ClubId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessLog_ClubId_OccurredAt",
                table: "AccessLog",
                columns: new[] { "ClubId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessLog_ClubId_QrTokenId",
                table: "AccessLog",
                columns: new[] { "ClubId", "QrTokenId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessLog_ClubId_ResponsibleMemberId",
                table: "AccessLog",
                columns: new[] { "ClubId", "ResponsibleMemberId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessLog_StaffUserId",
                table: "AccessLog",
                column: "StaffUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Announcement_AuthorUserId",
                table: "Announcement",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Announcement_ClubId_EventId",
                table: "Announcement",
                columns: new[] { "ClubId", "EventId" });

            migrationBuilder.CreateIndex(
                name: "IX_Announcement_ClubId_ImageFileId",
                table: "Announcement",
                columns: new[] { "ClubId", "ImageFileId" });

            migrationBuilder.CreateIndex(
                name: "IX_Announcement_ClubId_PublishedAt",
                table: "Announcement",
                columns: new[] { "ClubId", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AnnouncementRead_AnnouncementId_UserId",
                table: "AnnouncementRead",
                columns: new[] { "AnnouncementId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnnouncementRead_ClubId_AnnouncementId",
                table: "AnnouncementRead",
                columns: new[] { "ClubId", "AnnouncementId" });

            migrationBuilder.CreateIndex(
                name: "IX_AnnouncementRead_UserId",
                table: "AnnouncementRead",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AnnouncementTarget_AnnouncementId_UserId",
                table: "AnnouncementTarget",
                columns: new[] { "AnnouncementId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnnouncementTarget_ClubId_AnnouncementId",
                table: "AnnouncementTarget",
                columns: new[] { "ClubId", "AnnouncementId" });

            migrationBuilder.CreateIndex(
                name: "IX_AnnouncementTarget_UserId",
                table: "AnnouncementTarget",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Assembly_ClubId_MinutesFileId",
                table: "Assembly",
                columns: new[] { "ClubId", "MinutesFileId" });

            migrationBuilder.CreateIndex(
                name: "IX_Assembly_CreatedByUserId",
                table: "Assembly",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AssemblyAttendance_AssemblyId_MemberId",
                table: "AssemblyAttendance",
                columns: new[] { "AssemblyId", "MemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssemblyAttendance_ClubId_AssemblyId",
                table: "AssemblyAttendance",
                columns: new[] { "ClubId", "AssemblyId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssemblyAttendance_ClubId_MemberId",
                table: "AssemblyAttendance",
                columns: new[] { "ClubId", "MemberId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssemblyAttendance_RegisteredByUserId",
                table: "AssemblyAttendance",
                column: "RegisteredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Asset_ClubId_Code",
                table: "Asset",
                columns: new[] { "ClubId", "Code" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Asset_ClubId_ResponsibleEmployeeId",
                table: "Asset",
                columns: new[] { "ClubId", "ResponsibleEmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Asset_ClubId_SpaceId",
                table: "Asset",
                columns: new[] { "ClubId", "SpaceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Attachment_ClubId_FileId",
                table: "Attachment",
                columns: new[] { "ClubId", "FileId" });

            migrationBuilder.CreateIndex(
                name: "IX_Attachment_OwnerType_OwnerId",
                table: "Attachment",
                columns: new[] { "OwnerType", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Attachment_UploadedByUserId",
                table: "Attachment",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_ClubId_CreatedAt",
                table: "AuditLog",
                columns: new[] { "ClubId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_EntityType_EntityId",
                table: "AuditLog",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_UserId",
                table: "AuditLog",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Charge_ClubId_GuestId",
                table: "Charge",
                columns: new[] { "ClubId", "GuestId" });

            migrationBuilder.CreateIndex(
                name: "IX_Charge_ClubId_MemberId_Competence",
                table: "Charge",
                columns: new[] { "ClubId", "MemberId", "Competence" },
                unique: true,
                filter: "\"Type\" = 'Monthly' AND \"Status\" <> 'Cancelled'");

            migrationBuilder.CreateIndex(
                name: "IX_Charge_ClubId_OccurrenceId",
                table: "Charge",
                columns: new[] { "ClubId", "OccurrenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Charge_ClubId_ReservationId",
                table: "Charge",
                columns: new[] { "ClubId", "ReservationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Charge_ClubId_Status_DueDate",
                table: "Charge",
                columns: new[] { "ClubId", "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Charge_CreatedByUserId",
                table: "Charge",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChargeAdjustment_ClubId_ChargeId",
                table: "ChargeAdjustment",
                columns: new[] { "ClubId", "ChargeId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChargeAdjustment_UserId",
                table: "ChargeAdjustment",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Club_Cnpj",
                table: "Club",
                column: "Cnpj",
                unique: true,
                filter: "\"Cnpj\" IS NOT NULL AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Club_LogoFileId",
                table: "Club",
                column: "LogoFileId");

            migrationBuilder.CreateIndex(
                name: "IX_Club_Slug",
                table: "Club",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClubEvent_ClubId_ImageFileId",
                table: "ClubEvent",
                columns: new[] { "ClubId", "ImageFileId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClubEvent_ClubId_SpaceId",
                table: "ClubEvent",
                columns: new[] { "ClubId", "SpaceId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClubEvent_CreatedByUserId",
                table: "ClubEvent",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubOpeningHour_ClubId_DayOfWeek",
                table: "ClubOpeningHour",
                columns: new[] { "ClubId", "DayOfWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClubSettings_ClubId",
                table: "ClubSettings",
                column: "ClubId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dependent_ClubId_Cpf",
                table: "Dependent",
                columns: new[] { "ClubId", "Cpf" },
                unique: true,
                filter: "\"Cpf\" IS NOT NULL AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Dependent_ClubId_MemberId",
                table: "Dependent",
                columns: new[] { "ClubId", "MemberId" });

            migrationBuilder.CreateIndex(
                name: "IX_Dependent_ClubId_PhotoFileId",
                table: "Dependent",
                columns: new[] { "ClubId", "PhotoFileId" });

            migrationBuilder.CreateIndex(
                name: "IX_DependentPermission_ClubId_DependentId",
                table: "DependentPermission",
                columns: new[] { "ClubId", "DependentId" });

            migrationBuilder.CreateIndex(
                name: "IX_DependentPermission_DependentId_Code",
                table: "DependentPermission",
                columns: new[] { "DependentId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DependentPricingRule_ClubId",
                table: "DependentPricingRule",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceToken_ClubId",
                table: "DeviceToken",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceToken_Token",
                table: "DeviceToken",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceToken_UserId",
                table: "DeviceToken",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalCard_ClubId_CardNumber",
                table: "DigitalCard",
                columns: new[] { "ClubId", "CardNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DigitalCard_ClubId_PersonType_PersonId",
                table: "DigitalCard",
                columns: new[] { "ClubId", "PersonType", "PersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employee_ClubId_Cpf",
                table: "Employee",
                columns: new[] { "ClubId", "Cpf" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_EventRegistration_ClubId_EventId",
                table: "EventRegistration",
                columns: new[] { "ClubId", "EventId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventRegistration_EventId_UserId",
                table: "EventRegistration",
                columns: new[] { "EventId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventRegistration_UserId",
                table: "EventRegistration",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialEntry_ClubId_Kind_DueDate",
                table: "FinancialEntry",
                columns: new[] { "ClubId", "Kind", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialEntry_ClubId_MaintenanceOrderId",
                table: "FinancialEntry",
                columns: new[] { "ClubId", "MaintenanceOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialEntry_ClubId_ReceiptFileId",
                table: "FinancialEntry",
                columns: new[] { "ClubId", "ReceiptFileId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialEntry_CreatedByUserId",
                table: "FinancialEntry",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialRelease_ClubId_MemberId",
                table: "FinancialRelease",
                columns: new[] { "ClubId", "MemberId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialRelease_GrantedByUserId",
                table: "FinancialRelease",
                column: "GrantedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialRelease_RevokedByUserId",
                table: "FinancialRelease",
                column: "RevokedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Guest_AuthorizedByUserId",
                table: "Guest",
                column: "AuthorizedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Guest_ClubId_ChargeId",
                table: "Guest",
                columns: new[] { "ClubId", "ChargeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Guest_ClubId_MemberId",
                table: "Guest",
                columns: new[] { "ClubId", "MemberId" });

            migrationBuilder.CreateIndex(
                name: "IX_Guest_ClubId_ReservationId",
                table: "Guest",
                columns: new[] { "ClubId", "ReservationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Guest_ClubId_SpaceId",
                table: "Guest",
                columns: new[] { "ClubId", "SpaceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Guest_ClubId_VisitDate",
                table: "Guest",
                columns: new[] { "ClubId", "VisitDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Guest_RegisteredByUserId",
                table: "Guest",
                column: "RegisteredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Holiday_ClubId_Date",
                table: "Holiday",
                columns: new[] { "ClubId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Inspection_ClubId_MemberId",
                table: "Inspection",
                columns: new[] { "ClubId", "MemberId" });

            migrationBuilder.CreateIndex(
                name: "IX_Inspection_ClubId_ReservationId",
                table: "Inspection",
                columns: new[] { "ClubId", "ReservationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Inspection_ClubId_SpaceId",
                table: "Inspection",
                columns: new[] { "ClubId", "SpaceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Inspection_InspectorUserId",
                table: "Inspection",
                column: "InspectorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionItem_ClubId_InspectionId",
                table: "InspectionItem",
                columns: new[] { "ClubId", "InspectionId" });

            migrationBuilder.CreateIndex(
                name: "IX_KeyHandover_ClubId_MemberId",
                table: "KeyHandover",
                columns: new[] { "ClubId", "MemberId" });

            migrationBuilder.CreateIndex(
                name: "IX_KeyHandover_ClubId_ReservationId",
                table: "KeyHandover",
                columns: new[] { "ClubId", "ReservationId" });

            migrationBuilder.CreateIndex(
                name: "IX_KeyHandover_ClubId_SpaceId",
                table: "KeyHandover",
                columns: new[] { "ClubId", "SpaceId" });

            migrationBuilder.CreateIndex(
                name: "IX_KeyHandover_DeliveredByUserId",
                table: "KeyHandover",
                column: "DeliveredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_KeyHandover_ReceivedByUserId",
                table: "KeyHandover",
                column: "ReceivedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoginHistory_ClubId_CreatedAt",
                table: "LoginHistory",
                columns: new[] { "ClubId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginHistory_UserId",
                table: "LoginHistory",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceOrder_ClubId_AssetId",
                table: "MaintenanceOrder",
                columns: new[] { "ClubId", "AssetId" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceOrder_ClubId_Code",
                table: "MaintenanceOrder",
                columns: new[] { "ClubId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceOrder_ClubId_ResponsibleEmployeeId",
                table: "MaintenanceOrder",
                columns: new[] { "ClubId", "ResponsibleEmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceOrder_ClubId_SpaceId",
                table: "MaintenanceOrder",
                columns: new[] { "ClubId", "SpaceId" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceOrder_OpenedByUserId",
                table: "MaintenanceOrder",
                column: "OpenedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Manifestation_ClubId_AssemblyId",
                table: "Manifestation",
                columns: new[] { "ClubId", "AssemblyId" });

            migrationBuilder.CreateIndex(
                name: "IX_Manifestation_ClubId_MemberId",
                table: "Manifestation",
                columns: new[] { "ClubId", "MemberId" });

            migrationBuilder.CreateIndex(
                name: "IX_Manifestation_ClubId_PhotoFileId",
                table: "Manifestation",
                columns: new[] { "ClubId", "PhotoFileId" });

            migrationBuilder.CreateIndex(
                name: "IX_Manifestation_ClubId_Protocol",
                table: "Manifestation",
                columns: new[] { "ClubId", "Protocol" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Manifestation_ClubId_TypeId",
                table: "Manifestation",
                columns: new[] { "ClubId", "TypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Manifestation_UserId",
                table: "Manifestation",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ManifestationUpdate_ClubId_ManifestationId",
                table: "ManifestationUpdate",
                columns: new[] { "ClubId", "ManifestationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ManifestationUpdate_UserId",
                table: "ManifestationUpdate",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Member_ClubId_Cpf",
                table: "Member",
                columns: new[] { "ClubId", "Cpf" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Member_ClubId_FullName",
                table: "Member",
                columns: new[] { "ClubId", "FullName" });

            migrationBuilder.CreateIndex(
                name: "IX_Member_ClubId_Number",
                table: "Member",
                columns: new[] { "ClubId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Member_ClubId_PhotoFileId",
                table: "Member",
                columns: new[] { "ClubId", "PhotoFileId" });

            migrationBuilder.CreateIndex(
                name: "IX_Member_ClubId_Status",
                table: "Member",
                columns: new[] { "ClubId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyClosing_ClosedByUserId",
                table: "MonthlyClosing",
                column: "ClosedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyClosing_ClubId_Competence",
                table: "MonthlyClosing",
                columns: new[] { "ClubId", "Competence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notification_ClubId",
                table: "Notification",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_UserId_ReadAt",
                table: "Notification",
                columns: new[] { "UserId", "ReadAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreference_ClubId",
                table: "NotificationPreference",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreference_UserId_Category",
                table: "NotificationPreference",
                columns: new[] { "UserId", "Category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Occurrence_ChargeDecisionByUserId",
                table: "Occurrence",
                column: "ChargeDecisionByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Occurrence_ClubId_ChargeId",
                table: "Occurrence",
                columns: new[] { "ClubId", "ChargeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Occurrence_ClubId_Code",
                table: "Occurrence",
                columns: new[] { "ClubId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Occurrence_ClubId_InspectionId",
                table: "Occurrence",
                columns: new[] { "ClubId", "InspectionId" });

            migrationBuilder.CreateIndex(
                name: "IX_Occurrence_ClubId_MemberId",
                table: "Occurrence",
                columns: new[] { "ClubId", "MemberId" });

            migrationBuilder.CreateIndex(
                name: "IX_Occurrence_ClubId_ReservationId",
                table: "Occurrence",
                columns: new[] { "ClubId", "ReservationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Occurrence_ClubId_SpaceId",
                table: "Occurrence",
                columns: new[] { "ClubId", "SpaceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Occurrence_ReportedByUserId",
                table: "Occurrence",
                column: "ReportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetToken_TokenHash",
                table: "PasswordResetToken",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetToken_UserId",
                table: "PasswordResetToken",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_ClubId_ChargeId",
                table: "Payment",
                columns: new[] { "ClubId", "ChargeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Payment_ClubId_IdempotencyKey",
                table: "Payment",
                columns: new[] { "ClubId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payment_ClubId_MemberId",
                table: "Payment",
                columns: new[] { "ClubId", "MemberId" });

            migrationBuilder.CreateIndex(
                name: "IX_Payment_ClubId_PaidAt",
                table: "Payment",
                columns: new[] { "ClubId", "PaidAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Payment_ClubId_ReceiptFileId",
                table: "Payment",
                columns: new[] { "ClubId", "ReceiptFileId" });

            migrationBuilder.CreateIndex(
                name: "IX_Payment_ClubId_ReceiptNumber",
                table: "Payment",
                columns: new[] { "ClubId", "ReceiptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payment_GatewayReference",
                table: "Payment",
                column: "GatewayReference");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_RefundedByUserId",
                table: "Payment",
                column: "RefundedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_RegisteredByUserId",
                table: "Payment",
                column: "RegisteredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Poll_ClubId_AssemblyId",
                table: "Poll",
                columns: new[] { "ClubId", "AssemblyId" });

            migrationBuilder.CreateIndex(
                name: "IX_Poll_CreatedByUserId",
                table: "Poll",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PollOption_ClubId_PollId",
                table: "PollOption",
                columns: new[] { "ClubId", "PollId" });

            migrationBuilder.CreateIndex(
                name: "IX_Presence_ClubId_PersonType_PersonId",
                table: "Presence",
                columns: new[] { "ClubId", "PersonType", "PersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Presence_ClubId_ResponsibleMemberId",
                table: "Presence",
                columns: new[] { "ClubId", "ResponsibleMemberId" });

            migrationBuilder.CreateIndex(
                name: "IX_QrToken_ClubId_DisplayCode",
                table: "QrToken",
                columns: new[] { "ClubId", "DisplayCode" });

            migrationBuilder.CreateIndex(
                name: "IX_QrToken_ClubId_PersonType_PersonId",
                table: "QrToken",
                columns: new[] { "ClubId", "PersonType", "PersonId" });

            migrationBuilder.CreateIndex(
                name: "IX_QrToken_ConsumedByUserId",
                table: "QrToken",
                column: "ConsumedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QrToken_IssuedToUserId",
                table: "QrToken",
                column: "IssuedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QrToken_TokenHash",
                table: "QrToken",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_TokenHash",
                table: "RefreshToken",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_UserId",
                table: "RefreshToken",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_CancelledByUserId",
                table: "Reservation",
                column: "CancelledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_ClubId_ChargeId",
                table: "Reservation",
                columns: new[] { "ClubId", "ChargeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_ClubId_Code",
                table: "Reservation",
                columns: new[] { "ClubId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_ClubId_MemberId",
                table: "Reservation",
                columns: new[] { "ClubId", "MemberId" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_ClubId_PenaltyChargeId",
                table: "Reservation",
                columns: new[] { "ClubId", "PenaltyChargeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_ClubId_RequestedByDependentId",
                table: "Reservation",
                columns: new[] { "ClubId", "RequestedByDependentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_ClubId_SpaceId_StartAt",
                table: "Reservation",
                columns: new[] { "ClubId", "SpaceId", "StartAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_ClubId_Status",
                table: "Reservation",
                columns: new[] { "ClubId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_DecisionByUserId",
                table: "Reservation",
                column: "DecisionByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_RequestedByUserId",
                table: "Reservation",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationSlot_ClubId_ReservationId",
                table: "ReservationSlot",
                columns: new[] { "ClubId", "ReservationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReservationSlot_ClubId_SpaceId",
                table: "ReservationSlot",
                columns: new[] { "ClubId", "SpaceId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReservationSlot_SpaceId_StartAt",
                table: "ReservationSlot",
                columns: new[] { "SpaceId", "StartAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Role_ClubId_Name",
                table: "Role",
                columns: new[] { "ClubId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermission_ClubId_RoleId",
                table: "RolePermission",
                columns: new[] { "ClubId", "RoleId" });

            migrationBuilder.CreateIndex(
                name: "IX_RolePermission_RoleId_Permission",
                table: "RolePermission",
                columns: new[] { "RoleId", "Permission" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Space_ClubId_ImageFileId",
                table: "Space",
                columns: new[] { "ClubId", "ImageFileId" });

            migrationBuilder.CreateIndex(
                name: "IX_SpaceBlock_ClubId_MaintenanceOrderId",
                table: "SpaceBlock",
                columns: new[] { "ClubId", "MaintenanceOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_SpaceBlock_ClubId_SpaceId",
                table: "SpaceBlock",
                columns: new[] { "ClubId", "SpaceId" });

            migrationBuilder.CreateIndex(
                name: "IX_SpaceBlock_ClubId_StartAt_EndAt",
                table: "SpaceBlock",
                columns: new[] { "ClubId", "StartAt", "EndAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SpaceBlock_CreatedByUserId",
                table: "SpaceBlock",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SpaceSchedule_ClubId_SpaceId",
                table: "SpaceSchedule",
                columns: new[] { "ClubId", "SpaceId" });

            migrationBuilder.CreateIndex(
                name: "IX_SpaceSchedule_SpaceId_DayOfWeek",
                table: "SpaceSchedule",
                columns: new[] { "SpaceId", "DayOfWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StatusHistory_ClubId",
                table: "StatusHistory",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_StatusHistory_EntityType_EntityId",
                table: "StatusHistory",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_StatusHistory_UserId",
                table: "StatusHistory",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockItem_ClubId_SupplierId",
                table: "StockItem",
                columns: new[] { "ClubId", "SupplierId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovement_ClubId_StockItemId",
                table: "StockMovement",
                columns: new[] { "ClubId", "StockItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovement_ClubId_SupplierId",
                table: "StockMovement",
                columns: new[] { "ClubId", "SupplierId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovement_UserId",
                table: "StockMovement",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredFile_ClubId_OwnerMemberId",
                table: "StoredFile",
                columns: new[] { "ClubId", "OwnerMemberId" });

            migrationBuilder.CreateIndex(
                name: "IX_StoredFile_UploadedByUserId",
                table: "StoredFile",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_User_ClubId",
                table: "User",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_User_DependentId",
                table: "User",
                column: "DependentId");

            migrationBuilder.CreateIndex(
                name: "IX_User_Email",
                table: "User",
                column: "Email",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_User_EmployeeId",
                table: "User",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_User_MemberId",
                table: "User",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermission_ClubId",
                table: "UserPermission",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermission_UserId_Permission",
                table: "UserPermission",
                columns: new[] { "UserId", "Permission" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_ClubId_RoleId",
                table: "UserRole",
                columns: new[] { "ClubId", "RoleId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_UserId_RoleId",
                table: "UserRole",
                columns: new[] { "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vote_ClubId_MemberId",
                table: "Vote",
                columns: new[] { "ClubId", "MemberId" });

            migrationBuilder.CreateIndex(
                name: "IX_Vote_ClubId_PollId",
                table: "Vote",
                columns: new[] { "ClubId", "PollId" });

            migrationBuilder.CreateIndex(
                name: "IX_Vote_ClubId_PollOptionId",
                table: "Vote",
                columns: new[] { "ClubId", "PollOptionId" });

            migrationBuilder.CreateIndex(
                name: "IX_Vote_PollId_MemberId",
                table: "Vote",
                columns: new[] { "PollId", "MemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vote_UserId",
                table: "Vote",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AccessLog_Club_ClubId",
                table: "AccessLog",
                column: "ClubId",
                principalTable: "Club",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AccessLog_Member_ClubId_ResponsibleMemberId",
                table: "AccessLog",
                columns: new[] { "ClubId", "ResponsibleMemberId" },
                principalTable: "Member",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AccessLog_QrToken_ClubId_QrTokenId",
                table: "AccessLog",
                columns: new[] { "ClubId", "QrTokenId" },
                principalTable: "QrToken",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AccessLog_User_StaffUserId",
                table: "AccessLog",
                column: "StaffUserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Announcement_ClubEvent_ClubId_EventId",
                table: "Announcement",
                columns: new[] { "ClubId", "EventId" },
                principalTable: "ClubEvent",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Announcement_Club_ClubId",
                table: "Announcement",
                column: "ClubId",
                principalTable: "Club",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Announcement_StoredFile_ClubId_ImageFileId",
                table: "Announcement",
                columns: new[] { "ClubId", "ImageFileId" },
                principalTable: "StoredFile",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Announcement_User_AuthorUserId",
                table: "Announcement",
                column: "AuthorUserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AnnouncementRead_Club_ClubId",
                table: "AnnouncementRead",
                column: "ClubId",
                principalTable: "Club",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AnnouncementRead_User_UserId",
                table: "AnnouncementRead",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AnnouncementTarget_Club_ClubId",
                table: "AnnouncementTarget",
                column: "ClubId",
                principalTable: "Club",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AnnouncementTarget_User_UserId",
                table: "AnnouncementTarget",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Assembly_Club_ClubId",
                table: "Assembly",
                column: "ClubId",
                principalTable: "Club",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Assembly_StoredFile_ClubId_MinutesFileId",
                table: "Assembly",
                columns: new[] { "ClubId", "MinutesFileId" },
                principalTable: "StoredFile",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Assembly_User_CreatedByUserId",
                table: "Assembly",
                column: "CreatedByUserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AssemblyAttendance_Club_ClubId",
                table: "AssemblyAttendance",
                column: "ClubId",
                principalTable: "Club",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AssemblyAttendance_Member_ClubId_MemberId",
                table: "AssemblyAttendance",
                columns: new[] { "ClubId", "MemberId" },
                principalTable: "Member",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AssemblyAttendance_User_RegisteredByUserId",
                table: "AssemblyAttendance",
                column: "RegisteredByUserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Asset_Club_ClubId",
                table: "Asset",
                column: "ClubId",
                principalTable: "Club",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Asset_Employee_ClubId_ResponsibleEmployeeId",
                table: "Asset",
                columns: new[] { "ClubId", "ResponsibleEmployeeId" },
                principalTable: "Employee",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Asset_Space_ClubId_SpaceId",
                table: "Asset",
                columns: new[] { "ClubId", "SpaceId" },
                principalTable: "Space",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Attachment_Club_ClubId",
                table: "Attachment",
                column: "ClubId",
                principalTable: "Club",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Attachment_StoredFile_ClubId_FileId",
                table: "Attachment",
                columns: new[] { "ClubId", "FileId" },
                principalTable: "StoredFile",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Attachment_User_UploadedByUserId",
                table: "Attachment",
                column: "UploadedByUserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLog_Club_ClubId",
                table: "AuditLog",
                column: "ClubId",
                principalTable: "Club",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLog_User_UserId",
                table: "AuditLog",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Charge_Club_ClubId",
                table: "Charge",
                column: "ClubId",
                principalTable: "Club",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Charge_Guest_ClubId_GuestId",
                table: "Charge",
                columns: new[] { "ClubId", "GuestId" },
                principalTable: "Guest",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Charge_Member_ClubId_MemberId",
                table: "Charge",
                columns: new[] { "ClubId", "MemberId" },
                principalTable: "Member",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Charge_Occurrence_ClubId_OccurrenceId",
                table: "Charge",
                columns: new[] { "ClubId", "OccurrenceId" },
                principalTable: "Occurrence",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Charge_Reservation_ClubId_ReservationId",
                table: "Charge",
                columns: new[] { "ClubId", "ReservationId" },
                principalTable: "Reservation",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Charge_User_CreatedByUserId",
                table: "Charge",
                column: "CreatedByUserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChargeAdjustment_Club_ClubId",
                table: "ChargeAdjustment",
                column: "ClubId",
                principalTable: "Club",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChargeAdjustment_User_UserId",
                table: "ChargeAdjustment",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Club_StoredFile_LogoFileId",
                table: "Club",
                column: "LogoFileId",
                principalTable: "StoredFile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClubEvent_Space_ClubId_SpaceId",
                table: "ClubEvent",
                columns: new[] { "ClubId", "SpaceId" },
                principalTable: "Space",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClubEvent_StoredFile_ClubId_ImageFileId",
                table: "ClubEvent",
                columns: new[] { "ClubId", "ImageFileId" },
                principalTable: "StoredFile",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClubEvent_User_CreatedByUserId",
                table: "ClubEvent",
                column: "CreatedByUserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Dependent_Member_ClubId_MemberId",
                table: "Dependent",
                columns: new[] { "ClubId", "MemberId" },
                principalTable: "Member",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Dependent_StoredFile_ClubId_PhotoFileId",
                table: "Dependent",
                columns: new[] { "ClubId", "PhotoFileId" },
                principalTable: "StoredFile",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceToken_User_UserId",
                table: "DeviceToken",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EventRegistration_User_UserId",
                table: "EventRegistration",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialEntry_MaintenanceOrder_ClubId_MaintenanceOrderId",
                table: "FinancialEntry",
                columns: new[] { "ClubId", "MaintenanceOrderId" },
                principalTable: "MaintenanceOrder",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialEntry_StoredFile_ClubId_ReceiptFileId",
                table: "FinancialEntry",
                columns: new[] { "ClubId", "ReceiptFileId" },
                principalTable: "StoredFile",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialEntry_User_CreatedByUserId",
                table: "FinancialEntry",
                column: "CreatedByUserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialRelease_Member_ClubId_MemberId",
                table: "FinancialRelease",
                columns: new[] { "ClubId", "MemberId" },
                principalTable: "Member",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialRelease_User_GrantedByUserId",
                table: "FinancialRelease",
                column: "GrantedByUserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialRelease_User_RevokedByUserId",
                table: "FinancialRelease",
                column: "RevokedByUserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Guest_Member_ClubId_MemberId",
                table: "Guest",
                columns: new[] { "ClubId", "MemberId" },
                principalTable: "Member",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Guest_Reservation_ClubId_ReservationId",
                table: "Guest",
                columns: new[] { "ClubId", "ReservationId" },
                principalTable: "Reservation",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Guest_Space_ClubId_SpaceId",
                table: "Guest",
                columns: new[] { "ClubId", "SpaceId" },
                principalTable: "Space",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Guest_User_AuthorizedByUserId",
                table: "Guest",
                column: "AuthorizedByUserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Guest_User_RegisteredByUserId",
                table: "Guest",
                column: "RegisteredByUserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Inspection_Member_ClubId_MemberId",
                table: "Inspection",
                columns: new[] { "ClubId", "MemberId" },
                principalTable: "Member",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Inspection_Reservation_ClubId_ReservationId",
                table: "Inspection",
                columns: new[] { "ClubId", "ReservationId" },
                principalTable: "Reservation",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Inspection_Space_ClubId_SpaceId",
                table: "Inspection",
                columns: new[] { "ClubId", "SpaceId" },
                principalTable: "Space",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Inspection_User_InspectorUserId",
                table: "Inspection",
                column: "InspectorUserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KeyHandover_Member_ClubId_MemberId",
                table: "KeyHandover",
                columns: new[] { "ClubId", "MemberId" },
                principalTable: "Member",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KeyHandover_Reservation_ClubId_ReservationId",
                table: "KeyHandover",
                columns: new[] { "ClubId", "ReservationId" },
                principalTable: "Reservation",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KeyHandover_Space_ClubId_SpaceId",
                table: "KeyHandover",
                columns: new[] { "ClubId", "SpaceId" },
                principalTable: "Space",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KeyHandover_User_DeliveredByUserId",
                table: "KeyHandover",
                column: "DeliveredByUserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KeyHandover_User_ReceivedByUserId",
                table: "KeyHandover",
                column: "ReceivedByUserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LoginHistory_User_UserId",
                table: "LoginHistory",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceOrder_Space_ClubId_SpaceId",
                table: "MaintenanceOrder",
                columns: new[] { "ClubId", "SpaceId" },
                principalTable: "Space",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceOrder_User_OpenedByUserId",
                table: "MaintenanceOrder",
                column: "OpenedByUserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Manifestation_Member_ClubId_MemberId",
                table: "Manifestation",
                columns: new[] { "ClubId", "MemberId" },
                principalTable: "Member",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Manifestation_StoredFile_ClubId_PhotoFileId",
                table: "Manifestation",
                columns: new[] { "ClubId", "PhotoFileId" },
                principalTable: "StoredFile",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Manifestation_User_UserId",
                table: "Manifestation",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ManifestationUpdate_User_UserId",
                table: "ManifestationUpdate",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Member_StoredFile_ClubId_PhotoFileId",
                table: "Member",
                columns: new[] { "ClubId", "PhotoFileId" },
                principalTable: "StoredFile",
                principalColumns: new[] { "ClubId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Charge_Club_ClubId",
                table: "Charge");

            migrationBuilder.DropForeignKey(
                name: "FK_Dependent_Club_ClubId",
                table: "Dependent");

            migrationBuilder.DropForeignKey(
                name: "FK_Employee_Club_ClubId",
                table: "Employee");

            migrationBuilder.DropForeignKey(
                name: "FK_Guest_Club_ClubId",
                table: "Guest");

            migrationBuilder.DropForeignKey(
                name: "FK_Inspection_Club_ClubId",
                table: "Inspection");

            migrationBuilder.DropForeignKey(
                name: "FK_Member_Club_ClubId",
                table: "Member");

            migrationBuilder.DropForeignKey(
                name: "FK_Occurrence_Club_ClubId",
                table: "Occurrence");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservation_Club_ClubId",
                table: "Reservation");

            migrationBuilder.DropForeignKey(
                name: "FK_Space_Club_ClubId",
                table: "Space");

            migrationBuilder.DropForeignKey(
                name: "FK_StoredFile_Club_ClubId",
                table: "StoredFile");

            migrationBuilder.DropForeignKey(
                name: "FK_User_Club_ClubId",
                table: "User");

            migrationBuilder.DropForeignKey(
                name: "FK_Charge_Member_ClubId_MemberId",
                table: "Charge");

            migrationBuilder.DropForeignKey(
                name: "FK_Dependent_Member_ClubId_MemberId",
                table: "Dependent");

            migrationBuilder.DropForeignKey(
                name: "FK_Guest_Member_ClubId_MemberId",
                table: "Guest");

            migrationBuilder.DropForeignKey(
                name: "FK_Inspection_Member_ClubId_MemberId",
                table: "Inspection");

            migrationBuilder.DropForeignKey(
                name: "FK_Occurrence_Member_ClubId_MemberId",
                table: "Occurrence");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservation_Member_ClubId_MemberId",
                table: "Reservation");

            migrationBuilder.DropForeignKey(
                name: "FK_StoredFile_Member_ClubId_OwnerMemberId",
                table: "StoredFile");

            migrationBuilder.DropForeignKey(
                name: "FK_User_Member_MemberId",
                table: "User");

            migrationBuilder.DropForeignKey(
                name: "FK_Charge_User_CreatedByUserId",
                table: "Charge");

            migrationBuilder.DropForeignKey(
                name: "FK_Guest_User_AuthorizedByUserId",
                table: "Guest");

            migrationBuilder.DropForeignKey(
                name: "FK_Guest_User_RegisteredByUserId",
                table: "Guest");

            migrationBuilder.DropForeignKey(
                name: "FK_Inspection_User_InspectorUserId",
                table: "Inspection");

            migrationBuilder.DropForeignKey(
                name: "FK_Occurrence_User_ChargeDecisionByUserId",
                table: "Occurrence");

            migrationBuilder.DropForeignKey(
                name: "FK_Occurrence_User_ReportedByUserId",
                table: "Occurrence");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservation_User_CancelledByUserId",
                table: "Reservation");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservation_User_DecisionByUserId",
                table: "Reservation");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservation_User_RequestedByUserId",
                table: "Reservation");

            migrationBuilder.DropForeignKey(
                name: "FK_StoredFile_User_UploadedByUserId",
                table: "StoredFile");

            migrationBuilder.DropForeignKey(
                name: "FK_Dependent_StoredFile_ClubId_PhotoFileId",
                table: "Dependent");

            migrationBuilder.DropForeignKey(
                name: "FK_Space_StoredFile_ClubId_ImageFileId",
                table: "Space");

            migrationBuilder.DropForeignKey(
                name: "FK_Guest_Space_ClubId_SpaceId",
                table: "Guest");

            migrationBuilder.DropForeignKey(
                name: "FK_Inspection_Space_ClubId_SpaceId",
                table: "Inspection");

            migrationBuilder.DropForeignKey(
                name: "FK_Occurrence_Space_ClubId_SpaceId",
                table: "Occurrence");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservation_Space_ClubId_SpaceId",
                table: "Reservation");

            migrationBuilder.DropForeignKey(
                name: "FK_Charge_Guest_ClubId_GuestId",
                table: "Charge");

            migrationBuilder.DropForeignKey(
                name: "FK_Charge_Occurrence_ClubId_OccurrenceId",
                table: "Charge");

            migrationBuilder.DropForeignKey(
                name: "FK_Charge_Reservation_ClubId_ReservationId",
                table: "Charge");

            migrationBuilder.DropTable(
                name: "AccessLog");

            migrationBuilder.DropTable(
                name: "AnnouncementRead");

            migrationBuilder.DropTable(
                name: "AnnouncementTarget");

            migrationBuilder.DropTable(
                name: "AssemblyAttendance");

            migrationBuilder.DropTable(
                name: "Attachment");

            migrationBuilder.DropTable(
                name: "AuditLog");

            migrationBuilder.DropTable(
                name: "ChargeAdjustment");

            migrationBuilder.DropTable(
                name: "ClubOpeningHour");

            migrationBuilder.DropTable(
                name: "ClubSequence");

            migrationBuilder.DropTable(
                name: "ClubSettings");

            migrationBuilder.DropTable(
                name: "DependentPermission");

            migrationBuilder.DropTable(
                name: "DependentPricingRule");

            migrationBuilder.DropTable(
                name: "DeviceToken");

            migrationBuilder.DropTable(
                name: "DigitalCard");

            migrationBuilder.DropTable(
                name: "EventRegistration");

            migrationBuilder.DropTable(
                name: "FinancialEntry");

            migrationBuilder.DropTable(
                name: "FinancialRelease");

            migrationBuilder.DropTable(
                name: "Holiday");

            migrationBuilder.DropTable(
                name: "InspectionItem");

            migrationBuilder.DropTable(
                name: "KeyHandover");

            migrationBuilder.DropTable(
                name: "LoginHistory");

            migrationBuilder.DropTable(
                name: "ManifestationUpdate");

            migrationBuilder.DropTable(
                name: "MonthlyClosing");

            migrationBuilder.DropTable(
                name: "Notification");

            migrationBuilder.DropTable(
                name: "NotificationPreference");

            migrationBuilder.DropTable(
                name: "PasswordResetToken");

            migrationBuilder.DropTable(
                name: "Payment");

            migrationBuilder.DropTable(
                name: "Presence");

            migrationBuilder.DropTable(
                name: "RefreshToken");

            migrationBuilder.DropTable(
                name: "ReservationSlot");

            migrationBuilder.DropTable(
                name: "RolePermission");

            migrationBuilder.DropTable(
                name: "SpaceBlock");

            migrationBuilder.DropTable(
                name: "SpaceSchedule");

            migrationBuilder.DropTable(
                name: "StatusHistory");

            migrationBuilder.DropTable(
                name: "StockMovement");

            migrationBuilder.DropTable(
                name: "UserPermission");

            migrationBuilder.DropTable(
                name: "UserRole");

            migrationBuilder.DropTable(
                name: "Vote");

            migrationBuilder.DropTable(
                name: "QrToken");

            migrationBuilder.DropTable(
                name: "Announcement");

            migrationBuilder.DropTable(
                name: "Manifestation");

            migrationBuilder.DropTable(
                name: "MaintenanceOrder");

            migrationBuilder.DropTable(
                name: "StockItem");

            migrationBuilder.DropTable(
                name: "Role");

            migrationBuilder.DropTable(
                name: "PollOption");

            migrationBuilder.DropTable(
                name: "ClubEvent");

            migrationBuilder.DropTable(
                name: "ManifestationType");

            migrationBuilder.DropTable(
                name: "Asset");

            migrationBuilder.DropTable(
                name: "Supplier");

            migrationBuilder.DropTable(
                name: "Poll");

            migrationBuilder.DropTable(
                name: "Assembly");

            migrationBuilder.DropTable(
                name: "Club");

            migrationBuilder.DropTable(
                name: "Member");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Employee");

            migrationBuilder.DropTable(
                name: "StoredFile");

            migrationBuilder.DropTable(
                name: "Space");

            migrationBuilder.DropTable(
                name: "Guest");

            migrationBuilder.DropTable(
                name: "Occurrence");

            migrationBuilder.DropTable(
                name: "Inspection");

            migrationBuilder.DropTable(
                name: "Reservation");

            migrationBuilder.DropTable(
                name: "Charge");

            migrationBuilder.DropTable(
                name: "Dependent");
        }
    }
}
