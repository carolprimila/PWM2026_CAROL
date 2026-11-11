using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using ProjetoClubes.Api.Autorizacao;
using ProjetoClubes.Api.Middleware;
using ProjetoClubes.Application.Autenticacao;
using ProjetoClubes.Infrastructure.Persistence;
using ProjetoClubes.Infrastructure.Seguranca;

namespace ProjetoClubes.Api.Configuracao;

public static class ApiServiceExtensions
{
    public const string PoliticaCors = "clientes";
    public const string LimiteAutenticacao = "autenticacao";

    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AutenticacaoOptions>(configuration.GetSection(AutenticacaoOptions.Secao));

        services.AddControllers(o => o.SuppressAsyncSuffixInActionNames = true)
            .AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
            })
            .ConfigureApiBehaviorOptions(o =>
            {
                // Erros de binding (JSON malformado, tipos errados) no mesmo formato das validações.
                o.InvalidModelStateResponseFactory = ctx =>
                {
                    var problema = new ValidationProblemDetails(ctx.ModelState)
                    {
                        Status = 400,
                        Title = "Verifique os dados informados.",
                        Type = "https://httpstatuses.io/400",
                    };
                    problema.Extensions["codigo"] = "validacao";
                    return new BadRequestObjectResult(problema);
                };
            });

        services.AddProblemDetails();
        services.AddExceptionHandler<TratamentoGlobalExcecoes>();
        services.AddHealthChecks().AddDbContextCheck<AppDbContext>("postgres");

        AddAutenticacao(services, configuration);
        AddLimites(services, configuration);
        AddCors(services, configuration);
        AddSwagger(services);

        // X-Forwarded-For só é aceito de proxies conhecidos (o IP do cliente alimenta o limite de tentativas de login).
        // Padrão: apenas a própria máquina. Atrás de um proxy reverso/rede do Docker, liste as redes em Proxy:RedesConfiaveis.
        var redes = configuration.GetSection("Proxy:RedesConfiaveis").Get<string[]>() ?? [];
        services.Configure<ForwardedHeadersOptions>(o =>
        {
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            foreach (var rede in redes) o.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(rede));
        });
        return services;
    }

    private static void AddAutenticacao(IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.Secao).Get<JwtOptions>() ?? new JwtOptions();
        var chave = TokenService.CriarChave(jwt.Chave); // falha na partida se a chave não estiver configurada

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.MapInboundClaims = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Emissor,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audiencia,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = chave,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = "name",
                };
                o.Events = new JwtBearerEvents { OnTokenValidated = ValidacaoSessaoJwt.OnTokenValidated };
            });

        services.AddSingleton<IAuthorizationPolicyProvider, PermissaoPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissaoHandler>();
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
    }

    private static void AddLimites(IServiceCollection services, IConfiguration configuration)
    {
        var porMinuto = configuration.GetValue("LimiteRequisicoes:AutenticacaoPorMinuto", 20);
        var geralPorMinuto = configuration.GetValue("LimiteRequisicoes:GeralPorMinuto", 600);
        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            // Limite geral por usuário autenticado (ou por IP sem login): protege contra varredura e abuso de listas.
            o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http =>
            {
                var chave = http.User.FindFirst("sub")?.Value is { } sub ? $"u:{sub}" : $"ip:{http.Connection.RemoteIpAddress}";
                return http.Request.Path.StartsWithSegments("/health")
                    ? RateLimitPartition.GetNoLimiter(chave)
                    : RateLimitPartition.GetFixedWindowLimiter(chave,
                        _ => new FixedWindowRateLimiterOptions { PermitLimit = geralPorMinuto, Window = TimeSpan.FromMinutes(1) });
            });
            o.AddPolicy(LimiteAutenticacao, http => RateLimitPartition.GetFixedWindowLimiter(
                http.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = porMinuto, Window = TimeSpan.FromMinutes(1) }));
        });
    }

    private static void AddCors(IServiceCollection services, IConfiguration configuration)
    {
        var origens = configuration.GetSection("Cors:Origens").Get<string[]>() ?? [];
        services.AddCors(o => o.AddPolicy(PoliticaCors, p =>
            p.WithOrigins(origens).AllowAnyHeader().AllowAnyMethod()
                .WithExposedHeaders(ContextoRequisicaoMiddleware.CabecalhoCorrelacao)));
    }

    private static void AddSwagger(IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Projeto Clubes API",
                Version = "v1",
                Description = "API REST única consumida pelo Web administrativo e pelo aplicativo .NET MAUI.",
            });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Access token obtido em POST /api/v1/auth/login.",
            });
            c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", doc)] = [],
            });
            var xml = Path.Combine(AppContext.BaseDirectory, $"{typeof(ApiServiceExtensions).Assembly.GetName().Name}.xml");
            if (File.Exists(xml)) c.IncludeXmlComments(xml);
            c.CustomSchemaIds(t => t.FullName!.Replace("ProjetoClubes.Application.", "").Replace('+', '.'));
        });
    }
}
