using ProjetoClubes.Application.Arquivos;
using ProjetoClubes.Application.Carteirinhas;
using ProjetoClubes.Domain.Associados;
using ProjetoClubes.Domain.Common;
using ProjetoClubes.Domain.Dependentes;
using ProjetoClubes.Domain.Pessoas;

namespace ProjetoClubes.UnitTests.Dominio;

public class RegistroCadastralTests
{
    private static readonly DateTimeOffset Agora = DateTimeOffset.UtcNow;

    private static Associado Novo(bool exigeAprovacao)
    {
        var pessoa = new Pessoa(Guid.NewGuid(), "Ana Souza", new string('t', 32), Agora);
        return new Associado(pessoa.ClubeId, pessoa, "M1", Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today), exigeAprovacao, Agora);
    }

    [Fact]
    public void Fluxo_de_aprovacao_suspensao_e_reativacao()
    {
        var a = Novo(exigeAprovacao: true);
        Assert.Equal(SituacaoCadastral.Pendente, a.Situacao);
        a.Aprovar(Agora);
        Assert.Equal(SituacaoCadastral.Ativo, a.Situacao);
        a.Suspender("Inadimplência", Agora);
        Assert.Equal("Inadimplência", a.MotivoSituacao);
        a.Reativar(Agora);
        Assert.Equal(SituacaoCadastral.Ativo, a.Situacao);
        Assert.Null(a.MotivoSituacao);
    }

    [Fact]
    public void Transicoes_invalidas_e_motivo_obrigatorio()
    {
        var a = Novo(exigeAprovacao: false);
        Assert.Equal(SituacaoCadastral.Ativo, a.Situacao);
        Assert.Equal("situacao.transicao_invalida", Assert.Throws<DomainException>(() => a.Aprovar(Agora)).Codigo);
        Assert.Equal("situacao.motivo_obrigatorio", Assert.Throws<DomainException>(() => a.Suspender(" ", Agora)).Codigo);

        var p = Novo(exigeAprovacao: true);
        p.Rejeitar("Documentação incompleta", Agora);
        Assert.Throws<DomainException>(() => p.Reativar(Agora));
        Assert.False(p.Vigente);
    }
}

public class RegrasDependentesTests
{
    private static readonly DateOnly Hoje = new(2026, 11, 11);
    private static readonly CategoriaAssociado Categoria = new(Guid.NewGuid(), "Sócio") { PermiteDependentes = true };

    private static ConfiguracaoDependentes Cfg() => new(Guid.NewGuid());

    private static string Codigo(Action a) => Assert.Throws<DomainException>(a).Codigo;

    [Fact]
    public void Limite_padrao_e_limite_da_categoria()
    {
        var cfg = Cfg();
        cfg.LimitePadraoDependentes = 2;
        RegrasDependentes.ValidarInclusao(cfg, Categoria, Parentesco.Filho, Hoje.AddYears(-5), Hoje, 1, 0);
        Assert.Equal("dependente.limite_atingido",
            Codigo(() => RegrasDependentes.ValidarInclusao(cfg, Categoria, Parentesco.Filho, Hoje.AddYears(-5), Hoje, 2, 0)));

        var categoriaMaior = new CategoriaAssociado(Guid.NewGuid(), "Família") { LimiteDependentes = 6 };
        RegrasDependentes.ValidarInclusao(cfg, categoriaMaior, Parentesco.Filho, Hoje.AddYears(-5), Hoje, 5, 0);
    }

    [Fact]
    public void Categoria_sem_dependentes_parentesco_nao_permitido_e_conjuge_unico()
    {
        var cfg = Cfg();
        var simpatizante = new CategoriaAssociado(Guid.NewGuid(), "Simpatizante") { PermiteDependentes = false };
        Assert.Equal("dependente.categoria_sem_dependentes",
            Codigo(() => RegrasDependentes.ValidarInclusao(cfg, simpatizante, Parentesco.Filho, null, Hoje, 0, 0)));

        cfg.ParentescosPermitidos = [Parentesco.Conjuge, Parentesco.Filho];
        Assert.Equal("dependente.parentesco_nao_permitido",
            Codigo(() => RegrasDependentes.ValidarInclusao(cfg, Categoria, Parentesco.Irmao, null, Hoje, 0, 0)));

        Assert.Equal("dependente.limite_conjuge",
            Codigo(() => RegrasDependentes.ValidarInclusao(cfg, Categoria, Parentesco.Conjuge, null, Hoje, 1, 1)));
    }

    [Theory]
    [InlineData(24, true)]
    [InlineData(25, false)]
    public void Idade_maxima_de_filhos(int idade, bool permitido)
    {
        var cfg = Cfg();
        cfg.IdadeMaximaFilhos = 24;
        void Acao() => RegrasDependentes.ValidarInclusao(cfg, Categoria, Parentesco.Filho, Hoje.AddYears(-idade), Hoje, 0, 0);
        if (permitido) Acao();
        else Assert.Equal("dependente.idade_excedida", Codigo(Acao));
    }

