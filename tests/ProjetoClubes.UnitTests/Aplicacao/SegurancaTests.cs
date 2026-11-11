using System.IdentityModel.Tokens.Jwt;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Options;
using ProjetoClubes.Application.Autenticacao;
using ProjetoClubes.Application.Clubes;
using ProjetoClubes.Application.Seguranca;
using ProjetoClubes.Domain.Identidade;
using ProjetoClubes.Infrastructure.Seguranca;

namespace ProjetoClubes.UnitTests.Aplicacao;

public class PermissoesTests
{
    [Fact]
    public void Combinacao_une_perfis_concede_e_negacao_prevalece()
    {
        var efetivas = PermissaoResolver.Combinar(
            ["associados.visualizar", "financeiro.visualizar"],
            [("eventos.gerenciar", true), ("financeiro.visualizar", false), ("eventos.gerenciar", false)]);

        Assert.Contains("associados.visualizar", efetivas);
        Assert.DoesNotContain("financeiro.visualizar", efetivas);
        Assert.DoesNotContain("eventos.gerenciar", efetivas); // negação vence concessão
    }

    [Fact]
    public void Catalogo_sem_codigos_duplicados()
    {
        Assert.Equal(Permissoes.Todas.Count, Permissoes.Codigos.Count);
    }

    [Fact]
    public void Modelos_de_perfil_usam_apenas_permissoes_do_catalogo()
    {
        foreach (var modelo in ModelosPerfis.Todos)
            Assert.All(modelo.Permissoes, c => Assert.Contains(c, Permissoes.Codigos));
    }

    [Fact]
    public void Associado_e_dependente_nao_recebem_permissoes_administrativas()
    {
        foreach (var chave in new[] { ModelosPerfis.Associado, ModelosPerfis.Dependente })
            Assert.Equal([Permissoes.Portal.Acessar], ModelosPerfis.Todos.Single(m => m.Chave == chave).Permissoes);
    }

    [Fact]
    public void Instanciar_perfis_para_clube_cria_todos_os_modelos()
    {
        var clube = Guid.NewGuid();
        var catalogo = Permissoes.Codigos.ToDictionary(c => c, _ => Guid.NewGuid());
        var perfis = ModelosPerfis.InstanciarPara(clube, catalogo);
        Assert.Equal(ModelosPerfis.Todos.Count, perfis.Count);
        Assert.All(perfis, p => Assert.Equal(clube, p.ClubeId));
        Assert.Equal(Permissoes.Codigos.Count, perfis.Single(p => p.ChaveModelo == ModelosPerfis.Presidente).Permissoes.Count);
    }
}

public class TokensEHashTests
{
    private static TokenService CriarTokenService() => new(Options.Create(new JwtOptions
    {
        Chave = new string('k', 48), MinutosAccessToken = 15, DiasRefreshToken = 14,
    }), TimeProvider.System);

    [Fact]
    public void Access_token_contem_usuario_sessao_e_clube()
    {
        var usuario = new Usuario("Ana", "ana@clube.com");
        var clube = Guid.NewGuid();
        var sessao = new SessaoUsuario(usuario.Id, clube, "h", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), null, null, null);

        var gerado = CriarTokenService().GerarAccessToken(usuario, sessao);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(gerado.Token);

        Assert.Equal(usuario.Id.ToString(), jwt.Subject);
        Assert.Equal(sessao.Id.ToString(), jwt.Claims.Single(c => c.Type == ClaimsProjeto.Sessao).Value);
        Assert.Equal(clube.ToString(), jwt.Claims.Single(c => c.Type == ClaimsProjeto.Clube).Value);
        Assert.DoesNotContain(jwt.Claims, c => c.Type == ClaimsProjeto.SuperAdmin);
        Assert.DoesNotContain(jwt.Claims, c => c.Value.Contains("ana@clube.com")); // sem dados pessoais além do nome
    }

    [Fact]
    public void Chave_jwt_curta_e_recusada()
    {
        Assert.Throws<InvalidOperationException>(() => TokenService.CriarChave("curta"));
    }

    [Fact]
    public void Token_opaco_e_aleatorio_e_hash_e_deterministico()
    {
        var svc = CriarTokenService();
        var a = svc.GerarTokenOpaco();
        Assert.NotEqual(a, svc.GerarTokenOpaco());
        Assert.True(a.Length >= 43);
        Assert.Equal(svc.Hash(a), svc.Hash(a));
        Assert.Equal(64, svc.Hash(a).Length);
    }

    [Fact]
    public void Hash_de_senha_verifica_e_nao_guarda_texto_puro()
    {
        var h = new HashSenha();
        var hash = h.Gerar("SenhaForte123");
        Assert.DoesNotContain("SenhaForte123", hash);
        Assert.True(h.Verificar(hash, "SenhaForte123").Valida);
        Assert.False(h.Verificar(hash, "outra").Valida);
        Assert.False(h.Verificar("lixo-invalido", "x").Valida);
    }

    [Fact]
    public void Token_de_setup_valida_e_e_descartado()
    {
        var p = new SetupTokenProvider();
        Assert.False(p.Disponivel);
        var token = p.Ativar(null);
        Assert.False(p.Validar("errado"));
        Assert.True(p.Validar(token));
        p.Consumir();
        Assert.False(p.Disponivel);
        Assert.False(p.Validar(token));
    }
}

public class ValidadoresTests
{
    [Theory]
    [InlineData("curta1", false)]
    [InlineData("somenteletrasaqui", false)]
    [InlineData("1234567890123", false)]
    [InlineData("SenhaBoa2026", true)]
    public void Politica_de_senha(string senha, bool valida)
    {
        var r = new RedefinirSenhaRequestValidator().TestValidate(new RedefinirSenhaRequest("tok", senha));
        Assert.Equal(valida, !r.Errors.Any(e => e.PropertyName == "NovaSenha"));
    }

    [Fact]
    public void Clube_com_cnpj_uf_e_cep_invalidos()
    {
        var r = new SalvarClubeRequestValidator().TestValidate(new SalvarClubeRequest("Clube", "C1", "11.222.333/0001-80",
            new EnderecoDto(null, null, null, null, null, "XX", "123"), "123", "x", null));
        r.ShouldHaveValidationErrorFor(x => x.Cnpj);
        r.ShouldHaveValidationErrorFor(x => x.Endereco!.Uf);
        r.ShouldHaveValidationErrorFor(x => x.Endereco!.Cep);
        r.ShouldHaveValidationErrorFor(x => x.Telefone);
        r.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Configuracao_exige_cor_hexadecimal()
    {
        var v = new AtualizarConfiguracaoClubeRequestValidator();
        v.TestValidate(new AtualizarConfiguracaoClubeRequest("verde", null, "America/Sao_Paulo", true, true, null))
            .ShouldHaveValidationErrorFor(x => x.CorPrimaria);
        v.TestValidate(new AtualizarConfiguracaoClubeRequest("#2F5D50", null, "America/Sao_Paulo", true, true, null))
            .ShouldNotHaveAnyValidationErrors();
    }
}
