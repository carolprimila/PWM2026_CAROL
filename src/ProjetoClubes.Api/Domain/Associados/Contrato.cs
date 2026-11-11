using ProjetoClubes.Domain.Common;

namespace ProjetoClubes.Domain.Associados;

public enum SituacaoContrato
{
    EmAnalise = 1,
    Aprovado = 2,
    Reprovado = 3,
    /// <summary>O associado saiu do clube durante a vigência.</summary>
    Desligado = 4,
    /// <summary>Vigência terminou ou foi substituído pela renovação.</summary>
    Encerrado = 5,
}

/// <summary>
/// Contrato anual do associado. Ao ser aprovado gera as 12 parcelas (mensalidades) com o valor combinado; a renovação
/// cria o contrato do ano seguinte com os dados atualizados.
/// </summary>
public class ContratoAssociado : AuditableEntity, ITenantScoped
{
    private ContratoAssociado() { }

    public ContratoAssociado(Guid clubeId, Guid associadoId, string numero, DateOnly inicio, decimal valorMensal, int diaVencimento,
        IEnumerable<string> dependentes, Guid? contratoAnteriorId)
    {
        if (valorMensal < 0) throw new DomainException("contrato.valor", "O valor da mensalidade não pode ser negativo.");
        if (diaVencimento is < 1 or > 28) throw new DomainException("contrato.dia", "O dia de vencimento deve ser entre 1 e 28.");
        ClubeId = clubeId;
        AssociadoId = associadoId;
        Numero = numero;
        Inicio = new DateOnly(inicio.Year, inicio.Month, 1);
        Fim = Inicio.AddYears(1).AddDays(-1);
        ValorMensal = valorMensal;
        DiaVencimento = diaVencimento;
        Dependentes = dependentes.ToList();
        ContratoAnteriorId = contratoAnteriorId;
        Situacao = SituacaoContrato.EmAnalise;
    }

    public Guid ClubeId { get; private set; }
    public Guid AssociadoId { get; private set; }
    public string Numero { get; private set; } = null!;
    public SituacaoContrato Situacao { get; private set; }
    /// <summary>Primeiro dia do primeiro mês; o contrato vale 12 meses.</summary>
    public DateOnly Inicio { get; private set; }
    public DateOnly Fim { get; private set; }
    public decimal ValorMensal { get; private set; }
    public int DiaVencimento { get; private set; }
    /// <summary>Dependentes na assinatura (nome e parentesco), para o registro do contrato.</summary>
    public List<string> Dependentes { get; private set; } = [];
    public Guid? ContratoAnteriorId { get; private set; }
    public DateTimeOffset? AprovadoEm { get; private set; }
    public string? Motivo { get; private set; }

    public bool Vigente(DateOnly hoje) => Situacao == SituacaoContrato.Aprovado && Inicio <= hoje && hoje <= Fim;

    /// <summary>Competências (1º dia de cada mês) cobertas pelo contrato.</summary>
    public IEnumerable<DateOnly> Competencias() => Enumerable.Range(0, 12).Select(i => Inicio.AddMonths(i));

    public void Aprovar(DateTimeOffset agora)
    {
        if (Situacao != SituacaoContrato.EmAnalise) throw new DomainException("contrato.situacao", "Só um contrato em análise pode ser aprovado.");
        Situacao = SituacaoContrato.Aprovado;
        AprovadoEm = agora;
        Motivo = null;
    }

    public void Reprovar(string motivo)
    {
        if (Situacao != SituacaoContrato.EmAnalise) throw new DomainException("contrato.situacao", "Só um contrato em análise pode ser reprovado.");
        Motivo = Exigir(motivo);
        Situacao = SituacaoContrato.Reprovado;
    }

    public void Desligar(string motivo)
    {
        if (Situacao != SituacaoContrato.Aprovado) throw new DomainException("contrato.situacao", "Só um contrato aprovado pode ser desligado.");
        Motivo = Exigir(motivo);
        Situacao = SituacaoContrato.Desligado;
    }

    public void Encerrar(string motivo)
    {
        if (Situacao != SituacaoContrato.Aprovado) throw new DomainException("contrato.situacao", "Só um contrato aprovado pode ser encerrado.");
        Situacao = SituacaoContrato.Encerrado;
        Motivo = motivo;
    }

    private static string Exigir(string motivo) =>
        string.IsNullOrWhiteSpace(motivo) ? throw new DomainException("contrato.motivo", "Informe o motivo.") : motivo.Trim();
}
