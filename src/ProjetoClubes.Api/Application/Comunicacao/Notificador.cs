using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Domain.Comunicacao;

namespace ProjetoClubes.Application.Comunicacao;

/// <summary>
/// Envio de push (Firebase Cloud Messaging / APNs). A implementação padrão só registra em log: as notificações já ficam
/// gravadas na caixa de entrada do app. Integrar FCM/APNs = nova implementação desta interface (DT-028).
/// </summary>
public interface IEnvioPush
{
    Task EnviarAsync(Guid clubeId, IReadOnlyCollection<Guid> usuarioIds, string titulo, string texto, CancellationToken ct);
}

public interface INotificador
{
    /// <summary>
    /// Grava uma notificação para cada pessoa que tem login no app e tenta o push. Com <paramref name="incluirTitular"/>,
    /// o titular de um dependente também é avisado. Chamar depois de gravar a operação principal: falha de push não a desfaz.
    /// </summary>
    Task NotificarPessoasAsync(IEnumerable<Guid> pessoaIds, TipoNotificacao tipo, string titulo, string texto, Guid? referenciaId,
        CancellationToken ct, bool incluirTitular = false);

    /// <summary>Data/hora no fuso do clube, para textos ("07/12 às 11:00").</summary>
    Task<string> FormatarAsync(DateTimeOffset quando, CancellationToken ct, string formato = "dd/MM 'às' HH:mm");
}

public sealed class Notificador(IAppDbContext db, IUsuarioAtual atual, IEnvioPush push, TimeProvider relogio, ILogger<Notificador> log)
    : INotificador
{
    public async Task NotificarPessoasAsync(IEnumerable<Guid> pessoaIds, TipoNotificacao tipo, string titulo, string texto,
        Guid? referenciaId, CancellationToken ct, bool incluirTitular = false)
    {
        var clubeId = atual.ClubeIdObrigatorio;
        var ids = pessoaIds.Distinct().ToList();
        if (ids.Count == 0) return;
        if (incluirTitular)
            ids.AddRange(await db.Dependentes.Where(d => d.ClubeId == clubeId && ids.Contains(d.PessoaId))
                .Select(d => d.AssociadoTitular.PessoaId).ToListAsync(ct));

        var usuarios = await db.Pessoas.Where(p => p.ClubeId == clubeId && ids.Contains(p.Id) && p.UsuarioId != null)
            .Select(p => p.UsuarioId!.Value).Distinct().ToListAsync(ct);
        if (usuarios.Count == 0) return;

        var agora = relogio.GetUtcNow();
        foreach (var u in usuarios) db.Notificacoes.Add(new Notificacao(clubeId, u, tipo, titulo, texto, referenciaId, agora));
        await db.SaveChangesAsync(ct);

        try { await push.EnviarAsync(clubeId, usuarios, titulo, texto, ct); }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            log.LogWarning(e, "Falha no envio push de {Tipo} para {Quantidade} usuário(s); notificações já gravadas", tipo, usuarios.Count);
        }
    }

    public async Task<string> FormatarAsync(DateTimeOffset quando, CancellationToken ct, string formato = "dd/MM 'às' HH:mm")
    {
        var clubeId = atual.ClubeIdObrigatorio;
        var fuso = await db.ConfiguracoesClube.Where(c => c.ClubeId == clubeId).Select(c => c.FusoHorario).FirstOrDefaultAsync(ct)
                   ?? "America/Sao_Paulo";
        var tz = TimeZoneInfo.TryFindSystemTimeZoneById(fuso, out var z) ? z : TimeZoneInfo.Utc;
        return TimeZoneInfo.ConvertTime(quando, tz).ToString(formato);
    }
}
