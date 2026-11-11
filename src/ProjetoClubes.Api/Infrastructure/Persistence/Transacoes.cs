using ProjetoClubes.Application.Common;

namespace ProjetoClubes.Infrastructure.Persistence;

/// <summary>
/// Serializa operações concorrentes sobre o mesmo recurso (vagas de evento, agenda de espaço, saldo de produto,
/// limite de dependentes). Com SQLite há um único processo da API usando o arquivo, então uma trava em memória por chave
/// resolve; a transação do SQLite (BEGIN IMMEDIATE) ainda garante um gravador por vez no banco.
/// </summary>
public sealed class Transacoes(AppDbContext db) : ITransacoes
{
    // Travas "listradas": número fixo de semáforos, escolhido pelo hash da chave (não cresce com o número de recursos).
    private static readonly SemaphoreSlim[] Travas = Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

    public async Task<T> ComBloqueioAsync<T>(string chave, Func<Task<T>> acao, CancellationToken ct)
    {
        var trava = Travas[(uint)StringComparer.Ordinal.GetHashCode(chave) % (uint)Travas.Length];
        await trava.WaitAsync(ct);
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var resultado = await acao();
            await tx.CommitAsync(ct);
            return resultado;
        }
        finally { trava.Release(); }
    }
}
