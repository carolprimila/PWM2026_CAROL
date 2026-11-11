using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Infrastructure.Arquivos;
using ProjetoClubes.Infrastructure.Email;
using ProjetoClubes.Infrastructure.Persistence;
using ProjetoClubes.Infrastructure.Persistence.Interceptors;
using ProjetoClubes.Infrastructure.Seguranca;

namespace ProjetoClubes.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var conexao = configuration.GetConnectionString("Postgres");
        if (string.IsNullOrWhiteSpace(conexao))
            throw new InvalidOperationException(
                "ConnectionStrings:Postgres não configurada. Defina a variável de ambiente ConnectionStrings__Postgres " +
                "ou use 'dotnet user-secrets' no projeto da API.");

        services.AddScoped<UsuarioAtual>();
        services.AddScoped<IUsuarioAtual>(sp => sp.GetRequiredService<UsuarioAtual>());
        services.AddScoped<GravacaoInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseNpgsql(conexao, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory");
                npgsql.EnableRetryOnFailure(3);
                npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            });
            options.AddInterceptors(sp.GetRequiredService<GravacaoInterceptor>());
        });
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.Secao));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.Secao));
        services.AddSingleton<IHashSenha, HashSenha>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<SetupTokenProvider>();
        services.AddSingleton<ISetupTokenProvider>(sp => sp.GetRequiredService<SetupTokenProvider>());
        var provedorEmail = configuration[$"{EmailOptions.Secao}:Provedor"] ?? "Arquivo";
        if (string.Equals(provedorEmail, "Smtp", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(configuration[$"{EmailOptions.Secao}:Smtp:Host"]))
                throw new InvalidOperationException("Email:Provedor = Smtp exige Email:Smtp:Host.");
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        }
        else services.AddSingleton<IEmailSender, ArquivoEmailSender>();

        services.Configure<ArmazenamentoOptions>(configuration.GetSection(ArmazenamentoOptions.Secao));
        services.AddSingleton<IArmazenamentoArquivos, ArmazenamentoLocal>();
        services.AddSingleton<IGeradorQrCode, GeradorQrCode>();
        services.AddScoped<IGeradorSequencia, GeradorSequencia>();
        services.AddScoped<ITransacoes, Transacoes>();
        services.AddSingleton<Application.Comunicacao.IEnvioPush, Notificacoes.EnvioPushRegistro>();
        return services;
    }
}
