using VanderBakery.Web.Components;
using System.Globalization;

var ptBr = CultureInfo.GetCultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = ptBr;
CultureInfo.DefaultThreadCurrentUICulture = ptBr;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddHttpClient("BakeryApi", client => client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5293/"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
