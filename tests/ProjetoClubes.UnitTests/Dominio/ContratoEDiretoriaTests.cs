using ProjetoClubes.Domain.Associados;
using ProjetoClubes.Domain.Clubes;
using ProjetoClubes.Domain.Common;

namespace ProjetoClubes.UnitTests.Dominio;

public class ContratoAssociadoTests
{
    private static ContratoAssociado Novo(DateOnly inicio) =>
        new(Guid.NewGuid(), Guid.NewGuid(), "2026-0001", inicio, 150m, 10, ["Ana (Conjuge)"], null);

    [Fact]
    public void Contrato_anual_comeca_no_primeiro_dia_do_mes_e_cobre_12_competencias()
    {
        var c = Novo(new DateOnly(2026, 12, 17));
        Assert.Equal(new DateOnly(2026, 12, 1), c.Inicio);
        Assert.Equal(new DateOnly(2027, 11, 30), c.Fim);
        var comp = c.Competencias().ToList();
        Assert.Equal(12, comp.Count);
        Assert.Equal(new DateOnly(2027, 11, 1), comp[^1]);
        Assert.Equal(SituacaoContrato.EmAnalise, c.Situacao);
    }

    [Fact]
    public void Fluxo_analise_aprovado_desligado_com_motivo()
    {
        var c = Novo(new DateOnly(2026, 1, 1));
        Assert.Throws<DomainException>(() => c.Desligar("x"));
        c.Aprovar(DateTimeOffset.UtcNow);
        Assert.True(c.Vigente(new DateOnly(2026, 6, 1)));
        Assert.False(c.Vigente(new DateOnly(2027, 1, 1)));
        Assert.Throws<DomainException>(() => c.Aprovar(DateTimeOffset.UtcNow));
        Assert.Throws<DomainException>(() => c.Desligar("  "));
        c.Desligar("Mudou de cidade");
        Assert.Equal(SituacaoContrato.Desligado, c.Situacao);
        Assert.Equal("Mudou de cidade", c.Motivo);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(29)]
    public void Dia_de_vencimento_fora_de_1_a_28_e_recusado(int dia) =>
        Assert.Throws<DomainException>(() => new ContratoAssociado(Guid.NewGuid(), Guid.NewGuid(), "n", new DateOnly(2026, 1, 1), 10m, dia, [], null));
}

public class MembroDiretoriaTests
{
    [Fact]
    public void Sem_fim_informado_o_mandato_dura_dois_anos()
    {
        var m = new MembroDiretoria(Guid.NewGuid(), "Ana", "presidente", new DateOnly(2025, 1, 1), null);
        Assert.True(m.Presidente);
        Assert.Equal(new DateOnly(2026, 12, 31), m.FimMandato);
        Assert.True(m.Atual(new DateOnly(2026, 12, 31)));
        Assert.False(m.Atual(new DateOnly(2027, 1, 1)));
    }

    [Fact]
    public void Encerrar_antecipa_o_termino_e_nao_repete()
    {
        var m = new MembroDiretoria(Guid.NewGuid(), "Bruno", "Diretor social", new DateOnly(2025, 1, 1), new DateOnly(2026, 12, 31));
        m.Encerrar(new DateOnly(2026, 3, 10));
        Assert.Equal(new DateOnly(2026, 3, 10), m.TerminoEfetivo);
        Assert.False(m.Atual(new DateOnly(2026, 3, 11)));
        Assert.Throws<DomainException>(() => m.Encerrar(new DateOnly(2026, 3, 11)));
    }

    [Fact]
    public void Fim_antes_do_inicio_e_recusado() =>
        Assert.Throws<DomainException>(() => new MembroDiretoria(Guid.NewGuid(), "C", "Vice", new DateOnly(2026, 5, 1), new DateOnly(2026, 4, 1)));
}
