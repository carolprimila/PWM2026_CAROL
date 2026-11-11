using ProjetoClubes.Domain.Common;

namespace ProjetoClubes.Domain.Identidade;

public enum SituacaoVinculo
{
    Ativo = 1,
    Inativo = 2,
}

/// <summary>Vínculo de um usuário com um clube, com seus perfis e permissões individuais nesse clube.</summary>
public class UsuarioClube : AuditableEntity, ITenantScoped
{
    private readonly List<UsuarioClubePerfil> _perfis = [];
    private readonly List<UsuarioPermissao> _permissoes = [];

    private UsuarioClube() { }

    public UsuarioClube(Guid usuarioId, Guid clubeId)
    {
        UsuarioId = usuarioId;
        ClubeId = clubeId;
        Situacao = SituacaoVinculo.Ativo;
    }

    public Guid UsuarioId { get; private set; }
    public Usuario Usuario { get; private set; } = null!;
    public Guid ClubeId { get; private set; }
    public SituacaoVinculo Situacao { get; private set; }

    public IReadOnlyCollection<UsuarioClubePerfil> Perfis => _perfis;
    public IReadOnlyCollection<UsuarioPermissao> Permissoes => _permissoes;

    public bool Ativo => Situacao == SituacaoVinculo.Ativo;

    public void Ativar() => Situacao = SituacaoVinculo.Ativo;
    public void Inativar() => Situacao = SituacaoVinculo.Inativo;

    public void DefinirPerfis(IEnumerable<Guid> perfilIds)
    {
        var novos = perfilIds.ToHashSet();
        _perfis.RemoveAll(p => !novos.Contains(p.PerfilId));
        foreach (var id in novos.Where(id => _perfis.All(p => p.PerfilId != id)))
            _perfis.Add(new UsuarioClubePerfil(Id, id));
    }

    public void DefinirPermissoesIndividuais(IEnumerable<(Guid PermissaoId, bool Concedida)> itens)
    {
        var novos = itens.DistinctBy(i => i.PermissaoId).ToDictionary(i => i.PermissaoId, i => i.Concedida);
        _permissoes.RemoveAll(p => !novos.TryGetValue(p.PermissaoId, out var c) || c != p.Concedida);
        foreach (var (permissaoId, concedida) in novos.Where(n => _permissoes.All(p => p.PermissaoId != n.Key)))
            _permissoes.Add(new UsuarioPermissao(Id, permissaoId, concedida));
    }
}

public class UsuarioClubePerfil
{
    private UsuarioClubePerfil() { }

    public UsuarioClubePerfil(Guid usuarioClubeId, Guid perfilId)
    {
        UsuarioClubeId = usuarioClubeId;
        PerfilId = perfilId;
    }

    public Guid UsuarioClubeId { get; private set; }
    public Guid PerfilId { get; private set; }
    public Perfil Perfil { get; private set; } = null!;
}

/// <summary>Exceção individual às permissões dos perfis: concede (true) ou nega (false).</summary>
public class UsuarioPermissao
{
    private UsuarioPermissao() { }

    public UsuarioPermissao(Guid usuarioClubeId, Guid permissaoId, bool concedida)
    {
        UsuarioClubeId = usuarioClubeId;
        PermissaoId = permissaoId;
        Concedida = concedida;
    }

    public Guid UsuarioClubeId { get; private set; }
    public Guid PermissaoId { get; private set; }
    public Permissao Permissao { get; private set; } = null!;
    public bool Concedida { get; private set; }
}
