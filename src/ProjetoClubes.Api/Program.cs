using ProjetoClubes.Api.Configuracao;
using ProjetoClubes.Api.Middleware;
using ProjetoClubes.Application;
using ProjetoClubes.Infrastructure;
using ProjetoClubes.Infrastructure.Persistence.Seed;
using Serilog;

// Logger mínimo apenas para falhas antes da configuração completa do host.
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    VerificarConfiguracaoDeProducao(builder);

    builder.Services.AddSerilog((sp, cfg) => cfg
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(sp)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Aplicacao", "ProjetoClubes.Api"));

    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath)
        .AddApi(builder.Configuration);

    var app = builder.Build();

    app.UseForwardedHeaders();
    app.UseMiddleware<CabecalhosSegurancaMiddleware>();
    app.UseMiddleware<ContextoRequisicaoMiddleware>();
    // O log da requisição fica por fora do tratamento de erros: registra o status que o cliente realmente recebeu
    // (ex.: 401/422), e não um 500 por ter visto a exceção antes de ela ser tratada.
    app.UseSerilogRequestLogging(o =>
        o.GetLevel = (http, _, ex) => ex is not null || http.Response.StatusCode >= 500
            ? Serilog.Events.LogEventLevel.Error
            : http.Request.Path.StartsWithSegments("/health") || http.Response.StatusCode == 499
                ? Serilog.Events.LogEventLevel.Debug
                : Serilog.Events.LogEventLevel.Information);
    app.UseExceptionHandler();
    // Cliente desistiu da requisição (fechou o app, timeout, F5 no navegador): não é erro do servidor.
    app.Use(async (contexto, proximo) =>
    {
        try { await proximo(contexto); }
        catch (OperationCanceledException) when (contexto.RequestAborted.IsCancellationRequested)
        {
            if (!contexto.Response.HasStarted) contexto.Response.StatusCode = 499;
        }
    });

    if (app.Configuration.GetValue("Swagger:Habilitado", app.Environment.IsDevelopment()))
    {
        app.UseSwagger();
        app.UseSwaggerUI(o =>
        {
            o.SwaggerEndpoint("/swagger/v1/swagger.json", "Projeto Clubes API v1");
            o.DocumentTitle = "Projeto Clubes API";
        });
    }

    if (!app.Environment.IsDevelopment()) app.UseHsts();

    app.UseCors(ApiServiceExtensions.PoliticaCors);
    app.UseAuthentication();
    app.UseRateLimiter();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health").AllowAnonymous();

    await InicializadorBanco.ExecutarAsync(app.Services);
    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "A API foi encerrada por um erro na inicialização");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

// Em produção, recusa configurações perigosas antes de abrir a porta: dados de demonstração e CORS aberto/sem HTTPS.
static void VerificarConfiguracaoDeProducao(WebApplicationBuilder builder)
{
    if (!builder.Environment.IsProduction()) return;
    var c = builder.Configuration;
    if (c.GetValue("Seed:Demo", false))
        throw new InvalidOperationException("Seed:Demo não pode ser habilitado em produção.");
    if ((c["Jwt:Chave"] ?? "").Contains("DESENVOLVIMENTO", StringComparison.Ordinal))
        throw new InvalidOperationException("Em produção, defina uma Jwt:Chave própria (a chave de desenvolvimento do repositório é pública).");
    if (c.GetSection("Cors:Origens").Get<string[]>()?.Any(o => o == "*" || o.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                                                             && !o.Contains("localhost")) == true)
        throw new InvalidOperationException("Em produção, Cors:Origens deve listar apenas origens HTTPS.");
}

/// <summary>Exposto para os testes de integração (WebApplicationFactory).</summary>
public partial class Program;
