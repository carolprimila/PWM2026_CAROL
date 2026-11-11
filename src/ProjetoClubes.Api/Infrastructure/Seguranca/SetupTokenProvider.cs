using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ProjetoClubes.Application.Common;

namespace ProjetoClubes.Infrastructure.Seguranca;

/// <summary>
/// Guarda em memória o token de inicialização. Gerado na partida da API apenas quando não existe Superadmin
/// (ou definido por Setup__Token). Comparação em tempo constante; descartado após o uso.
/// </summary>
public sealed class SetupTokenProvider : ISetupTokenProvider
{
    private readonly Lock _trava = new();
    private string? _token;

    public bool Disponivel
    {
        get { lock (_trava) return _token is not null; }
    }

    /// <summary>Ativa o token (configurado ou aleatório) e o retorna para exibição no log.</summary>
    public string Ativar(string? configurado)
    {
        lock (_trava)
        {
            _token = string.IsNullOrWhiteSpace(configurado)
                ? Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(24))
                : configurado.Trim();
            return _token;
        }
    }

    public bool Validar(string token)
    {
        lock (_trava)
        {
            if (_token is null || string.IsNullOrEmpty(token)) return false;
            return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(_token), Encoding.UTF8.GetBytes(token.Trim()));
        }
    }

    public void Consumir()
    {
        lock (_trava) _token = null;
    }
}
