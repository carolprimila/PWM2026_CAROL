namespace ProjetoClubes.API.Middleware;

/// <summary>
/// Cabeçalhos de segurança em todas as respostas. A API só devolve JSON/arquivos, então o CSP é o mais restrito possível
/// (exceto no Swagger UI, que precisa de scripts e estilos próprios).
/// </summary>
public sealed class CabecalhosSegurancaMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var h = context.Response.Headers;
            h["X-Content-Type-Options"] = "nosniff";
            h["X-Frame-Options"] = "DENY";
            h["Referrer-Policy"] = "no-referrer";
            h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            h["Cross-Origin-Resource-Policy"] = "same-site";
            if (!context.Request.Path.StartsWithSegments("/swagger"))
                h["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
            // Dados pessoais não ficam em cache de proxies/navegador.
            if (context.Request.Path.StartsWithSegments("/api")) h.CacheControl = "no-store";
            return Task.CompletedTask;
        });
        return next(context);
    }
}
