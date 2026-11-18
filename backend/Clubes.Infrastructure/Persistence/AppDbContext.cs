using System.Linq.Expressions;
using System.Text.Json;
using Clubes.Application.Common;
using Clubes.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Clubes.Infrastructure.Persistence;

/// <summary>Contador por clube para matrículas, protocolos e recibos.</summary>
public class ClubSequence
{
    public Guid ClubId { get; set; }
    public string Name { get; set; } = "";
    public long Value { get; set; }
}

public class AppDbContext(DbContextOptions<AppDbContext> options, RequestContext context) : DbContext(options), IAppDb
{
    private static readonly string[] AuditHidden = ["PasswordHash", "TokenHash", "GatewayPayload"];
    private static readonly JsonSerializerOptions AuditJson = new() { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };
    /// <summary>Referências apenas informativas, sem chave estrangeira, para não criar ciclo de gravação (Guest.ChargeId já garante o vínculo).</summary>
    private static readonly HashSet<(string Entity, string Property)> FkSkipped = [("Charge", "GuestId")];
    private static readonly Dictionary<string, Type> FkAliases = new()
    {
        ["File"] = typeof(StoredFile),
        ["Event"] = typeof(ClubEvent),
    };

    /// <summary>Clube corrente — referenciado pelos filtros globais (avaliado a cada consulta).</summary>
    public Guid? CurrentClubId => context.ClubId;

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(40);
        builder.Properties<decimal>().HavePrecision(14, 2);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasPostgresExtension("btree_gist");

