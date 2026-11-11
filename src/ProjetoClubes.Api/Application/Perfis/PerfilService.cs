using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Application.Seguranca;
using ProjetoClubes.Domain.Identidade;

namespace ProjetoClubes.Application.Perfis;

public sealed record PermissaoDto(string Codigo, string Modulo, string Descricao);

public sealed record ModuloPermissoesDto(string Modulo, IReadOnlyList<PermissaoDto> Permissoes);

public sealed record PerfilDto(Guid Id, string Nome, string? Descricao, bool Padrao, int TotalUsuarios,
    IReadOnlyList<string> Permissoes);

public sealed record SalvarPerfilRequest(string Nome, string? Descricao, IReadOnlyList<string> Permissoes);

public sealed class SalvarPerfilRequestValidator : AbstractValidator<SalvarPerfilRequest>
{
    public SalvarPerfilRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().WithMessage("Informe o nome do perfil.").MaximumLength(80);
        RuleFor(x => x.Descricao).MaximumLength(300);
        RuleFor(x => x.Permissoes).NotNull();
        RuleForEach(x => x.Permissoes).Must(c => Seguranca.Permissoes.Codigos.Contains(c))
            .WithMessage("Permissão desconhecida: {PropertyValue}.");
    }
}

public interface IPerfilService
{
    IReadOnlyList<ModuloPermissoesDto> ListarCatalogo();
    Task<IReadOnlyList<PerfilDto>> ListarAsync(CancellationToken ct);
    Task<PerfilDto> ObterAsync(Guid id, CancellationToken ct);
    Task<PerfilDto> CriarAsync(SalvarPerfilRequest request, CancellationToken ct);
    Task<PerfilDto> AtualizarAsync(Guid id, SalvarPerfilRequest request, CancellationToken ct);
    Task ExcluirAsync(Guid id, CancellationToken ct);
}

/// <summary>Perfis de acesso do clube selecionado.</summary>
public sealed class PerfilService(
    IAppDbContext db,
    IUsuarioAtual atual,
    IPermissaoResolver permissoes,
    IValidator<SalvarPerfilRequest> validator) : IPerfilService
{
    private Guid ClubeId => atual.ClubeIdObrigatorio;

    private IQueryable<Perfil> PerfisDoClube() => db.Perfis.Where(p => p.ClubeId == ClubeId);

    public IReadOnlyList<ModuloPermissoesDto> ListarCatalogo() =>
        Seguranca.Permissoes.Todas.GroupBy(p => p.Modulo)
            .Select(g => new ModuloPermissoesDto(g.Key, g.Select(p => new PermissaoDto(p.Codigo, p.Modulo, p.Descricao)).ToList()))
            .ToList();

    public async Task<IReadOnlyList<PerfilDto>> ListarAsync(CancellationToken ct)
    {
        await permissoes.ExigirAsync(Seguranca.Permissoes.Usuarios.Visualizar, ct);
        return await Projetar(PerfisDoClube().AsNoTracking().OrderBy(p => p.Nome)).ToListAsync(ct);
    }

    public async Task<PerfilDto> ObterAsync(Guid id, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Seguranca.Permissoes.Usuarios.Visualizar, ct);
        return await Projetar(PerfisDoClube().AsNoTracking().Where(p => p.Id == id)).FirstOrDefaultAsync(ct)
               ?? throw new NaoEncontradoException("Perfil");
    }

    public async Task<PerfilDto> CriarAsync(SalvarPerfilRequest request, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Seguranca.Permissoes.Usuarios.GerenciarPerfis, ct);
        await validator.ValidarAsync(request, ct);
        await GarantirNomeUnicoAsync(null, request.Nome, ct);
        await ImpedirEscalonamentoAsync(request.Permissoes, ct);

        var perfil = new Perfil(ClubeId, request.Nome, request.Descricao);
        perfil.DefinirPermissoes(await IdsAsync(request.Permissoes, ct));
        db.Perfis.Add(perfil);
        await db.SaveChangesAsync(ct);
        permissoes.Invalidar();
        return await ObterAsync(perfil.Id, ct);
    }

    public async Task<PerfilDto> AtualizarAsync(Guid id, SalvarPerfilRequest request, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Seguranca.Permissoes.Usuarios.GerenciarPerfis, ct);
        await validator.ValidarAsync(request, ct);
        var perfil = await PerfisDoClube().Include(p => p.Permissoes).FirstOrDefaultAsync(p => p.Id == id, ct)
                     ?? throw new NaoEncontradoException("Perfil");
        await GarantirNomeUnicoAsync(id, request.Nome, ct);
        await ImpedirEscalonamentoAsync(request.Permissoes, ct);

        perfil.Nome = request.Nome.Trim();
        perfil.Descricao = request.Descricao;
        perfil.DefinirPermissoes(await IdsAsync(request.Permissoes, ct));
        await db.SaveChangesAsync(ct);
        permissoes.Invalidar();
        return await ObterAsync(id, ct);
    }

    public async Task ExcluirAsync(Guid id, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Seguranca.Permissoes.Usuarios.GerenciarPerfis, ct);
        var perfil = await PerfisDoClube().FirstOrDefaultAsync(p => p.Id == id, ct) ?? throw new NaoEncontradoException("Perfil");
        if (await db.UsuarioClubePerfis.AnyAsync(up => up.PerfilId == id, ct))
            throw new ConflitoException("perfil.em_uso", "Este perfil está atribuído a usuários. Remova-o deles antes de excluir.");
        db.Perfis.Remove(perfil); // exclusão lógica aplicada na gravação
        await db.SaveChangesAsync(ct);
        permissoes.Invalidar();
    }

    private IQueryable<PerfilDto> Projetar(IQueryable<Perfil> query) =>
        query.Select(p => new PerfilDto(p.Id, p.Nome, p.Descricao, p.ChaveModelo != null,
            db.UsuarioClubePerfis.Count(up => up.PerfilId == p.Id && up.UsuarioClubeId != Guid.Empty),
            p.Permissoes.Select(pp => pp.Permissao.Codigo).OrderBy(c => c).ToList()));

    private async Task<List<Guid>> IdsAsync(IEnumerable<string> codigos, CancellationToken ct)
    {
        var lista = codigos.Distinct().ToList();
        return await db.Permissoes.Where(p => lista.Contains(p.Codigo)).Select(p => p.Id).ToListAsync(ct);
    }

    private async Task GarantirNomeUnicoAsync(Guid? id, string nome, CancellationToken ct)
    {
        var n = nome.Trim().ToLower();
        if (await PerfisDoClube().AnyAsync(p => p.Nome.ToLower() == n && p.Id != id, ct))
            throw new ValidationException([new ValidationFailure(nameof(SalvarPerfilRequest.Nome), "Já existe um perfil com este nome.")]);
    }

    private async Task ImpedirEscalonamentoAsync(IEnumerable<string> codigos, CancellationToken ct)
    {
        if (atual.SuperAdmin) return;
        var minhas = await permissoes.ObterDoUsuarioAtualAsync(ct);
        var excedentes = codigos.Where(c => !minhas.Contains(c)).Distinct().ToList();
        if (excedentes.Count > 0)
            throw new AcessoNegadoException($"Você não pode conceder permissões que não possui: {string.Join(", ", excedentes)}.");
    }
}
