using ProjetoClubes.Domain.Common;

namespace ProjetoClubes.Domain.Identidade;

/// <summary>
/// Sessão autenticada (um dispositivo/navegador). Guarda apenas o hash do refresh token atual
/// e do anterior, para rotação com detecção de reutilização.
/// </summary>
public class SessaoUsuario : Entity, INaoAuditavel
{
    private SessaoUsuario() { }

    public SessaoUsuario(Guid usuarioId, Guid? clubeId, string refreshTokenHash, DateTimeOffset agora,
        DateTimeOffset expiraEm, string? ip, string? userAgent, string? dispositivo)
    {
        UsuarioId = usuarioId;
        ClubeId = clubeId;
        RefreshTokenHash = refreshTokenHash;
        CriadaEm = agora;
        UltimoUsoEm = agora;
        ExpiraEm = expiraEm;
        Ip = ip;
        UserAgent = userAgent;
        Dispositivo = dispositivo;
    }

    public Guid UsuarioId { get; private set; }
    public Usuario Usuario { get; private set; } = null!;
    /// <summary>Clube ativo nesta sessão (validado pelo servidor).</summary>
    public Guid? ClubeId { get; private set; }
    public string RefreshTokenHash { get; private set; } = null!;
    public string? RefreshTokenHashAnterior { get; private set; }
    public DateTimeOffset CriadaEm { get; private set; }
    public DateTimeOffset UltimoUsoEm { get; private set; }
    public DateTimeOffset ExpiraEm { get; private set; }
    public DateTimeOffset? RevogadaEm { get; private set; }
    public string? MotivoRevogacao { get; private set; }
    public string? Ip { get; private set; }
    public string? UserAgent { get; private set; }
    public string? Dispositivo { get; private set; }

    public bool Ativa(DateTimeOffset agora) => RevogadaEm is null && ExpiraEm > agora;

    public void Rotacionar(string novoHash, DateTimeOffset agora, DateTimeOffset novaExpiracao)
    {
        RefreshTokenHashAnterior = RefreshTokenHash;
        RefreshTokenHash = novoHash;
        UltimoUsoEm = agora;
        ExpiraEm = novaExpiracao;
    }

    public void TrocarClube(Guid? clubeId) => ClubeId = clubeId;

    public void Revogar(DateTimeOffset agora, string motivo)
    {
        if (RevogadaEm is not null) return;
        RevogadaEm = agora;
        MotivoRevogacao = motivo;
    }
}

public class TokenRecuperacaoSenha : Entity, INaoAuditavel
{
    private TokenRecuperacaoSenha() { }

    public TokenRecuperacaoSenha(Guid usuarioId, string tokenHash, string carimboSeguranca, DateTimeOffset agora,
        DateTimeOffset expiraEm, string? ip)
    {
        UsuarioId = usuarioId;
        TokenHash = tokenHash;
        CarimboSeguranca = carimboSeguranca;
        CriadoEm = agora;
        ExpiraEm = expiraEm;
        Ip = ip;
    }

    public Guid UsuarioId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    /// <summary>Carimbo do usuário na emissão; se as credenciais mudarem, o token deixa de valer.</summary>
    public string CarimboSeguranca { get; private set; } = null!;
    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset ExpiraEm { get; private set; }
    public DateTimeOffset? UsadoEm { get; private set; }
    public string? Ip { get; private set; }

    public bool Valido(DateTimeOffset agora, string carimboAtual) =>
        UsadoEm is null && ExpiraEm > agora && CarimboSeguranca == carimboAtual;

    public void MarcarUsado(DateTimeOffset agora) => UsadoEm = agora;
}

public enum EventoAcesso
{
    LoginSucesso = 1,
    LoginFalha = 2,
    Logout = 3,
    TokenRenovado = 4,
    ReusoRefreshToken = 5,
    BloqueioTemporario = 6,
    SenhaAlterada = 7,
    RecuperacaoSolicitada = 8,
    RecuperacaoConcluida = 9,
    TrocaClube = 10,
    SessaoRevogada = 11,
    UsuarioBloqueado = 12,
    UsuarioDesbloqueado = 13,
    InicializacaoSistema = 14,
}

/// <summary>Histórico de eventos de autenticação (imutável).</summary>
public class HistoricoAcesso : Entity, INaoAuditavel
{
    private HistoricoAcesso() { }

    public HistoricoAcesso(EventoAcesso evento, bool sucesso, DateTimeOffset ocorridoEm, Guid? usuarioId,
        string? emailInformado, Guid? clubeId, Guid? sessaoId, string? ip, string? userAgent, string? detalhe)
    {
        Evento = evento;
        Sucesso = sucesso;
        OcorridoEm = ocorridoEm;
        UsuarioId = usuarioId;
        EmailInformado = emailInformado;
        ClubeId = clubeId;
        SessaoId = sessaoId;
        Ip = ip;
        UserAgent = userAgent;
        Detalhe = detalhe;
    }

    public EventoAcesso Evento { get; private set; }
    public bool Sucesso { get; private set; }
    public DateTimeOffset OcorridoEm { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public string? EmailInformado { get; private set; }
    public Guid? ClubeId { get; private set; }
    public Guid? SessaoId { get; private set; }
    public string? Ip { get; private set; }
    public string? UserAgent { get; private set; }
    public string? Detalhe { get; private set; }
}