        var entityTypes = typeof(Entity).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsSubclassOf(typeof(Entity))).ToList();
        foreach (var type in entityTypes) b.Entity(type);

        b.Entity<ClubSequence>().HasKey(s => new { s.ClubId, s.Name });

        ConfigureIndexes(b);
        ConfigureForeignKeysByName(b, entityTypes);
        ConfigureTenantIsolation(b);
        ConfigureFilters(b);

        foreach (var fk in b.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }

    private static void ConfigureIndexes(ModelBuilder b)
    {
        b.Entity<Club>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => x.Cnpj).IsUnique().HasFilter("\"Cnpj\" IS NOT NULL AND \"IsDeleted\" = false");
        });
        b.Entity<ClubSettings>(e =>
        {
            e.HasIndex(x => x.ClubId).IsUnique();
            e.Property(x => x.LateFinePercent).HasPrecision(9, 4);
            e.Property(x => x.DailyInterestPercent).HasPrecision(9, 4);
        });
        b.Entity<ClubOpeningHour>().HasIndex(x => new { x.ClubId, x.DayOfWeek }).IsUnique();
        b.Entity<User>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique().HasFilter("\"IsDeleted\" = false");
            e.HasIndex(x => x.ClubId);
        });
        b.Entity<Role>().HasIndex(x => new { x.ClubId, x.Name }).IsUnique();
        b.Entity<RolePermission>().HasIndex(x => new { x.RoleId, x.Permission }).IsUnique();
        b.Entity<UserRole>().HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
        b.Entity<UserPermission>().HasIndex(x => new { x.UserId, x.Permission }).IsUnique();
        b.Entity<RefreshToken>().HasIndex(x => x.TokenHash).IsUnique();
        b.Entity<PasswordResetToken>().HasIndex(x => x.TokenHash).IsUnique();
        b.Entity<LoginHistory>().HasIndex(x => new { x.ClubId, x.CreatedAt });
        b.Entity<AuditLog>(e =>
        {
            e.HasIndex(x => new { x.ClubId, x.CreatedAt });
            e.HasIndex(x => new { x.EntityType, x.EntityId });
            e.Property(x => x.BeforeJson).HasColumnType("jsonb");
            e.Property(x => x.AfterJson).HasColumnType("jsonb");
        });
        b.Entity<StatusHistory>().HasIndex(x => new { x.EntityType, x.EntityId });
        b.Entity<Attachment>().HasIndex(x => new { x.OwnerType, x.OwnerId });

        b.Entity<Member>(e =>
        {
            e.HasIndex(x => new { x.ClubId, x.Cpf }).IsUnique().HasFilter("\"IsDeleted\" = false");
            e.HasIndex(x => new { x.ClubId, x.Number }).IsUnique();
            e.HasIndex(x => new { x.ClubId, x.Status });
            e.HasIndex(x => new { x.ClubId, x.FullName });
        });
        b.Entity<Dependent>(e =>
        {
            e.HasIndex(x => new { x.ClubId, x.Cpf }).IsUnique().HasFilter("\"Cpf\" IS NOT NULL AND \"IsDeleted\" = false");
            e.HasOne(x => x.Member).WithMany(m => m.Dependents).HasForeignKey(x => x.MemberId);
        });
        b.Entity<DependentPermission>(e =>
        {
            e.HasIndex(x => new { x.DependentId, x.Code }).IsUnique();
            e.Ignore(x => x.IsEffective);
        });
        b.Entity<Employee>().HasIndex(x => new { x.ClubId, x.Cpf }).IsUnique().HasFilter("\"IsDeleted\" = false");
        b.Entity<DigitalCard>(e =>
        {
            e.HasIndex(x => new { x.ClubId, x.PersonType, x.PersonId }).IsUnique();
            e.HasIndex(x => new { x.ClubId, x.CardNumber }).IsUnique();
        });
        b.Entity<QrToken>(e =>
        {
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => new { x.ClubId, x.DisplayCode });
            e.HasIndex(x => new { x.ClubId, x.PersonType, x.PersonId });
        });
        b.Entity<AccessLog>().HasIndex(x => new { x.ClubId, x.OccurredAt });
        b.Entity<Presence>().HasIndex(x => new { x.ClubId, x.PersonType, x.PersonId }).IsUnique();
        b.Entity<Guest>().HasIndex(x => new { x.ClubId, x.VisitDate });

        b.Entity<Charge>(e =>
        {
            e.Ignore(x => x.Total);
            e.Ignore(x => x.Balance);
            e.Property<uint>("Version").IsRowVersion();
            e.Property(x => x.LateFinePercentSnapshot).HasPrecision(9, 4);
            e.Property(x => x.DailyInterestPercentSnapshot).HasPrecision(9, 4);
            e.Property(x => x.CompositionSnapshot).HasColumnType("jsonb");
            // Uma única mensalidade ativa por associado e competência: geração idempotente.
            e.HasIndex(x => new { x.ClubId, x.MemberId, x.Competence }).IsUnique()
                .HasFilter("\"Type\" = 'Monthly' AND \"Status\" <> 'Cancelled'");
            e.HasIndex(x => new { x.ClubId, x.Status, x.DueDate });
            e.HasMany(x => x.Payments).WithOne(p => p.Charge).HasForeignKey(p => p.ChargeId);
            e.HasMany(x => x.Adjustments).WithOne(p => p.Charge).HasForeignKey(p => p.ChargeId);
        });
        b.Entity<Payment>(e =>
        {
            e.HasIndex(x => new { x.ClubId, x.IdempotencyKey }).IsUnique();
            e.HasIndex(x => new { x.ClubId, x.ReceiptNumber }).IsUnique();
            e.HasIndex(x => x.GatewayReference);
            e.HasIndex(x => new { x.ClubId, x.PaidAt });
        });
        b.Entity<FinancialEntry>().HasIndex(x => new { x.ClubId, x.Kind, x.DueDate });
        b.Entity<MonthlyClosing>().HasIndex(x => new { x.ClubId, x.Competence }).IsUnique();

        b.Entity<SpaceSchedule>(e =>
        {
            e.HasIndex(x => new { x.SpaceId, x.DayOfWeek }).IsUnique();
            e.HasOne(x => x.Space).WithMany(s => s.Schedules).HasForeignKey(x => x.SpaceId);
        });
        b.Entity<Holiday>().HasIndex(x => new { x.ClubId, x.Date });
        b.Entity<SpaceBlock>().HasIndex(x => new { x.ClubId, x.StartAt, x.EndAt });
        b.Entity<Reservation>(e =>
        {
            e.HasIndex(x => new { x.ClubId, x.Code }).IsUnique();
            e.HasIndex(x => new { x.ClubId, x.SpaceId, x.StartAt });
            e.HasIndex(x => new { x.ClubId, x.Status });
            e.HasMany(x => x.Slots).WithOne(s => s.Reservation).HasForeignKey(s => s.ReservationId);
            e.HasOne(x => x.Space).WithMany().HasForeignKey(x => x.SpaceId);
            e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId);
        });
        b.Entity<ReservationSlot>().HasIndex(x => new { x.SpaceId, x.StartAt });
        b.Entity<Occurrence>().HasIndex(x => new { x.ClubId, x.Code }).IsUnique();
        b.Entity<MaintenanceOrder>().HasIndex(x => new { x.ClubId, x.Code }).IsUnique();
        b.Entity<Asset>().HasIndex(x => new { x.ClubId, x.Code }).IsUnique().HasFilter("\"IsDeleted\" = false");
        b.Entity<StockItem>(e =>
        {
            e.Property(x => x.Quantity).HasPrecision(14, 3);
            e.Property(x => x.MinQuantity).HasPrecision(14, 3);
            e.Property<uint>("Version").IsRowVersion();
        });
        b.Entity<StockMovement>(e =>
        {
            e.Property(x => x.Quantity).HasPrecision(14, 3);
            e.Property(x => x.BalanceAfter).HasPrecision(14, 3);
        });

        b.Entity<EventRegistration>().HasIndex(x => new { x.EventId, x.UserId }).IsUnique();
        b.Entity<AnnouncementTarget>().HasIndex(x => new { x.AnnouncementId, x.UserId }).IsUnique();
        b.Entity<AnnouncementRead>().HasIndex(x => new { x.AnnouncementId, x.UserId }).IsUnique();
        b.Entity<Announcement>().HasIndex(x => new { x.ClubId, x.PublishedAt });
        b.Entity<Notification>().HasIndex(x => new { x.UserId, x.ReadAt });
        b.Entity<DeviceToken>().HasIndex(x => x.Token).IsUnique();
        b.Entity<NotificationPreference>().HasIndex(x => new { x.UserId, x.Category }).IsUnique();
        b.Entity<Manifestation>(e =>
        {
            e.HasIndex(x => new { x.ClubId, x.Protocol }).IsUnique();
            e.HasOne(x => x.Type).WithMany().HasForeignKey(x => x.TypeId);
        });
        b.Entity<AssemblyAttendance>().HasIndex(x => new { x.AssemblyId, x.MemberId }).IsUnique();
        // Um voto por associado em cada votação, garantido pelo banco.
        b.Entity<Vote>().HasIndex(x => new { x.PollId, x.MemberId }).IsUnique();
    }

    /// <summary>
    /// Integridade referencial por convenção: toda propriedade Guid terminada em "XxxId" que ainda não
    /// participa de um relacionamento ganha chave estrangeira para a entidade Xxx.
    /// </summary>
    private static void ConfigureForeignKeysByName(ModelBuilder b, List<Type> entityTypes)
    {
        var targets = entityTypes.ToDictionary(t => t.Name, t => t);
        foreach (var alias in FkAliases) targets[alias.Key] = alias.Value;
        var names = targets.Keys.OrderByDescending(k => k.Length).ToList();

        foreach (var entity in b.Model.GetEntityTypes().ToList())
        {
            foreach (var prop in entity.GetProperties().ToList())
            {
                var clr = Nullable.GetUnderlyingType(prop.ClrType) ?? prop.ClrType;
                if (clr != typeof(Guid) || prop.Name == "Id" || !prop.Name.EndsWith("Id")) continue;
                if (entity.GetForeignKeys().Any(fk => fk.Properties.Contains(prop))) continue;
                if (FkSkipped.Contains((entity.ClrType.Name, prop.Name))) continue;
                var stem = prop.Name[..^2];
                var match = names.FirstOrDefault(stem.EndsWith);
                if (match is null) continue;
                b.Entity(entity.ClrType).HasOne(targets[match]).WithMany().HasForeignKey(prop.Name);
            }
        }
    }

    /// <summary>
    /// Isolamento no banco: relacionamentos entre entidades de clube usam chave estrangeira composta
    /// (ClubId, XxxId) → (ClubId, Id). Assim o próprio PostgreSQL rejeita qualquer vínculo entre
    /// registros de clubes diferentes, mesmo que a aplicação falhe.
    /// </summary>
    private static void ConfigureTenantIsolation(ModelBuilder b)
    {
        static bool IsTenant(IReadOnlyEntityType t) => typeof(ITenantEntity).IsAssignableFrom(t.ClrType);

        foreach (var entity in b.Model.GetEntityTypes().Where(IsTenant).ToList())
        {
            var clubProp = entity.FindProperty(nameof(ITenantEntity.ClubId))!;
            foreach (var fk in entity.GetForeignKeys().ToList())
            {
                var principal = fk.PrincipalEntityType;
                if (!IsTenant(principal) || fk.Properties.Count != 1 || fk.Properties[0] == clubProp) continue;
                var keyProps = new[] { principal.FindProperty(nameof(ITenantEntity.ClubId))!, principal.FindProperty(nameof(Entity.Id))! };
                var key = principal.FindKey(keyProps) ?? principal.AddKey(keyProps);
                fk.SetProperties([clubProp, fk.Properties[0]], key);
            }
        }
    }

    private void ConfigureFilters(ModelBuilder b)
    {
        foreach (var entity in b.Model.GetEntityTypes().ToList())
        {
            var clr = entity.ClrType;
            var p = Expression.Parameter(clr, "e");
            Expression? body = null;

            var clubProp = clr.GetProperty("ClubId");
            var isolated = typeof(ITenantEntity).IsAssignableFrom(clr) || clr == typeof(User) || clr == typeof(AuditLog) || clr == typeof(LoginHistory);
            if (isolated && clubProp is not null)
            {
                var current = Expression.Property(Expression.Constant(this), nameof(CurrentClubId));
                var left = Expression.Convert(Expression.Property(p, clubProp), typeof(Guid?));
                body = Expression.Equal(left, current);
            }
            if (typeof(ISoftDelete).IsAssignableFrom(clr))
            {
                var notDeleted = Expression.Not(Expression.Property(p, nameof(ISoftDelete.IsDeleted)));
                body = body is null ? notDeleted : Expression.AndAlso(body, notDeleted);
            }
            if (body is not null) b.Entity(clr).HasQueryFilter(Expression.Lambda(body, p));
        }
    }

    public async Task<long> NextSequenceAsync(string name, CancellationToken ct = default)
    {
        var clubId = context.RequireClub();
        var result = await Database.SqlQuery<long>($"""
            INSERT INTO "ClubSequence" ("ClubId", "Name", "Value") VALUES ({clubId}, {name}, 1)
            ON CONFLICT ("ClubId", "Name") DO UPDATE SET "Value" = "ClubSequence"."Value" + 1
            RETURNING "Value"
            """).ToListAsync(ct);
        return result[0];
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var audits = new List<AuditLog>();

        foreach (var entry in ChangeTracker.Entries().ToList())
        {
            if (entry.State is EntityState.Detached or EntityState.Unchanged) continue;

            if (entry.Entity is ITenantEntity tenant)
            {
                if (entry.State == EntityState.Added && tenant.ClubId == Guid.Empty) tenant.ClubId = context.RequireClub();
                if (context.ClubId is { } current && tenant.ClubId != current)
                    throw DomainException.Forbidden("Operação bloqueada: o registro pertence a outro clube.");
                if (entry.State == EntityState.Modified && entry.Property(nameof(ITenantEntity.ClubId)).IsModified)
                    throw DomainException.Forbidden("O clube de um registro não pode ser alterado.");
            }

            if (entry.State == EntityState.Deleted && entry.Entity is ISoftDelete soft)
            {
                entry.State = EntityState.Modified;
                soft.IsDeleted = true;
                soft.DeletedAt = now;
            }
            if (entry.State == EntityState.Modified && entry.Entity is Entity e) e.UpdatedAt = now;

            if (entry.Entity is Entity and not INotAudited)
            {
                var log = BuildAudit(entry);
                if (log is not null) audits.Add(log);
            }
        }

        if (audits.Count > 0) Set<AuditLog>().AddRange(audits);
        return await base.SaveChangesAsync(ct);
    }

    private AuditLog? BuildAudit(EntityEntry entry)
    {
        var before = new Dictionary<string, object?>();
        var after = new Dictionary<string, object?>();
        var softDeleted = entry.Entity is ISoftDelete { IsDeleted: true } && entry.Property(nameof(ISoftDelete.IsDeleted)).IsModified;

        foreach (var prop in entry.Properties)
        {
            var name = prop.Metadata.Name;
            if (prop.Metadata.IsShadowProperty() || name is "UpdatedAt" or "CreatedAt") continue;
            var hidden = AuditHidden.Contains(name);
            switch (entry.State)
            {
                case EntityState.Added:
                    if (prop.CurrentValue is not null) after[name] = hidden ? "***" : prop.CurrentValue;
                    break;
                case EntityState.Deleted:
                    if (prop.OriginalValue is not null) before[name] = hidden ? "***" : prop.OriginalValue;
                    break;
                case EntityState.Modified when prop.IsModified && !Equals(prop.OriginalValue, prop.CurrentValue):
                    before[name] = hidden ? "***" : prop.OriginalValue;
                    after[name] = hidden ? "***" : prop.CurrentValue;
                    break;
            }
        }
        if (entry.State == EntityState.Modified && after.Count == 0) return null;

        var clubId = entry.Entity switch
        {
            ITenantEntity t => t.ClubId,
            Club c => c.Id,
            User u => u.ClubId,
            _ => context.ClubId,
        };
        return new AuditLog
        {
            ClubId = clubId,
            UserId = context.UserId,
            UserName = context.UserName,
            Operation = entry.State switch
            {
                EntityState.Added => "Criação",
                EntityState.Deleted => "Exclusão",
                _ => softDeleted ? "Exclusão" : "Alteração",
            },
            EntityType = entry.Metadata.ClrType.Name,
            EntityId = ((Entity)entry.Entity).Id.ToString(),
            BeforeJson = before.Count > 0 ? JsonSerializer.Serialize(before, AuditJson) : null,
            AfterJson = after.Count > 0 ? JsonSerializer.Serialize(after, AuditJson) : null,
            Origin = context.Origin,
            Justification = context.Justification,
        };
    }
}
