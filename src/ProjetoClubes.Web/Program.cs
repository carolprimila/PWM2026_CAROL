using System.Globalization;
using Microsoft.AspNetCore.DataProtection;
using ProjetoClubes.Web.Components;
using ProjetoClubes.Web.Servicos;

var ptBr = CultureInfo.GetCultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = ptBr;
CultureInfo.DefaultThreadCurrentUICulture = ptBr;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// O Web só conversa com a API REST (nunca com o banco).
builder.Services.AddHttpClient("api", c => c.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5080/"));
builder.Services.AddScoped<Sessao>();

// A sessão do navegador (refresh token em ProtectedSessionStorage) é cifrada com o DataProtection. Em produção as chaves
// precisam ficar num volume persistente, senão todos são deslogados a cada reinício do container.
if (builder.Configuration["DataProtection:Pasta"] is { Length: > 0 } pastaChaves)
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(pastaChaves)).SetApplicationName("ProjetoClubes.Web");
builder.Services.AddScoped<Api>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.Use((ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"] = "SAMEORIGIN";
    h["Referrer-Policy"] = "strict-origin-when-cross-origin";
    h["Content-Security-Policy"] = "frame-ancestors 'self'";
    h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    return next();
});
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
