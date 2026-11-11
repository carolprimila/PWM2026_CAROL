using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Application.Seguranca;
using ProjetoClubes.Domain.Associados;

namespace ProjetoClubes.Application.Cadastros;

public sealed record CategoriaDto(Guid Id, string Nome, string? Descricao, bool DireitoVoto, bool PodeSerVotado,
    bool PermiteDependentes, int? LimiteDependentes, int Ordem, bool Ativa, bool Padrao, int TotalAssociados);

public sealed record SalvarCategoriaRequest(string Nome, string? Descricao, bool DireitoVoto, bool PodeSerVotado,
    bool PermiteDependentes, int? LimiteDependentes, int Ordem, bool Ativa = true);

public sealed record InteresseDto(Guid Id, string Nome);

public sealed record SalvarInteresseRequest(string Nome);

public sealed class SalvarCategoriaRequestValidator : AbstractValidator<SalvarCategoriaRequest>
{
    public SalvarCategoriaRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().WithMessage("Informe o nome da categoria.").MaximumLength(80);
        RuleFor(x => x.Descricao).MaximumLength(300);
        RuleFor(x => x.LimiteDependentes).InclusiveBetween(0, 50).When(x => x.LimiteDependentes.HasValue);
        RuleFor(x => x).Must(x => !x.PodeSerVotado || x.DireitoVoto)
            .WithMessage("Para poder ser votado, a categoria precisa ter direito a voto.").OverridePropertyName("PodeSerVotado");
    }
}

public sealed class SalvarInteresseRequestValidator : AbstractValidator<SalvarInteresseRequest>
{
    public SalvarInteresseRequestValidator() => RuleFor(x => x.Nome).NotEmpty().MaximumLength(60);
}

public interface ICategoriaService
{
    Task<IReadOnlyList<CategoriaDto>> ListarAsync(bool incluirInativas, CancellationToken ct);
    Task<CategoriaDto> CriarAsync(SalvarCategoriaRequest request, CancellationToken ct);
    Task<CategoriaDto> AtualizarAsync(Guid id, SalvarCategoriaRequest request, CancellationToken ct);
    Task ExcluirAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<InteresseDto>> ListarInteressesAsync(CancellationToken ct);
    Task<InteresseDto> CriarInteresseAsync(SalvarInteresseRequest request, CancellationToken ct);
    Task ExcluirInteresseAsync(Guid id, CancellationToken ct);
}

