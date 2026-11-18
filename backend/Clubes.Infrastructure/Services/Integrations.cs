using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Clubes.Application.Common;
using Clubes.Domain;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Clubes.Infrastructure.Services;

// ---------- E-mail (SMTP) ----------

public class SmtpOptions
{
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? User { get; set; }
    public string? Password { get; set; }
    public string? From { get; set; }
    public string FromName { get; set; } = "Clube";
    public bool UseSsl { get; set; } = true;
}

public class SmtpEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    private readonly SmtpOptions _opt = options.Value;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_opt.Host) && !string.IsNullOrWhiteSpace(_opt.From);

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        if (!IsConfigured) throw new InvalidOperationException("Envio de e-mail não configurado (Email:Smtp).");
        using var client = new SmtpClient(_opt.Host, _opt.Port) { EnableSsl = _opt.UseSsl };
        if (!string.IsNullOrEmpty(_opt.User)) client.Credentials = new NetworkCredential(_opt.User, _opt.Password);
        using var mail = new MailMessage(new MailAddress(_opt.From!, _opt.FromName), new MailAddress(message.To)) { Subject = message.Subject, Body = message.Body };
        await client.SendMailAsync(mail, ct);
    }
}

// ---------- Push (Firebase Cloud Messaging) ----------

public class PushOptions
{
    /// <summary>Caminho do arquivo JSON da conta de serviço do Firebase. Sem ele, o push fica desabilitado.</summary>
    public string? FirebaseCredentialsFile { get; set; }
}

public class FirebasePushSender : IPushSender
{
    private readonly FirebaseMessaging? _messaging;

    public FirebasePushSender(IOptions<PushOptions> options, ILogger<FirebasePushSender> logger)
    {
        var path = options.Value.FirebaseCredentialsFile;
        if (string.IsNullOrWhiteSpace(path)) return;
        if (!File.Exists(path))
        {
            logger.LogError("Arquivo de credenciais do Firebase não encontrado em {Path}. Notificações push desabilitadas.", path);
            return;
        }
        var app = FirebaseApp.DefaultInstance ?? FirebaseApp.Create(new AppOptions { Credential = CredentialFactory.FromFile<ServiceAccountCredential>(path).ToGoogleCredential() });
        _messaging = FirebaseMessaging.GetMessaging(app);
    }

    public bool IsConfigured => _messaging is not null;

    public async Task SendAsync(IReadOnlyCollection<string> deviceTokens, string title, string body, IDictionary<string, string>? data = null, CancellationToken ct = default)
    {
        if (_messaging is null) throw new InvalidOperationException("Push não configurado (Push:FirebaseCredentialsFile).");
#pragma warning disable CS0618 // os tokens de registro FCM continuam sendo o identificador entregue pelos aplicativos
        var message = new MulticastMessage
        {
            Tokens = deviceTokens.ToList(),
#pragma warning restore CS0618
            Notification = new FirebaseAdmin.Messaging.Notification { Title = title, Body = body },
            Data = data is null ? null : new Dictionary<string, string>(data),
        };
        var response = await _messaging.SendEachForMulticastAsync(message, ct);
        if (response.SuccessCount == 0 && response.FailureCount > 0)
            throw new InvalidOperationException(response.Responses.First(r => !r.IsSuccess).Exception?.Message ?? "Falha no envio de push.");
    }
}

// ---------- Gateway de pagamento ----------

public class PaymentOptions
{
    /// <summary>"None" (padrão) ou "Sandbox" (somente para homologação; recusado em produção).</summary>
    public string Provider { get; set; } = "None";
    public string? SandboxSecret { get; set; }
}

/// <summary>Sem gateway contratado: o pagamento pelo aplicativo informa indisponibilidade e orienta a secretaria.</summary>
public class DisabledPaymentGateway : IPaymentGateway
{
    public string Provider => "None";
    public bool IsConfigured => false;
    public Task<GatewayCheckout> CreateCheckoutAsync(Guid paymentId, decimal amount, string description, PaymentMethod method, string payerName, string payerDocument, CancellationToken ct = default)
        => throw new DomainException("Gateway de pagamento não configurado.", "gateway_nao_configurado", 503);
    public GatewayNotification? ParseNotification(string body, IDictionary<string, string> headers) => null;
}

/// <summary>
/// Gateway de homologação: percorre o mesmo fluxo de um provedor real (checkout pendente + confirmação
/// por notificação assinada com HMAC), sem movimentar dinheiro. Não é registrado em ambiente de produção.
/// </summary>
public class SandboxPaymentGateway(IOptions<PaymentOptions> options) : IPaymentGateway
{
    public const string SignatureHeader = "X-Signature";
    private readonly string _secret = options.Value.SandboxSecret ?? "";

    public string Provider => "Sandbox";
    public bool IsConfigured => _secret.Length >= 16;

    public Task<GatewayCheckout> CreateCheckoutAsync(Guid paymentId, decimal amount, string description, PaymentMethod method, string payerName, string payerDocument, CancellationToken ct = default)
    {
        var reference = "sbx_" + paymentId.ToString("N");
        return Task.FromResult(new GatewayCheckout(Provider, reference, null, method == PaymentMethod.Pix ? $"SANDBOX-PIX-{reference}" : null, DateTime.UtcNow.AddMinutes(30)));
    }

    public GatewayNotification? ParseNotification(string body, IDictionary<string, string> headers)
    {
        if (!IsConfigured || !headers.TryGetValue(SignatureHeader, out var signature)) return null;
        var expected = Sign(body, _secret);
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(signature))) return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            var reference = doc.RootElement.GetProperty("reference").GetString();
            var paid = doc.RootElement.GetProperty("paid").GetBoolean();
            return reference is null ? null : new GatewayNotification(reference, paid, body);
        }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException) { return null; }
    }

    public static string Sign(string body, string secret)
        => Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(body)));
}
