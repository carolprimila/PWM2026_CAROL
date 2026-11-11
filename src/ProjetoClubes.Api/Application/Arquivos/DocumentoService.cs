using System.Security.Cryptography;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using ProjetoClubes.Application.Cadastros;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Application.Seguranca;
using ProjetoClubes.Domain.Arquivos;

namespace ProjetoClubes.Application.Arquivos;

public sealed record ArquivoEnviado(Stream Conteudo, string NomeArquivo, long Tamanho);

public sealed record ConteudoArquivo(Stream Conteudo, string ContentType, string NomeArquivo);

public sealed record DocumentoDto(Guid Id, Guid PessoaId, TipoDocumento Tipo, string? Descricao, string NomeArquivo,
    string ContentType, long TamanhoBytes, SituacaoDocumento Situacao, string? ObservacaoAnalise, DateTimeOffset EnviadoEm);

public sealed record AnalisarDocumentoRequest(bool Aprovado, string? Observacao);

public interface IDocumentoService
{
    Task<IReadOnlyList<DocumentoDto>> ListarAsync(Guid pessoaId, CancellationToken ct);
    Task<DocumentoDto> EnviarAsync(Guid pessoaId, TipoDocumento tipo, string? descricao, ArquivoEnviado arquivo, CancellationToken ct);
    Task<ConteudoArquivo> BaixarAsync(Guid documentoId, CancellationToken ct);
    Task<DocumentoDto> AnalisarAsync(Guid documentoId, AnalisarDocumentoRequest request, CancellationToken ct);
    Task ExcluirAsync(Guid documentoId, CancellationToken ct);
    Task EnviarFotoAsync(Guid pessoaId, ArquivoEnviado arquivo, CancellationToken ct);
    Task<ConteudoArquivo> BaixarFotoAsync(Guid pessoaId, CancellationToken ct);
}

