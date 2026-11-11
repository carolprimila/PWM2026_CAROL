using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Domain.Identidade;

namespace ProjetoClubes.Infrastructure.Seguranca;

public sealed class TokenService(IOptions<JwtOptions> opcoes, TimeProvider relogio) : ITokenService
{
    private readonly JwtOptions _opcoes = opcoes.Value;

    public TimeSpan DuracaoRefreshToken => TimeSpan.FromDays(_opcoes.DiasRefreshToken);

    public AccessTokenGerado GerarAccessToken(Usuario usuario, SessaoUsuario sessao)
    {
        var agora = relogio.GetUtcNow();
        var expira = agora.AddMinutes(_opcoes.MinutosAccessToken);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimsProjeto.Sessao, sessao.Id.ToString()),
            new(JwtRegisteredClaimNames.Name, usuario.Nome),
        };
        if (sessao.ClubeId is { } clube) claims.Add(new Claim(ClaimsProjeto.Clube, clube.ToString()));
        if (usuario.SuperAdmin) claims.Add(new Claim(ClaimsProjeto.SuperAdmin, "true"));

        var credenciais = new SigningCredentials(CriarChave(_opcoes.Chave), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_opcoes.Emissor, _opcoes.Audiencia, claims,
            notBefore: agora.UtcDateTime, expires: expira.UtcDateTime, signingCredentials: credenciais);
        return new AccessTokenGerado(new JwtSecurityTokenHandler().WriteToken(token), expira);
    }

    public string GerarTokenOpaco() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));

    public string Hash(string token) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public static SymmetricSecurityKey CriarChave(string chave)
    {
        var bytes = Encoding.UTF8.GetBytes(chave);
        if (bytes.Length < 32)
            throw new InvalidOperationException(
                "Jwt:Chave ausente ou curta demais (mínimo 32 bytes). Defina a variável de ambiente Jwt__Chave.");
        return new SymmetricSecurityKey(bytes);
    }
}
