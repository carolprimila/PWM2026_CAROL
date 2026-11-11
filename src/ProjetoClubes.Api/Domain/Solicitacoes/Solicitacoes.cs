using ProjetoClubes.Domain.Common;

namespace ProjetoClubes.Domain.Solicitacoes;

public enum TipoSolicitacao
{
    Solicitacao = 1,
    Reclamacao = 2,
    Sugestao = 3,
    ProblemaEstrutural = 4,
    Documento = 5,
    AlteracaoCadastral = 6,
}

public enum SituacaoSolicitacao
{
    Aberta = 1,
    EmAndamento = 2,
    Concluida = 3,
    Cancelada = 4,
}

/// <summary>Solicitação, reclamação ou sugestão com protocolo e histórico de respostas.</summary>
public class Solicitacao : AuditableEntity, ITenantScoped
{
    private readonly List<RespostaSolicitacao> _respostas = [];

    private Solicitacao() { }

    public Solicitacao(Guid clubeId, string protocolo, TipoSolicitacao tipo, string assunto, string descricao, Guid? pessoaId, DateOnly? prazo)
    {
        if (string.IsNullOrWhiteSpace(assunto) || string.IsNullOrWhiteSpace(descricao))
            throw new DomainException("solicitacao.campos", "Informe o assunto e a descrição.");
        ClubeId = clubeId;
        Protocolo = protocolo;
        Tipo = tipo;
        Assunto = assunto.Trim();
        Descricao = descricao.Trim();
        PessoaId = pessoaId;
        Prazo = prazo;
        Situacao = SituacaoSolicitacao.Aberta;
    }

    public Guid ClubeId { get; private set; }
    public string Protocolo { get; private set; } = null!;
    public TipoSolicitacao Tipo { get; private set; }
    public string Assunto { get; private set; } = null!;
    public string Descricao { get; private set; } = null!;
    /// <summary>Associado/dependente que abriu (nulo quando registrada pela administração sem autor).</summary>
    public Guid? PessoaId { get; private set; }
    public Guid? ResponsavelId { get; private set; }
    public DateOnly? Prazo { get; set; }
    public SituacaoSolicitacao Situacao { get; private set; }

    public IReadOnlyCollection<RespostaSolicitacao> Respostas => _respostas;

    public bool Encerrada => Situacao is SituacaoSolicitacao.Concluida or SituacaoSolicitacao.Cancelada;

    public RespostaSolicitacao Responder(string texto, Guid? autorUsuarioId, bool daAdministracao, bool interna, DateTimeOffset agora)
    {
        if (Encerrada) throw new DomainException("solicitacao.encerrada", "Esta solicitação já foi encerrada.");
        if (string.IsNullOrWhiteSpace(texto)) throw new DomainException("solicitacao.texto", "Escreva a mensagem.");
        var r = new RespostaSolicitacao(ClubeId, Id, texto.Trim(), autorUsuarioId, daAdministracao, interna, agora);
        _respostas.Add(r);
        if (daAdministracao && !interna && Situacao == SituacaoSolicitacao.Aberta) Situacao = SituacaoSolicitacao.EmAndamento;
        return r;
    }

    public void AlterarSituacao(SituacaoSolicitacao nova, Guid? usuarioId, DateTimeOffset agora)
    {
        if (Encerrada) throw new DomainException("solicitacao.encerrada", "Esta solicitação já foi encerrada.");
        if (nova == Situacao) return;
        Situacao = nova;
        _respostas.Add(new RespostaSolicitacao(ClubeId, Id, $"Situação alterada para {Rotulo(nova)}.", usuarioId, true, false, agora));
    }

    public void AtribuirResponsavel(Guid? usuarioId) => ResponsavelId = usuarioId;

    public static string Rotulo(SituacaoSolicitacao s) => s switch
    {
        SituacaoSolicitacao.Aberta => "Aberta",
        SituacaoSolicitacao.EmAndamento => "Em andamento",
        SituacaoSolicitacao.Concluida => "Concluída",
        _ => "Cancelada",
    };
}

public class RespostaSolicitacao : Entity, ITenantScoped
{
    private RespostaSolicitacao() { }

    internal RespostaSolicitacao(Guid clubeId, Guid solicitacaoId, string texto, Guid? autorUsuarioId, bool daAdministracao,
        bool interna, DateTimeOffset agora)
    {
        ClubeId = clubeId;
        SolicitacaoId = solicitacaoId;
        Texto = texto;
        AutorUsuarioId = autorUsuarioId;
        DaAdministracao = daAdministracao;
        Interna = interna;
        CriadaEm = agora;
    }

    public Guid ClubeId { get; private set; }
    public Guid SolicitacaoId { get; private set; }
    public string Texto { get; private set; } = null!;
    public Guid? AutorUsuarioId { get; private set; }
    public bool DaAdministracao { get; private set; }
    /// <summary>Anotação interna da administração (não aparece para o associado).</summary>
    public bool Interna { get; private set; }
    public DateTimeOffset CriadaEm { get; private set; }
}
