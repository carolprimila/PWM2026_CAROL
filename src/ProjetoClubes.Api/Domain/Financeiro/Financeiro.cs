using ProjetoClubes.Domain.Common;

namespace ProjetoClubes.Domain.Financeiro;

public enum SituacaoMensalidade
{
    Aberta = 1,
    Paga = 2,
    Isenta = 3,
    Cancelada = 4,
}

public enum FormaPagamento
{
    Dinheiro = 1,
    Pix = 2,
    CartaoDebito = 3,
    CartaoCredito = 4,
    Boleto = 5,
    Transferencia = 6,
}

public enum TipoLancamento
{
    Receita = 1,
    Despesa = 2,
}

/// <summary>Regras financeiras do clube (encargos por atraso). Nada fixo em código.</summary>
public class ConfiguracaoFinanceira : AuditableEntity, ITenantScoped
{
    private ConfiguracaoFinanceira() { }

    public ConfiguracaoFinanceira(Guid clubeId) => ClubeId = clubeId;

    public Guid ClubeId { get; private set; }
    /// <summary>Multa única sobre o valor, aplicada após o vencimento (ex.: 2%).</summary>
    public decimal MultaPercentual { get; set; } = 2m;
    /// <summary>Juros simples ao mês, cobrados por dia de atraso (ex.: 1% a.m.).</summary>
    public decimal JurosMensalPercentual { get; set; } = 1m;
    /// <summary>Dias após o vencimento sem encargos.</summary>
    public int DiasTolerancia { get; set; }
}

/// <summary>Plano de mensalidade, normalmente um por categoria de associado.</summary>
public class PlanoMensalidade : AuditableEntity, ITenantScoped, ISoftDelete
{
    private PlanoMensalidade() { }

    public PlanoMensalidade(Guid clubeId, string nome, Guid categoriaId, decimal valorMensal, int diaVencimento)
    {
        ClubeId = clubeId;
        Nome = nome.Trim();
        CategoriaId = categoriaId;
        DefinirValores(valorMensal, diaVencimento);
        Ativo = true;
    }

    public Guid ClubeId { get; private set; }
    public string Nome { get; set; } = null!;
    public Guid CategoriaId { get; set; }
    public decimal ValorMensal { get; private set; }
    public int DiaVencimento { get; private set; }
    public bool Ativo { get; set; }

    public bool Excluido { get; private set; }
    public DateTimeOffset? ExcluidoEm { get; private set; }
    public Guid? ExcluidoPor { get; private set; }

    public void DefinirValores(decimal valorMensal, int diaVencimento)
    {
        if (valorMensal < 0) throw new DomainException("plano.valor_invalido", "O valor da mensalidade não pode ser negativo.");
        if (diaVencimento is < 1 or > 28) throw new DomainException("plano.dia_invalido", "O dia de vencimento deve estar entre 1 e 28.");
        ValorMensal = Math.Round(valorMensal, 2);
        DiaVencimento = diaVencimento;
    }

    public void MarcarExcluido(DateTimeOffset quando, Guid? por)
    {
        Excluido = true;
        ExcluidoEm = quando;
        ExcluidoPor = por;
    }
}

/// <summary>Cobrança mensal de um associado (titular), já com o adicional de dependentes quando o clube cobra.</summary>
public class Mensalidade : AuditableEntity, ITenantScoped
{
    private Mensalidade() { }

    public Mensalidade(Guid clubeId, Guid associadoId, Guid planoId, DateOnly competencia, DateOnly vencimento,
        decimal valorPlano, decimal valorDependentes, int quantidadeDependentes)
    {
        if (competencia.Day != 1) throw new DomainException("mensalidade.competencia", "A competência deve ser o primeiro dia do mês.");
        ClubeId = clubeId;
        AssociadoId = associadoId;
        PlanoId = planoId;
        Competencia = competencia;
        Vencimento = vencimento;
        ValorPlano = valorPlano;
        ValorDependentes = valorDependentes;
        QuantidadeDependentes = quantidadeDependentes;
        Situacao = SituacaoMensalidade.Aberta;
    }

    public Guid ClubeId { get; private set; }
    public Guid AssociadoId { get; private set; }
    public Guid PlanoId { get; private set; }
    public DateOnly Competencia { get; private set; }
    public DateOnly Vencimento { get; private set; }
    public decimal ValorPlano { get; private set; }
    public decimal ValorDependentes { get; private set; }
    public int QuantidadeDependentes { get; private set; }
    public decimal Valor => ValorPlano + ValorDependentes;
    public SituacaoMensalidade Situacao { get; private set; }
    public string? Motivo { get; private set; }

    public bool Vencida(DateOnly hoje) => Situacao == SituacaoMensalidade.Aberta && Vencimento < hoje;

    public void MarcarPaga()
    {
        ExigirAberta("pagar");
        Situacao = SituacaoMensalidade.Paga;
    }

    public void Estornar()
    {
        if (Situacao != SituacaoMensalidade.Paga)
            throw new DomainException("mensalidade.nao_paga", "Somente mensalidades pagas podem ter o pagamento estornado.");
        Situacao = SituacaoMensalidade.Aberta;
    }

    public void Isentar(string motivo) => Encerrar(SituacaoMensalidade.Isenta, motivo, "isentar");

    public void Cancelar(string motivo) => Encerrar(SituacaoMensalidade.Cancelada, motivo, "cancelar");

    private void Encerrar(SituacaoMensalidade nova, string motivo, string acao)
    {
        ExigirAberta(acao);
        if (string.IsNullOrWhiteSpace(motivo)) throw new DomainException("situacao.motivo_obrigatorio", "Informe o motivo.");
        Situacao = nova;
        Motivo = motivo.Trim();
    }

