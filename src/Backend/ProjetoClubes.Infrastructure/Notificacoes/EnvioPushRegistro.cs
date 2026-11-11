using Microsoft.Extensions.Logging;
using ProjetoClubes.Application.Comunicacao;

namespace ProjetoClubes.Infrastructure.Notificacoes;

/// <summary>
/// Implementação padrão de push: só registra em log. As notificações já ficam na caixa de entrada do app.
/// Para ativar Firebase Cloud Messaging/APNs, criar outra implementação de <see cref="IEnvioPush"/> com as
/// credenciais em configuração segura e o token do dispositivo registrado pelo app (DT-028).
/// </summary>
public sealed class EnvioPushRegistro(ILogger<EnvioPushRegistro> logger) : IEnvioPush
{
    public Task EnviarAsync(Guid clubeId, IReadOnlyCollection<Guid> usuarioIds, string titulo, string texto, CancellationToken ct)
    {
        logger.LogInformation("Push não configurado: notificação \"{Titulo}\" gravada para {Quantidade} usuário(s) do clube {ClubeId}",
            titulo, usuarioIds.Count, clubeId);
        return Task.CompletedTask;
    }
}
