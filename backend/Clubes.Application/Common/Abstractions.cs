using Clubes.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Clubes.Application.Common;

/// <summary>Acesso a dados usado pela camada de aplicação (implementado pelo DbContext na infraestrutura).</summary>
public interface IAppDb
{
    DbSet<T> Set<T>() where T : class;
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    /// <summary>Próximo número de uma sequência por clube (matrícula, protocolo, recibo...). Atômico no banco.</summary>
    Task<long> NextSequenceAsync(string name, CancellationToken ct = default);
}

/// <summary>
/// Contexto da requisição: quem está autenticado e em qual clube opera. Preenchido pela API a partir
/// do token validado (nunca a partir de dados do corpo da requisição) ou por tarefas em segundo plano.
/// </summary>
public class RequestContext
{
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public Guid? ClubId { get; set; }
    public UserKind? Kind { get; set; }
    public Guid? MemberId { get; set; }
    public Guid? DependentId { get; set; }
    public string? Origin { get; set; }
    /// <summary>Justificativa informada pelo operador para a operação corrente (gravada na auditoria).</summary>
    public string? Justification { get; set; }

    public bool IsStaff => Kind is UserKind.SuperAdmin or UserKind.ClubAdmin or UserKind.Staff;
    public bool IsAdmin => Kind is UserKind.SuperAdmin or UserKind.ClubAdmin;

    public Guid RequireUser() => UserId ?? throw new DomainException("Autenticação necessária.", "nao_autenticado", 401);
    public Guid RequireClub() => ClubId ?? throw new DomainException("Selecione um clube para executar esta operação.", "clube_nao_selecionado", 400);
    public Guid RequireMember() => MemberId ?? throw DomainException.Forbidden("Operação disponível somente para associados.");
}

public record TokenPair(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken, DateTime RefreshTokenExpiresAt);

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateAccessToken(User user);
    string CreateOpaqueToken(int bytes = 32);
    string Hash(string token);
    TimeSpan RefreshTokenLifetime { get; }
}

public interface IFileStorage
{
    Task<string> SaveAsync(Guid clubId, string fileName, Stream content, CancellationToken ct = default);
    Task<Stream> OpenAsync(string storagePath, CancellationToken ct = default);
    Task DeleteAsync(string storagePath, CancellationToken ct = default);
}

public record EmailMessage(string To, string Subject, string Body);

public interface IEmailSender
{
    /// <summary>Indica se há um provedor de e-mail configurado.</summary>
    bool IsConfigured { get; }
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}

public interface IPushSender
{
    bool IsConfigured { get; }
    Task SendAsync(IReadOnlyCollection<string> deviceTokens, string title, string body, IDictionary<string, string>? data = null, CancellationToken ct = default);
}

public record GatewayCheckout(string Provider, string Reference, string? CheckoutUrl, string? PixCopyPaste, DateTime? ExpiresAt);
public record GatewayNotification(string Reference, bool Paid, string? RawPayload);

/// <summary>
/// Integração com gateway de pagamento. O sistema nunca armazena dados de cartão: o pagamento é
/// concluído no ambiente do provedor e confirmado por notificação (webhook) assinada.
/// </summary>
public interface IPaymentGateway
{
    string Provider { get; }
    bool IsConfigured { get; }
    Task<GatewayCheckout> CreateCheckoutAsync(Guid paymentId, decimal amount, string description, PaymentMethod method, string payerName, string payerDocument, CancellationToken ct = default);
    /// <summary>Valida a assinatura e interpreta a notificação recebida do provedor.</summary>
    GatewayNotification? ParseNotification(string body, IDictionary<string, string> headers);
}

/// <summary>Publica eventos em tempo real para os clientes conectados do clube.</summary>
public interface IRealtimeNotifier
{
    Task PublishAsync(Guid clubId, string eventName, object payload, CancellationToken ct = default);
    Task PublishToUserAsync(Guid userId, string eventName, object payload, CancellationToken ct = default);
}

public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

public class PageQuery
{
    private int _page = 1;
    private int _pageSize = 20;
    public int Page { get => _page; set => _page = Math.Max(1, value); }
    public int PageSize { get => _pageSize; set => _pageSize = Math.Clamp(value, 1, 200); }
    public string? Search { get; set; }
    public string? Sort { get; set; }
    public bool Desc { get; set; }
}

public static class QueryExtensions
{
    public static async Task<PagedResult<T>> ToPagedAsync<T>(this IQueryable<T> query, PageQuery page, CancellationToken ct = default)
    {
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page.Page - 1) * page.PageSize).Take(page.PageSize).ToListAsync(ct);
        return new PagedResult<T>(items, total, page.Page, page.PageSize);
    }

    public static async Task<T> FirstOr404Async<T>(this IQueryable<T> query, string what, CancellationToken ct = default) where T : class
        => await query.FirstOrDefaultAsync(ct) ?? throw DomainException.NotFound(what);

    public static string OnlyDigits(this string? value) => new((value ?? "").Where(char.IsDigit).ToArray());

    /// <summary>Padrão para ILIKE com escape dos curingas informados pelo usuário.</summary>
    public static string Like(this string? term) => "%" + (term ?? "").Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
}

public static class Cpf
{
    public static bool IsValid(string? value)
    {
        var d = value.OnlyDigits();
        if (d.Length != 11 || d.Distinct().Count() == 1) return false;
        for (var j = 9; j < 11; j++)
        {
            var sum = 0;
            for (var i = 0; i < j; i++) sum += (d[i] - '0') * (j + 1 - i);
            var digit = sum * 10 % 11 % 10;
            if (digit != d[j] - '0') return false;
        }
        return true;
    }

    /// <summary>Mascara o CPF para exibição a quem não precisa do número completo (LGPD).</summary>
    public static string? Mask(string? value)
    {
        var d = value.OnlyDigits();
        return d.Length == 11 ? $"***.{d.Substring(3, 3)}.{d.Substring(6, 3)}-**" : null;
    }
}
