using ProjetoClubes.Domain.Common;

namespace ProjetoClubes.Domain.Clubes;

public enum SituacaoClube
{
    Ativo = 1,
    Inativo = 2,
}

/// <summary>Clube (tenant). Raiz de todos os dados isolados por ClubeId.</summary>
public class Clube : AuditableEntity, ISoftDelete
{
    private Clube() { }

    public Clube(string nome, string sigla, string? cnpj, bool demonstracao = false)
    {
        Nome = nome.Trim();
        DefinirSigla(sigla);
        Cnpj = string.IsNullOrWhiteSpace(cnpj) ? null : Documentos.SomenteDigitos(cnpj);
        Situacao = SituacaoClube.Ativo;
        Demonstracao = demonstracao;
        Configuracao = new ConfiguracaoClube(Id);
    }

    public string Nome { get; set; } = null!;
    public string Sigla { get; private set; } = null!;
    public string? Cnpj { get; set; }
    public Endereco Endereco { get; set; } = new();
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    /// <summary>Referência a arquivo armazenado (módulo de documentos, Etapa 2).</summary>
    public string? LogotipoArquivo { get; set; }
    public DateOnly? DataFundacao { get; set; }
    public SituacaoClube Situacao { get; private set; }
    /// <summary>Clubes de demonstração ficam separados de dados reais e podem ser removidos em bloco.</summary>
    public bool Demonstracao { get; private set; }

    public ConfiguracaoClube Configuracao { get; private set; } = null!;

    public bool Excluido { get; private set; }
    public DateTimeOffset? ExcluidoEm { get; private set; }
    public Guid? ExcluidoPor { get; private set; }

    public bool Ativo => Situacao == SituacaoClube.Ativo && !Excluido;

    public void DefinirSigla(string sigla)
    {
        var s = sigla.Trim().ToUpperInvariant();
        if (s.Length is < 2 or > 20)
            throw new DomainException("clube.sigla_invalida", "A sigla deve ter entre 2 e 20 caracteres.");
        Sigla = s;
    }

    public void Ativar() => Situacao = SituacaoClube.Ativo;

    public void Desativar() => Situacao = SituacaoClube.Inativo;

    public void MarcarExcluido(DateTimeOffset quando, Guid? por)
    {
        Excluido = true;
        ExcluidoEm = quando;
        ExcluidoPor = por;
    }
}
