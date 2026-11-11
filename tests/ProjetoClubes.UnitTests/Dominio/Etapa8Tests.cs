using ProjetoClubes.Domain.Common;
using ProjetoClubes.Domain.Pessoas;

namespace ProjetoClubes.UnitTests.Dominio;

public class LgpdTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 11, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Anonimizar_apaga_dados_pessoais_troca_o_qr_e_nao_repete()
    {
        var p = new Pessoa(Guid.NewGuid(), "Maria das Dores Silva", new string('a', 43), Agora)
        {
            Rg = "12.345.678-9", DataNascimento = new DateOnly(1980, 5, 1), Telefone = "45999990000", Email = "maria@exemplo.com",
            UsuarioId = Guid.NewGuid(), FotoArquivoId = Guid.NewGuid(),
            Endereco = new Endereco { Logradouro = "Rua A", Numero = "10", Cidade = "Foz do Iguaçu", Uf = "PR" },
        };
        p.DefinirCpf("529.982.247-25");
        var tokenAntigo = p.TokenCarteirinha;

        p.Anonimizar(new string('b', 43), Agora);

        Assert.StartsWith("Pessoa anonimizada", p.NomeCompleto);
        Assert.DoesNotContain("Maria", p.NomeCompleto);
        Assert.Null(p.Cpf);
        Assert.Null(p.Rg);
        Assert.Null(p.DataNascimento);
        Assert.Null(p.Telefone);
        Assert.Null(p.Email);
        Assert.Null(p.UsuarioId);
        Assert.Null(p.FotoArquivoId);
        Assert.Null(p.Endereco.Logradouro);
        Assert.Null(p.Endereco.Cidade);
        Assert.NotEqual(tokenAntigo, p.TokenCarteirinha);
        Assert.Equal(Agora, p.AnonimizadaEm);
        Assert.Equal("pessoa.anonimizada", Assert.Throws<DomainException>(() => p.Anonimizar(new string('c', 43), Agora)).Codigo);
    }
}
