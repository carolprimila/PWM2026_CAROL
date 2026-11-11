using ProjetoClubes.Domain.Common;
using ProjetoClubes.Domain.Financeiro;

namespace ProjetoClubes.UnitTests.Dominio;

public class CalculoFinanceiroTests
{
    private static readonly DateOnly Vencimento = new(2026, 10, 10);

    private static ConfiguracaoFinanceira Cfg(decimal multa = 2, decimal jurosMes = 1, int tolerancia = 0) =>
        new(Guid.NewGuid()) { MultaPercentual = multa, JurosMensalPercentual = jurosMes, DiasTolerancia = tolerancia };

    [Fact]
    public void Sem_encargos_ate_o_vencimento_e_dentro_da_tolerancia()
    {
        Assert.Equal(0m, CalculoFinanceiro.Encargos(100m, Vencimento, Vencimento, Cfg()));
        Assert.Equal(0m, CalculoFinanceiro.Encargos(100m, Vencimento, Vencimento.AddDays(-5), Cfg()));
        Assert.Equal(0m, CalculoFinanceiro.Encargos(100m, Vencimento, Vencimento.AddDays(3), Cfg(tolerancia: 3)));
    }

    [Fact]
    public void Multa_unica_mais_juros_pro_rata()
    {
        // 30 dias de atraso: multa 2% (R$ 2,00) + juros 1% a.m. (R$ 1,00) = R$ 3,00
        Assert.Equal(3.00m, CalculoFinanceiro.Encargos(100m, Vencimento, Vencimento.AddDays(30), Cfg()));
        // 1 dia: R$ 2,00 + R$ 0,0333 → 2,03
        Assert.Equal(2.03m, CalculoFinanceiro.Encargos(100m, Vencimento, Vencimento.AddDays(1), Cfg()));
        // Clube sem encargos configurados
        Assert.Equal(0m, CalculoFinanceiro.Encargos(100m, Vencimento, Vencimento.AddDays(90), Cfg(0, 0)));
    }

    [Fact]
    public void Vencimento_da_competencia()
    {
        Assert.Equal(new DateOnly(2026, 2, 28), CalculoFinanceiro.Vencimento(new DateOnly(2026, 2, 1), 28));
    }
}

public class MensalidadeTests
{
    private static Mensalidade Nova() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 11, 1),
        new DateOnly(2026, 11, 10), 100m, 25m, 1);

    [Fact]
    public void Valor_inclui_dependentes_e_competencia_deve_ser_dia_1()
    {
        Assert.Equal(125m, Nova().Valor);
        Assert.Throws<DomainException>(() => new Mensalidade(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 11, 5),
            new DateOnly(2026, 11, 10), 100m, 0m, 0));
    }

    [Fact]
    public void Pagar_estornar_e_nao_pagar_duas_vezes()
    {
        var m = Nova();
        Assert.True(m.Vencida(new DateOnly(2026, 11, 11)));
        Assert.False(m.Vencida(new DateOnly(2026, 11, 10)));
        m.MarcarPaga();
        Assert.False(m.Vencida(new DateOnly(2026, 12, 1)));
        Assert.Throws<DomainException>(() => m.MarcarPaga());
        m.Estornar();
        Assert.Equal(SituacaoMensalidade.Aberta, m.Situacao);
    }

    [Fact]
    public void Isencao_exige_motivo_e_so_vale_para_aberta()
    {
        var m = Nova();
        Assert.Throws<DomainException>(() => m.Isentar(" "));
        m.Isentar("Diretoria");
        Assert.Equal(SituacaoMensalidade.Isenta, m.Situacao);
        Assert.Throws<DomainException>(() => m.Cancelar("x"));
    }

    [Fact]
    public void Plano_valida_valor_e_dia()
    {
        Assert.Throws<DomainException>(() => new PlanoMensalidade(Guid.NewGuid(), "P", Guid.NewGuid(), -1m, 10));
        Assert.Throws<DomainException>(() => new PlanoMensalidade(Guid.NewGuid(), "P", Guid.NewGuid(), 10m, 31));
    }
}
