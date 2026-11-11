using ProjetoClubes.Application.Common;

namespace ProjetoClubes.Infrastructure.Seguranca;

/// <summary>
/// Contexto do usuário por escopo (requisição). Preenchido pela API somente depois que o JWT, a sessão e o vínculo
/// com o clube foram validados no servidor; ou marcado como <see cref="Sistema"/> por rotinas internas.
/// </summary>
public sealed class UsuarioAtual : IUsuarioAtual
{
    public bool Autenticado { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public Guid? SessaoId { get; private set; }
    public Guid? ClubeId { get; private set; }
    public bool SuperAdmin { get; private set; }
    public bool Sistema { get; private set; }
    public string? Ip { get; private set; }
    public string? UserAgent { get; private set; }
    public string? CorrelationId { get; private set; }

    public void DefinirRequisicao(string? ip, string? userAgent, string? correlationId)
    {
        Ip = ip;
        UserAgent = userAgent;
        CorrelationId = correlationId;
    }

    public void DefinirAutenticado(Guid usuarioId, Guid sessaoId, Guid? clubeId, bool superAdmin)
    {
        Autenticado = true;
        UsuarioId = usuarioId;
        SessaoId = sessaoId;
        ClubeId = clubeId;
        SuperAdmin = superAdmin;
    }

    /// <summary>Rotinas internas (seed, migrações, tarefas). Nunca usar em fluxo de requisição.</summary>
    public void DefinirSistema(Guid? clubeId = null)
    {
        Sistema = true;
        ClubeId = clubeId;
    }
}
