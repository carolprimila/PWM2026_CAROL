namespace ProjetoClubes.Mobile;

/// <summary>Endereço da API usado pelo app.</summary>
internal static class Configuracao
{
    /// <summary>
    /// API pública (HTTPS) das versões publicadas (Release). Troque pelo endereço real antes de gerar o pacote das lojas —
    /// o Android bloqueia HTTP sem TLS em versões de produção.
    /// </summary>
    public const string UrlApiProducao = "https://api.example.com/";

#if DEBUG
    // Desenvolvimento: o emulador Android enxerga o computador em 10.0.2.2.
    public static string UrlApi => DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5080/" : "http://localhost:5080/";
#else
    public static string UrlApi => UrlApiProducao;
#endif
}
