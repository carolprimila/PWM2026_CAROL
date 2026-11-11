using Microsoft.EntityFrameworkCore;
using ProjetoClubes.Application.Cadastros;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Application.Seguranca;
using ProjetoClubes.Domain.Associados;
using ProjetoClubes.Domain.Common;
using ProjetoClubes.Domain.Dependentes;
using ProjetoClubes.Domain.Pessoas;

namespace ProjetoClubes.Application.Carteirinhas;

public sealed record ClubeCarteirinhaDto(Guid Id, string Nome, string Sigla, string CorPrimaria, string? NomeExibicao);

public sealed record CarteirinhaDto(
    Guid PessoaId,
    string Tipo,
    Guid? AssociadoId,
    Guid? DependenteId,
    string Nome,
    string NumeroIdentificacao,
    SituacaoCadastral Situacao,
    string? Categoria,
    string? TitularNome,
    Parentesco? Parentesco,
    string? ParentescoDescricao,
    ClubeCarteirinhaDto Clube,
    bool TemFoto,
    // Conteúdo do QR Code: identificador opaco, sem CPF ou outros dados pessoais.
    string QrCodeConteudo);

/// <summary>Formato do conteúdo do QR Code da carteirinha.</summary>
public static class CodigoCarteirinha
{
    public const string Prefixo = "PCLUBES:1:";

    public static string Montar(string token) => Prefixo + token;

    /// <summary>Extrai o token aceitando o conteúdo completo do QR Code ou somente o token (digitação manual).</summary>
    public static string? Extrair(string? codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return null;
        var c = codigo.Trim();
        if (c.StartsWith(Prefixo, StringComparison.Ordinal)) c = c[Prefixo.Length..];
        return c.Length is >= 20 and <= 100 && c.All(ch => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_') ? c : null;
    }
}

public interface ICarteirinhaService
{
    Task<CarteirinhaDto> DoAssociadoAsync(Guid associadoId, CancellationToken ct);
    Task<CarteirinhaDto> DoDependenteAsync(Guid dependenteId, CancellationToken ct);
    Task<byte[]> QrCodePngAsync(Guid pessoaId, CancellationToken ct);
    /// <summary>Gera novo código (ex.: celular perdido). O QR Code anterior deixa de funcionar.</summary>
    Task RegenerarAsync(Guid pessoaId, CancellationToken ct);
    /// <summary>Carteirinhas visíveis no portal: a própria e, para titulares, as dos dependentes.</summary>
    Task<IReadOnlyList<CarteirinhaDto>> DoPortalAsync(CancellationToken ct);
}

public sealed class CarteirinhaService(
    IAppDbContext db,
    IUsuarioAtual atual,
    IAcessoPessoas acesso,
    ITokenService tokens,
    IGeradorQrCode qrCode,
    TimeProvider relogio) : ICarteirinhaService
{
    public async Task<CarteirinhaDto> DoAssociadoAsync(Guid associadoId, CancellationToken ct)
    {
        var a = await db.Associados.AsNoTracking().Include(x => x.Pessoa).Include(x => x.Categoria)
                    .FirstOrDefaultAsync(x => x.Id == associadoId && x.ClubeId == atual.ClubeIdObrigatorio, ct)
                ?? throw new NaoEncontradoException("Associado");
        await acesso.ExigirAsync(a.PessoaId, AcaoPessoa.VerDados, ct);
        return DeAssociado(a, await ClubeAsync(ct));
    }

    public async Task<CarteirinhaDto> DoDependenteAsync(Guid dependenteId, CancellationToken ct)
    {
        var d = await CarregarDependentes().FirstOrDefaultAsync(x => x.Id == dependenteId, ct)
                ?? throw new NaoEncontradoException("Dependente");
        await acesso.ExigirAsync(d.PessoaId, AcaoPessoa.VerDados, ct);
        return DeDependente(d, await ClubeAsync(ct));
    }

    public async Task<byte[]> QrCodePngAsync(Guid pessoaId, CancellationToken ct)
    {
        var pessoa = await acesso.ExigirAsync(pessoaId, AcaoPessoa.VerDados, ct);
        return qrCode.GerarPng(CodigoCarteirinha.Montar(pessoa.TokenCarteirinha));
    }

    public async Task RegenerarAsync(Guid pessoaId, CancellationToken ct)
    {
        var pessoa = await acesso.ExigirAsync(pessoaId, AcaoPessoa.RegenerarCarteirinha, ct);
        pessoa.RegenerarTokenCarteirinha(tokens.GerarTokenOpaco(), relogio.GetUtcNow());
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<CarteirinhaDto>> DoPortalAsync(CancellationToken ct)
    {
        var portal = await acesso.ObterPortalAsync(ct);
        var clube = await ClubeAsync(ct);
        var lista = new List<CarteirinhaDto>();
        if (portal.Associado is { } a)
        {
            var titular = await db.Associados.AsNoTracking().Include(x => x.Pessoa).Include(x => x.Categoria)
                .FirstAsync(x => x.Id == a.Id, ct);
            lista.Add(DeAssociado(titular, clube));
            var deps = await CarregarDependentes()
                .Where(d => d.AssociadoTitularId == a.Id && d.Situacao != SituacaoCadastral.Rejeitado && d.Situacao != SituacaoCadastral.Inativo)
                .OrderBy(d => d.NumeroIdentificacao).ToListAsync(ct);
            lista.AddRange(deps.Select(d => DeDependente(d, clube)));
        }
        else if (portal.Dependente is { } dep)
        {
            lista.Add(DeDependente(await CarregarDependentes().FirstAsync(d => d.Id == dep.Id, ct), clube));
        }
        return lista;
    }

    private IQueryable<Dependente> CarregarDependentes() =>
        db.Dependentes.AsNoTracking().Include(x => x.Pessoa).Include(x => x.AssociadoTitular).ThenInclude(t => t.Pessoa)
            .Where(x => x.ClubeId == atual.ClubeIdObrigatorio);

    private async Task<ClubeCarteirinhaDto> ClubeAsync(CancellationToken ct) =>
        await db.Clubes.AsNoTracking().Where(c => c.Id == atual.ClubeIdObrigatorio)
            .Select(c => new ClubeCarteirinhaDto(c.Id, c.Nome, c.Sigla, c.Configuracao.CorPrimaria, c.Configuracao.NomeExibicao))
            .FirstAsync(ct);

    private static CarteirinhaDto DeAssociado(Associado a, ClubeCarteirinhaDto clube) => new(a.PessoaId, "Associado", a.Id, null,
        a.Pessoa.NomeCompleto, a.Matricula, a.Situacao, a.Categoria.Nome, null, null, null, clube,
        a.Pessoa.FotoArquivoId is not null, CodigoCarteirinha.Montar(a.Pessoa.TokenCarteirinha));

    private static CarteirinhaDto DeDependente(Dependente d, ClubeCarteirinhaDto clube) => new(d.PessoaId, "Dependente", null, d.Id,
        d.Pessoa.NomeCompleto, d.NumeroIdentificacao, d.Situacao, null, d.AssociadoTitular.Pessoa.NomeCompleto, d.Parentesco,
        d.ParentescoDescricao, clube, d.Pessoa.FotoArquivoId is not null, CodigoCarteirinha.Montar(d.Pessoa.TokenCarteirinha));
}
