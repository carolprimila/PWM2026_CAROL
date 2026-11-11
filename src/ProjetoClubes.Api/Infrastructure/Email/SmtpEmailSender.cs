using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjetoClubes.Application.Common;

namespace ProjetoClubes.Infrastructure.Email;

/// <summary>
/// Envio real por SMTP (qualquer provedor transacional que aceite SMTP com TLS). Configuração em Email:Smtp;
/// a senha vem de variável de ambiente/segredo, nunca do appsettings versionado.
/// </summary>
public sealed class SmtpEmailSender(IOptions<EmailOptions> opcoes, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task EnviarAsync(MensagemEmail mensagem, CancellationToken ct = default)
    {
        var o = opcoes.Value;
        using var cliente = new SmtpClient(o.Smtp.Host, o.Smtp.Porta)
        {
            EnableSsl = o.Smtp.UsarTls,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Credentials = string.IsNullOrEmpty(o.Smtp.Usuario) ? null : new NetworkCredential(o.Smtp.Usuario, o.Smtp.Senha),
        };
        using var email = new MailMessage(o.Remetente, mensagem.Para, mensagem.Assunto, mensagem.CorpoTexto)
        {
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8,
        };
        await cliente.SendMailAsync(email, ct);
        // Não registra o destinatário no log (dado pessoal): só o assunto.
        logger.LogInformation("E-mail enviado por SMTP (assunto: {Assunto})", mensagem.Assunto);
    }
}
