using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Domain.Identidade;

namespace ProjetoClubes.Application.Seguranca;

public interface IPermissaoResolver
{
    /// <summary>Permissões efetivas do usuário no clube (perfis + concessões − negações).</summary>
    Task<IReadOnlySet<string>> ObterAsync(Guid usuarioId, Guid clubeId, CancellationToken ct = default);

    /// <summary>Permissões do usuário atual no clube atual. Superadmin recebe todas.</summary>
    Task<IReadOnlySet<string>> ObterDoUsuarioAtualAsync(CancellationToken ct = default);

    Task<bool> PossuiAsync(string permissao, CancellationToken ct = default);

    Task ExigirAsync(string permissao, CancellationToken ct = default);

    /// <summary>Invalida o cache (chamar após alterar perfis ou acessos).</summary>
    void Invalidar();
}

public sealed class PermissaoResolver(IAppDbContext db, IUsuarioAtual usuario, IMemoryCache cache) : IPermissaoResolver
{
    private const string ChaveVersao = "permissoes:versao";
    private static readonly TimeSpan Duracao = TimeSpan.FromSeconds(60);

    public async Task<IReadOnlySet<string>> ObterAsync(Guid usuarioId, Guid clubeId, CancellationToken ct = default)
    {
        var versao = cache.GetOrCreate(ChaveVersao, e => { e.Priority = CacheItemPriority.NeverRemove; return 0L; });
        var chave = $"permissoes:{versao}:{usuarioId}:{clubeId}";
        if (cache.TryGetValue(chave, out IReadOnlySet<string>? emCache) && emCache is not null) return emCache;

        var vinculo = await db.UsuarioClubes.IgnoreQueryFilters(FiltrosConsulta.SomenteTenant)
            .Where(v => v.UsuarioId == usuarioId && v.ClubeId == clubeId && v.Situacao == SituacaoVinculo.Ativo)
            .Select(v => new { v.Id })
            .FirstOrDefaultAsync(ct);

        IReadOnlySet<string> resultado;
        if (vinculo is null)
        {
            resultado = new HashSet<string>();
        }
        else
        {
            var dosPerfis = await db.UsuarioClubePerfis.IgnoreQueryFilters(FiltrosConsulta.SomenteTenant)
                .Where(up => up.UsuarioClubeId == vinculo.Id && !up.Perfil.Excluido && up.Perfil.ClubeId == clubeId)
                .SelectMany(up => up.Perfil.Permissoes.Select(pp => pp.Permissao.Codigo))
                .ToListAsync(ct);

            var individuais = await db.UsuarioPermissoes
                .Where(up => up.UsuarioClubeId == vinculo.Id)
                .Select(up => new { up.Permissao.Codigo, up.Concedida })
                .ToListAsync(ct);

            resultado = Combinar(dosPerfis, individuais.Select(i => (i.Codigo, i.Concedida)));
        }

        cache.Set(chave, resultado, Duracao);
        return resultado;
    }

    /// <summary>Regra de combinação: união dos perfis, mais concessões, menos negações (negação prevalece).</summary>
    internal static IReadOnlySet<string> Combinar(IEnumerable<string> dosPerfis,
        IEnumerable<(string Codigo, bool Concedida)> individuais)
    {
        var set = new HashSet<string>(dosPerfis, StringComparer.Ordinal);
        var lista = individuais.ToList();
        foreach (var (codigo, _) in lista.Where(i => i.Concedida)) set.Add(codigo);
        foreach (var (codigo, _) in lista.Where(i => !i.Concedida)) set.Remove(codigo);
        return set;
    }

    public async Task<IReadOnlySet<string>> ObterDoUsuarioAtualAsync(CancellationToken ct = default)
    {
        if (!usuario.Autenticado || usuario.UsuarioId is null) return new HashSet<string>();
        if (usuario.SuperAdmin) return Permissoes.Codigos;
        if (usuario.ClubeId is null) return new HashSet<string>();
        return await ObterAsync(usuario.UsuarioId.Value, usuario.ClubeId.Value, ct);
    }

    public async Task<bool> PossuiAsync(string permissao, CancellationToken ct = default) =>
        (await ObterDoUsuarioAtualAsync(ct)).Contains(permissao);

    public async Task ExigirAsync(string permissao, CancellationToken ct = default)
    {
        if (!await PossuiAsync(permissao, ct)) throw new AcessoNegadoException();
    }

    public void Invalidar()
    {
        var versao = cache.GetOrCreate(ChaveVersao, e => { e.Priority = CacheItemPriority.NeverRemove; return 0L; });
        cache.Set(ChaveVersao, versao + 1, new MemoryCacheEntryOptions { Priority = CacheItemPriority.NeverRemove });
    }
}
