namespace ProjetoClubes.Infrastructure.Seguranca;

public sealed class JwtOptions
{
    public const string Secao = "Jwt";

    public string Emissor { get; set; } = "ProjetoClubes";
    public string Audiencia { get; set; } = "ProjetoClubes.Clientes";
    /// <summary>Chave HMAC-SHA256 com pelo menos 32 bytes. Obrigatória; vem de variável de ambiente (Jwt__Chave).</summary>
    public string Chave { get; set; } = string.Empty;
    public int MinutosAccessToken { get; set; } = 15;
    public int DiasRefreshToken { get; set; } = 14;
}

public static class ClaimsProjeto
{
    public const string Sessao = "sid";
    public const string Clube = "clube_id";
    public const string SuperAdmin = "sa";
}
