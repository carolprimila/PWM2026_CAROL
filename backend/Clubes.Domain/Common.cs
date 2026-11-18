namespace Clubes.Domain;

/// <summary>Base de todas as entidades persistidas.</summary>
public abstract class Entity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Entidade pertencente a um clube. O isolamento é aplicado por filtro global e por chaves compostas no banco.</summary>
public interface ITenantEntity
{
    Guid ClubId { get; set; }
}

/// <summary>Exclusão lógica: o registro nunca é removido fisicamente.</summary>
public interface ISoftDelete
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}

/// <summary>Entidades que não entram na auditoria automática (logs técnicos, tokens, etc.).</summary>
public interface INotAudited;

public abstract class TenantEntity : Entity, ITenantEntity
{
    public Guid ClubId { get; set; }
}

/// <summary>Erro de regra de negócio. A API converte em resposta HTTP padronizada.</summary>
public class DomainException(string message, string code = "regra_de_negocio", int status = 422) : Exception(message)
{
    public string Code { get; } = code;
    public int Status { get; } = status;

    public static DomainException NotFound(string what) => new($"{what} não encontrado(a).", "nao_encontrado", 404);
    public static DomainException Forbidden(string message = "Você não tem permissão para executar esta operação.") => new(message, "acesso_negado", 403);
    public static DomainException Conflict(string message) => new(message, "conflito", 409);
    public static DomainException NotConfigured(string what) => new($"Configuração pendente: {what}. Defina este parâmetro em Configurações antes de continuar.", "configuracao_pendente", 422);
}
