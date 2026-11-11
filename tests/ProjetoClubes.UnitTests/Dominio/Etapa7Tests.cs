using ProjetoClubes.Domain.Assembleias;
using ProjetoClubes.Domain.Common;
using ProjetoClubes.Domain.Comunicacao;
using ProjetoClubes.Domain.Operacional;

namespace ProjetoClubes.UnitTests.Dominio;

public class AssembleiasTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 11, 12, 0, 0, TimeSpan.Zero);

    private static Assembleia Nova(bool exigeAdimplencia = false) =>
        new(Guid.NewGuid(), "AGO 2026", TipoAssembleia.Ordinaria, Agora.AddDays(10), "Salão social", "Edital", exigeAdimplencia);

    [Theory]
    [InlineData(true, true, 0, false, null)]
    [InlineData(true, true, 2, false, null)]
    [InlineData(true, true, 2, true, "Possui mensalidades vencidas.")]
    [InlineData(true, false, 0, true, "A categoria do associado não tem direito a voto.")]
    [InlineData(false, true, 0, true, "Cadastro não está ativo.")]
    public void Aptidao_para_votar_segue_estatuto(bool ativo, bool categoriaVota, int vencidas, bool exigeAdimplencia, string? motivo) =>
        Assert.Equal(motivo, RegrasVoto.MotivoInapto(ativo, categoriaVota, vencidas, exigeAdimplencia));

    [Fact]
    public void Ciclo_completo_com_votacao_decisao_e_ata_imutavel()
    {
        var a = Nova();
        Assert.Throws<DomainException>(() => a.Convocar(Agora)); // sem pauta
        var contas = a.AdicionarPauta("Aprovação das contas", null, comVotacao: true, opcoes: null);
        var informes = a.AdicionarPauta("Informes", null, comVotacao: false, opcoes: null);
        Assert.Equal(["Sim", "Não", "Abstenção"], contas.Opcoes);
        a.Convocar(Agora);
        Assert.Throws<DomainException>(() => a.AbrirVotacao(contas.Id)); // ainda não instalada
        a.Instalar();
        Assert.Throws<DomainException>(() => a.AbrirVotacao(informes.Id)); // pauta sem votação
        Assert.Throws<DomainException>(() => a.RegistrarDecisao(contas.Id, "Aprovadas")); // votação nem aberta
        a.RegistrarDecisao(informes.Id, "Diretoria apresentou o calendário de obras.");
        a.AbrirVotacao(contas.Id);
        contas.ExigirOpcaoValida(0);
        Assert.Throws<DomainException>(() => contas.ExigirOpcaoValida(3));
        Assert.Throws<DomainException>(() => a.RegistrarDecisao(contas.Id, "Aprovadas")); // votação aberta
        Assert.Throws<DomainException>(() => a.Encerrar(Agora)); // votação aberta
        a.EncerrarVotacao(contas.Id);
        Assert.Throws<DomainException>(() => contas.ExigirOpcaoValida(0)); // votação fechada
        a.RegistrarDecisao(contas.Id, "Contas aprovadas por maioria.");
        Assert.Throws<DomainException>(() => a.RegistrarAta("Ata", null, Agora)); // antes do encerramento
        a.Encerrar(Agora);
        a.RegistrarAta("Ata da AGO", null, Agora);
        Assert.Throws<DomainException>(() => a.RegistrarAta("Outra", null, Agora));
        Assert.Throws<DomainException>(() => a.AdicionarPauta("Nova", null, false, null));
    }

    [Fact]
    public void Regra_de_adimplencia_nao_muda_depois_da_convocacao()
    {
        var a = Nova(exigeAdimplencia: false);
        a.AdicionarPauta("Eleição", null, true, ["Chapa 1", "Chapa 2", "Branco"]);
        a.Convocar(Agora);
        Assert.Throws<DomainException>(() => a.Alterar(a.Titulo, a.Tipo, a.DataHora, a.Local, a.Convocacao, exigeAdimplencia: true));
        a.Alterar("AGO 2026 — nova data", a.Tipo, a.DataHora.AddDays(1), a.Local, a.Convocacao, exigeAdimplencia: false);
    }

    [Fact]
    public void Votacao_precisa_de_duas_opcoes_distintas()
    {
        var a = Nova();
        Assert.Throws<DomainException>(() => a.AdicionarPauta("X", null, true, ["Sim", "sim", " "]));
    }

    [Fact]
    public void Cancelamento_so_antes_da_instalacao_e_com_motivo()
    {
        var a = Nova();
        a.AdicionarPauta("Informes", null, false, null);
        Assert.Throws<DomainException>(() => a.Cancelar(" "));
        a.Convocar(Agora);
        a.Instalar();
        Assert.Throws<DomainException>(() => a.Cancelar("Chuva"));
    }
}

public class EstoquePatrimonioFuncionariosTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 11, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Saida_maior_que_saldo_e_recusada_e_ajuste_define_o_saldo()
    {
        var p = new Produto(Guid.NewGuid(), "Cloro", "kg", 10, "Piscina");
        var e = p.Movimentar(TipoMovimentacaoEstoque.Entrada, 25, null, null, Agora);
        Assert.Equal(25, e.SaldoApos);
        var ex = Assert.Throws<DomainException>(() => p.Movimentar(TipoMovimentacaoEstoque.Saida, 30, null, null, Agora));
        Assert.Equal("estoque.insuficiente", ex.Codigo);
        p.Movimentar(TipoMovimentacaoEstoque.Saida, 20, "Tratamento semanal", null, Agora);
        Assert.True(p.AbaixoDoMinimo);
        Assert.Throws<DomainException>(() => p.Movimentar(TipoMovimentacaoEstoque.Ajuste, 8, null, null, Agora)); // ajuste sem motivo
        var ajuste = p.Movimentar(TipoMovimentacaoEstoque.Ajuste, 8, "Inventário", null, Agora);
        Assert.Equal(3, ajuste.Quantidade); // variação registrada com sinal
        Assert.Equal(8, p.Quantidade);
        Assert.Throws<DomainException>(() => p.Movimentar(TipoMovimentacaoEstoque.Entrada, 0, null, null, Agora));
    }

    [Fact]
    public void Compra_atendida_uma_unica_vez()
    {
        var s = new SolicitacaoCompra(Guid.NewGuid(), Guid.NewGuid(), 10, null);
        s.Atender(10, 150m);
        Assert.Throws<DomainException>(() => s.Atender(10, 150m));
        Assert.Throws<DomainException>(() => s.Cancelar());
        Assert.Throws<DomainException>(() => new SolicitacaoCompra(Guid.NewGuid(), Guid.NewGuid(), 0, null));
    }

    [Fact]
    public void Bem_em_manutencao_volta_ao_uso_e_baixado_nao_muda_mais()
    {
        var b = new Bem(Guid.NewGuid(), "PAT-000001", "Cortador de grama", null, "Depósito", 2500m, new DateOnly(2024, 3, 1), EstadoConservacao.Bom);
        Assert.Null(b.Movimentar("Depósito", EstadoConservacao.Bom));
        Assert.Contains("Localização", b.Movimentar("Garagem", EstadoConservacao.Regular));
        var m = b.AbrirManutencao("Troca da lâmina", new DateOnly(2026, 11, 1), "Oficina Silva");
        Assert.Equal(SituacaoBem.EmManutencao, b.Situacao);
        Assert.Throws<DomainException>(() => b.AbrirManutencao("Outra", new DateOnly(2026, 11, 2), null));
        Assert.Throws<DomainException>(() => b.Baixar("Quebrado"));
        Assert.Throws<DomainException>(() => b.ConcluirManutencao(m.Id, new DateOnly(2026, 10, 1), 100m, EstadoConservacao.Bom)); // antes do início
        b.ConcluirManutencao(m.Id, new DateOnly(2026, 11, 5), 180m, EstadoConservacao.Bom);
        Assert.Equal(SituacaoBem.EmUso, b.Situacao);
        b.Baixar("Substituído por modelo novo");
        Assert.Throws<DomainException>(() => b.Movimentar("Sede", EstadoConservacao.Ruim));
    }

    [Fact]
    public void Ferias_nao_sobrepoem_e_definem_a_situacao_do_dia()
    {
        var f = new Funcionario(Guid.NewGuid(), "Marta Lima", null, "Recepcionista", "Secretaria", new DateOnly(2023, 2, 1));
        f.RegistrarAusencia(TipoAusencia.Ferias, new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 30), null);
        var ex = Assert.Throws<DomainException>(() =>
            f.RegistrarAusencia(TipoAusencia.Afastamento, new DateOnly(2026, 12, 30), new DateOnly(2027, 1, 5), null));
        Assert.Equal("funcionario.ausencia_sobreposta", ex.Codigo);
        f.AtualizarSituacao(new DateOnly(2026, 12, 15));
        Assert.Equal(SituacaoFuncionario.Ferias, f.Situacao);
        f.AtualizarSituacao(new DateOnly(2027, 1, 2));
        Assert.Equal(SituacaoFuncionario.Ativo, f.Situacao);
        Assert.Throws<DomainException>(() => f.Desligar(new DateOnly(2022, 1, 1)));
        f.Desligar(new DateOnly(2027, 2, 1));
        f.AtualizarSituacao(new DateOnly(2026, 12, 15));
        Assert.Equal(SituacaoFuncionario.Desligado, f.Situacao);
        Assert.Throws<DomainException>(() => new Funcionario(Guid.NewGuid(), "X", "111.111.111-11", "Zelador", null, new DateOnly(2024, 1, 1)));
    }

    [Fact]
    public void Comunicado_publicado_nao_troca_publico_nem_confirmacao()
    {
        var c = new Comunicado(Guid.NewGuid(), "Piscina fechada", "Manutenção na sexta.", PublicoComunicado.Todos, false, false, null);
        c.Publicar(Agora);
        Assert.Throws<DomainException>(() => c.Publicar(Agora));
        Assert.Throws<DomainException>(() => c.Alterar("Piscina fechada", "x", PublicoComunicado.Titulares, false, false, null));
        c.Alterar("Piscina fechada (atualizado)", "Manutenção na sexta e no sábado.", PublicoComunicado.Todos, false, true, null);
        Assert.True(c.VisivelEm(Agora));
        var expirado = new Comunicado(Guid.NewGuid(), "A", "B", PublicoComunicado.Todos, false, false, Agora.AddDays(-1));
        Assert.Throws<DomainException>(() => expirado.Publicar(Agora));
    }
}
