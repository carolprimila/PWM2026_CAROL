using ProjetoClubes.Domain.Common;

namespace ProjetoClubes.Domain.Acesso;

public enum TipoMovimentoAcesso
{
    Entrada = 1,
    Saida = 2,
}

/// <summary>Registro de entrada/saída (ou tentativa recusada) na portaria, com o funcionário responsável.</summary>
public class AcessoClube : Entity, ITenantScoped, INaoAuditavel
{
    private AcessoClube() { }

    public AcessoClube(Guid clubeId, Guid? pessoaId, Guid? associadoId, Guid? dependenteId, TipoMovimentoAcesso tipo,
        bool autorizado, string? motivo, Guid? registradoPor, DateTimeOffset agora, string? observacao)
    {
        ClubeId = clubeId;
        PessoaId = pessoaId;
        AssociadoId = associadoId;
        DependenteId = dependenteId;
        Tipo = tipo;
        Autorizado = autorizado;
        Motivo = motivo;
        RegistradoPor = registradoPor;
        OcorridoEm = agora;
        Observacao = observacao;
    }

    public Guid ClubeId { get; private set; }
    /// <summary>Nulo quando o código lido não corresponde a ninguém deste clube.</summary>
    public Guid? PessoaId { get; private set; }
    public Guid? AssociadoId { get; private set; }
    public Guid? DependenteId { get; private set; }
    public TipoMovimentoAcesso Tipo { get; private set; }
    public bool Autorizado { get; private set; }
    public string? Motivo { get; private set; }
    public Guid? RegistradoPor { get; private set; }
    public DateTimeOffset OcorridoEm { get; private set; }
    public string? Observacao { get; private set; }
}
