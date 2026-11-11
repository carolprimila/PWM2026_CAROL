using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjetoClubes.Application.Common;

namespace ProjetoClubes.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string Secao = "Email";

    /// <summary>"Arquivo" (desenvolvimento: grava .eml em disco). Um provedor SMTP/transacional entra aqui em produção.</summary>
    public string Provedor { get; set; } = "Arquivo";
    public string PastaSaida { get; set; } = "App_Data/emails";
    public string Remetente { get; set; } = "nao-responda@projetoclubes.local";

    /// <summary>Usado quando <see cref="Provedor"/> = "Smtp". Senha somente por variável de ambiente/segredo.</summary>
    public SmtpOptions Smtp { get; set; } = new();
}

public sealed class SmtpOptions
{
    public string Host { get; set; } = "";
    public int Porta { get; set; } = 587;
    public string? Usuario { get; set; }
    public string? Senha { get; set; }
    /// <summary>STARTTLS/SSL. Só desligue para um servidor de testes local.</summary>
    public bool UsarTls { get; set; } = true;
}

/// <summary>
/// Envio de e-mail para desenvolvimento: grava a mensagem em um arquivo .eml local em vez de enviá-la.
/// Não substitui um provedor real — em produção registre outra implementação de <see cref="IEmailSender"/>.
/// </summary>
public sealed class ArquivoEmailSender(IOptions<EmailOptions> opcoes, ILogger<ArquivoEmailSender> logger) : IEmailSender
{
    public async Task EnviarAsync(MensagemEmail mensagem, CancellationToken ct = default)
    {
        var pasta = Path.GetFullPath(opcoes.Value.PastaSaida);
        Directory.CreateDirectory(pasta);
        var arquivo = Path.Combine(pasta, $"{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid():N}.eml");
        var conteudo = new StringBuilder()
            .AppendLine($"From: {opcoes.Value.Remetente}")
            .AppendLine($"To: {mensagem.Para}")
            .AppendLine($"Subject: {mensagem.Assunto}")
            .AppendLine("Content-Type: text/plain; charset=utf-8")
            .AppendLine()
            .AppendLine(mensagem.CorpoTexto)
            .ToString();
        await File.WriteAllTextAsync(arquivo, conteudo, Encoding.UTF8, ct);
        logger.LogInformation("E-mail de desenvolvimento gravado em {Arquivo} (assunto: {Assunto})", arquivo, mensagem.Assunto);
    }
}
