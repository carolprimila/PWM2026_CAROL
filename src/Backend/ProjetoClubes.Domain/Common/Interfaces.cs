namespace ProjetoClubes.Domain.Common;

/// <summary>Registro pertencente a um único clube. Isolado por filtro global e interceptor de gravação.</summary>
public interface ITenantScoped
{
    Guid ClubeId { get; }
}

/// <summary>Registro com exclusão lógica.</summary>
public interface ISoftDelete
{
    bool Excluido { get; }
    DateTimeOffset? ExcluidoEm { get; }
    Guid? ExcluidoPor { get; }
    void MarcarExcluido(DateTimeOffset quando, Guid? por);
}

public interface IAuditable
{
    DateTimeOffset CriadoEm { get; set; }
    Guid? CriadoPor { get; set; }
    DateTimeOffset? AtualizadoEm { get; set; }
    Guid? AtualizadoPor { get; set; }
}

/// <summary>Entidades que não devem gerar registros em LogsAuditoria (ex.: os próprios logs).</summary>
public interface INaoAuditavel;