    private void ExigirAberta(string acao)
    {
        if (Situacao != SituacaoMensalidade.Aberta)
            throw new DomainException("mensalidade.nao_aberta", $"Não é possível {acao} uma mensalidade com situação \"{Situacao}\".");
    }
}

/// <summary>
/// Pagamento registrado pela tesouraria (confirmação manual). Integrações futuras (Pix, boleto, gateways)
/// registrarão pagamentos pelo mesmo caminho, somente após confirmação do provedor (webhook) — nunca por suposição.
/// </summary>
public class Pagamento : AuditableEntity, ITenantScoped
{
    private Pagamento() { }

    public Pagamento(Guid clubeId, Guid mensalidadeId, long numeroRecibo, decimal valor, decimal encargos, DateOnly data,
        FormaPagamento forma, string? observacao)
    {
        if (valor <= 0) throw new DomainException("pagamento.valor_invalido", "O valor pago deve ser maior que zero.");
        ClubeId = clubeId;
        MensalidadeId = mensalidadeId;
        NumeroRecibo = numeroRecibo;
        Valor = Math.Round(valor, 2);
        Encargos = Math.Round(encargos, 2);
        Data = data;
        Forma = forma;
        Observacao = observacao;
    }

    public Guid ClubeId { get; private set; }
    public Guid MensalidadeId { get; private set; }
    public Mensalidade Mensalidade { get; private set; } = null!;
    public long NumeroRecibo { get; private set; }
    public decimal Valor { get; private set; }
    /// <summary>Parte do valor referente a multa e juros.</summary>
    public decimal Encargos { get; private set; }
    public DateOnly Data { get; private set; }
    public FormaPagamento Forma { get; private set; }
    public string? Observacao { get; private set; }
    public bool Estornado { get; private set; }
    public string? MotivoEstorno { get; private set; }

    public void Estornar(string motivo)
    {
        if (Estornado) throw new DomainException("pagamento.ja_estornado", "Este pagamento já foi estornado.");
        if (string.IsNullOrWhiteSpace(motivo)) throw new DomainException("situacao.motivo_obrigatorio", "Informe o motivo do estorno.");
        Estornado = true;
        MotivoEstorno = motivo.Trim();
    }
}

public class CategoriaFinanceira : AuditableEntity, ITenantScoped, ISoftDelete
{
    private CategoriaFinanceira() { }

    public CategoriaFinanceira(Guid clubeId, string nome, TipoLancamento tipo)
    {
        ClubeId = clubeId;
        Nome = nome.Trim();
        Tipo = tipo;
    }

    public Guid ClubeId { get; private set; }
    public string Nome { get; set; } = null!;
    public TipoLancamento Tipo { get; private set; }

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

/// <summary>Receita ou despesa avulsa. Sem data de pagamento é conta a receber/pagar.</summary>
public class LancamentoFinanceiro : AuditableEntity, ITenantScoped, ISoftDelete
{
    private LancamentoFinanceiro() { }

    public LancamentoFinanceiro(Guid clubeId, TipoLancamento tipo, Guid categoriaId, string descricao, decimal valor, DateOnly vencimento)
    {
        ClubeId = clubeId;
        Tipo = tipo;
        CategoriaId = categoriaId;
        Atualizar(descricao, valor, vencimento);
    }

    public Guid ClubeId { get; private set; }
    public TipoLancamento Tipo { get; private set; }
    public Guid CategoriaId { get; set; }
    public CategoriaFinanceira Categoria { get; private set; } = null!;
    public string Descricao { get; private set; } = null!;
    public decimal Valor { get; private set; }
    public DateOnly Vencimento { get; private set; }
    public DateOnly? PagoEm { get; private set; }

    public bool Excluido { get; private set; }
    public DateTimeOffset? ExcluidoEm { get; private set; }
    public Guid? ExcluidoPor { get; private set; }

    public void Atualizar(string descricao, decimal valor, DateOnly vencimento)
    {
        if (valor <= 0) throw new DomainException("lancamento.valor_invalido", "O valor deve ser maior que zero.");
        if (string.IsNullOrWhiteSpace(descricao)) throw new DomainException("lancamento.descricao", "Informe a descrição.");
        Descricao = descricao.Trim();
        Valor = Math.Round(valor, 2);
        Vencimento = vencimento;
    }

    public void MarcarPago(DateOnly? data) => PagoEm = data;

    public void MarcarExcluido(DateTimeOffset quando, Guid? por)
    {
        Excluido = true;
        ExcluidoEm = quando;
        ExcluidoPor = por;
    }
}

/// <summary>Cálculos financeiros puros (testados em unidade).</summary>
public static class CalculoFinanceiro
{
    public static decimal Arredondar(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    /// <summary>Multa única + juros simples pró-rata (juros mensal / 30 por dia), após a tolerância.</summary>
    public static decimal Encargos(decimal valor, DateOnly vencimento, DateOnly pagamento, ConfiguracaoFinanceira cfg)
    {
        var diasAtraso = pagamento.DayNumber - vencimento.DayNumber;
        if (diasAtraso <= cfg.DiasTolerancia) return 0m;
        var multa = valor * cfg.MultaPercentual / 100m;
        var juros = valor * cfg.JurosMensalPercentual / 100m / 30m * diasAtraso;
        return Arredondar(multa + juros);
    }

    public static DateOnly Vencimento(DateOnly competencia, int dia) => new(competencia.Year, competencia.Month, dia);

    public static DateOnly Competencia(int ano, int mes) => new(ano, mes, 1);
}
