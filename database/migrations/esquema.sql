CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE EXTENSION IF NOT EXISTS btree_gist;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "AccessLog" (
        "Id" uuid NOT NULL,
        "PersonType" character varying(40),
        "PersonId" uuid,
        "PersonName" text,
        "ResponsibleMemberId" uuid,
        "Operation" character varying(40) NOT NULL,
        "Result" character varying(40) NOT NULL,
        "DenialReason" text,
        "StaffUserId" uuid,
        "Origin" text,
        "QrTokenId" uuid,
        "OccurredAt" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_AccessLog" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "Announcement" (
        "Id" uuid NOT NULL,
        "Title" text NOT NULL,
        "Body" text NOT NULL,
        "Category" character varying(40) NOT NULL,
        "Priority" character varying(40) NOT NULL,
        "AudienceMembers" boolean NOT NULL,
        "AudienceDependents" boolean NOT NULL,
        "AudienceStaff" boolean NOT NULL,
        "IsSpecific" boolean NOT NULL,
        "ImageFileId" uuid,
        "EventId" uuid,
        "PublishedAt" timestamp with time zone,
        "ExpiresAt" timestamp with time zone,
        "AuthorUserId" uuid NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_Announcement" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_Announcement_ClubId_Id" UNIQUE ("ClubId", "Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "AnnouncementRead" (
        "Id" uuid NOT NULL,
        "AnnouncementId" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_AnnouncementRead" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AnnouncementRead_Announcement_ClubId_AnnouncementId" FOREIGN KEY ("ClubId", "AnnouncementId") REFERENCES "Announcement" ("ClubId", "Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "AnnouncementTarget" (
        "Id" uuid NOT NULL,
        "AnnouncementId" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_AnnouncementTarget" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AnnouncementTarget_Announcement_ClubId_AnnouncementId" FOREIGN KEY ("ClubId", "AnnouncementId") REFERENCES "Announcement" ("ClubId", "Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "Assembly" (
        "Id" uuid NOT NULL,
        "Title" text NOT NULL,
        "ScheduledAt" timestamp with time zone NOT NULL,
        "Location" text,
        "Agenda" text,
        "Status" character varying(40) NOT NULL,
        "Minutes" text,
        "MinutesFileId" uuid,
        "CreatedByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_Assembly" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_Assembly_ClubId_Id" UNIQUE ("ClubId", "Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "AssemblyAttendance" (
        "Id" uuid NOT NULL,
        "AssemblyId" uuid NOT NULL,
        "MemberId" uuid NOT NULL,
        "RegisteredByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_AssemblyAttendance" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AssemblyAttendance_Assembly_ClubId_AssemblyId" FOREIGN KEY ("ClubId", "AssemblyId") REFERENCES "Assembly" ("ClubId", "Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "Asset" (
        "Id" uuid NOT NULL,
        "Code" text NOT NULL,
        "Name" text NOT NULL,
        "Category" text,
        "Description" text,
        "Location" text,
        "SpaceId" uuid,
        "Condition" character varying(40) NOT NULL,
        "AcquiredOn" date,
        "Value" numeric(14,2),
        "ResponsibleEmployeeId" uuid,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_Asset" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_Asset_ClubId_Id" UNIQUE ("ClubId", "Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "Attachment" (
        "Id" uuid NOT NULL,
        "OwnerType" text NOT NULL,
        "OwnerId" uuid NOT NULL,
        "FileId" uuid NOT NULL,
        "Title" text,
        "Category" text,
        "UploadedByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_Attachment" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "AuditLog" (
        "Id" uuid NOT NULL,
        "ClubId" uuid,
        "UserId" uuid,
        "UserName" text,
        "Operation" text NOT NULL,
        "EntityType" text NOT NULL,
        "EntityId" text,
        "BeforeJson" jsonb,
        "AfterJson" jsonb,
        "Origin" text,
        "Justification" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_AuditLog" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "Charge" (
        "Id" uuid NOT NULL,
        "MemberId" uuid NOT NULL,
        "Type" character varying(40) NOT NULL,
        "Description" text NOT NULL,
        "Competence" text NOT NULL,
        "DueDate" date NOT NULL,
        "DelinquentFrom" date NOT NULL,
        "BaseFee" numeric(14,2) NOT NULL,
        "DependentsCount" integer NOT NULL,
        "DependentsAmount" numeric(14,2) NOT NULL,
        "CompositionSnapshot" jsonb,
        "OriginalAmount" numeric(14,2) NOT NULL,
        "Additions" numeric(14,2) NOT NULL,
        "Discounts" numeric(14,2) NOT NULL,
        "FineAmount" numeric(14,2) NOT NULL,
        "InterestAmount" numeric(14,2) NOT NULL,
        "FeesAmount" numeric(14,2) NOT NULL,
        "PaidAmount" numeric(14,2) NOT NULL,
        "GraceDaysSnapshot" integer NOT NULL,
        "LateFinePercentSnapshot" numeric(9,4),
        "DailyInterestPercentSnapshot" numeric(9,4),
        "Status" character varying(40) NOT NULL,
        "CancelReason" text,
        "ReservationId" uuid,
        "GuestId" uuid,
        "OccurrenceId" uuid,
        "CreatedByUserId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_Charge" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_Charge_ClubId_Id" UNIQUE ("ClubId", "Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "ChargeAdjustment" (
        "Id" uuid NOT NULL,
        "ChargeId" uuid NOT NULL,
        "Kind" character varying(40) NOT NULL,
        "Amount" numeric(14,2) NOT NULL,
        "Reason" text NOT NULL,
        "UserId" uuid,
        "IsAutomatic" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_ChargeAdjustment" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ChargeAdjustment_Charge_ClubId_ChargeId" FOREIGN KEY ("ClubId", "ChargeId") REFERENCES "Charge" ("ClubId", "Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "Club" (
        "Id" uuid NOT NULL,
        "Name" text NOT NULL,
        "LegalName" text,
        "Cnpj" text,
        "Slug" text NOT NULL,
        "Email" text,
        "Phone" text,
        "ZipCode" text,
        "Street" text,
        "Number" text,
        "Complement" text,
        "District" text,
        "City" text,
        "State" text,
        "LogoFileId" uuid,
        "About" text,
        "TimeZoneId" text NOT NULL,
        "IsActive" boolean NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_Club" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "ClubOpeningHour" (
        "Id" uuid NOT NULL,
        "DayOfWeek" character varying(40) NOT NULL,
        "IsClosed" boolean NOT NULL,
        "OpenMinute" integer NOT NULL,
        "CloseMinute" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_ClubOpeningHour" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ClubOpeningHour_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "ClubSequence" (
        "ClubId" uuid NOT NULL,
        "Name" text NOT NULL,
        "Value" bigint NOT NULL,
        CONSTRAINT "PK_ClubSequence" PRIMARY KEY ("ClubId", "Name"),
        CONSTRAINT "FK_ClubSequence_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "ClubSettings" (
        "Id" uuid NOT NULL,
        "MonthlyBaseFee" numeric(14,2),
        "DueDay" integer,
        "GraceDays" integer,
        "LateFinePercent" numeric(9,4),
        "DailyInterestPercent" numeric(9,4),
        "DependentPricingMode" character varying(40) NOT NULL,
        "DependentFixedAmount" numeric(14,2),
        "BlockAccessWhenDelinquent" boolean,
        "BlockReservationsWhenDelinquent" boolean,
        "DelinquentReservationPolicy" character varying(40),
        "AllowAdvancePayment" boolean,
        "FamilyCompositionChangeRule" text,
        "AutoGenerateMonthlyCharges" boolean NOT NULL,
        "DueReminderDays" integer NOT NULL,
        "MaxDependentsPerMember" integer NOT NULL,
        "MaxGuestsPerMember" integer NOT NULL,
        "MemberCanToggleDependentPermissions" boolean NOT NULL,
        "MaxAdvanceDays" integer,
        "MaxConcurrentReservations" integer,
        "FreeCancellationHours" integer,
        "LateCancellationFee" numeric(14,2),
        "NoShowFee" numeric(14,2),
        "RequireAdvancePayment" boolean,
        "ReservationApprovalMode" character varying(40),
        "GuestFee" numeric(14,2),
        "GuestAdvanceRegistrationRequired" boolean,
        "GuestDocumentRequired" boolean,
        "GuestEntryWithoutMemberAllowed" boolean,
        "QrTokenTtlSeconds" integer NOT NULL,
        "HolidayRules" text,
        "MinorsRules" text,
        "MinorsAccompanimentRules" text,
        "PoolPolicy" text,
        "EventProcedures" text,
        "SpaceUsagePolicy" text,
        "PrivacyPolicy" text,
        "PrivacyPolicyVersion" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_ClubSettings" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ClubSettings_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "DependentPricingRule" (
        "Id" uuid NOT NULL,
        "FromCount" integer NOT NULL,
        "ToCount" integer NOT NULL,
        "Amount" numeric(14,2) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_DependentPricingRule" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_DependentPricingRule_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "DigitalCard" (
        "Id" uuid NOT NULL,
        "PersonType" character varying(40) NOT NULL,
        "PersonId" uuid NOT NULL,
        "CardNumber" text NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_DigitalCard" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_DigitalCard_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "Employee" (
        "Id" uuid NOT NULL,
        "FullName" text NOT NULL,
        "Cpf" text NOT NULL,
        "Phone" text,
        "Email" text,
        "Position" text,
        "Department" text,
        "HiredAt" date,
        "Status" character varying(40) NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_Employee" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_Employee_ClubId_Id" UNIQUE ("ClubId", "Id"),
        CONSTRAINT "FK_Employee_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "Holiday" (
        "Id" uuid NOT NULL,
        "Date" date NOT NULL,
        "Name" text NOT NULL,
        "Scope" character varying(40) NOT NULL,
        "IsClosed" boolean NOT NULL,
        "OpenMinute" integer,
        "CloseMinute" integer,
        "Notes" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_Holiday" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Holiday_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "ManifestationType" (
        "Id" uuid NOT NULL,
        "Name" text NOT NULL,
        "IsSuggestion" boolean NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_ManifestationType" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_ManifestationType_ClubId_Id" UNIQUE ("ClubId", "Id"),
        CONSTRAINT "FK_ManifestationType_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "Role" (
        "Id" uuid NOT NULL,
        "Name" text NOT NULL,
        "Description" text,
        "IsSystem" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_Role" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_Role_ClubId_Id" UNIQUE ("ClubId", "Id"),
        CONSTRAINT "FK_Role_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "Supplier" (
        "Id" uuid NOT NULL,
        "Name" text NOT NULL,
        "Document" text,
        "Phone" text,
        "Email" text,
        "Notes" text,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_Supplier" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_Supplier_ClubId_Id" UNIQUE ("ClubId", "Id"),
        CONSTRAINT "FK_Supplier_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "RolePermission" (
        "Id" uuid NOT NULL,
        "RoleId" uuid NOT NULL,
        "Permission" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_RolePermission" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_RolePermission_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_RolePermission_Role_ClubId_RoleId" FOREIGN KEY ("ClubId", "RoleId") REFERENCES "Role" ("ClubId", "Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "StockItem" (
        "Id" uuid NOT NULL,
        "Name" text NOT NULL,
        "Category" text,
        "Unit" text NOT NULL,
        "Quantity" numeric(14,3) NOT NULL,
        "MinQuantity" numeric(14,3) NOT NULL,
        "SupplierId" uuid,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_StockItem" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_StockItem_ClubId_Id" UNIQUE ("ClubId", "Id"),
        CONSTRAINT "FK_StockItem_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_StockItem_Supplier_ClubId_SupplierId" FOREIGN KEY ("ClubId", "SupplierId") REFERENCES "Supplier" ("ClubId", "Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "ClubEvent" (
        "Id" uuid NOT NULL,
        "Title" text NOT NULL,
        "Description" text,
        "ImageFileId" uuid,
        "StartAt" timestamp with time zone NOT NULL,
        "EndAt" timestamp with time zone NOT NULL,
        "Location" text,
        "SpaceId" uuid,
        "Capacity" integer,
        "Audience" character varying(40) NOT NULL,
        "AllowMembers" boolean NOT NULL,
        "AllowDependents" boolean NOT NULL,
        "AllowStaff" boolean NOT NULL,
        "RegistrationRequired" boolean NOT NULL,
        "RegistrationDeadline" timestamp with time zone,
        "CancellationDeadline" timestamp with time zone,
        "Status" character varying(40) NOT NULL,
        "CancelReason" text,
        "CreatedByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_ClubEvent" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_ClubEvent_ClubId_Id" UNIQUE ("ClubId", "Id"),
        CONSTRAINT "FK_ClubEvent_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "Dependent" (
        "Id" uuid NOT NULL,
        "MemberId" uuid NOT NULL,
        "FullName" text NOT NULL,
        "Cpf" text,
        "Rg" text,
        "BirthDate" date,
        "Gender" text,
        "Relationship" text NOT NULL,
        "PhotoFileId" uuid,
        "Email" text,
        "Phone" text,
        "Status" character varying(40) NOT NULL,
        "StatusReason" text,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_Dependent" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_Dependent_ClubId_Id" UNIQUE ("ClubId", "Id"),
        CONSTRAINT "FK_Dependent_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "DependentPermission" (
        "Id" uuid NOT NULL,
        "DependentId" uuid NOT NULL,
        "Code" text NOT NULL,
        "AllowedByAdmin" boolean NOT NULL,
        "EnabledByMember" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_DependentPermission" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_DependentPermission_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_DependentPermission_Dependent_ClubId_DependentId" FOREIGN KEY ("ClubId", "DependentId") REFERENCES "Dependent" ("ClubId", "Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "DeviceToken" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Token" text NOT NULL,
        "Platform" text NOT NULL,
        "LastSeenAt" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_DeviceToken" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_DeviceToken_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "EventRegistration" (
        "Id" uuid NOT NULL,
        "EventId" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "PersonName" text NOT NULL,
        "PersonKind" character varying(40) NOT NULL,
        "Status" character varying(40) NOT NULL,
        "Attended" boolean,
        "CancelledAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_EventRegistration" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_EventRegistration_ClubEvent_ClubId_EventId" FOREIGN KEY ("ClubId", "EventId") REFERENCES "ClubEvent" ("ClubId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_EventRegistration_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "FinancialEntry" (
        "Id" uuid NOT NULL,
        "Kind" character varying(40) NOT NULL,
        "Category" text NOT NULL,
        "Description" text NOT NULL,
        "Counterparty" text,
        "Amount" numeric(14,2) NOT NULL,
        "DueDate" date NOT NULL,
        "PaidOn" date,
        "Status" character varying(40) NOT NULL,
        "Notes" text,
        "CancelReason" text,
        "MaintenanceOrderId" uuid,
        "ReceiptFileId" uuid,
        "CreatedByUserId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_FinancialEntry" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_FinancialEntry_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "FinancialRelease" (
        "Id" uuid NOT NULL,
        "MemberId" uuid NOT NULL,
        "Reason" text NOT NULL,
        "ValidUntil" date NOT NULL,
        "GrantedByUserId" uuid NOT NULL,
        "RevokedAt" timestamp with time zone,
        "RevokedByUserId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_FinancialRelease" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_FinancialRelease_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "Guest" (
        "Id" uuid NOT NULL,
        "MemberId" uuid NOT NULL,
        "FullName" text NOT NULL,
        "Document" text,
        "Phone" text,
        "VisitDate" date NOT NULL,
        "SpaceId" uuid,
        "ReservationId" uuid,
        "FeeAmount" numeric(14,2) NOT NULL,
        "ChargeId" uuid,
        "Authorization" character varying(40) NOT NULL,
        "AuthorizedByUserId" uuid,
        "AuthorizationNote" text,
        "RegisteredByUserId" uuid NOT NULL,
        "EnteredAt" timestamp with time zone,
        "ExitedAt" timestamp with time zone,
        "IsCancelled" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_Guest" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_Guest_ClubId_Id" UNIQUE ("ClubId", "Id"),
        CONSTRAINT "FK_Guest_Charge_ClubId_ChargeId" FOREIGN KEY ("ClubId", "ChargeId") REFERENCES "Charge" ("ClubId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Guest_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "Inspection" (
        "Id" uuid NOT NULL,
        "SpaceId" uuid NOT NULL,
        "ReservationId" uuid,
        "MemberId" uuid,
        "Moment" character varying(40) NOT NULL,
        "InspectorUserId" uuid NOT NULL,
        "PerformedAt" timestamp with time zone NOT NULL,
        "Notes" text,
        "HasDamage" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_Inspection" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_Inspection_ClubId_Id" UNIQUE ("ClubId", "Id"),
        CONSTRAINT "FK_Inspection_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "InspectionItem" (
        "Id" uuid NOT NULL,
        "InspectionId" uuid NOT NULL,
        "Category" character varying(40) NOT NULL,
        "Description" text NOT NULL,
        "IsOk" boolean NOT NULL,
        "IsDamage" boolean NOT NULL,
        "Notes" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_InspectionItem" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_InspectionItem_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_InspectionItem_Inspection_ClubId_InspectionId" FOREIGN KEY ("ClubId", "InspectionId") REFERENCES "Inspection" ("ClubId", "Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "KeyHandover" (
        "Id" uuid NOT NULL,
        "ReservationId" uuid NOT NULL,
        "SpaceId" uuid NOT NULL,
        "MemberId" uuid,
        "ReceiverName" text NOT NULL,
        "DeliveredByUserId" uuid NOT NULL,
        "PickedUpAt" timestamp with time zone NOT NULL,
        "ExpectedReturnAt" timestamp with time zone NOT NULL,
        "ConditionOnDelivery" text,
        "ReturnedAt" timestamp with time zone,
        "ReceivedByUserId" uuid,
        "ConditionOnReturn" text,
        "Pendencies" text,
        "PendenciesResolved" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_KeyHandover" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_KeyHandover_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "LoginHistory" (
        "Id" uuid NOT NULL,
        "ClubId" uuid,
        "UserId" uuid,
        "Email" text NOT NULL,
        "Success" boolean NOT NULL,
        "Reason" text,
        "Ip" text,
        "UserAgent" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_LoginHistory" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_LoginHistory_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "MaintenanceOrder" (
        "Id" uuid NOT NULL,
        "Code" text NOT NULL,
        "Title" text NOT NULL,
        "Description" text,
        "Kind" character varying(40) NOT NULL,
        "SpaceId" uuid,
        "AssetId" uuid,
        "ResponsibleEmployeeId" uuid,
        "ResponsibleName" text,
        "OpenedAt" timestamp with time zone NOT NULL,
        "DueDate" date,
        "Status" character varying(40) NOT NULL,
        "EstimatedCost" numeric(14,2),
        "ActualCost" numeric(14,2),
        "CompletedAt" timestamp with time zone,
        "OpenedByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_MaintenanceOrder" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_MaintenanceOrder_ClubId_Id" UNIQUE ("ClubId", "Id"),
        CONSTRAINT "FK_MaintenanceOrder_Asset_ClubId_AssetId" FOREIGN KEY ("ClubId", "AssetId") REFERENCES "Asset" ("ClubId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_MaintenanceOrder_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_MaintenanceOrder_Employee_ClubId_ResponsibleEmployeeId" FOREIGN KEY ("ClubId", "ResponsibleEmployeeId") REFERENCES "Employee" ("ClubId", "Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "Manifestation" (
        "Id" uuid NOT NULL,
        "Protocol" text NOT NULL,
        "TypeId" uuid NOT NULL,
        "Description" text NOT NULL,
        "PhotoFileId" uuid,
        "UserId" uuid NOT NULL,
        "AuthorName" text NOT NULL,
        "AuthorKind" character varying(40) NOT NULL,
        "MemberId" uuid,
        "Status" character varying(40) NOT NULL,
        "Classification" text,
        "AssemblyId" uuid,
        "ClosedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_Manifestation" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_Manifestation_ClubId_Id" UNIQUE ("ClubId", "Id"),
        CONSTRAINT "FK_Manifestation_Assembly_ClubId_AssemblyId" FOREIGN KEY ("ClubId", "AssemblyId") REFERENCES "Assembly" ("ClubId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Manifestation_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Manifestation_ManifestationType_ClubId_TypeId" FOREIGN KEY ("ClubId", "TypeId") REFERENCES "ManifestationType" ("ClubId", "Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "ManifestationUpdate" (
        "Id" uuid NOT NULL,
        "ManifestationId" uuid NOT NULL,
        "Status" character varying(40) NOT NULL,
        "Message" text,
        "IsVisibleToAuthor" boolean NOT NULL,
        "UserId" uuid NOT NULL,
        "UserName" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_ManifestationUpdate" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ManifestationUpdate_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_ManifestationUpdate_Manifestation_ClubId_ManifestationId" FOREIGN KEY ("ClubId", "ManifestationId") REFERENCES "Manifestation" ("ClubId", "Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "Member" (
        "Id" uuid NOT NULL,
        "Number" integer NOT NULL,
        "FullName" text NOT NULL,
        "Cpf" text NOT NULL,
        "Rg" text,
        "BirthDate" date,
        "Gender" text,
        "MaritalStatus" text,
        "Email" text,
        "Phone" text,
        "ZipCode" text,
        "Street" text,
        "AddressNumber" text,
        "Complement" text,
        "District" text,
        "City" text,
        "State" text,
        "PhotoFileId" uuid,
        "JoinedAt" date,
        "Status" character varying(40) NOT NULL,
        "Notes" text,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_Member" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_Member_ClubId_Id" UNIQUE ("ClubId", "Id"),
        CONSTRAINT "FK_Member_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "Presence" (
        "Id" uuid NOT NULL,
        "PersonType" character varying(40) NOT NULL,
        "PersonId" uuid NOT NULL,
        "PersonName" text NOT NULL,
        "ResponsibleMemberId" uuid,
        "EnteredAt" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_Presence" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Presence_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Presence_Member_ClubId_ResponsibleMemberId" FOREIGN KEY ("ClubId", "ResponsibleMemberId") REFERENCES "Member" ("ClubId", "Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "User" (
        "Id" uuid NOT NULL,
        "ClubId" uuid,
        "Name" text NOT NULL,
        "Email" text NOT NULL,
        "PasswordHash" text NOT NULL,
        "Kind" character varying(40) NOT NULL,
        "MemberId" uuid,
        "DependentId" uuid,
        "EmployeeId" uuid,
        "IsBlocked" boolean NOT NULL,
        "BlockReason" text,
        "MustChangePassword" boolean NOT NULL,
        "FailedLoginCount" integer NOT NULL,
        "LockoutUntil" timestamp with time zone,
        "LastLoginAt" timestamp with time zone,
        "PrivacyAcceptedAt" timestamp with time zone,
        "PrivacyAcceptedVersion" text,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_User" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_User_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_User_Dependent_DependentId" FOREIGN KEY ("DependentId") REFERENCES "Dependent" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_User_Employee_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employee" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_User_Member_MemberId" FOREIGN KEY ("MemberId") REFERENCES "Member" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "MonthlyClosing" (
        "Id" uuid NOT NULL,
        "Competence" text NOT NULL,
        "ChargesIssued" numeric(14,2) NOT NULL,
        "PaymentsReceived" numeric(14,2) NOT NULL,
        "OtherRevenue" numeric(14,2) NOT NULL,
        "Expenses" numeric(14,2) NOT NULL,
        "OpenBalance" numeric(14,2) NOT NULL,
        "Notes" text,
        "ClosedByUserId" uuid NOT NULL,
        "ClosedAt" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_MonthlyClosing" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_MonthlyClosing_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_MonthlyClosing_User_ClosedByUserId" FOREIGN KEY ("ClosedByUserId") REFERENCES "User" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "Notification" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Category" character varying(40) NOT NULL,
        "Type" text NOT NULL,
        "Title" text NOT NULL,
        "Body" text NOT NULL,
        "EntityType" text,
        "EntityId" uuid,
        "IsMandatory" boolean NOT NULL,
        "ReadAt" timestamp with time zone,
        "PushStatus" character varying(40) NOT NULL,
        "PushSentAt" timestamp with time zone,
        "EmailStatus" character varying(40) NOT NULL,
        "EmailSentAt" timestamp with time zone,
        "DeliveryError" text,
        "DispatchedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_Notification" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Notification_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Notification_User_UserId" FOREIGN KEY ("UserId") REFERENCES "User" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "NotificationPreference" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Category" character varying(40) NOT NULL,
        "InAppEnabled" boolean NOT NULL,
        "PushEnabled" boolean NOT NULL,
        "EmailEnabled" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_NotificationPreference" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_NotificationPreference_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_NotificationPreference_User_UserId" FOREIGN KEY ("UserId") REFERENCES "User" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "PasswordResetToken" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "TokenHash" text NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "UsedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_PasswordResetToken" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PasswordResetToken_User_UserId" FOREIGN KEY ("UserId") REFERENCES "User" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "Poll" (
        "Id" uuid NOT NULL,
        "AssemblyId" uuid,
        "Title" text NOT NULL,
        "Description" text,
        "OpensAt" timestamp with time zone,
        "ClosesAt" timestamp with time zone,
        "Status" character varying(40) NOT NULL,
        "Criterion" character varying(40) NOT NULL,
        "ResultsPublished" boolean NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_Poll" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_Poll_ClubId_Id" UNIQUE ("ClubId", "Id"),
        CONSTRAINT "FK_Poll_Assembly_ClubId_AssemblyId" FOREIGN KEY ("ClubId", "AssemblyId") REFERENCES "Assembly" ("ClubId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Poll_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Poll_User_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") REFERENCES "User" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "QrToken" (
        "Id" uuid NOT NULL,
        "PersonType" character varying(40) NOT NULL,
        "PersonId" uuid NOT NULL,
        "Operation" character varying(40) NOT NULL,
        "TokenHash" text NOT NULL,
        "DisplayCode" text NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "ConsumedAt" timestamp with time zone,
        "ConsumedByUserId" uuid,
        "IssuedToUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_QrToken" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_QrToken_ClubId_Id" UNIQUE ("ClubId", "Id"),
        CONSTRAINT "FK_QrToken_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_QrToken_User_ConsumedByUserId" FOREIGN KEY ("ConsumedByUserId") REFERENCES "User" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_QrToken_User_IssuedToUserId" FOREIGN KEY ("IssuedToUserId") REFERENCES "User" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "RefreshToken" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "TokenHash" text NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "RevokedAt" timestamp with time zone,
        "RevokedReason" text,
        "ReplacedByHash" text,
        "Device" text,
        "Ip" text,
        "LastUsedAt" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_RefreshToken" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_RefreshToken_User_UserId" FOREIGN KEY ("UserId") REFERENCES "User" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "StatusHistory" (
        "Id" uuid NOT NULL,
        "EntityType" text NOT NULL,
        "EntityId" uuid NOT NULL,
        "FromStatus" text,
        "ToStatus" text NOT NULL,
        "Note" text,
        "UserId" uuid,
        "UserName" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_StatusHistory" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_StatusHistory_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_StatusHistory_User_UserId" FOREIGN KEY ("UserId") REFERENCES "User" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "StockMovement" (
        "Id" uuid NOT NULL,
        "StockItemId" uuid NOT NULL,
        "Kind" character varying(40) NOT NULL,
        "Quantity" numeric(14,3) NOT NULL,
        "BalanceAfter" numeric(14,3) NOT NULL,
        "UnitCost" numeric(14,2),
        "Reason" text,
        "SupplierId" uuid,
        "UserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_StockMovement" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_StockMovement_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_StockMovement_StockItem_ClubId_StockItemId" FOREIGN KEY ("ClubId", "StockItemId") REFERENCES "StockItem" ("ClubId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_StockMovement_Supplier_ClubId_SupplierId" FOREIGN KEY ("ClubId", "SupplierId") REFERENCES "Supplier" ("ClubId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_StockMovement_User_UserId" FOREIGN KEY ("UserId") REFERENCES "User" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "StoredFile" (
        "Id" uuid NOT NULL,
        "FileName" text NOT NULL,
        "ContentType" text NOT NULL,
        "Size" bigint NOT NULL,
        "StoragePath" text NOT NULL,
        "Scope" character varying(40) NOT NULL,
        "UploadedByUserId" uuid NOT NULL,
        "OwnerMemberId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_StoredFile" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_StoredFile_ClubId_Id" UNIQUE ("ClubId", "Id"),
        CONSTRAINT "FK_StoredFile_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_StoredFile_Member_ClubId_OwnerMemberId" FOREIGN KEY ("ClubId", "OwnerMemberId") REFERENCES "Member" ("ClubId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_StoredFile_User_UploadedByUserId" FOREIGN KEY ("UploadedByUserId") REFERENCES "User" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "UserPermission" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Permission" text NOT NULL,
        "Granted" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_UserPermission" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_UserPermission_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_UserPermission_User_UserId" FOREIGN KEY ("UserId") REFERENCES "User" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "UserRole" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "RoleId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_UserRole" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_UserRole_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_UserRole_Role_ClubId_RoleId" FOREIGN KEY ("ClubId", "RoleId") REFERENCES "Role" ("ClubId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_UserRole_User_UserId" FOREIGN KEY ("UserId") REFERENCES "User" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "PollOption" (
        "Id" uuid NOT NULL,
        "PollId" uuid NOT NULL,
        "Text" text NOT NULL,
        "SortOrder" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_PollOption" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_PollOption_ClubId_Id" UNIQUE ("ClubId", "Id"),
        CONSTRAINT "FK_PollOption_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_PollOption_Poll_ClubId_PollId" FOREIGN KEY ("ClubId", "PollId") REFERENCES "Poll" ("ClubId", "Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "Payment" (
        "Id" uuid NOT NULL,
        "ChargeId" uuid NOT NULL,
        "MemberId" uuid NOT NULL,
        "Amount" numeric(14,2) NOT NULL,
        "Method" character varying(40) NOT NULL,
        "Channel" character varying(40) NOT NULL,
        "Status" character varying(40) NOT NULL,
        "PaidAt" timestamp with time zone,
        "RegisteredByUserId" uuid,
        "IdempotencyKey" text NOT NULL,
        "ReceiptNumber" text NOT NULL,
        "GatewayProvider" text,
        "GatewayReference" text,
        "GatewayPayload" text,
        "ReceiptFileId" uuid,
        "Notes" text,
        "RefundedAt" timestamp with time zone,
        "RefundReason" text,
        "RefundedByUserId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_Payment" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Payment_Charge_ClubId_ChargeId" FOREIGN KEY ("ClubId", "ChargeId") REFERENCES "Charge" ("ClubId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Payment_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Payment_Member_ClubId_MemberId" FOREIGN KEY ("ClubId", "MemberId") REFERENCES "Member" ("ClubId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Payment_StoredFile_ClubId_ReceiptFileId" FOREIGN KEY ("ClubId", "ReceiptFileId") REFERENCES "StoredFile" ("ClubId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Payment_User_RefundedByUserId" FOREIGN KEY ("RefundedByUserId") REFERENCES "User" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Payment_User_RegisteredByUserId" FOREIGN KEY ("RegisteredByUserId") REFERENCES "User" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "Space" (
        "Id" uuid NOT NULL,
        "Name" text NOT NULL,
        "Type" character varying(40) NOT NULL,
        "Subtype" text,
        "Description" text,
        "Capacity" integer,
        "IsReservable" boolean NOT NULL,
        "BookingMode" character varying(40) NOT NULL,
        "SlotMinutes" integer NOT NULL,
        "FixedDays" integer NOT NULL,
        "HourlyPrice" numeric(14,2),
        "PeriodPrice" numeric(14,2),
        "CleaningFee" numeric(14,2),
        "ChargeMode" character varying(40),
        "ApprovalMode" character varying(40),
        "UsageRules" text,
        "ImageFileId" uuid,
        "IsActive" boolean NOT NULL,
        "SortOrder" integer NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_Space" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_Space_ClubId_Id" UNIQUE ("ClubId", "Id"),
        CONSTRAINT "FK_Space_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Space_StoredFile_ClubId_ImageFileId" FOREIGN KEY ("ClubId", "ImageFileId") REFERENCES "StoredFile" ("ClubId", "Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "Vote" (
        "Id" uuid NOT NULL,
        "PollId" uuid NOT NULL,
        "PollOptionId" uuid NOT NULL,
        "MemberId" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_Vote" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Vote_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Vote_Member_ClubId_MemberId" FOREIGN KEY ("ClubId", "MemberId") REFERENCES "Member" ("ClubId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Vote_PollOption_ClubId_PollOptionId" FOREIGN KEY ("ClubId", "PollOptionId") REFERENCES "PollOption" ("ClubId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Vote_Poll_ClubId_PollId" FOREIGN KEY ("ClubId", "PollId") REFERENCES "Poll" ("ClubId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Vote_User_UserId" FOREIGN KEY ("UserId") REFERENCES "User" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "Reservation" (
        "Id" uuid NOT NULL,
        "Code" text NOT NULL,
        "SpaceId" uuid NOT NULL,
        "Kind" character varying(40) NOT NULL,
        "Title" text,
        "MemberId" uuid,
        "RequestedByUserId" uuid NOT NULL,
        "RequestedByDependentId" uuid,
        "StartAt" timestamp with time zone NOT NULL,
        "EndAt" timestamp with time zone NOT NULL,
        "PeopleCount" integer NOT NULL,
        "Status" character varying(40) NOT NULL,
        "RentalAmount" numeric(14,2) NOT NULL,
        "CleaningFee" numeric(14,2) NOT NULL,
        "TotalAmount" numeric(14,2) NOT NULL,
        "ChargeMode" character varying(40),
        "ChargeId" uuid,
        "Notes" text,
        "DecisionByUserId" uuid,
        "DecisionAt" timestamp with time zone,
        "DecisionReason" text,
        "CancelledAt" timestamp with time zone,
        "CancelledByUserId" uuid,
        "CancellationReason" text,
        "Attendance" character varying(40) NOT NULL,
        "AttendanceNote" text,
        "PenaltyChargeId" uuid,
        "ReminderSentAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_Reservation" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_Reservation_ClubId_Id" UNIQUE ("ClubId", "Id"),
        CONSTRAINT "FK_Reservation_Charge_ClubId_ChargeId" FOREIGN KEY ("ClubId", "ChargeId") REFERENCES "Charge" ("ClubId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Reservation_Charge_ClubId_PenaltyChargeId" FOREIGN KEY ("ClubId", "PenaltyChargeId") REFERENCES "Charge" ("ClubId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Reservation_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Reservation_Dependent_ClubId_RequestedByDependentId" FOREIGN KEY ("ClubId", "RequestedByDependentId") REFERENCES "Dependent" ("ClubId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Reservation_Member_ClubId_MemberId" FOREIGN KEY ("ClubId", "MemberId") REFERENCES "Member" ("ClubId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Reservation_Space_ClubId_SpaceId" FOREIGN KEY ("ClubId", "SpaceId") REFERENCES "Space" ("ClubId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Reservation_User_CancelledByUserId" FOREIGN KEY ("CancelledByUserId") REFERENCES "User" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Reservation_User_DecisionByUserId" FOREIGN KEY ("DecisionByUserId") REFERENCES "User" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Reservation_User_RequestedByUserId" FOREIGN KEY ("RequestedByUserId") REFERENCES "User" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "SpaceBlock" (
        "Id" uuid NOT NULL,
        "SpaceId" uuid,
        "StartAt" timestamp with time zone NOT NULL,
        "EndAt" timestamp with time zone NOT NULL,
        "Reason" text NOT NULL,
        "Kind" character varying(40) NOT NULL,
        "MaintenanceOrderId" uuid,
        "CreatedByUserId" uuid NOT NULL,
        "IsCancelled" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_SpaceBlock" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_SpaceBlock_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_SpaceBlock_MaintenanceOrder_ClubId_MaintenanceOrderId" FOREIGN KEY ("ClubId", "MaintenanceOrderId") REFERENCES "MaintenanceOrder" ("ClubId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_SpaceBlock_Space_ClubId_SpaceId" FOREIGN KEY ("ClubId", "SpaceId") REFERENCES "Space" ("ClubId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_SpaceBlock_User_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") REFERENCES "User" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "SpaceSchedule" (
        "Id" uuid NOT NULL,
        "SpaceId" uuid NOT NULL,
        "DayOfWeek" character varying(40) NOT NULL,
        "OpenMinute" integer NOT NULL,
        "CloseMinute" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_SpaceSchedule" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_SpaceSchedule_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_SpaceSchedule_Space_ClubId_SpaceId" FOREIGN KEY ("ClubId", "SpaceId") REFERENCES "Space" ("ClubId", "Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "Occurrence" (
        "Id" uuid NOT NULL,
        "Code" text NOT NULL,
        "Kind" character varying(40) NOT NULL,
        "Description" text NOT NULL,
        "SpaceId" uuid,
        "ReservationId" uuid,
        "MemberId" uuid,
        "InspectionId" uuid,
        "ReportedByUserId" uuid NOT NULL,
        "OccurredAt" timestamp with time zone NOT NULL,
        "Status" character varying(40) NOT NULL,
        "Resolution" text,
        "ProposedChargeAmount" numeric(14,2),
        "ChargeDecision" character varying(40) NOT NULL,
        "ChargeDecisionByUserId" uuid,
        "ChargeDecisionAt" timestamp with time zone,
        "ChargeDecisionReason" text,
        "ChargeId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_Occurrence" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_Occurrence_ClubId_Id" UNIQUE ("ClubId", "Id"),
        CONSTRAINT "FK_Occurrence_Charge_ClubId_ChargeId" FOREIGN KEY ("ClubId", "ChargeId") REFERENCES "Charge" ("ClubId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Occurrence_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Occurrence_Inspection_ClubId_InspectionId" FOREIGN KEY ("ClubId", "InspectionId") REFERENCES "Inspection" ("ClubId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Occurrence_Member_ClubId_MemberId" FOREIGN KEY ("ClubId", "MemberId") REFERENCES "Member" ("ClubId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Occurrence_Reservation_ClubId_ReservationId" FOREIGN KEY ("ClubId", "ReservationId") REFERENCES "Reservation" ("ClubId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Occurrence_Space_ClubId_SpaceId" FOREIGN KEY ("ClubId", "SpaceId") REFERENCES "Space" ("ClubId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Occurrence_User_ChargeDecisionByUserId" FOREIGN KEY ("ChargeDecisionByUserId") REFERENCES "User" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Occurrence_User_ReportedByUserId" FOREIGN KEY ("ReportedByUserId") REFERENCES "User" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE TABLE "ReservationSlot" (
        "Id" uuid NOT NULL,
        "ReservationId" uuid NOT NULL,
        "SpaceId" uuid NOT NULL,
        "StartAt" timestamp with time zone NOT NULL,
        "EndAt" timestamp with time zone NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "ClubId" uuid NOT NULL,
        CONSTRAINT "PK_ReservationSlot" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ReservationSlot_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_ReservationSlot_Reservation_ClubId_ReservationId" FOREIGN KEY ("ClubId", "ReservationId") REFERENCES "Reservation" ("ClubId", "Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_ReservationSlot_Space_ClubId_SpaceId" FOREIGN KEY ("ClubId", "SpaceId") REFERENCES "Space" ("ClubId", "Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_AccessLog_ClubId_OccurredAt" ON "AccessLog" ("ClubId", "OccurredAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_AccessLog_ClubId_QrTokenId" ON "AccessLog" ("ClubId", "QrTokenId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_AccessLog_ClubId_ResponsibleMemberId" ON "AccessLog" ("ClubId", "ResponsibleMemberId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_AccessLog_StaffUserId" ON "AccessLog" ("StaffUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Announcement_AuthorUserId" ON "Announcement" ("AuthorUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Announcement_ClubId_EventId" ON "Announcement" ("ClubId", "EventId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Announcement_ClubId_ImageFileId" ON "Announcement" ("ClubId", "ImageFileId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Announcement_ClubId_PublishedAt" ON "Announcement" ("ClubId", "PublishedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_AnnouncementRead_AnnouncementId_UserId" ON "AnnouncementRead" ("AnnouncementId", "UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_AnnouncementRead_ClubId_AnnouncementId" ON "AnnouncementRead" ("ClubId", "AnnouncementId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_AnnouncementRead_UserId" ON "AnnouncementRead" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_AnnouncementTarget_AnnouncementId_UserId" ON "AnnouncementTarget" ("AnnouncementId", "UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_AnnouncementTarget_ClubId_AnnouncementId" ON "AnnouncementTarget" ("ClubId", "AnnouncementId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_AnnouncementTarget_UserId" ON "AnnouncementTarget" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Assembly_ClubId_MinutesFileId" ON "Assembly" ("ClubId", "MinutesFileId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Assembly_CreatedByUserId" ON "Assembly" ("CreatedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_AssemblyAttendance_AssemblyId_MemberId" ON "AssemblyAttendance" ("AssemblyId", "MemberId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_AssemblyAttendance_ClubId_AssemblyId" ON "AssemblyAttendance" ("ClubId", "AssemblyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_AssemblyAttendance_ClubId_MemberId" ON "AssemblyAttendance" ("ClubId", "MemberId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_AssemblyAttendance_RegisteredByUserId" ON "AssemblyAttendance" ("RegisteredByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_Asset_ClubId_Code" ON "Asset" ("ClubId", "Code") WHERE "IsDeleted" = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Asset_ClubId_ResponsibleEmployeeId" ON "Asset" ("ClubId", "ResponsibleEmployeeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Asset_ClubId_SpaceId" ON "Asset" ("ClubId", "SpaceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Attachment_ClubId_FileId" ON "Attachment" ("ClubId", "FileId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Attachment_OwnerType_OwnerId" ON "Attachment" ("OwnerType", "OwnerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Attachment_UploadedByUserId" ON "Attachment" ("UploadedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_AuditLog_ClubId_CreatedAt" ON "AuditLog" ("ClubId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_AuditLog_EntityType_EntityId" ON "AuditLog" ("EntityType", "EntityId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_AuditLog_UserId" ON "AuditLog" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Charge_ClubId_GuestId" ON "Charge" ("ClubId", "GuestId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_Charge_ClubId_MemberId_Competence" ON "Charge" ("ClubId", "MemberId", "Competence") WHERE "Type" = 'Monthly' AND "Status" <> 'Cancelled';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Charge_ClubId_OccurrenceId" ON "Charge" ("ClubId", "OccurrenceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Charge_ClubId_ReservationId" ON "Charge" ("ClubId", "ReservationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Charge_ClubId_Status_DueDate" ON "Charge" ("ClubId", "Status", "DueDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Charge_CreatedByUserId" ON "Charge" ("CreatedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_ChargeAdjustment_ClubId_ChargeId" ON "ChargeAdjustment" ("ClubId", "ChargeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_ChargeAdjustment_UserId" ON "ChargeAdjustment" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_Club_Cnpj" ON "Club" ("Cnpj") WHERE "Cnpj" IS NOT NULL AND "IsDeleted" = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Club_LogoFileId" ON "Club" ("LogoFileId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_Club_Slug" ON "Club" ("Slug");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_ClubEvent_ClubId_ImageFileId" ON "ClubEvent" ("ClubId", "ImageFileId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_ClubEvent_ClubId_SpaceId" ON "ClubEvent" ("ClubId", "SpaceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_ClubEvent_CreatedByUserId" ON "ClubEvent" ("CreatedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_ClubOpeningHour_ClubId_DayOfWeek" ON "ClubOpeningHour" ("ClubId", "DayOfWeek");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_ClubSettings_ClubId" ON "ClubSettings" ("ClubId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_Dependent_ClubId_Cpf" ON "Dependent" ("ClubId", "Cpf") WHERE "Cpf" IS NOT NULL AND "IsDeleted" = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Dependent_ClubId_MemberId" ON "Dependent" ("ClubId", "MemberId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Dependent_ClubId_PhotoFileId" ON "Dependent" ("ClubId", "PhotoFileId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_DependentPermission_ClubId_DependentId" ON "DependentPermission" ("ClubId", "DependentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_DependentPermission_DependentId_Code" ON "DependentPermission" ("DependentId", "Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_DependentPricingRule_ClubId" ON "DependentPricingRule" ("ClubId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_DeviceToken_ClubId" ON "DeviceToken" ("ClubId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_DeviceToken_Token" ON "DeviceToken" ("Token");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_DeviceToken_UserId" ON "DeviceToken" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_DigitalCard_ClubId_CardNumber" ON "DigitalCard" ("ClubId", "CardNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_DigitalCard_ClubId_PersonType_PersonId" ON "DigitalCard" ("ClubId", "PersonType", "PersonId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_Employee_ClubId_Cpf" ON "Employee" ("ClubId", "Cpf") WHERE "IsDeleted" = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_EventRegistration_ClubId_EventId" ON "EventRegistration" ("ClubId", "EventId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_EventRegistration_EventId_UserId" ON "EventRegistration" ("EventId", "UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_EventRegistration_UserId" ON "EventRegistration" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_FinancialEntry_ClubId_Kind_DueDate" ON "FinancialEntry" ("ClubId", "Kind", "DueDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_FinancialEntry_ClubId_MaintenanceOrderId" ON "FinancialEntry" ("ClubId", "MaintenanceOrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_FinancialEntry_ClubId_ReceiptFileId" ON "FinancialEntry" ("ClubId", "ReceiptFileId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_FinancialEntry_CreatedByUserId" ON "FinancialEntry" ("CreatedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_FinancialRelease_ClubId_MemberId" ON "FinancialRelease" ("ClubId", "MemberId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_FinancialRelease_GrantedByUserId" ON "FinancialRelease" ("GrantedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_FinancialRelease_RevokedByUserId" ON "FinancialRelease" ("RevokedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Guest_AuthorizedByUserId" ON "Guest" ("AuthorizedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Guest_ClubId_ChargeId" ON "Guest" ("ClubId", "ChargeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Guest_ClubId_MemberId" ON "Guest" ("ClubId", "MemberId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Guest_ClubId_ReservationId" ON "Guest" ("ClubId", "ReservationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Guest_ClubId_SpaceId" ON "Guest" ("ClubId", "SpaceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Guest_ClubId_VisitDate" ON "Guest" ("ClubId", "VisitDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Guest_RegisteredByUserId" ON "Guest" ("RegisteredByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Holiday_ClubId_Date" ON "Holiday" ("ClubId", "Date");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Inspection_ClubId_MemberId" ON "Inspection" ("ClubId", "MemberId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Inspection_ClubId_ReservationId" ON "Inspection" ("ClubId", "ReservationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Inspection_ClubId_SpaceId" ON "Inspection" ("ClubId", "SpaceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Inspection_InspectorUserId" ON "Inspection" ("InspectorUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_InspectionItem_ClubId_InspectionId" ON "InspectionItem" ("ClubId", "InspectionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_KeyHandover_ClubId_MemberId" ON "KeyHandover" ("ClubId", "MemberId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_KeyHandover_ClubId_ReservationId" ON "KeyHandover" ("ClubId", "ReservationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_KeyHandover_ClubId_SpaceId" ON "KeyHandover" ("ClubId", "SpaceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_KeyHandover_DeliveredByUserId" ON "KeyHandover" ("DeliveredByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_KeyHandover_ReceivedByUserId" ON "KeyHandover" ("ReceivedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_LoginHistory_ClubId_CreatedAt" ON "LoginHistory" ("ClubId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_LoginHistory_UserId" ON "LoginHistory" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_MaintenanceOrder_ClubId_AssetId" ON "MaintenanceOrder" ("ClubId", "AssetId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_MaintenanceOrder_ClubId_Code" ON "MaintenanceOrder" ("ClubId", "Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_MaintenanceOrder_ClubId_ResponsibleEmployeeId" ON "MaintenanceOrder" ("ClubId", "ResponsibleEmployeeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_MaintenanceOrder_ClubId_SpaceId" ON "MaintenanceOrder" ("ClubId", "SpaceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_MaintenanceOrder_OpenedByUserId" ON "MaintenanceOrder" ("OpenedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Manifestation_ClubId_AssemblyId" ON "Manifestation" ("ClubId", "AssemblyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Manifestation_ClubId_MemberId" ON "Manifestation" ("ClubId", "MemberId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Manifestation_ClubId_PhotoFileId" ON "Manifestation" ("ClubId", "PhotoFileId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_Manifestation_ClubId_Protocol" ON "Manifestation" ("ClubId", "Protocol");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Manifestation_ClubId_TypeId" ON "Manifestation" ("ClubId", "TypeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Manifestation_UserId" ON "Manifestation" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_ManifestationUpdate_ClubId_ManifestationId" ON "ManifestationUpdate" ("ClubId", "ManifestationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_ManifestationUpdate_UserId" ON "ManifestationUpdate" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_Member_ClubId_Cpf" ON "Member" ("ClubId", "Cpf") WHERE "IsDeleted" = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Member_ClubId_FullName" ON "Member" ("ClubId", "FullName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_Member_ClubId_Number" ON "Member" ("ClubId", "Number");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Member_ClubId_PhotoFileId" ON "Member" ("ClubId", "PhotoFileId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Member_ClubId_Status" ON "Member" ("ClubId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_MonthlyClosing_ClosedByUserId" ON "MonthlyClosing" ("ClosedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_MonthlyClosing_ClubId_Competence" ON "MonthlyClosing" ("ClubId", "Competence");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Notification_ClubId" ON "Notification" ("ClubId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Notification_UserId_ReadAt" ON "Notification" ("UserId", "ReadAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_NotificationPreference_ClubId" ON "NotificationPreference" ("ClubId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_NotificationPreference_UserId_Category" ON "NotificationPreference" ("UserId", "Category");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Occurrence_ChargeDecisionByUserId" ON "Occurrence" ("ChargeDecisionByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Occurrence_ClubId_ChargeId" ON "Occurrence" ("ClubId", "ChargeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_Occurrence_ClubId_Code" ON "Occurrence" ("ClubId", "Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Occurrence_ClubId_InspectionId" ON "Occurrence" ("ClubId", "InspectionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Occurrence_ClubId_MemberId" ON "Occurrence" ("ClubId", "MemberId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Occurrence_ClubId_ReservationId" ON "Occurrence" ("ClubId", "ReservationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Occurrence_ClubId_SpaceId" ON "Occurrence" ("ClubId", "SpaceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Occurrence_ReportedByUserId" ON "Occurrence" ("ReportedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_PasswordResetToken_TokenHash" ON "PasswordResetToken" ("TokenHash");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_PasswordResetToken_UserId" ON "PasswordResetToken" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Payment_ClubId_ChargeId" ON "Payment" ("ClubId", "ChargeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_Payment_ClubId_IdempotencyKey" ON "Payment" ("ClubId", "IdempotencyKey");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Payment_ClubId_MemberId" ON "Payment" ("ClubId", "MemberId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Payment_ClubId_PaidAt" ON "Payment" ("ClubId", "PaidAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Payment_ClubId_ReceiptFileId" ON "Payment" ("ClubId", "ReceiptFileId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_Payment_ClubId_ReceiptNumber" ON "Payment" ("ClubId", "ReceiptNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Payment_GatewayReference" ON "Payment" ("GatewayReference");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Payment_RefundedByUserId" ON "Payment" ("RefundedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Payment_RegisteredByUserId" ON "Payment" ("RegisteredByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Poll_ClubId_AssemblyId" ON "Poll" ("ClubId", "AssemblyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Poll_CreatedByUserId" ON "Poll" ("CreatedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_PollOption_ClubId_PollId" ON "PollOption" ("ClubId", "PollId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_Presence_ClubId_PersonType_PersonId" ON "Presence" ("ClubId", "PersonType", "PersonId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Presence_ClubId_ResponsibleMemberId" ON "Presence" ("ClubId", "ResponsibleMemberId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_QrToken_ClubId_DisplayCode" ON "QrToken" ("ClubId", "DisplayCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_QrToken_ClubId_PersonType_PersonId" ON "QrToken" ("ClubId", "PersonType", "PersonId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_QrToken_ConsumedByUserId" ON "QrToken" ("ConsumedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_QrToken_IssuedToUserId" ON "QrToken" ("IssuedToUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_QrToken_TokenHash" ON "QrToken" ("TokenHash");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_RefreshToken_TokenHash" ON "RefreshToken" ("TokenHash");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_RefreshToken_UserId" ON "RefreshToken" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Reservation_CancelledByUserId" ON "Reservation" ("CancelledByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Reservation_ClubId_ChargeId" ON "Reservation" ("ClubId", "ChargeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_Reservation_ClubId_Code" ON "Reservation" ("ClubId", "Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Reservation_ClubId_MemberId" ON "Reservation" ("ClubId", "MemberId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Reservation_ClubId_PenaltyChargeId" ON "Reservation" ("ClubId", "PenaltyChargeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Reservation_ClubId_RequestedByDependentId" ON "Reservation" ("ClubId", "RequestedByDependentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Reservation_ClubId_SpaceId_StartAt" ON "Reservation" ("ClubId", "SpaceId", "StartAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Reservation_ClubId_Status" ON "Reservation" ("ClubId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Reservation_DecisionByUserId" ON "Reservation" ("DecisionByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Reservation_RequestedByUserId" ON "Reservation" ("RequestedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_ReservationSlot_ClubId_ReservationId" ON "ReservationSlot" ("ClubId", "ReservationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_ReservationSlot_ClubId_SpaceId" ON "ReservationSlot" ("ClubId", "SpaceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_ReservationSlot_SpaceId_StartAt" ON "ReservationSlot" ("SpaceId", "StartAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_Role_ClubId_Name" ON "Role" ("ClubId", "Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_RolePermission_ClubId_RoleId" ON "RolePermission" ("ClubId", "RoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_RolePermission_RoleId_Permission" ON "RolePermission" ("RoleId", "Permission");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Space_ClubId_ImageFileId" ON "Space" ("ClubId", "ImageFileId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_SpaceBlock_ClubId_MaintenanceOrderId" ON "SpaceBlock" ("ClubId", "MaintenanceOrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_SpaceBlock_ClubId_SpaceId" ON "SpaceBlock" ("ClubId", "SpaceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_SpaceBlock_ClubId_StartAt_EndAt" ON "SpaceBlock" ("ClubId", "StartAt", "EndAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_SpaceBlock_CreatedByUserId" ON "SpaceBlock" ("CreatedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_SpaceSchedule_ClubId_SpaceId" ON "SpaceSchedule" ("ClubId", "SpaceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_SpaceSchedule_SpaceId_DayOfWeek" ON "SpaceSchedule" ("SpaceId", "DayOfWeek");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_StatusHistory_ClubId" ON "StatusHistory" ("ClubId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_StatusHistory_EntityType_EntityId" ON "StatusHistory" ("EntityType", "EntityId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_StatusHistory_UserId" ON "StatusHistory" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_StockItem_ClubId_SupplierId" ON "StockItem" ("ClubId", "SupplierId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_StockMovement_ClubId_StockItemId" ON "StockMovement" ("ClubId", "StockItemId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_StockMovement_ClubId_SupplierId" ON "StockMovement" ("ClubId", "SupplierId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_StockMovement_UserId" ON "StockMovement" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_StoredFile_ClubId_OwnerMemberId" ON "StoredFile" ("ClubId", "OwnerMemberId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_StoredFile_UploadedByUserId" ON "StoredFile" ("UploadedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_User_ClubId" ON "User" ("ClubId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_User_DependentId" ON "User" ("DependentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_User_Email" ON "User" ("Email") WHERE "IsDeleted" = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_User_EmployeeId" ON "User" ("EmployeeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_User_MemberId" ON "User" ("MemberId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_UserPermission_ClubId" ON "UserPermission" ("ClubId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_UserPermission_UserId_Permission" ON "UserPermission" ("UserId", "Permission");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_UserRole_ClubId_RoleId" ON "UserRole" ("ClubId", "RoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_UserRole_UserId_RoleId" ON "UserRole" ("UserId", "RoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Vote_ClubId_MemberId" ON "Vote" ("ClubId", "MemberId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Vote_ClubId_PollId" ON "Vote" ("ClubId", "PollId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Vote_ClubId_PollOptionId" ON "Vote" ("ClubId", "PollOptionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE UNIQUE INDEX "IX_Vote_PollId_MemberId" ON "Vote" ("PollId", "MemberId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    CREATE INDEX "IX_Vote_UserId" ON "Vote" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "AccessLog" ADD CONSTRAINT "FK_AccessLog_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "AccessLog" ADD CONSTRAINT "FK_AccessLog_Member_ClubId_ResponsibleMemberId" FOREIGN KEY ("ClubId", "ResponsibleMemberId") REFERENCES "Member" ("ClubId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "AccessLog" ADD CONSTRAINT "FK_AccessLog_QrToken_ClubId_QrTokenId" FOREIGN KEY ("ClubId", "QrTokenId") REFERENCES "QrToken" ("ClubId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "AccessLog" ADD CONSTRAINT "FK_AccessLog_User_StaffUserId" FOREIGN KEY ("StaffUserId") REFERENCES "User" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Announcement" ADD CONSTRAINT "FK_Announcement_ClubEvent_ClubId_EventId" FOREIGN KEY ("ClubId", "EventId") REFERENCES "ClubEvent" ("ClubId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Announcement" ADD CONSTRAINT "FK_Announcement_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Announcement" ADD CONSTRAINT "FK_Announcement_StoredFile_ClubId_ImageFileId" FOREIGN KEY ("ClubId", "ImageFileId") REFERENCES "StoredFile" ("ClubId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Announcement" ADD CONSTRAINT "FK_Announcement_User_AuthorUserId" FOREIGN KEY ("AuthorUserId") REFERENCES "User" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "AnnouncementRead" ADD CONSTRAINT "FK_AnnouncementRead_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "AnnouncementRead" ADD CONSTRAINT "FK_AnnouncementRead_User_UserId" FOREIGN KEY ("UserId") REFERENCES "User" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "AnnouncementTarget" ADD CONSTRAINT "FK_AnnouncementTarget_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "AnnouncementTarget" ADD CONSTRAINT "FK_AnnouncementTarget_User_UserId" FOREIGN KEY ("UserId") REFERENCES "User" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Assembly" ADD CONSTRAINT "FK_Assembly_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Assembly" ADD CONSTRAINT "FK_Assembly_StoredFile_ClubId_MinutesFileId" FOREIGN KEY ("ClubId", "MinutesFileId") REFERENCES "StoredFile" ("ClubId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Assembly" ADD CONSTRAINT "FK_Assembly_User_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") REFERENCES "User" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "AssemblyAttendance" ADD CONSTRAINT "FK_AssemblyAttendance_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "AssemblyAttendance" ADD CONSTRAINT "FK_AssemblyAttendance_Member_ClubId_MemberId" FOREIGN KEY ("ClubId", "MemberId") REFERENCES "Member" ("ClubId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "AssemblyAttendance" ADD CONSTRAINT "FK_AssemblyAttendance_User_RegisteredByUserId" FOREIGN KEY ("RegisteredByUserId") REFERENCES "User" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Asset" ADD CONSTRAINT "FK_Asset_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Asset" ADD CONSTRAINT "FK_Asset_Employee_ClubId_ResponsibleEmployeeId" FOREIGN KEY ("ClubId", "ResponsibleEmployeeId") REFERENCES "Employee" ("ClubId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Asset" ADD CONSTRAINT "FK_Asset_Space_ClubId_SpaceId" FOREIGN KEY ("ClubId", "SpaceId") REFERENCES "Space" ("ClubId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Attachment" ADD CONSTRAINT "FK_Attachment_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Attachment" ADD CONSTRAINT "FK_Attachment_StoredFile_ClubId_FileId" FOREIGN KEY ("ClubId", "FileId") REFERENCES "StoredFile" ("ClubId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Attachment" ADD CONSTRAINT "FK_Attachment_User_UploadedByUserId" FOREIGN KEY ("UploadedByUserId") REFERENCES "User" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "AuditLog" ADD CONSTRAINT "FK_AuditLog_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "AuditLog" ADD CONSTRAINT "FK_AuditLog_User_UserId" FOREIGN KEY ("UserId") REFERENCES "User" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Charge" ADD CONSTRAINT "FK_Charge_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Charge" ADD CONSTRAINT "FK_Charge_Guest_ClubId_GuestId" FOREIGN KEY ("ClubId", "GuestId") REFERENCES "Guest" ("ClubId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Charge" ADD CONSTRAINT "FK_Charge_Member_ClubId_MemberId" FOREIGN KEY ("ClubId", "MemberId") REFERENCES "Member" ("ClubId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Charge" ADD CONSTRAINT "FK_Charge_Occurrence_ClubId_OccurrenceId" FOREIGN KEY ("ClubId", "OccurrenceId") REFERENCES "Occurrence" ("ClubId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Charge" ADD CONSTRAINT "FK_Charge_Reservation_ClubId_ReservationId" FOREIGN KEY ("ClubId", "ReservationId") REFERENCES "Reservation" ("ClubId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Charge" ADD CONSTRAINT "FK_Charge_User_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") REFERENCES "User" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "ChargeAdjustment" ADD CONSTRAINT "FK_ChargeAdjustment_Club_ClubId" FOREIGN KEY ("ClubId") REFERENCES "Club" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "ChargeAdjustment" ADD CONSTRAINT "FK_ChargeAdjustment_User_UserId" FOREIGN KEY ("UserId") REFERENCES "User" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Club" ADD CONSTRAINT "FK_Club_StoredFile_LogoFileId" FOREIGN KEY ("LogoFileId") REFERENCES "StoredFile" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "ClubEvent" ADD CONSTRAINT "FK_ClubEvent_Space_ClubId_SpaceId" FOREIGN KEY ("ClubId", "SpaceId") REFERENCES "Space" ("ClubId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "ClubEvent" ADD CONSTRAINT "FK_ClubEvent_StoredFile_ClubId_ImageFileId" FOREIGN KEY ("ClubId", "ImageFileId") REFERENCES "StoredFile" ("ClubId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "ClubEvent" ADD CONSTRAINT "FK_ClubEvent_User_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") REFERENCES "User" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Dependent" ADD CONSTRAINT "FK_Dependent_Member_ClubId_MemberId" FOREIGN KEY ("ClubId", "MemberId") REFERENCES "Member" ("ClubId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Dependent" ADD CONSTRAINT "FK_Dependent_StoredFile_ClubId_PhotoFileId" FOREIGN KEY ("ClubId", "PhotoFileId") REFERENCES "StoredFile" ("ClubId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "DeviceToken" ADD CONSTRAINT "FK_DeviceToken_User_UserId" FOREIGN KEY ("UserId") REFERENCES "User" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "EventRegistration" ADD CONSTRAINT "FK_EventRegistration_User_UserId" FOREIGN KEY ("UserId") REFERENCES "User" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "FinancialEntry" ADD CONSTRAINT "FK_FinancialEntry_MaintenanceOrder_ClubId_MaintenanceOrderId" FOREIGN KEY ("ClubId", "MaintenanceOrderId") REFERENCES "MaintenanceOrder" ("ClubId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "FinancialEntry" ADD CONSTRAINT "FK_FinancialEntry_StoredFile_ClubId_ReceiptFileId" FOREIGN KEY ("ClubId", "ReceiptFileId") REFERENCES "StoredFile" ("ClubId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "FinancialEntry" ADD CONSTRAINT "FK_FinancialEntry_User_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") REFERENCES "User" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "FinancialRelease" ADD CONSTRAINT "FK_FinancialRelease_Member_ClubId_MemberId" FOREIGN KEY ("ClubId", "MemberId") REFERENCES "Member" ("ClubId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "FinancialRelease" ADD CONSTRAINT "FK_FinancialRelease_User_GrantedByUserId" FOREIGN KEY ("GrantedByUserId") REFERENCES "User" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "FinancialRelease" ADD CONSTRAINT "FK_FinancialRelease_User_RevokedByUserId" FOREIGN KEY ("RevokedByUserId") REFERENCES "User" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Guest" ADD CONSTRAINT "FK_Guest_Member_ClubId_MemberId" FOREIGN KEY ("ClubId", "MemberId") REFERENCES "Member" ("ClubId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Guest" ADD CONSTRAINT "FK_Guest_Reservation_ClubId_ReservationId" FOREIGN KEY ("ClubId", "ReservationId") REFERENCES "Reservation" ("ClubId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Guest" ADD CONSTRAINT "FK_Guest_Space_ClubId_SpaceId" FOREIGN KEY ("ClubId", "SpaceId") REFERENCES "Space" ("ClubId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Guest" ADD CONSTRAINT "FK_Guest_User_AuthorizedByUserId" FOREIGN KEY ("AuthorizedByUserId") REFERENCES "User" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Guest" ADD CONSTRAINT "FK_Guest_User_RegisteredByUserId" FOREIGN KEY ("RegisteredByUserId") REFERENCES "User" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Inspection" ADD CONSTRAINT "FK_Inspection_Member_ClubId_MemberId" FOREIGN KEY ("ClubId", "MemberId") REFERENCES "Member" ("ClubId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Inspection" ADD CONSTRAINT "FK_Inspection_Reservation_ClubId_ReservationId" FOREIGN KEY ("ClubId", "ReservationId") REFERENCES "Reservation" ("ClubId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Inspection" ADD CONSTRAINT "FK_Inspection_Space_ClubId_SpaceId" FOREIGN KEY ("ClubId", "SpaceId") REFERENCES "Space" ("ClubId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Inspection" ADD CONSTRAINT "FK_Inspection_User_InspectorUserId" FOREIGN KEY ("InspectorUserId") REFERENCES "User" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "KeyHandover" ADD CONSTRAINT "FK_KeyHandover_Member_ClubId_MemberId" FOREIGN KEY ("ClubId", "MemberId") REFERENCES "Member" ("ClubId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "KeyHandover" ADD CONSTRAINT "FK_KeyHandover_Reservation_ClubId_ReservationId" FOREIGN KEY ("ClubId", "ReservationId") REFERENCES "Reservation" ("ClubId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "KeyHandover" ADD CONSTRAINT "FK_KeyHandover_Space_ClubId_SpaceId" FOREIGN KEY ("ClubId", "SpaceId") REFERENCES "Space" ("ClubId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "KeyHandover" ADD CONSTRAINT "FK_KeyHandover_User_DeliveredByUserId" FOREIGN KEY ("DeliveredByUserId") REFERENCES "User" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "KeyHandover" ADD CONSTRAINT "FK_KeyHandover_User_ReceivedByUserId" FOREIGN KEY ("ReceivedByUserId") REFERENCES "User" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "LoginHistory" ADD CONSTRAINT "FK_LoginHistory_User_UserId" FOREIGN KEY ("UserId") REFERENCES "User" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "MaintenanceOrder" ADD CONSTRAINT "FK_MaintenanceOrder_Space_ClubId_SpaceId" FOREIGN KEY ("ClubId", "SpaceId") REFERENCES "Space" ("ClubId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "MaintenanceOrder" ADD CONSTRAINT "FK_MaintenanceOrder_User_OpenedByUserId" FOREIGN KEY ("OpenedByUserId") REFERENCES "User" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Manifestation" ADD CONSTRAINT "FK_Manifestation_Member_ClubId_MemberId" FOREIGN KEY ("ClubId", "MemberId") REFERENCES "Member" ("ClubId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Manifestation" ADD CONSTRAINT "FK_Manifestation_StoredFile_ClubId_PhotoFileId" FOREIGN KEY ("ClubId", "PhotoFileId") REFERENCES "StoredFile" ("ClubId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Manifestation" ADD CONSTRAINT "FK_Manifestation_User_UserId" FOREIGN KEY ("UserId") REFERENCES "User" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "ManifestationUpdate" ADD CONSTRAINT "FK_ManifestationUpdate_User_UserId" FOREIGN KEY ("UserId") REFERENCES "User" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    ALTER TABLE "Member" ADD CONSTRAINT "FK_Member_StoredFile_ClubId_PhotoFileId" FOREIGN KEY ("ClubId", "PhotoFileId") REFERENCES "StoredFile" ("ClubId", "Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125139_Inicial') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261117125139_Inicial', '10.0.15');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125208_RestricoesAvancadas') THEN
    ALTER TABLE "ReservationSlot"
        ADD CONSTRAINT "CK_ReservationSlot_Periodo" CHECK ("EndAt" > "StartAt");

    ALTER TABLE "ReservationSlot"
        ADD CONSTRAINT "EX_ReservationSlot_NoOverlap"
        EXCLUDE USING gist ("SpaceId" WITH =, tstzrange("StartAt", "EndAt", '[)') WITH &&)
        WHERE ("IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125208_RestricoesAvancadas') THEN
    CREATE OR REPLACE FUNCTION auditlog_somente_insercao() RETURNS trigger AS $$
    BEGIN
        RAISE EXCEPTION 'Registros de auditoria são imutáveis (operação % não permitida).', TG_OP
            USING ERRCODE = 'integrity_constraint_violation';
    END;
    $$ LANGUAGE plpgsql;

    CREATE TRIGGER "TR_AuditLog_Imutavel"
        BEFORE UPDATE OR DELETE ON "AuditLog"
        FOR EACH ROW EXECUTE FUNCTION auditlog_somente_insercao();

    CREATE TRIGGER "TR_AuditLog_SemTruncate"
        BEFORE TRUNCATE ON "AuditLog"
        FOR EACH STATEMENT EXECUTE FUNCTION auditlog_somente_insercao();
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125208_RestricoesAvancadas') THEN
    ALTER TABLE "Charge" ADD CONSTRAINT "CK_Charge_Valores" CHECK (
        "OriginalAmount" >= 0 AND "Additions" >= 0 AND "Discounts" >= 0 AND "FineAmount" >= 0
        AND "InterestAmount" >= 0 AND "FeesAmount" >= 0 AND "PaidAmount" >= 0
        AND "PaidAmount" <= "OriginalAmount" + "Additions" + "FineAmount" + "InterestAmount" + "FeesAmount" - "Discounts");
    ALTER TABLE "Payment" ADD CONSTRAINT "CK_Payment_Valor" CHECK ("Amount" > 0);
    ALTER TABLE "FinancialEntry" ADD CONSTRAINT "CK_FinancialEntry_Valor" CHECK ("Amount" > 0);
    ALTER TABLE "StockItem" ADD CONSTRAINT "CK_StockItem_Saldo" CHECK ("Quantity" >= 0);
    ALTER TABLE "SpaceBlock" ADD CONSTRAINT "CK_SpaceBlock_Periodo" CHECK ("EndAt" > "StartAt");
    ALTER TABLE "Reservation" ADD CONSTRAINT "CK_Reservation_Pessoas" CHECK ("PeopleCount" >= 1);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117125208_RestricoesAvancadas') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261117125208_RestricoesAvancadas', '10.0.15');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117132628_ChargeGuestSemChaveEstrangeira') THEN
    ALTER TABLE "Charge" DROP CONSTRAINT "FK_Charge_Guest_ClubId_GuestId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117132628_ChargeGuestSemChaveEstrangeira') THEN
    ALTER TABLE "Guest" DROP CONSTRAINT "AK_Guest_ClubId_Id";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117132628_ChargeGuestSemChaveEstrangeira') THEN
    DROP INDEX "IX_Charge_ClubId_GuestId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261117132628_ChargeGuestSemChaveEstrangeira') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261117132628_ChargeGuestSemChaveEstrangeira', '10.0.15');
    END IF;
END $EF$;
COMMIT;

