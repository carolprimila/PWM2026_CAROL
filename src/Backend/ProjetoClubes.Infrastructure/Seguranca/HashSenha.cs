using Microsoft.AspNetCore.Identity;
using ProjetoClubes.Application.Common;

namespace ProjetoClubes.Infrastructure.Seguranca;

/// <summary>PBKDF2 (formato v3 do ASP.NET Core Identity: HMAC-SHA512, 100 mil+ iterações, salt aleatório).</summary>
public sealed class HashSenha : IHashSenha
{
    private static readonly object Usuario = new();
    private readonly PasswordHasher<object> _hasher = new();

    public string Gerar(string senha) => _hasher.HashPassword(Usuario, senha);

    public (bool Valida, bool PrecisaRehash) Verificar(string hash, string senha)
    {
        try
        {
            return _hasher.VerifyHashedPassword(Usuario, hash, senha) switch
            {
                PasswordVerificationResult.Success => (true, false),
                PasswordVerificationResult.SuccessRehashNeeded => (true, true),
                _ => (false, false),
            };
        }
        catch (FormatException)
        {
            return (false, false);
        }
    }
}
