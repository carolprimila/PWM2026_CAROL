using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Clubes.Application.Common;
using Clubes.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Clubes.Infrastructure.Services;

public class JwtOptions
{
    public string Issuer { get; set; } = "clubes-api";
    public string Audience { get; set; } = "clubes-clients";
    /// <summary>Chave de assinatura (mínimo de 32 caracteres). Deve vir de variável de ambiente/segredo, nunca do repositório.</summary>
    public string Key { get; set; } = "";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 30;
}

public static class ClubClaims
{
    public const string Kind = "kind";
    public const string Club = "club";
    public const string Member = "member";
    public const string Dependent = "dependent";
}

public class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions _opt = options.Value;

    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(_opt.RefreshTokenDays);

    public (string Token, DateTime ExpiresAt) CreateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Name, user.Name),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClubClaims.Kind, user.Kind.ToString()),
        };
        if (user.ClubId is { } club) claims.Add(new Claim(ClubClaims.Club, club.ToString()));
        if (user.MemberId is { } member) claims.Add(new Claim(ClubClaims.Member, member.ToString()));
        if (user.DependentId is { } dependent) claims.Add(new Claim(ClubClaims.Dependent, dependent.ToString()));

        var expires = DateTime.UtcNow.AddMinutes(_opt.AccessTokenMinutes);
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_opt.Issuer, _opt.Audience, claims, DateTime.UtcNow, expires, credentials);
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public string CreateOpaqueToken(int bytes = 32) => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(bytes));

    public string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

public class StorageOptions
{
    public string RootPath { get; set; } = "storage";
}

/// <summary>
/// Armazenamento de arquivos em disco (volume Docker). Os arquivos ficam fora da raiz pública e só
/// são entregues pelo endpoint autenticado, que valida clube e permissão a cada download.
/// </summary>
public class LocalFileStorage(IOptions<StorageOptions> options) : IFileStorage
{
    private readonly string _root = Path.GetFullPath(options.Value.RootPath);

    public async Task<string> SaveAsync(Guid clubId, string fileName, Stream content, CancellationToken ct = default)
    {
        // O nome físico é gerado pelo servidor; o nome original é preservado apenas nos metadados.
        var relative = Path.Combine(clubId.ToString("N"), DateTime.UtcNow.ToString("yyyy-MM"), Guid.NewGuid().ToString("N") + Path.GetExtension(fileName).ToLowerInvariant());
        var full = Resolve(relative);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await using var file = new FileStream(full, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await content.CopyToAsync(file, ct);
        return relative.Replace('\\', '/');
    }

    public Task<Stream> OpenAsync(string storagePath, CancellationToken ct = default)
    {
        var full = Resolve(storagePath);
        if (!File.Exists(full)) throw DomainException.NotFound("Arquivo");
        return Task.FromResult<Stream>(new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true));
    }

    public Task DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        var full = Resolve(storagePath);
        if (File.Exists(full)) File.Delete(full);
        return Task.CompletedTask;
    }

    private string Resolve(string relative)
    {
        var full = Path.GetFullPath(Path.Combine(_root, relative));
        if (!full.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal)) throw DomainException.Forbidden("Caminho de arquivo inválido.");
        return full;
    }
}