/// <summary>
/// Documentos e fotos. Arquivos ficam em armazenamento privado e só saem por estes métodos, depois da
/// verificação de permissão (administração) ou de titularidade (portal). O tipo é conferido pelo conteúdo
/// (assinatura do arquivo), não pela extensão ou pelo Content-Type informados pelo cliente.
/// </summary>
public sealed class DocumentoService(
    IAppDbContext db,
    IUsuarioAtual atual,
    IAcessoPessoas acesso,
    IPermissaoResolver permissoes,
    IArmazenamentoArquivos armazenamento,
    TimeProvider relogio) : IDocumentoService
{
    public const long TamanhoMaximoDocumento = 10 * 1024 * 1024;
    public const long TamanhoMaximoFoto = 5 * 1024 * 1024;

    public async Task<IReadOnlyList<DocumentoDto>> ListarAsync(Guid pessoaId, CancellationToken ct)
    {
        await acesso.ExigirAsync(pessoaId, AcaoPessoa.VerDocumentos, ct);
        return await db.DocumentosPessoais.AsNoTracking().Where(d => d.PessoaId == pessoaId)
            .OrderByDescending(d => d.CriadoEm)
            .Select(d => new DocumentoDto(d.Id, d.PessoaId, d.Tipo, d.Descricao, d.Arquivo.NomeOriginal, d.Arquivo.ContentType,
                d.Arquivo.TamanhoBytes, d.Situacao, d.ObservacaoAnalise, d.CriadoEm))
            .ToListAsync(ct);
    }

    public async Task<DocumentoDto> EnviarAsync(Guid pessoaId, TipoDocumento tipo, string? descricao, ArquivoEnviado arquivo,
        CancellationToken ct)
    {
        if (!Enum.IsDefined(tipo)) throw Invalido("tipo", "Tipo de documento inválido.");
        if (descricao?.Length > 200) throw Invalido("descricao", "Descrição muito longa.");
        var pessoa = await acesso.ExigirAsync(pessoaId, AcaoPessoa.EnviarDocumentos, ct);
        var registro = await ArmazenarAsync(arquivo, TamanhoMaximoDocumento, TiposArquivo.Documentos, ct);
        var doc = new DocumentoPessoal(pessoa.ClubeId, pessoa.Id, registro, tipo, descricao?.Trim());
        db.DocumentosPessoais.Add(doc);
        await db.SaveChangesAsync(ct);
        return new DocumentoDto(doc.Id, doc.PessoaId, doc.Tipo, doc.Descricao, registro.NomeOriginal, registro.ContentType,
            registro.TamanhoBytes, doc.Situacao, null, doc.CriadoEm);
    }

    public async Task<ConteudoArquivo> BaixarAsync(Guid documentoId, CancellationToken ct)
    {
        var doc = await ObterDocumentoAsync(documentoId, ct);
        await acesso.ExigirAsync(doc.PessoaId, AcaoPessoa.VerDocumentos, ct);
        return new ConteudoArquivo(await armazenamento.AbrirAsync(doc.Arquivo.Chave, ct), doc.Arquivo.ContentType, doc.Arquivo.NomeOriginal);
    }

    public async Task<DocumentoDto> AnalisarAsync(Guid documentoId, AnalisarDocumentoRequest r, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Documentos.Gerenciar, ct);
        var doc = await ObterDocumentoAsync(documentoId, ct);
        doc.Analisar(r.Aprovado, r.Observacao, atual.UsuarioId, relogio.GetUtcNow());
        await db.SaveChangesAsync(ct);
        return new DocumentoDto(doc.Id, doc.PessoaId, doc.Tipo, doc.Descricao, doc.Arquivo.NomeOriginal, doc.Arquivo.ContentType,
            doc.Arquivo.TamanhoBytes, doc.Situacao, doc.ObservacaoAnalise, doc.CriadoEm);
    }

    public async Task ExcluirAsync(Guid documentoId, CancellationToken ct)
    {
        var doc = await ObterDocumentoAsync(documentoId, ct);
        if (!await permissoes.PossuiAsync(Permissoes.Documentos.Gerenciar, ct))
        {
            // No portal, o próprio titular só remove documentos ainda não analisados.
            await acesso.ExigirAsync(doc.PessoaId, AcaoPessoa.EnviarDocumentos, ct);
            if (doc.Situacao != SituacaoDocumento.Pendente)
                throw new ConflitoException("documento.analisado", "Documentos já analisados só podem ser removidos pela administração.");
        }
        db.DocumentosPessoais.Remove(doc);
        await db.SaveChangesAsync(ct);
    }

    public async Task EnviarFotoAsync(Guid pessoaId, ArquivoEnviado arquivo, CancellationToken ct)
    {
        var pessoa = await acesso.ExigirAsync(pessoaId, AcaoPessoa.EditarFoto, ct);
        var registro = await ArmazenarAsync(arquivo, TamanhoMaximoFoto, TiposArquivo.Imagens, ct);
        pessoa.FotoArquivoId = registro.Id;
        await db.SaveChangesAsync(ct);
    }

    public async Task<ConteudoArquivo> BaixarFotoAsync(Guid pessoaId, CancellationToken ct)
    {
        var pessoa = await acesso.ExigirAsync(pessoaId, AcaoPessoa.VerFoto, ct);
        var foto = pessoa.FotoArquivoId is { } id ? await db.Arquivos.FirstOrDefaultAsync(a => a.Id == id, ct) : null;
        if (foto is null) throw new NaoEncontradoException("Foto");
        return new ConteudoArquivo(await armazenamento.AbrirAsync(foto.Chave, ct), foto.ContentType, foto.NomeOriginal);
    }

    private async Task<DocumentoPessoal> ObterDocumentoAsync(Guid id, CancellationToken ct) =>
        await db.DocumentosPessoais.Include(d => d.Arquivo)
            .FirstOrDefaultAsync(d => d.Id == id && d.ClubeId == atual.ClubeIdObrigatorio, ct)
        ?? throw new NaoEncontradoException("Documento");

    private async Task<Arquivo> ArmazenarAsync(ArquivoEnviado arquivo, long maximo, IReadOnlyList<TipoArquivo> aceitos, CancellationToken ct)
    {
        if (arquivo.Tamanho <= 0) throw Invalido("arquivo", "Selecione um arquivo.");
        if (arquivo.Tamanho > maximo) throw Invalido("arquivo", $"O arquivo deve ter no máximo {maximo / 1024 / 1024} MB.");

        using var memoria = new MemoryStream();
        await arquivo.Conteudo.CopyToAsync(memoria, ct);
        if (memoria.Length > maximo) throw Invalido("arquivo", $"O arquivo deve ter no máximo {maximo / 1024 / 1024} MB.");
        var bytes = memoria.ToArray();

        var tipo = TiposArquivo.Identificar(bytes, aceitos)
                   ?? throw Invalido("arquivo", $"Formato não aceito. Envie: {string.Join(", ", aceitos.Select(t => t.Extensao.ToUpperInvariant()))}.");

        var clubeId = atual.ClubeIdObrigatorio;
        memoria.Position = 0;
        var chave = await armazenamento.SalvarAsync(clubeId, memoria, tipo.Extensao, ct);
        var nome = Path.GetFileName(arquivo.NomeArquivo);
        if (string.IsNullOrWhiteSpace(nome) || nome.Length > 150) nome = $"arquivo.{tipo.Extensao}";

        var registro = new Arquivo(clubeId, nome, tipo.ContentType, bytes.Length,
            Convert.ToHexStringLower(SHA256.HashData(bytes)), chave);
        db.Arquivos.Add(registro);
        return registro;
    }

    private static ValidationException Invalido(string campo, string mensagem) => new([new ValidationFailure(campo, mensagem)]);
}

public sealed record TipoArquivo(string Extensao, string ContentType, Func<byte[], bool> Assinatura);

/// <summary>Identificação do formato pela assinatura binária (magic bytes).</summary>
public static class TiposArquivo
{
    public static readonly TipoArquivo Pdf = new("pdf", "application/pdf", b => Comeca(b, "%PDF-"u8));
    public static readonly TipoArquivo Jpeg = new("jpg", "image/jpeg", b => b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF);
    public static readonly TipoArquivo Png = new("png", "image/png", b => Comeca(b, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]));
    public static readonly TipoArquivo Webp = new("webp", "image/webp",
        b => b.Length > 12 && Comeca(b, "RIFF"u8) && b.AsSpan(8, 4).SequenceEqual("WEBP"u8));

    public static readonly IReadOnlyList<TipoArquivo> Imagens = [Jpeg, Png, Webp];
    public static readonly IReadOnlyList<TipoArquivo> Documentos = [Pdf, Jpeg, Png, Webp];

    public static TipoArquivo? Identificar(byte[] conteudo, IReadOnlyList<TipoArquivo> aceitos) =>
        aceitos.FirstOrDefault(t => t.Assinatura(conteudo));

    private static bool Comeca(byte[] b, ReadOnlySpan<byte> prefixo) => b.AsSpan().StartsWith(prefixo);
}
