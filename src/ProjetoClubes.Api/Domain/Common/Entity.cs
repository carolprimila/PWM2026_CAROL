namespace ProjetoClubes.Domain.Common;

/// <summary>Entidade base com identificador UUIDv7 (ordenável por tempo de criação).</summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.CreateVersion7();
}

/// <summary>Entidade com trilha de criação/alteração preenchida automaticamente na gravação.</summary>
public abstract class AuditableEntity : Entity, IAuditable
{
    public DateTimeOffset CriadoEm { get; set; }
    public Guid? CriadoPor { get; set; }
    public DateTimeOffset? AtualizadoEm { get; set; }
    public Guid? AtualizadoPor { get; set; }
}
