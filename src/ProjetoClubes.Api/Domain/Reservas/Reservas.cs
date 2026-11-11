using ProjetoClubes.Domain.Common;

namespace ProjetoClubes.Domain.Reservas;

public enum TipoEspaco
{
    SalaoSocial = 1,
    Quiosque = 2,
    Churrasqueira = 3,
    Quadra = 4,
    CampoFutebol = 5,
    Piscina = 6,
    Camping = 7,
    SalaReuniao = 8,
    Outro = 99,
}

public enum SituacaoReserva
{
    Pendente = 1,
    Aprovada = 2,
    Recusada = 3,
    Cancelada = 4,
}

public class Espaco : AuditableEntity, ITenantScoped, ISoftDelete
{
    private Espaco() { }

    public Espaco(Guid clubeId, string nome, TipoEspaco tipo)
    {
        ClubeId = clubeId;
        Nome = nome.Trim();
        Tipo = tipo;
        Ativo = true;
    }

    public Guid ClubeId { get; private set; }
    public string Nome { get; set; } = null!;
    public TipoEspaco Tipo { get; set; }
    public string? Descricao { get; set; }
    public int? Capacidade { get; set; }
    public decimal ValorLocacao { get; set; }
    public decimal TaxaAdicional { get; set; }
    public bool ExigeAprovacao { get; set; } = true;
    public string? Regras { get; set; }
    /// <summary>
    /// Quantas reservas cada associado (com seus dependentes) pode ter por mês em espaços deste tipo. Nulo = sem limite.
    /// </summary>
    public int? LimiteReservasPorMes { get; set; }
    public bool Ativo { get; set; }

    public bool Excluido { get; private set; }
    public DateTimeOffset? ExcluidoEm { get; private set; }
    public Guid? ExcluidoPor { get; private set; }

    public void MarcarExcluido(DateTimeOffset quando, Guid? por)
    {
        Excluido = true;
        ExcluidoEm = quando;
        ExcluidoPor = por;
    }
}

/// <summary>
/// Reserva de um espaço em um intervalo [Inicio, Fim). Pendentes e aprovadas não podem se sobrepor no mesmo espaço —
/// regra validada na API e garantida no banco por restrição de exclusão (EX_Reservas_SemConflito).
/// </summary>
public class Reserva : AuditableEntity, ITenantScoped
{
    private Reserva() { }

    public Reserva(Guid clubeId, Guid espacoId, Guid? pessoaId, Guid? eventoId, DateTimeOffset inicio, DateTimeOffset fim,
        string? finalidade, decimal valor, bool exigeAprovacao)
    {
        if (fim <= inicio) throw new DomainException("periodo.invalido", "O término deve ser depois do início.");
        ClubeId = clubeId;
        EspacoId = espacoId;
        PessoaId = pessoaId;
        EventoId = eventoId;
        Inicio = inicio.ToUniversalTime();
        Fim = fim.ToUniversalTime();
        Finalidade = finalidade?.Trim();
        Valor = valor;
        Situacao = exigeAprovacao ? SituacaoReserva.Pendente : SituacaoReserva.Aprovada;
    }

    public Guid ClubeId { get; private set; }
    public Guid EspacoId { get; private set; }
    public Espaco Espaco { get; private set; } = null!;
    /// <summary>Associado/dependente solicitante (nulo para reservas internas do clube).</summary>
    public Guid? PessoaId { get; private set; }
    public Guid? EventoId { get; private set; }
    public DateTimeOffset Inicio { get; private set; }
    public DateTimeOffset Fim { get; private set; }
    public string? Finalidade { get; private set; }
    public decimal Valor { get; private set; }
    public SituacaoReserva Situacao { get; private set; }
    public string? Motivo { get; private set; }

    public bool Ocupa => Situacao is SituacaoReserva.Pendente or SituacaoReserva.Aprovada;

    public static bool Conflita(DateTimeOffset inicioA, DateTimeOffset fimA, DateTimeOffset inicioB, DateTimeOffset fimB) =>
        inicioA < fimB && inicioB < fimA;

    public void Aprovar()
    {
        if (Situacao != SituacaoReserva.Pendente)
            throw new DomainException("reserva.nao_pendente", "Somente reservas pendentes podem ser aprovadas.");
        Situacao = SituacaoReserva.Aprovada;
    }

    public void Recusar(string motivo) => Encerrar(SituacaoReserva.Recusada, motivo, [SituacaoReserva.Pendente]);

    public void Cancelar(string motivo) => Encerrar(SituacaoReserva.Cancelada, motivo, [SituacaoReserva.Pendente, SituacaoReserva.Aprovada]);

    public void AlterarPeriodo(DateTimeOffset inicio, DateTimeOffset fim)
    {
        if (!Ocupa) throw new DomainException("reserva.encerrada", "Esta reserva não pode mais ser alterada.");
        if (fim <= inicio) throw new DomainException("periodo.invalido", "O término deve ser depois do início.");
        Inicio = inicio.ToUniversalTime();
        Fim = fim.ToUniversalTime();
    }

    private void Encerrar(SituacaoReserva nova, string motivo, SituacaoReserva[] permitidas)
    {
        if (!permitidas.Contains(Situacao))
            throw new DomainException("reserva.transicao_invalida", $"Não é possível alterar uma reserva com situação \"{Situacao}\".");
        if (string.IsNullOrWhiteSpace(motivo)) throw new DomainException("situacao.motivo_obrigatorio", "Informe o motivo.");
        Situacao = nova;
        Motivo = motivo.Trim();
    }
}
