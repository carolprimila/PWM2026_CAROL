using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Infrastructure.Persistence;
using QRCoder;

namespace ProjetoClubes.Infrastructure.Arquivos;

public sealed class ArmazenamentoOptions
{
    public const string Secao = "Arquivos";

    /// <summary>Pasta privada (fora de wwwroot). Em produção, um volume persistente com backup.</summary>
    public string Pasta { get; set; } = "App_Data/arquivos";
}

/// <summary>
/// Armazenamento em disco local, organizado por clube. A chave é gerada no servidor (sem usar o nome enviado
/// pelo cliente) e validada contra path traversal. Substituível por um provedor de objetos (S3/Azure Blob).
/// </summary>
public sealed class ArmazenamentoLocal(IOptions<ArmazenamentoOptions> opcoes) : IArmazenamentoArquivos
{
    private string Raiz => Path.GetFullPath(opcoes.Value.Pasta);

    public async Task<string> SalvarAsync(Guid clubeId, Stream conteudo, string extensao, CancellationToken ct)
    {
        if (!extensao.All(char.IsAsciiLetterOrDigit)) throw new ArgumentException("Extensão inválida.", nameof(extensao));
        var chave = $"{clubeId:N}/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}.{extensao}";
        var caminho = Resolver(chave);
        Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);
        await using var destino = new FileStream(caminho, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await conteudo.CopyToAsync(destino, ct);
        return chave;
    }

    public Task<Stream> AbrirAsync(string chave, CancellationToken ct)
    {
        var caminho = Resolver(chave);
        if (!File.Exists(caminho)) throw new FileNotFoundException("Arquivo não encontrado no armazenamento.");
        return Task.FromResult<Stream>(new FileStream(caminho, FileMode.Open, FileAccess.Read, FileShare.Read));
    }

    public Task RemoverAsync(string chave, CancellationToken ct)
    {
        var caminho = Resolver(chave);
        if (File.Exists(caminho)) File.Delete(caminho);
        return Task.CompletedTask;
    }

    private string Resolver(string chave)
    {
        var caminho = Path.GetFullPath(Path.Combine(Raiz, chave));
        if (!caminho.StartsWith(Raiz + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("Chave de arquivo inválida.");
        return caminho;
    }
}

/// <summary>Contador atômico por clube via INSERT … ON CONFLICT DO UPDATE … RETURNING (seguro sob concorrência).</summary>
public sealed class GeradorSequencia(AppDbContext db) : IGeradorSequencia
{
    public async Task<long> ProximoAsync(Guid clubeId, string nome, CancellationToken ct)
    {
        var valores = await db.Database.SqlQuery<long>($"""
            INSERT INTO "SequenciasClube" ("ClubeId", "Nome", "Valor") VALUES ({clubeId}, {nome}, 1)
            ON CONFLICT ("ClubeId", "Nome") DO UPDATE SET "Valor" = "SequenciasClube"."Valor" + 1
            RETURNING "Valor" AS "Value"
            """).ToListAsync(ct);
        return valores[0];
    }
}

public sealed class GeradorQrCode : IGeradorQrCode
{
    public byte[] GerarPng(string conteudo, int pixelsPorModulo = 10)
    {
        using var gerador = new QRCodeGenerator();
        using var dados = gerador.CreateQrCode(conteudo, QRCodeGenerator.ECCLevel.Q);
        return new PngByteQRCode(dados).GetGraphic(pixelsPorModulo);
    }
}
