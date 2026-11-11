using ProjetoClubes.Domain.Common;

namespace ProjetoClubes.Domain.Auditoria;

public enum AcaoAuditoria
{
    Inclusao = 1,
    Alteracao = 2,
    Exclusao = 3,
}

/// <summary>Registro imutável de alteração de dados, gerado automaticamente na gravação.</summary>
public class LogAuditoria : Entity, INaoAuditavel
{
    private LogAuditoria() { }

    public LogAuditoria(Guid? clubeId, Guid? usuarioId, string entidade, string entidadeId, AcaoAuditoria acao,
        string? alteracoes, DateTimeOffset ocorridoEm, string? ip, string? correlationId)
    {
        ClubeId = clubeId;
        UsuarioId = usuarioId;
        Entidade = entidade;
        EntidadeId = entidadeId;
        Acao = acao;
        Alteracoes = alteracoes;
        OcorridoEm = ocorridoEm;
        Ip = ip;
        CorrelationId = correlationId;
    }

    public Guid? ClubeId { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public string Entidade { get; private set; } = null!;
    public string EntidadeId { get; private set; } = null!;
    public AcaoAuditoria Acao { get; private set; }
    /// <summary>JSON com valores anteriores e novos das propriedades alteradas (dados sensíveis mascarados).</summary>
    public string? Alteracoes { get; private set; }
    public DateTimeOffset OcorridoEm { get; private set; }
    public string? Ip { get; private set; }
    public string? CorrelationId { get; private set; }
}
