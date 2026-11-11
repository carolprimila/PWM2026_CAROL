using Microsoft.EntityFrameworkCore;
using ProjetoClubes.Application.Common;

namespace ProjetoClubes.Infrastructure.Persistence;

/// <summary>
/// Transação + pg_advisory_xact_lock(chave): serializa operações concorrentes sobre o mesmo recurso.
/// Roda dentro da estratégia de execução do Npgsql (exigida por causa do EnableRetryOnFailure).
/// </summary>
public sealed class Transacoes(AppDbContext db) : ITransacoes
{
    public async Task<T> ComBloqueioAsync<T>(string chave, Func<Task<T>> acao, CancellationToken ct)
    {
        var estrategia = db.Database.CreateExecutionStrategy();
        return await estrategia.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({chave}))", ct);
            var resultado = await acao();
            await tx.CommitAsync(ct);
            return resultado;
        });
    }
}
