using ProjetoClubes.Domain.Common;

namespace ProjetoClubes.Domain.Comunicacao;

public enum PublicoComunicado
{
    /// <summary>Associados e dependentes com acesso ao app.</summary>
    Todos = 1,
    /// <summary>Somente associados titulares.</summary>
    Titulares = 2,
}

/// <summary>Aviso do mural/comunicado oficial do clube. Rascunho até ser publicado; pode exigir confirmação de leitura.</summary>
public class Comunicado : AuditableEntity, ITenantScoped, ISoftDelete
{
    private Comunicado() { }

    public Comunicado(Guid clubeId, string titulo, string texto, PublicoComunicado publico, bool exigeConfirmacao, bool fixado,
        DateTimeOffset? expiraEm)
    {
        ClubeId = clubeId;
        Alterar(titulo, texto, publico, exigeConfirmacao, fixado, expiraEm);
    }

    public Guid ClubeId { get; private set; }
    public string Titulo { get; private set; } = null!;
    public string Texto { get; private set; } = null!;
    public PublicoComunicado Publico { get; private set; }
    public bool ExigeConfirmacao { get; private set; }
    public bool Fixado { get; private set; }
    public DateTimeOffset? PublicadoEm { get; private set; }
    public DateTimeOffset? ExpiraEm { get; private set; }
    public bool Excluido { get; private set; }
    public DateTimeOffset? ExcluidoEm { get; private set; }
    public Guid? ExcluidoPor { get; private set; }

    public bool Publicado => PublicadoEm is not null;

    public bool VisivelEm(DateTimeOffset agora) => Publicado && (ExpiraEm is null || ExpiraEm > agora);

    public void Alterar(string titulo, string texto, PublicoComunicado publico, bool exigeConfirmacao, bool fixado, DateTimeOffset? expiraEm)
    {
        if (string.IsNullOrWhiteSpace(titulo) || string.IsNullOrWhiteSpace(texto))
            throw new DomainException("comunicado.campos", "Informe o título e o texto do comunicado.");
        // Depois de publicado, quem já confirmou a leitura leu este público e esta regra: não se troca.
        if (Publicado && (publico != Publico || exigeConfirmacao != ExigeConfirmacao))
            throw new DomainException("comunicado.publicado", "O público e a confirmação de leitura não mudam depois da publicação.");
        Titulo = titulo.Trim();
        Texto = texto.Trim();
        Publico = publico;
        ExigeConfirmacao = exigeConfirmacao;
        Fixado = fixado;
        ExpiraEm = expiraEm?.ToUniversalTime();
    }

    public void Publicar(DateTimeOffset agora)
    {
        if (Publicado) throw new DomainException("comunicado.publicado", "Este comunicado já foi publicado.");
        if (ExpiraEm is { } e && e <= agora) throw new DomainException("comunicado.expirado", "A data de expiração já passou.");
        PublicadoEm = agora;
    }

    public void MarcarExcluido(DateTimeOffset quando, Guid? por)
    {
        Excluido = true;
        ExcluidoEm = quando;
        ExcluidoPor = por;
    }
}

/// <summary>Leitura de um comunicado por uma pessoa (uma por pessoa). Confirmação explícita quando o comunicado exige.</summary>
public class LeituraComunicado : Entity, ITenantScoped, INaoAuditavel
{
    private LeituraComunicado() { }

    public LeituraComunicado(Guid clubeId, Guid comunicadoId, Guid pessoaId, DateTimeOffset agora)
    {
        ClubeId = clubeId;
        ComunicadoId = comunicadoId;
        PessoaId = pessoaId;
        LidoEm = agora;
    }

    public Guid ClubeId { get; private set; }
    public Guid ComunicadoId { get; private set; }
    public Guid PessoaId { get; private set; }
    public DateTimeOffset LidoEm { get; private set; }
    public DateTimeOffset? ConfirmadoEm { get; private set; }

    public void Confirmar(DateTimeOffset agora) => ConfirmadoEm ??= agora;
}

public enum TipoNotificacao
{
    Comunicado = 1,
    Reserva = 2,
    Solicitacao = 3,
    Dependente = 4,
    Assembleia = 5,
    Votacao = 6,
    Geral = 7,
}

/// <summary>
/// Notificação para um usuário no clube (caixa de entrada do app). É gravada sempre; o envio push (FCM/APNs) é uma
/// etapa adicional e opcional da infraestrutura.
/// </summary>
public class Notificacao : Entity, ITenantScoped, INaoAuditavel
{
    private Notificacao() { }

    public Notificacao(Guid clubeId, Guid usuarioId, TipoNotificacao tipo, string titulo, string texto, Guid? referenciaId, DateTimeOffset agora)
    {
        ClubeId = clubeId;
        UsuarioId = usuarioId;
        Tipo = tipo;
        Titulo = titulo.Length > 150 ? titulo[..150] : titulo;
        Texto = texto.Length > 1000 ? texto[..1000] : texto;
        ReferenciaId = referenciaId;
        CriadaEm = agora;
    }

    public Guid ClubeId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public TipoNotificacao Tipo { get; private set; }
    public string Titulo { get; private set; } = null!;
    public string Texto { get; private set; } = null!;
    public Guid? ReferenciaId { get; private set; }
    public DateTimeOffset CriadaEm { get; private set; }
    public DateTimeOffset? LidaEm { get; private set; }

    public void MarcarLida(DateTimeOffset agora) => LidaEm ??= agora;
}
