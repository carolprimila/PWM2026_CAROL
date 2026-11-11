using ProjetoClubes.Domain.Common;
using ProjetoClubes.Domain.Eventos;
using ProjetoClubes.Domain.Reservas;
using ProjetoClubes.Domain.Solicitacoes;

namespace ProjetoClubes.UnitTests.Dominio;

public class EventosTests
{
    private static InscricaoEvento Inscricao(int acompanhantes) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), acompanhantes, SituacaoInscricao.ListaEspera);

    [Theory]
    [InlineData(null, 500, 3, SituacaoInscricao.Confirmada)]
    [InlineData(10, 7, 3, SituacaoInscricao.Confirmada)]
    [InlineData(10, 8, 3, SituacaoInscricao.ListaEspera)]
    public void Vaga_conta_inscrito_e_acompanhantes(int? capacidade, int ocupados, int pedidos, SituacaoInscricao esperado) =>
        Assert.Equal(esperado, RegrasEventos.SituacaoParaNovaInscricao(capacidade, ocupados, pedidos));

    [Fact]
    public void Promove_lista_de_espera_em_ordem_pulando_quem_nao_cabe()
    {
        var grande = Inscricao(3); // 4 lugares
        var pequena = Inscricao(0); // 1 lugar
        var outra = Inscricao(1);   // 2 lugares
        var promovidos = RegrasEventos.Promover(10, 7, [grande, pequena, outra]).ToList();
        Assert.Equal([pequena, outra], promovidos);
    }

    [Fact]
    public void Evento_cancelado_nao_aceita_inscricao_nem_reabre()
    {
        var e = new Evento(Guid.NewGuid(), "Festa", DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(1).AddHours(4), PublicoEvento.Geral);
        e.ExigirInscricoesAbertas(DateTimeOffset.UtcNow);
        e.Cancelar("Chuva");
        Assert.Throws<DomainException>(() => e.ExigirInscricoesAbertas(DateTimeOffset.UtcNow));
        Assert.Throws<DomainException>(() => e.ReabrirInscricoes());
        var agora = DateTimeOffset.UtcNow;
        Assert.Throws<DomainException>(() => new Evento(Guid.NewGuid(), "X", agora, agora, PublicoEvento.Geral));
        var local = new Evento(Guid.NewGuid(), "Y", new DateTimeOffset(2026, 12, 7, 11, 0, 0, TimeSpan.FromHours(-3)),
            new DateTimeOffset(2026, 12, 8, 1, 0, 0, TimeSpan.FromHours(-3)), PublicoEvento.Geral);
        Assert.Equal(TimeSpan.Zero, local.Inicio.Offset);
        Assert.Equal(14, local.Inicio.Hour); // 11h em Brasília = 14h UTC
    }
}

public class ReservasTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 31, 11, 0, 0, TimeSpan.FromHours(-3));

    [Fact]
    public void Conflito_considera_intervalo_semiaberto()
    {
        // Feijoada de 31/10 11h a 01/11 02h ocupa o espaço durante todo o intervalo.
        var fim = T0.AddHours(15);
        Assert.True(Reserva.Conflita(T0, fim, T0.AddHours(14), T0.AddHours(16)));
        Assert.True(Reserva.Conflita(T0, fim, T0.AddHours(-1), T0.AddMinutes(1)));
        Assert.False(Reserva.Conflita(T0, fim, fim, fim.AddHours(2)));       // começa quando a outra termina
        Assert.False(Reserva.Conflita(T0, fim, T0.AddHours(-3), T0));         // termina quando a outra começa
    }

    [Fact]
    public void Transicoes_de_reserva()
    {
        var r = new Reserva(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, T0, T0.AddHours(3), "Aniversário", 100m, exigeAprovacao: true);
        Assert.Equal(SituacaoReserva.Pendente, r.Situacao);
        Assert.Throws<DomainException>(() => r.Recusar(" "));
        r.Aprovar();
        Assert.Throws<DomainException>(() => r.Aprovar());
        r.Cancelar("Desistência");
        Assert.False(r.Ocupa);
        Assert.Throws<DomainException>(() => r.AlterarPeriodo(T0, T0.AddHours(1)));
    }
}

public class SolicitacoesTests
{
    [Fact]
    public void Resposta_da_secretaria_coloca_em_andamento_e_encerrada_nao_aceita_resposta()
    {
        var s = new Solicitacao(Guid.NewGuid(), "2026-000001", TipoSolicitacao.Reclamacao, "Lâmpada", "Lâmpada queimada na quadra", Guid.NewGuid(), null);
        s.Responder("Anotação", Guid.NewGuid(), daAdministracao: true, interna: true, DateTimeOffset.UtcNow);
        Assert.Equal(SituacaoSolicitacao.Aberta, s.Situacao);
        s.Responder("Já pedimos a troca.", Guid.NewGuid(), daAdministracao: true, interna: false, DateTimeOffset.UtcNow);
        Assert.Equal(SituacaoSolicitacao.EmAndamento, s.Situacao);
        s.AlterarSituacao(SituacaoSolicitacao.Concluida, null, DateTimeOffset.UtcNow);
        Assert.Throws<DomainException>(() => s.Responder("Obrigado", null, false, false, DateTimeOffset.UtcNow));
        Assert.Equal(3, s.Respostas.Count);
    }
}
