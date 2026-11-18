using System.Security.Claims;
using Clubes.Application.Access;
using Clubes.Application.Common;
using Clubes.Domain;
using Clubes.Infrastructure.Persistence;
using Clubes.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Clubes.API.Infrastructure;

public record ApiError(string Code, string Message, IDictionary<string, string[]>? Errors = null, string? TraceId = null);

/// <summary>Tratamento global de exceções: toda falha vira uma resposta padronizada, sem expor detalhes internos.</summary>
public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext http)
    {
        try { await next(http); }
        catch (DomainException ex)
        {
            if (ex.Status is 401 or 403) logger.LogWarning("Acesso negado em {Path}: {Message} (usuário {User})", http.Request.Path, ex.Message, http.User.FindFirstValue("sub"));
            await WriteAsync(http, ex.Status, new ApiError(ex.Code, ex.Message));
        }
        catch (DbUpdateConcurrencyException)
        {
            await WriteAsync(http, 409, new ApiError("conflito", "O registro foi alterado por outra operação. Atualize e tente novamente."));
        }
        catch (OperationCanceledException) when (http.RequestAborted.IsCancellationRequested) { /* cliente desistiu da requisição */ }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro não tratado em {Method} {Path}", http.Request.Method, http.Request.Path);
            await WriteAsync(http, 500, new ApiError("erro_interno", "Ocorreu um erro inesperado. Tente novamente em instantes.", TraceId: http.TraceIdentifier));
        }
    }

    private static async Task WriteAsync(HttpContext http, int status, ApiError error)
    {
        if (http.Response.HasStarted) return;
        http.Response.Clear();
        http.Response.StatusCode = status;
        await http.Response.WriteAsJsonAsync(error);
    }
}

/// <summary>
/// Preenche o contexto da requisição a partir do token já validado. O clube vem sempre do token;
/// somente o superadministrador pode escolher o clube em que opera (cabeçalho X-Club-Id), e o
/// servidor confirma que o clube existe.
/// </summary>
public class RequestContextMiddleware(RequestDelegate next)
{
    public const string ClubHeader = "X-Club-Id";

    public async Task InvokeAsync(HttpContext http, RequestContext ctx, AppDbContext db)
    {
        var user = http.User;
        ctx.Origin = $"{http.Connection.RemoteIpAddress} · {Truncate(http.Request.Headers.UserAgent.ToString(), 120)}";
        if (user.Identity?.IsAuthenticated == true && Guid.TryParse(user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            ctx.UserId = userId;
            ctx.UserName = user.FindFirstValue("name") ?? user.FindFirstValue(ClaimTypes.Name);
            ctx.Kind = Enum.TryParse<UserKind>(user.FindFirstValue(ClubClaims.Kind), out var kind) ? kind : null;
            ctx.ClubId = Guid.TryParse(user.FindFirstValue(ClubClaims.Club), out var club) ? club : null;
            ctx.MemberId = Guid.TryParse(user.FindFirstValue(ClubClaims.Member), out var member) ? member : null;
            ctx.DependentId = Guid.TryParse(user.FindFirstValue(ClubClaims.Dependent), out var dependent) ? dependent : null;

            if (ctx.Kind == UserKind.SuperAdmin && Guid.TryParse(http.Request.Headers[ClubHeader], out var selected))
            {
                if (!await db.Set<Club>().AnyAsync(c => c.Id == selected, http.RequestAborted))
                    throw DomainException.NotFound("Clube");
                ctx.ClubId = selected;
            }
        }
        await next(http);
    }

    private static string Truncate(string value, int max) => value.Length > max ? value[..max] : value;
}

/// <summary>
/// Autorização por permissão, sempre validada no servidor. Com <see cref="Self"/> = true, associados e
/// dependentes também são aceitos — nesse caso os serviços restringem os dados ao próprio usuário.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class PermissionAttribute(params string[] anyOf) : Attribute, IAsyncAuthorizationFilter
{
    public bool Self { get; set; }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var ctx = context.HttpContext.RequestServices.GetRequiredService<RequestContext>();
        if (ctx.UserId is null) { context.Result = Deny(401, "nao_autenticado", "Autenticação necessária."); return; }
        if (ctx.Kind is UserKind.Member or UserKind.Dependent)
        {
            if (!Self) context.Result = Deny(403, "acesso_negado", "Você não tem permissão para executar esta operação.");
            return;
        }
        var permissions = await context.HttpContext.RequestServices.GetRequiredService<PermissionService>().GetForCurrentUserAsync(context.HttpContext.RequestAborted);
        if (!anyOf.Any(permissions.Contains)) context.Result = Deny(403, "acesso_negado", "Você não tem permissão para executar esta operação.");
    }

    private static ObjectResult Deny(int status, string code, string message) => new(new ApiError(code, message)) { StatusCode = status };
}

/// <summary>Restringe o endpoint a tipos de usuário (ex.: somente associados e dependentes).</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class KindsAttribute(params UserKind[] kinds) : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var ctx = context.HttpContext.RequestServices.GetRequiredService<RequestContext>();
        if (ctx.Kind is not { } kind || !kinds.Contains(kind))
            context.Result = new ObjectResult(new ApiError("acesso_negado", "Esta funcionalidade não está disponível para o seu perfil.")) { StatusCode = 403 };
    }
}

[ApiController]
[Authorize]
[Produces("application/json")]
public abstract class ApiController : ControllerBase
{
    protected string? Ip => HttpContext.Connection.RemoteIpAddress?.ToString();
    protected string UserAgent => Request.Headers.UserAgent.ToString();
}

// ---------- Tempo real ----------

[Authorize]
public class ClubHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var user = Context.User;
        if (Guid.TryParse(user?.FindFirstValue("sub") ?? user?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, SignalRNotifier.UserGroup(userId));
        // O grupo do clube vem exclusivamente do token: um cliente não consegue escutar eventos de outro clube.
        if (Guid.TryParse(user?.FindFirstValue(ClubClaims.Club), out var clubId))
            await Groups.AddToGroupAsync(Context.ConnectionId, SignalRNotifier.ClubGroup(clubId));
        await base.OnConnectedAsync();
    }
}

public class SignalRNotifier(IHubContext<ClubHub> hub) : IRealtimeNotifier
{
    public static string ClubGroup(Guid clubId) => $"club:{clubId:N}";
    public static string UserGroup(Guid userId) => $"user:{userId:N}";

    public Task PublishAsync(Guid clubId, string eventName, object payload, CancellationToken ct = default)
        => hub.Clients.Group(ClubGroup(clubId)).SendAsync(eventName, payload, ct);

    public Task PublishToUserAsync(Guid userId, string eventName, object payload, CancellationToken ct = default)
        => hub.Clients.Group(UserGroup(userId)).SendAsync(eventName, payload, ct);
}
