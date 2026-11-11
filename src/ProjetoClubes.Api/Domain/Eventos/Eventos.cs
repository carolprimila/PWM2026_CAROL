using ProjetoClubes.Domain.Common;

namespace ProjetoClubes.Domain.Eventos;

public enum PublicoEvento
{
    /// <summary>Somente associados titulares.</summary>
    Associados = 1,
    /// <summary>Associados e dependentes (com permissão de participar de eventos).</summary>
    AssociadosEDependentes = 2,
    /// <summary>Aberto a todos do clube; não associados entram como acompanhantes.</summary>
    Geral = 3,
}

public enum SituacaoEvento
{
    InscricoesAbertas = 1,
    InscricoesEncerradas = 2,
    Cancelado = 3,
}

public enum SituacaoInscricao
{
    Confirmada = 1,
    ListaEspera = 2,
    Cancelada = 3,
}

public class Evento : AuditableEntity, ITenantScoped, ISoftDelete
{
    private Evento() { }

    public Evento(Guid clubeId, string nome, DateTimeOffset inicio, DateTimeOffset fim, PublicoEvento publico)
    {
        ClubeId = clubeId;
        Nome = nome.Trim();
        DefinirPeriodo(inicio, fim);
        Publico = publico;
        Situacao = SituacaoEvento.InscricoesAbertas;
    }

    public Guid ClubeId { get; private set; }
    public string Nome { get; set; } = null!;
    public string? Descricao { get; set; }
    public string? Local { get; set; }
    /// <summary>Espaço ocupado pelo evento (gera uma reserva aprovada no período).</summary>
    public Guid? EspacoId { get; set; }
    public DateTimeOffset Inicio { get; private set; }
    public DateTimeOffset Fim { get; private set; }
    /// <summary>Vagas (pessoas, contando acompanhantes). Nulo = sem limite.</summary>
    public int? Capacidade { get; set; }
    public PublicoEvento Publico { get; set; }
    /// <summary>Valor por pessoa, informativo (cobrança fica com a tesouraria).</summary>
    public decimal Valor { get; set; }
    public int MaximoAcompanhantes { get; set; }
    public SituacaoEvento Situacao { get; private set; }
    public string? MotivoCancelamento { get; private set; }
    /// <summary>Quando o clube divulgou o evento aos associados (aviso no app e e-mail).</summary>
    public DateTimeOffset? DivulgadoEm { get; set; }

    public bool Excluido { get; private set; }
    public DateTimeOffset? ExcluidoEm { get; private set; }
    public Guid? ExcluidoPor { get; private set; }

    public void DefinirPeriodo(DateTimeOffset inicio, DateTimeOffset fim)
    {
        if (fim <= inicio) throw new DomainException("periodo.invalido", "O término deve ser depois do início.");
        // Datas são gravadas em UTC (timestamptz); o fuso de quem enviou é preservado só na conversão.
        Inicio = inicio.ToUniversalTime();
        Fim = fim.ToUniversalTime();
    }

    public void ExigirInscricoesAbertas(DateTimeOffset agora)
    {
        if (Situacao != SituacaoEvento.InscricoesAbertas)
            throw new DomainException("evento.inscricoes_fechadas", "As inscrições deste evento não estão abertas.");
        if (agora >= Fim) throw new DomainException("evento.encerrado", "Este evento já terminou.");
    }

    public void EncerrarInscricoes() => Situacao = Situacao == SituacaoEvento.Cancelado ? Situacao : SituacaoEvento.InscricoesEncerradas;

    public void ReabrirInscricoes()
    {
        if (Situacao == SituacaoEvento.Cancelado) throw new DomainException("evento.cancelado", "Evento cancelado não pode ser reaberto.");
        Situacao = SituacaoEvento.InscricoesAbertas;
    }

    public void Cancelar(string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo)) throw new DomainException("situacao.motivo_obrigatorio", "Informe o motivo do cancelamento.");
        Situacao = SituacaoEvento.Cancelado;
        MotivoCancelamento = motivo.Trim();
    }

    public void MarcarExcluido(DateTimeOffset quando, Guid? por)
    {
        Excluido = true;
        ExcluidoEm = quando;
        ExcluidoPor = por;
    }
}

public class InscricaoEvento : AuditableEntity, ITenantScoped
{
    private InscricaoEvento() { }

    public InscricaoEvento(Guid clubeId, Guid eventoId, Guid pessoaId, int acompanhantes, SituacaoInscricao situacao)
    {
        ClubeId = clubeId;
        EventoId = eventoId;
        PessoaId = pessoaId;
        Acompanhantes = acompanhantes;
        Situacao = situacao;
    }

    public Guid ClubeId { get; private set; }
    public Guid EventoId { get; private set; }
    public Guid PessoaId { get; private set; }
    public int Acompanhantes { get; private set; }
    public SituacaoInscricao Situacao { get; private set; }
    public DateTimeOffset? PresencaEm { get; private set; }

    /// <summary>Lugares ocupados: a pessoa e seus acompanhantes.</summary>
    public int Lugares => 1 + Acompanhantes;

    public void Cancelar() => Situacao = SituacaoInscricao.Cancelada;

    public void Promover() => Situacao = SituacaoInscricao.Confirmada;

    public void RegistrarPresenca(DateTimeOffset agora)
    {
        if (Situacao != SituacaoInscricao.Confirmada)
            throw new DomainException("inscricao.nao_confirmada", "A inscrição desta pessoa não está confirmada.");
        PresencaEm ??= agora;
    }
}

/// <summary>Regras de vagas (puras, testadas em unidade).</summary>
public static class RegrasEventos
{
    /// <summary>Confirma se couber; senão vai para a lista de espera.</summary>
    public static SituacaoInscricao SituacaoParaNovaInscricao(int? capacidade, int lugaresOcupados, int lugaresPedidos) =>
        capacidade is null || lugaresOcupados + lugaresPedidos <= capacidade
            ? SituacaoInscricao.Confirmada
            : SituacaoInscricao.ListaEspera;

    /// <summary>Quem da lista de espera (em ordem de chegada) cabe nas vagas liberadas.</summary>
    public static IEnumerable<InscricaoEvento> Promover(int? capacidade, int lugaresOcupados, IEnumerable<InscricaoEvento> espera)
    {
        var ocupados = lugaresOcupados;
        foreach (var i in espera)
        {
            if (capacidade is not null && ocupados + i.Lugares > capacidade) continue;
            ocupados += i.Lugares;
            yield return i;
        }
    }
}
