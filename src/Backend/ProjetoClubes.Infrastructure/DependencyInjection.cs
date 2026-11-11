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
    /// <summary>
    /// Banco SQLite num arquivo (como no VanderBakery): nada para instalar; o arquivo é criado na primeira execução.
    /// Caminho relativo = dentro da pasta da API. Sem configuração, usa "projetoclubes.db".
    /// </summary>
    public static string ConexaoSqlite(string? configurada, string pastaBase)
    {
        var b = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(
            string.IsNullOrWhiteSpace(configurada) ? "Data Source=projetoclubes.db" : configurada);
        if (b.DataSource != ":memory:" && !Path.IsPathRooted(b.DataSource))
            b.DataSource = Path.GetFullPath(Path.Combine(pastaBase, b.DataSource));
        if (b.DataSource != ":memory:") Directory.CreateDirectory(Path.GetDirectoryName(b.DataSource)!);
        b.DefaultTimeout = 30; // espera o outro gravador terminar em vez de falhar com "database is locked"
        b.ForeignKeys = true;
        return b.ToString();
    }

    /// <summary>Banco, segurança, e-mail, arquivos e notificações. <paramref name="pastaBase"/>: pasta da API, usada nos caminhos relativos do banco.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, string pastaBase)
    {
        var conexao = ConexaoSqlite(configuration.GetConnectionString("Default"), pastaBase);

        services.AddScoped<UsuarioAtual>();
        services.AddScoped<IUsuarioAtual>(sp => sp.GetRequiredService<UsuarioAtual>());
        services.AddScoped<GravacaoInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseSqlite(conexao, sqlite => sqlite.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery));
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
