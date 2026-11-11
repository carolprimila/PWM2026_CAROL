using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Domain.Auditoria;
using ProjetoClubes.Domain.Clubes;
using ProjetoClubes.Domain.Common;

namespace ProjetoClubes.Infrastructure.Persistence.Interceptors;

/// <summary>Tentativa de gravar dados de um clube diferente do clube autorizado na requisição.</summary>
public sealed class ViolacaoTenantException(string mensagem) : Exception(mensagem);

/// <summary>
/// Executado em toda gravação, nesta ordem:
/// 1. converte exclusões físicas em exclusão lógica (<see cref="ISoftDelete"/>);
/// 2. preenche trilha de criação/alteração (<see cref="IAuditable"/>);
/// 3. aplica e valida o ClubeId (<see cref="ITenantScoped"/>) — defesa em profundidade do isolamento;
/// 4. gera registros em LogsAuditoria com o diff das propriedades (dados sensíveis mascarados).
/// </summary>
public sealed class GravacaoInterceptor(IUsuarioAtual usuario, TimeProvider relogio) : SaveChangesInterceptor
{
    private static readonly HashSet<string> Sensiveis = new(StringComparer.OrdinalIgnoreCase)
    {
        "SenhaHash", "CarimboSeguranca", "RefreshTokenHash", "RefreshTokenHashAnterior", "TokenHash",
    };

    private static readonly HashSet<string> Ignoradas = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(IAuditable.CriadoEm), nameof(IAuditable.CriadoPor), nameof(IAuditable.AtualizadoEm), nameof(IAuditable.AtualizadoPor),
    };

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null) Processar(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null) Processar(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    private void Processar(DbContext contexto)
    {
        var agora = relogio.GetUtcNow();
        contexto.ChangeTracker.DetectChanges();
        var entradas = contexto.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var e in entradas) AplicarExclusaoLogica(e, agora);
        foreach (var e in entradas) AplicarTrilha(e, agora);
        foreach (var e in entradas) ValidarTenant(e);

        var logs = entradas.Where(e => e.Entity is not INaoAuditavel).Select(e => CriarLog(e, agora)).ToList();
        contexto.AddRange(logs);
    }

    private void AplicarExclusaoLogica(EntityEntry e, DateTimeOffset agora)
    {
        if (e is not { State: EntityState.Deleted, Entity: ISoftDelete soft }) return;
        e.State = EntityState.Modified;
        soft.MarcarExcluido(agora, usuario.UsuarioId);
    }

    private void AplicarTrilha(EntityEntry e, DateTimeOffset agora)
    {
        if (e.Entity is not IAuditable a) return;
        if (e.State == EntityState.Added)
        {
            a.CriadoEm = agora;
            a.CriadoPor = usuario.UsuarioId;
        }
        else if (e.State == EntityState.Modified)
        {
            a.AtualizadoEm = agora;
            a.AtualizadoPor = usuario.UsuarioId;
            e.Property(nameof(IAuditable.CriadoEm)).IsModified = false;
            e.Property(nameof(IAuditable.CriadoPor)).IsModified = false;
        }
    }

    private void ValidarTenant(EntityEntry e)
    {
        if (e.Entity is not ITenantScoped) return;
        var prop = e.Property(nameof(ITenantScoped.ClubeId));
        var podeQualquerClube = usuario.Sistema || usuario.SuperAdmin;

        if (e.State == EntityState.Added)
        {
            if ((Guid)prop.CurrentValue! == Guid.Empty)
                prop.CurrentValue = usuario.ClubeId ?? throw new ViolacaoTenantException(
                    $"Inclusão de {e.Metadata.ClrType.Name} sem clube selecionado.");
            else if (!podeQualquerClube && (Guid)prop.CurrentValue! != usuario.ClubeId)
                throw new ViolacaoTenantException($"Inclusão de {e.Metadata.ClrType.Name} em clube não autorizado.");
            return;
        }

        var original = (Guid)prop.OriginalValue!;
        if (prop.IsModified && (Guid)prop.CurrentValue! != original && !usuario.Sistema)
            throw new ViolacaoTenantException($"Não é permitido mover {e.Metadata.ClrType.Name} para outro clube.");
        if (!podeQualquerClube && original != usuario.ClubeId)
            throw new ViolacaoTenantException($"Alteração de {e.Metadata.ClrType.Name} de clube não autorizado.");
    }

    private LogAuditoria CriarLog(EntityEntry e, DateTimeOffset agora)
    {
        var acao = e.State switch
        {
            EntityState.Added => AcaoAuditoria.Inclusao,
            EntityState.Deleted => AcaoAuditoria.Exclusao,
            _ when e.Entity is ISoftDelete { Excluido: true } && e.Property(nameof(ISoftDelete.Excluido)).IsModified
                => AcaoAuditoria.Exclusao,
            _ => AcaoAuditoria.Alteracao,
        };

        var chave = e.Metadata.FindPrimaryKey()!.Properties.Select(p => e.Property(p.Name).CurrentValue?.ToString());
        var clubeId = e.Entity switch
        {
            ITenantScoped t => t.ClubeId,
            Clube c => c.Id,
            ConfiguracaoClube cfg => cfg.ClubeId,
            _ => usuario.ClubeId,
        };

        return new LogAuditoria(clubeId, usuario.UsuarioId, e.Metadata.ClrType.Name, string.Join("|", chave), acao,
            SerializarAlteracoes(e), agora, usuario.Ip, usuario.CorrelationId);
    }

    private static string? SerializarAlteracoes(EntityEntry e)
    {
        var dados = new Dictionary<string, object?>();
        var propriedades = e.Properties.Select(p => (p.Metadata.Name, Entrada: (MemberEntry)p, p.IsModified, p.OriginalValue, p.CurrentValue))
            .Concat(e.ComplexProperties.SelectMany(c => c.Properties.Select(p =>
                ($"{c.Metadata.Name}.{p.Metadata.Name}", Entrada: (MemberEntry)p, p.IsModified, p.OriginalValue, p.CurrentValue))));

        foreach (var (nome, _, modificada, original, atual) in propriedades)
        {
            var nomeCurto = nome[(nome.LastIndexOf('.') + 1)..];
            if (Ignoradas.Contains(nomeCurto)) continue;
            var sensivel = Sensiveis.Contains(nomeCurto);
            switch (e.State)
            {
                case EntityState.Added:
                    if (atual is not null) dados[nome] = sensivel ? "***" : atual;
                    break;
                case EntityState.Deleted:
                    dados[nome] = sensivel ? "***" : original;
                    break;
                default:
                    if (modificada && !Equals(original, atual))
                        dados[nome] = sensivel ? new { de = "***", para = "***" } : new { de = original, para = atual };
                    break;
            }
        }

        return dados.Count == 0 ? null : JsonSerializer.Serialize(dados);
    }
}
