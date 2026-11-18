using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Clubes.API.Infrastructure;
using Clubes.Application.Common;
using Clubes.Infrastructure;
using Clubes.Infrastructure.Persistence;
using Clubes.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;

var culture = CultureInfo.GetCultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, logger) => logger
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(Path.Combine(context.Configuration["Logging:Directory"] ?? "logs", "clubes-.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30));

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (jwt.Key.Length < 32) throw new InvalidOperationException("Configure Jwt:Key (variável de ambiente Jwt__Key) com ao menos 32 caracteres.");

builder.Services.AddClubes(builder.Configuration, builder.Environment);
builder.Services.AddSignalR().AddJsonProtocol(o => o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton<IRealtimeNotifier, SignalRNotifier>();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState.Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(e => e.Key, e => e.Value!.Errors.Select(x => ValidationMessages.Translate(x.ErrorMessage)).ToArray());
        return new BadRequestObjectResult(new ApiError("validacao", "Verifique os campos informados.", errors));
    });

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.MapInboundClaims = false;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = jwt.Issuer,
        ValidateAudience = true, ValidAudience = jwt.Audience,
        ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
        ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = "name",
    };
    // WebSockets não enviam cabeçalho Authorization: o SignalR recebe o token pela query string.
    o.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs")) context.Token = token;
            return Task.CompletedTask;
        },
        // Respostas 401/403 do próprio middleware também seguem o formato padronizado de erro.
        OnChallenge = async context =>
        {
            context.HandleResponse();
            if (context.Response.HasStarted) return;
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new ApiError("nao_autenticado", "Autenticação necessária."));
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new ApiError("acesso_negado", "Você não tem permissão para executar esta operação."));
        },
    };
});
builder.Services.AddAuthorization();

var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()
    .WithExposedHeaders("Content-Disposition")));

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.OnRejected = async (context, ct) =>
        await context.HttpContext.Response.WriteAsJsonAsync(new ApiError("muitas_requisicoes", "Muitas requisições. Aguarde um instante e tente novamente."), ct);
    static string Client(HttpContext http) => http.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
    var general = builder.Configuration.GetValue("RateLimit:PerMinute", 600);
    var auth = builder.Configuration.GetValue("RateLimit:AuthPerMinute", 10);
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http =>
        RateLimitPartition.GetFixedWindowLimiter(Client(http), _ => new FixedWindowRateLimiterOptions { PermitLimit = general, Window = TimeSpan.FromMinutes(1) }));
    o.AddPolicy("auth", http =>
        RateLimitPartition.GetFixedWindowLimiter("auth:" + Client(http), _ => new FixedWindowRateLimiterOptions { PermitLimit = auth, Window = TimeSpan.FromMinutes(1) }));
});

builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Clubes API", Version = "v1",
        Description = "API REST do sistema de gestão de clubes. Autentique-se em /api/auth/login e informe o token no botão Authorize.",
    });
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", Description = "Token de acesso (JWT)" });
    o.AddSecurityRequirement(document => new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] });
    o.CustomSchemaIds(SwaggerNaming.SchemaId);
    o.SupportNonNullableReferenceTypes();
    o.SchemaFilter<RequireNonNullableSchemaFilter>();
    var xml = Path.Combine(AppContext.BaseDirectory, "Clubes.API.xml");
    if (File.Exists(xml)) o.IncludeXmlComments(xml);
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
    await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().RunAsync();

app.UseForwardedHeaders();
app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionMiddleware>();
app.Use(async (http, next) =>
{
    http.Response.Headers.XContentTypeOptions = "nosniff";
    http.Response.Headers.XFrameOptions = "DENY";
    http.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});

if (builder.Configuration.GetValue("Swagger:Enabled", app.Environment.IsDevelopment()))
{
    app.UseSwagger();
    app.UseSwaggerUI(o => o.DocumentTitle = "Clubes API");
}

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<RequestContextMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ClubHub>("/hubs/clube");

// Monitoramento de disponibilidade: verifica a API e a conexão com o PostgreSQL.
app.MapGet("/health", async (AppDbContext db, CancellationToken ct) =>
{
    var database = await db.Database.CanConnectAsync(ct);
    return Results.Json(new { status = database ? "ok" : "degradado", database, time = DateTime.UtcNow }, statusCode: database ? 200 : 503);
}).AllowAnonymous().DisableRateLimiting();

app.Run();

public partial class Program;
