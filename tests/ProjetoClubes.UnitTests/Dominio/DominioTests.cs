using ProjetoClubes.Domain.Clubes;
using ProjetoClubes.Domain.Common;
using ProjetoClubes.Domain.Identidade;

namespace ProjetoClubes.UnitTests.Dominio;

public class DocumentosTests
{
    [Theory]
    [InlineData("529.982.247-25", true)]
    [InlineData("52998224725", true)]
    [InlineData("529.982.247-24", false)]
    [InlineData("111.111.111-11", false)]
    [InlineData("123", false)]
    [InlineData(null, false)]
    public void Cpf(string? cpf, bool esperado) => Assert.Equal(esperado, Documentos.CpfValido(cpf));

    [Theory]
    [InlineData("11.222.333/0001-81", true)]
    [InlineData("11222333000181", true)]
    [InlineData("11.222.333/0001-80", false)]
    [InlineData("00000000000000", false)]
    public void Cnpj(string cnpj, bool esperado) => Assert.Equal(esperado, Documentos.CnpjValido(cnpj));
}

public class UsuarioTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 11, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Normaliza_email_e_rejeita_invalido()
    {
        var u = new Usuario("Ana", "  Ana@Clube.COM ");
        Assert.Equal("ana@clube.com", u.EmailNormalizado);
        Assert.Throws<DomainException>(() => new Usuario("X", "sem-arroba"));
    }

    [Fact]
    public void Bloqueia_temporariamente_apos_tentativas_e_libera_depois()
    {
        var u = new Usuario("Ana", "ana@clube.com");
        for (var i = 0; i < 4; i++) Assert.False(u.RegistrarFalhaLogin(Agora, 5, TimeSpan.FromMinutes(15)));
        Assert.True(u.RegistrarFalhaLogin(Agora, 5, TimeSpan.FromMinutes(15)));

        Assert.False(u.PodeAutenticar(Agora.AddMinutes(14)));
        Assert.True(u.PodeAutenticar(Agora.AddMinutes(16)));
    }

    [Fact]
    public void Sucesso_zera_falhas()
    {
        var u = new Usuario("Ana", "ana@clube.com");
        u.RegistrarFalhaLogin(Agora, 5, TimeSpan.FromMinutes(15));
        u.RegistrarLoginSucesso(Agora);
        Assert.Equal(0, u.TentativasFalhas);
        Assert.Equal(Agora, u.UltimoAcessoEm);
    }

    [Fact]
    public void Troca_de_senha_e_bloqueio_renovam_carimbo_de_seguranca()
    {
        var u = new Usuario("Ana", "ana@clube.com");
        var c1 = u.CarimboSeguranca;
        u.DefinirSenhaHash("hash", Agora);
        var c2 = u.CarimboSeguranca;
        u.Bloquear();
        Assert.NotEqual(c1, c2);
        Assert.NotEqual(c2, u.CarimboSeguranca);
        Assert.False(u.PodeAutenticar(Agora));
    }

    [Fact]
    public void Token_de_recuperacao_invalido_apos_troca_de_senha()
    {
        var u = new Usuario("Ana", "ana@clube.com");
        var token = new TokenRecuperacaoSenha(u.Id, "h", u.CarimboSeguranca, Agora, Agora.AddHours(1), null);
        Assert.True(token.Valido(Agora, u.CarimboSeguranca));
        u.DefinirSenhaHash("novo", Agora);
        Assert.False(token.Valido(Agora, u.CarimboSeguranca));
    }
}

public class SessaoTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 11, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Rotacao_guarda_hash_anterior_e_revogacao_encerra()
    {
        var s = new SessaoUsuario(Guid.NewGuid(), null, "h1", Agora, Agora.AddDays(14), null, null, null);
        s.Rotacionar("h2", Agora.AddMinutes(1), Agora.AddDays(15));
        Assert.Equal("h2", s.RefreshTokenHash);
        Assert.Equal("h1", s.RefreshTokenHashAnterior);
        Assert.True(s.Ativa(Agora));
        s.Revogar(Agora, "teste");
        Assert.False(s.Ativa(Agora));
        Assert.False(new SessaoUsuario(Guid.NewGuid(), null, "x", Agora, Agora.AddDays(1), null, null, null).Ativa(Agora.AddDays(2)));
    }
}

public class ClubeEPerfilTests
{
    [Fact]
    public void Clube_novo_nasce_ativo_com_configuracao()
    {
        var c = new Clube("Clube Foz 2", " foz2 ", "11.222.333/0001-81");
        Assert.Equal("FOZ2", c.Sigla);
        Assert.Equal("11222333000181", c.Cnpj);
        Assert.True(c.Ativo);
        Assert.Equal(c.Id, c.Configuracao.ClubeId);
        c.Desativar();
        Assert.False(c.Ativo);
    }

    [Fact]
    public void Perfil_padrao_nao_pode_ser_excluido_mas_personalizado_pode()
    {
        var clube = Guid.NewGuid();
        var padrao = new Perfil(clube, "Presidente", null, "presidente");
        Assert.Throws<DomainException>(() => padrao.MarcarExcluido(DateTimeOffset.UtcNow, null));

        var custom = new Perfil(clube, "Bilheteria", null);
        custom.MarcarExcluido(DateTimeOffset.UtcNow, null);
        Assert.True(custom.Excluido);
    }

    [Fact]
    public void Definir_permissoes_substitui_conjunto_sem_duplicar()
    {
        var p = new Perfil(Guid.NewGuid(), "X", null);
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        p.DefinirPermissoes([a, b, a]);
        Assert.Equal(2, p.Permissoes.Count);
        p.DefinirPermissoes([b]);
        Assert.Equal([b], p.Permissoes.Select(x => x.PermissaoId));
    }
}