/// <summary>Categorias de associados e interesses do clube selecionado.</summary>
public sealed class CategoriaService(
    IAppDbContext db,
    IUsuarioAtual atual,
    IPermissaoResolver permissoes,
    IValidator<SalvarCategoriaRequest> validator,
    IValidator<SalvarInteresseRequest> interesseValidator) : ICategoriaService
{
    private Guid ClubeId => atual.ClubeIdObrigatorio;

    public async Task<IReadOnlyList<CategoriaDto>> ListarAsync(bool incluirInativas, CancellationToken ct)
    {
        await ExigirLeituraAsync(ct);
        return await db.CategoriasAssociados.AsNoTracking()
            .Where(c => c.ClubeId == ClubeId && (incluirInativas || c.Ativa))
            .OrderBy(c => c.Ordem).ThenBy(c => c.Nome)
            .Select(c => new CategoriaDto(c.Id, c.Nome, c.Descricao, c.DireitoVoto, c.PodeSerVotado, c.PermiteDependentes,
                c.LimiteDependentes, c.Ordem, c.Ativa, c.ChaveModelo != null,
                db.Associados.Count(a => a.CategoriaId == c.Id)))
            .ToListAsync(ct);
    }

    public async Task<CategoriaDto> CriarAsync(SalvarCategoriaRequest request, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Clubes.Configurar, ct);
        await validator.ValidarAsync(request, ct);
        await GarantirNomeUnicoAsync(null, request.Nome, ct);
        var c = new CategoriaAssociado(ClubeId, request.Nome);
        Aplicar(c, request);
        db.CategoriasAssociados.Add(c);
        await db.SaveChangesAsync(ct);
        return (await ListarAsync(true, ct)).Single(x => x.Id == c.Id);
    }

    public async Task<CategoriaDto> AtualizarAsync(Guid id, SalvarCategoriaRequest request, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Clubes.Configurar, ct);
        await validator.ValidarAsync(request, ct);
        var c = await db.CategoriasAssociados.FirstOrDefaultAsync(x => x.Id == id && x.ClubeId == ClubeId, ct)
                ?? throw new NaoEncontradoException("Categoria");
        await GarantirNomeUnicoAsync(id, request.Nome, ct);
        c.Nome = request.Nome.Trim();
        Aplicar(c, request);
        await db.SaveChangesAsync(ct);
        return (await ListarAsync(true, ct)).Single(x => x.Id == id);
    }

    public async Task ExcluirAsync(Guid id, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Clubes.Configurar, ct);
        var c = await db.CategoriasAssociados.FirstOrDefaultAsync(x => x.Id == id && x.ClubeId == ClubeId, ct)
                ?? throw new NaoEncontradoException("Categoria");
        if (await db.Associados.IgnoreQueryFilters(FiltrosConsulta.SomenteTenant).AnyAsync(a => a.CategoriaId == id, ct))
            throw new ConflitoException("categoria.em_uso",
                "Há associados nesta categoria. Desative a categoria em vez de excluí-la.");
        db.CategoriasAssociados.Remove(c);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<InteresseDto>> ListarInteressesAsync(CancellationToken ct)
    {
        return await db.Interesses.AsNoTracking().Where(i => i.ClubeId == ClubeId).OrderBy(i => i.Nome)
            .Select(i => new InteresseDto(i.Id, i.Nome)).ToListAsync(ct);
    }

    public async Task<InteresseDto> CriarInteresseAsync(SalvarInteresseRequest request, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Clubes.Configurar, ct);
        await interesseValidator.ValidarAsync(request, ct);
        var nome = request.Nome.Trim();
        if (await db.Interesses.AnyAsync(i => i.ClubeId == ClubeId && i.Nome.ToLower() == nome.ToLower(), ct))
            throw new ConflitoException("interesse.duplicado", "Este interesse já está cadastrado.");
        var i = new Interesse(ClubeId, nome);
        db.Interesses.Add(i);
        await db.SaveChangesAsync(ct);
        return new InteresseDto(i.Id, i.Nome);
    }

    public async Task ExcluirInteresseAsync(Guid id, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Clubes.Configurar, ct);
        var i = await db.Interesses.FirstOrDefaultAsync(x => x.Id == id && x.ClubeId == ClubeId, ct)
                ?? throw new NaoEncontradoException("Interesse");
        db.Interesses.Remove(i);
        await db.SaveChangesAsync(ct);
    }

    private async Task ExigirLeituraAsync(CancellationToken ct)
    {
        var minhas = await permissoes.ObterDoUsuarioAtualAsync(ct);
        if (!minhas.Contains(Permissoes.Associados.Visualizar) && !minhas.Contains(Permissoes.Clubes.Configurar))
            throw new AcessoNegadoException();
    }

    private async Task GarantirNomeUnicoAsync(Guid? id, string nome, CancellationToken ct)
    {
        var n = nome.Trim().ToLower();
        if (await db.CategoriasAssociados.AnyAsync(c => c.ClubeId == ClubeId && c.Nome.ToLower() == n && c.Id != id, ct))
            throw new ValidationException([new ValidationFailure("Nome", "Já existe uma categoria com este nome.")]);
    }

    private static void Aplicar(CategoriaAssociado c, SalvarCategoriaRequest r)
    {
        c.Descricao = r.Descricao?.Trim();
        c.DireitoVoto = r.DireitoVoto;
        c.PodeSerVotado = r.PodeSerVotado;
        c.PermiteDependentes = r.PermiteDependentes;
        c.LimiteDependentes = r.LimiteDependentes;
        c.Ordem = r.Ordem;
        c.Ativa = r.Ativa;
    }
}
