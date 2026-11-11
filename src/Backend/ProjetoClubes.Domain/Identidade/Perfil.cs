using ProjetoClubes.Domain.Common;

namespace ProjetoClubes.Domain.Identidade;

/// <summary>Perfil de acesso de um clube (ex.: Presidente, Tesoureiro). Conjunto editável de permissões.</summary>
public class Perfil : AuditableEntity, ITenantScoped, ISoftDelete
{
    private readonly List<PerfilPermissao> _permissoes = [];

    private Perfil() { }

    public Perfil(Guid clubeId, string nome, string? descricao, string? chaveModelo = null)
    {
        ClubeId = clubeId;
        Nome = nome.Trim();
        Descricao = descricao;
        ChaveModelo = chaveModelo;
    }

    public Guid ClubeId { get; private set; }
    public string Nome { get; set; } = null!;
    public string? Descricao { get; set; }
    /// <summary>Chave do modelo de origem (ex.: "presidente"). Perfis de modelo não podem ser excluídos.</summary>
    public string? ChaveModelo { get; private set; }

    public IReadOnlyCollection<PerfilPermissao> Permissoes => _permissoes;

    public bool Excluido { get; private set; }
    public DateTimeOffset? ExcluidoEm { get; private set; }
    public Guid? ExcluidoPor { get; private set; }

    public bool DeModelo => ChaveModelo is not null;

    public void DefinirPermissoes(IEnumerable<Guid> permissaoIds)
    {
        var novos = permissaoIds.ToHashSet();
        _permissoes.RemoveAll(p => !novos.Contains(p.PermissaoId));
        foreach (var id in novos.Where(id => _permissoes.All(p => p.PermissaoId != id)))
            _permissoes.Add(new PerfilPermissao(Id, id));
    }

    public void MarcarExcluido(DateTimeOffset quando, Guid? por)
    {
        if (DeModelo)
            throw new DomainException("perfil.modelo_nao_excluivel",
                "Perfis padrão do clube não podem ser excluídos. Edite as permissões dele.");
        Excluido = true;
        ExcluidoEm = quando;
        ExcluidoPor = por;
    }
}

public class PerfilPermissao
{
    private PerfilPermissao() { }

    public PerfilPermissao(Guid perfilId, Guid permissaoId)
    {
        PerfilId = perfilId;
        PermissaoId = permissaoId;
    }

    public Guid PerfilId { get; private set; }
    public Guid PermissaoId { get; private set; }
    public Permissao Permissao { get; private set; } = null!;
}

/// <summary>Permissão do catálogo global (sincronizado a partir do código).</summary>
public class Permissao : Entity, INaoAuditavel
{
    private Permissao() { }

    public Permissao(string codigo, string modulo, string descricao)
    {
        Codigo = codigo;
        Modulo = modulo;
        Descricao = descricao;
    }

    public string Codigo { get; private set; } = null!;
    public string Modulo { get; set; } = null!;
    public string Descricao { get; set; } = null!;
}
