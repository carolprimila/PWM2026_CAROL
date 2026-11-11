using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjetoClubes.Application.Seguranca;
using ProjetoClubes.Domain.Identidade;
using ProjetoClubes.Infrastructure.Seguranca;

namespace ProjetoClubes.Infrastructure.Persistence.Seed;

public sealed class BancoOptions
{
    public const string Secao = "Banco";

    /// <summary>Aplica migrations pendentes na partida da API (recomendado só em desenvolvimento).</summary>
    public bool AplicarMigrationsAoIniciar { get; set; }
}

/// <summary>
/// Rotina de partida: migrations (opcional), sincronização do catálogo de permissões, token de
/// inicialização (se não houver Superadmin) e dados de demonstração (opcional, só com Seed__Demo=true).
/// </summary>
public static class InicializadorBanco
{
    public static async Task ExecutarAsync(IServiceProvider raiz, CancellationToken ct = default)
    {
        await using var escopo = raiz.CreateAsyncScope();
        var sp = escopo.ServiceProvider;
        sp.GetRequiredService<UsuarioAtual>().DefinirSistema();
        var db = sp.GetRequiredService<AppDbContext>();
        var config = sp.GetRequiredService<IConfiguration>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("ProjetoClubes.Inicializacao");

        if (config.GetSection(BancoOptions.Secao).Get<BancoOptions>()?.AplicarMigrationsAoIniciar == true)
        {
            var pendentes = (await db.Database.GetPendingMigrationsAsync(ct)).ToList();
            if (pendentes.Count > 0)
            {
                logger.LogInformation("Aplicando {Quantidade} migration(s): {Migrations}", pendentes.Count, pendentes);
                await db.Database.MigrateAsync(ct);
            }
        }

        await SincronizarPermissoesAsync(db, logger, ct);
        await GarantirPadroesDosClubesAsync(sp, db, ct);

        if (!await db.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.SuperAdmin, ct))
        {
            var token = sp.GetRequiredService<SetupTokenProvider>().Ativar(config["Setup:Token"]);
            logger.LogWarning(
                "Sistema ainda não inicializado. Para criar o primeiro Superadmin, use POST /api/v1/setup/inicializar " +
                "com o token de inicialização: {TokenInicializacao} (válido até o uso ou reinício da API).", token);
        }

        if (config.GetValue<bool>("Seed:Demo"))
            await DadosDemonstracao.CriarAsync(sp, config["Seed:SenhaDemo"], logger, ct);
    }

    /// <summary>Clubes criados antes de um módulo existir recebem as configurações iniciais dele (idempotente).</summary>
    private static async Task GarantirPadroesDosClubesAsync(IServiceProvider sp, AppDbContext db, CancellationToken ct)
    {
        var inicializador = sp.GetRequiredService<Application.Cadastros.IInicializadorClube>();
        foreach (var clubeId in await db.Clubes.IgnoreQueryFilters().Select(c => c.Id).ToListAsync(ct))
            await inicializador.GarantirPadroesAsync(clubeId, ct);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Insere permissões novas e atualiza descrições. Códigos removidos do código são apenas reportados.</summary>
    internal static async Task SincronizarPermissoesAsync(AppDbContext db, ILogger logger, CancellationToken ct)
    {
        var existentes = await db.Permissoes.ToDictionaryAsync(p => p.Codigo, ct);
        var novas = 0;
        foreach (var def in Permissoes.Todas)
        {
            if (existentes.TryGetValue(def.Codigo, out var p))
            {
                p.Modulo = def.Modulo;
                p.Descricao = def.Descricao;
            }
            else
            {
                db.Permissoes.Add(new Permissao(def.Codigo, def.Modulo, def.Descricao));
                novas++;
            }
        }

        var obsoletas = existentes.Keys.Except(Permissoes.Codigos).ToList();
        if (obsoletas.Count > 0)
            logger.LogWarning("Permissões no banco que não existem mais no catálogo: {Codigos}", obsoletas);

        await db.SaveChangesAsync(ct);
        if (novas > 0) logger.LogInformation("{Quantidade} permissão(ões) adicionada(s) ao catálogo", novas);
    }
}