    [Fact]
    public void Menor_nao_recebe_permissao_de_reserva_por_padrao_e_login_por_idade()
    {
        var cfg = Cfg();
        Assert.False(RegrasDependentes.PermissoesIniciais(cfg, Hoje.AddYears(-10), Hoje).SolicitacaoReservas);
        Assert.True(RegrasDependentes.PermissoesIniciais(cfg, Hoje.AddYears(-30), Hoje).SolicitacaoReservas);
        cfg.MenoresPodemSolicitarReservas = true;
        Assert.True(RegrasDependentes.PermissoesIniciais(cfg, Hoje.AddYears(-10), Hoje).SolicitacaoReservas);

        Assert.False(RegrasDependentes.PodeTerLoginProprio(cfg, Hoje.AddYears(-11), Hoje));
        Assert.True(RegrasDependentes.PodeTerLoginProprio(cfg, Hoje.AddYears(-12), Hoje));
        Assert.False(RegrasDependentes.PodeTerLoginProprio(cfg, null, Hoje));
    }

    [Fact]
    public void Nascimento_futuro_e_recusado()
    {
        Assert.Equal("pessoa.nascimento_futuro",
            Codigo(() => RegrasDependentes.ValidarIdade(Cfg(), Parentesco.Conjuge, Hoje.AddDays(1), Hoje)));
    }

    [Fact]
    public void Transferencia_encerra_vinculo_anterior()
    {
        var agora = DateTimeOffset.UtcNow;
        var clube = Guid.NewGuid();
        var pa = new Pessoa(clube, "Titular A", new string('a', 32), agora);
        var pb = new Pessoa(clube, "Titular B", new string('b', 32), agora);
        var a = new Associado(clube, pa, "A1", Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today), false, agora);
        var b = new Associado(clube, pb, "B1", Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today), false, agora);
        var dep = new Dependente(clube, new Pessoa(clube, "Filho X", new string('c', 32), agora), a, Parentesco.Filho, null, "A1-01",
            new PermissoesDependente(), false, null, agora);

        dep.TransferirTitular(b, "B1-01", "Guarda", null, agora.AddDays(1));
        Assert.Equal(b.Id, dep.AssociadoTitularId);
        Assert.Equal("B1-01", dep.NumeroIdentificacao);
        Assert.Equal(2, dep.Vinculos.Count);
        Assert.Single(dep.Vinculos, v => v.Fim is null);
        Assert.Throws<DomainException>(() => dep.TransferirTitular(b, "B1-02", "x", null, agora));
    }
}

public class PessoaECarteirinhaTests
{
    [Theory]
    [InlineData("2000-11-11", "2026-11-11", 26)]
    [InlineData("2000-11-12", "2026-11-11", 25)]
    [InlineData("2008-02-29", "2026-02-27", 17)]
    public void Calculo_de_idade(string nascimento, string hoje, int esperado) =>
        Assert.Equal(esperado, Pessoa.CalcularIdade(DateOnly.Parse(nascimento), DateOnly.Parse(hoje)));

    [Fact]
    public void Cpf_mascarado_e_cpf_invalido_recusado()
    {
        Assert.Equal("***.982.247-**", Pessoa.MascararCpf("52998224725"));
        var p = new Pessoa(Guid.NewGuid(), "Ana Souza", new string('t', 32), DateTimeOffset.UtcNow);
        Assert.Throws<DomainException>(() => p.DefinirCpf("123.456.789-00"));
        p.DefinirCpf("529.982.247-25");
        Assert.Equal("52998224725", p.Cpf);
    }

    [Fact]
    public void Codigo_do_qr_nao_contem_dados_pessoais_e_e_extraido()
    {
        var token = new string('x', 43);
        var codigo = CodigoCarteirinha.Montar(token);
        Assert.StartsWith("PCLUBES:1:", codigo);
        Assert.Equal(token, CodigoCarteirinha.Extrair(codigo));
        Assert.Equal(token, CodigoCarteirinha.Extrair(token));
        Assert.Null(CodigoCarteirinha.Extrair("PCLUBES:1:'; drop table"));
        Assert.Null(CodigoCarteirinha.Extrair("curto"));
    }

    [Fact]
    public void Tipo_de_arquivo_pela_assinatura()
    {
        Assert.Equal("pdf", TiposArquivo.Identificar("%PDF-1.7 conteudo"u8.ToArray(), TiposArquivo.Documentos)?.Extensao);
        Assert.Equal("png", TiposArquivo.Identificar([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0], TiposArquivo.Imagens)?.Extensao);
        Assert.Equal("jpg", TiposArquivo.Identificar([0xFF, 0xD8, 0xFF, 0xE0, 0], TiposArquivo.Imagens)?.Extensao);
        Assert.Null(TiposArquivo.Identificar("%PDF-1.7"u8.ToArray(), TiposArquivo.Imagens)); // PDF não serve como foto
        Assert.Null(TiposArquivo.Identificar("MZ executavel"u8.ToArray(), TiposArquivo.Documentos));
    }
}
