using Clubes.Application.Access;
using Clubes.Application.Clubs;
using Clubes.Application.Common;
using Clubes.Domain;
using Clubes.Infrastructure.Jobs;
using Clubes.Infrastructure.Persistence;
using Clubes.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Clubes.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddClubes(this IServiceCollection services, IConfiguration config, IHostEnvironment environment)
    {
        services.Configure<JwtOptions>(config.GetSection("Jwt"));
        services.Configure<AuthOptions>(config.GetSection("Auth"));
        services.Configure<StorageOptions>(config.GetSection("Storage"));
        services.Configure<SmtpOptions>(config.GetSection("Email:Smtp"));
        services.Configure<PushOptions>(config.GetSection("Push"));
        services.Configure<PaymentOptions>(config.GetSection("Payments"));
        services.Configure<RoutineOptions>(config.GetSection("Routines"));
        services.Configure<BootstrapOptions>(config.GetSection("Bootstrap"));

        var connection = config.GetConnectionString("Default") ?? throw new InvalidOperationException("ConnectionStrings:Default não configurada.");
        services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connection));
        services.AddScoped<IAppDb>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<RequestContext>();

        // Serviços de aplicação registrados por convenção: toda classe concreta *Service, além dos utilitários abaixo.
        foreach (var type in typeof(AuthService).Assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false, IsNested: false } && t.Name.EndsWith("Service")))
            services.AddScoped(type);
        services.AddScoped<ClubClock>();
        services.AddScoped<Application.Admin.ClubRoutines>();

        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<IPushSender, FirebasePushSender>();

        var provider = config["Payments:Provider"] ?? "None";
        if (provider.Equals("Sandbox", StringComparison.OrdinalIgnoreCase))
        {
            if (environment.IsProduction()) throw new InvalidOperationException("O gateway Sandbox não pode ser usado em produção.");
            services.AddSingleton<IPaymentGateway, SandboxPaymentGateway>();
        }
        else services.AddSingleton<IPaymentGateway, DisabledPaymentGateway>();

        services.AddScoped<DatabaseInitializer>();
        services.AddHostedService<RoutineWorker>();
        services.AddHostedService<NotificationDispatcher>();
        return services;
    }
}

public class BootstrapOptions
{
    public bool AutoMigrate { get; set; } = true;
    public string? SuperAdminName { get; set; }
    public string? SuperAdminEmail { get; set; }
    public string? SuperAdminPassword { get; set; }
    public string? ClubName { get; set; }
    public string? ClubAdminName { get; set; }
    public string? ClubAdminEmail { get; set; }
    public string? ClubAdminPassword { get; set; }
    public bool ApplyStandardStructure { get; set; } = true;
}

/// <summary>
/// Preparação inicial do banco: aplica as migrations e, somente em banco vazio, cria o
/// superadministrador da plataforma e o primeiro clube a partir das variáveis de ambiente.
/// Não cria dados de exemplo.
/// </summary>
public class DatabaseInitializer(AppDbContext db, RequestContext ctx, IPasswordHasher<User> hasher, IOptions<BootstrapOptions> options, ILogger<DatabaseInitializer> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var opt = options.Value;
        if (opt.AutoMigrate) await db.Database.MigrateAsync(ct);
        (ctx.UserName, ctx.Origin) = ("Sistema", "inicializacao");

        if (!string.IsNullOrWhiteSpace(opt.SuperAdminEmail) && !await db.Set<User>().IgnoreQueryFilters().AnyAsync(u => u.Kind == UserKind.SuperAdmin, ct))
        {
            PasswordPolicy.Ensure(opt.SuperAdminPassword ?? "");
            var user = new User { Name = opt.SuperAdminName ?? "Superadministrador", Email = opt.SuperAdminEmail.Trim().ToLowerInvariant(), Kind = UserKind.SuperAdmin, MustChangePassword = true };
            user.PasswordHash = hasher.HashPassword(user, opt.SuperAdminPassword!);
            db.Set<User>().Add(user);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Superadministrador inicial criado: {Email}", user.Email);
        }

        if (!string.IsNullOrWhiteSpace(opt.ClubName) && !string.IsNullOrWhiteSpace(opt.ClubAdminEmail) && !await db.Set<Club>().IgnoreQueryFilters().AnyAsync(ct))
        {
            PasswordPolicy.Ensure(opt.ClubAdminPassword ?? "");
            var club = new Club { Name = opt.ClubName.Trim(), Slug = opt.ClubName.Trim().ToLowerInvariant().Replace(' ', '-') };
            db.Set<Club>().Add(club);
            ClubProvisioning.Provision(db, club, opt.ApplyStandardStructure);
            var admin = new User { ClubId = club.Id, Name = opt.ClubAdminName ?? $"Administrador {club.Name}", Email = opt.ClubAdminEmail.Trim().ToLowerInvariant(), Kind = UserKind.ClubAdmin, MustChangePassword = true };
            admin.PasswordHash = hasher.HashPassword(admin, opt.ClubAdminPassword!);
            db.Set<User>().Add(admin);
            ctx.ClubId = club.Id;
            try { await db.SaveChangesAsync(ct); }
            finally { ctx.ClubId = null; }
            logger.LogInformation("Clube inicial criado: {Club} (administrador {Email})", club.Name, admin.Email);
        }
    }
}

/// <summary>Usado pelas ferramentas do EF Core (dotnet ef) para criar o contexto em tempo de projeto.</summary>
public class DesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Default") ?? "Host=localhost;Port=5432;Database=clubes;Username=clubes;Password=clubes";
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options;
        return new AppDbContext(options, new RequestContext());
    }
}
