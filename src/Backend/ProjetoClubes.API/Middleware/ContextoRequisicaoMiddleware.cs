using Serilog.Context;
using ProjetoClubes.Infrastructure.Seguranca;

namespace ProjetoClubes.API.Middleware;

/// <summary>Define Correlation-Id, IP e User-Agent da requisição e cabeçalhos de segurança básicos.</summary>
public sealed class ContextoRequisicaoMiddleware(RequestDelegate next)
{
    public const string CabecalhoCorrelacao = "X-Correlation-Id";
    public const string ChaveItem = "CorrelationId";

    public async Task InvokeAsync(HttpContext context, UsuarioAtual usuario)
    {
        var correlacao = context.Request.Headers[CabecalhoCorrelacao].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlacao) || correlacao.Length > 64) correlacao = context.TraceIdentifier;

        context.Items[ChaveItem] = correlacao;
        usuario.DefinirRequisicao(context.Connection.RemoteIpAddress?.ToString(),
            context.Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua : null, correlacao);

        context.Response.OnStarting(() =>
        {
            var h = context.Response.Headers;
            h[CabecalhoCorrelacao] = correlacao;
            h.XContentTypeOptions = "nosniff";
            h.XFrameOptions = "DENY";
            h["Referrer-Policy"] = "no-referrer";
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlacao))
            await next(context);
    }
}
