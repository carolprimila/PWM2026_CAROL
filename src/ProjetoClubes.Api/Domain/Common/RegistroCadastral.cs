namespace ProjetoClubes.Domain.Common;

/// <summary>Situação cadastral comum a associados e dependentes.</summary>
public enum SituacaoCadastral
{
    Pendente = 1,
    Ativo = 2,
    Suspenso = 3,
    Inativo = 4,
    Rejeitado = 5,
}

/// <summary>
/// Base de cadastros sujeitos a aprovação (associados e dependentes), com máquina de estados:
/// Pendente → Ativo | Rejeitado; Ativo → Suspenso | Inativo; Suspenso/Inativo → Ativo.
/// </summary>
public abstract class RegistroCadastral : AuditableEntity, ITenantScoped, ISoftDelete
{
    public Guid ClubeId { get; protected set; }
    public SituacaoCadastral Situacao { get; protected set; }
    public string? MotivoSituacao { get; protected set; }
    public DateTimeOffset? SituacaoAlteradaEm { get; protected set; }

    public bool Excluido { get; private set; }
    public DateTimeOffset? ExcluidoEm { get; private set; }
    public Guid? ExcluidoPor { get; private set; }

    /// <summary>Pendente, ativo ou suspenso: ainda conta para limites e unicidade.</summary>
    public bool Vigente => !Excluido && Situacao is SituacaoCadastral.Pendente or SituacaoCadastral.Ativo or SituacaoCadastral.Suspenso;

    protected void DefinirSituacaoInicial(bool exigeAprovacao, DateTimeOffset agora)
    {
        Situacao = exigeAprovacao ? SituacaoCadastral.Pendente : SituacaoCadastral.Ativo;
        SituacaoAlteradaEm = agora;
    }

    public void Aprovar(DateTimeOffset agora)
    {
        Exigir("aprovar", SituacaoCadastral.Pendente);
        Mudar(SituacaoCadastral.Ativo, null, agora);
    }

    public void Rejeitar(string motivo, DateTimeOffset agora)
    {
        Exigir("rejeitar", SituacaoCadastral.Pendente);
        Mudar(SituacaoCadastral.Rejeitado, ExigirMotivo(motivo), agora);
    }

    public void Suspender(string motivo, DateTimeOffset agora)
    {
        Exigir("suspender", SituacaoCadastral.Ativo);
        Mudar(SituacaoCadastral.Suspenso, ExigirMotivo(motivo), agora);
    }

    public void Reativar(DateTimeOffset agora)
    {
        Exigir("reativar", SituacaoCadastral.Suspenso, SituacaoCadastral.Inativo);
        Mudar(SituacaoCadastral.Ativo, null, agora);
    }

    public void Inativar(string motivo, DateTimeOffset agora)
    {
        Exigir("inativar", SituacaoCadastral.Pendente, SituacaoCadastral.Ativo, SituacaoCadastral.Suspenso);
        Mudar(SituacaoCadastral.Inativo, ExigirMotivo(motivo), agora);
    }

    public virtual void MarcarExcluido(DateTimeOffset quando, Guid? por)
    {
        Excluido = true;
        ExcluidoEm = quando;
        ExcluidoPor = por;
    }

    private void Exigir(string acao, params SituacaoCadastral[] permitidas)
    {
        if (Excluido || !permitidas.Contains(Situacao))
            throw new DomainException("situacao.transicao_invalida",
                $"Não é possível {acao} um cadastro com situação \"{Situacao}\".");
    }

    private static string ExigirMotivo(string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new DomainException("situacao.motivo_obrigatorio", "Informe o motivo.");
        return motivo.Trim();
    }

    private void Mudar(SituacaoCadastral nova, string? motivo, DateTimeOffset agora)
    {
        Situacao = nova;
        MotivoSituacao = motivo;
        SituacaoAlteradaEm = agora;
    }
}

/// <summary>Linha de histórico de negócio (legível para a administração), complementar aos logs de auditoria.</summary>
public abstract class RegistroHistorico : Entity, ITenantScoped, INaoAuditavel
{
    public Guid ClubeId { get; protected set; }
    public string Evento { get; protected set; } = null!;
    public string Descricao { get; protected set; } = null!;
    public Guid? UsuarioId { get; protected set; }
    public DateTimeOffset OcorridoEm { get; protected set; }
}

/// <summary>Contador por clube (ex.: matrícula). Incrementado de forma atômica no banco.</summary>
public class SequenciaClube : INaoAuditavel
{
    public Guid ClubeId { get; private set; }
    public string Nome { get; private set; } = null!;
    public long Valor { get; private set; }
}
