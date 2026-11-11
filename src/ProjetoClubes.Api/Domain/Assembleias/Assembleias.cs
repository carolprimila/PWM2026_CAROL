using ProjetoClubes.Domain.Common;

namespace ProjetoClubes.Domain.Assembleias;

public enum TipoAssembleia
{
    Ordinaria = 1,
    Extraordinaria = 2,
}

public enum SituacaoAssembleia
{
    /// <summary>Em preparação (pautas sendo montadas); ainda não visível aos associados.</summary>
    Rascunho = 1,
    /// <summary>Convocação publicada.</summary>
    Convocada = 2,
    /// <summary>Instalada: presenças e votações acontecem.</summary>
    EmAndamento = 3,
    Encerrada = 4,
    Cancelada = 5,
}

public enum SituacaoVotacao
{
    NaoIniciada = 1,
    Aberta = 2,
    Encerrada = 3,
}

/// <summary>
/// Assembleia com convocação, pautas, votações e ata. Presenças e votos são registros próprios (muitas linhas,
/// unicidade garantida no banco); a assembleia controla o ciclo de vida.
/// </summary>
public class Assembleia : AuditableEntity, ITenantScoped
{
    private readonly List<PautaAssembleia> _pautas = [];

    private Assembleia() { }

    public Assembleia(Guid clubeId, string titulo, TipoAssembleia tipo, DateTimeOffset dataHora, string? local, string? convocacao,
        bool exigeAdimplencia)
    {
        ClubeId = clubeId;
        Situacao = SituacaoAssembleia.Rascunho;
        Alterar(titulo, tipo, dataHora, local, convocacao, exigeAdimplencia);
    }

    public Guid ClubeId { get; private set; }
    public string Titulo { get; private set; } = null!;
    public TipoAssembleia Tipo { get; private set; }
    public DateTimeOffset DataHora { get; private set; }
    public string? Local { get; private set; }
    /// <summary>Texto do edital de convocação.</summary>
    public string? Convocacao { get; private set; }
    /// <summary>Somente associados sem mensalidades vencidas podem votar (regra do estatuto, por assembleia).</summary>
    public bool ExigeAdimplencia { get; private set; }
    public SituacaoAssembleia Situacao { get; private set; }
    public DateTimeOffset? ConvocadaEm { get; private set; }
    public DateTimeOffset? EncerradaEm { get; private set; }
    public string? Ata { get; private set; }
    public DateTimeOffset? AtaRegistradaEm { get; private set; }
    public Guid? AtaRegistradaPor { get; private set; }
    public string? MotivoCancelamento { get; private set; }

    public IReadOnlyCollection<PautaAssembleia> Pautas => _pautas;

    public bool RecebePresencas => Situacao is SituacaoAssembleia.Convocada or SituacaoAssembleia.EmAndamento;

    /// <summary>Associados podem confirmar presença e votar pelo aplicativo, sem passar pela mesa.</summary>
    public bool ParticipacaoPeloApp { get; private set; }

    public void DefinirParticipacaoPeloApp(bool permitir)
    {
        if (permitir == ParticipacaoPeloApp) return;
        if (Situacao is not (SituacaoAssembleia.Rascunho or SituacaoAssembleia.Convocada or SituacaoAssembleia.EmAndamento))
            throw new DomainException("assembleia.situacao", "A assembleia não pode mais ser alterada.");
        ParticipacaoPeloApp = permitir;
    }

    public void Alterar(string titulo, TipoAssembleia tipo, DateTimeOffset dataHora, string? local, string? convocacao, bool exigeAdimplencia)
    {
        if (Situacao is not (SituacaoAssembleia.Rascunho or SituacaoAssembleia.Convocada))
            throw new DomainException("assembleia.situacao", "A assembleia não pode mais ser alterada.");
        if (string.IsNullOrWhiteSpace(titulo)) throw new DomainException("assembleia.titulo", "Informe o título.");
        // Mudar a regra de voto depois de convocada mudaria quem pode votar sem aviso.
        if (Situacao == SituacaoAssembleia.Convocada && exigeAdimplencia != ExigeAdimplencia)
            throw new DomainException("assembleia.regra_voto", "A regra de adimplência não muda depois da convocação.");
        Titulo = titulo.Trim();
        Tipo = tipo;
        DataHora = dataHora.ToUniversalTime();
        Local = string.IsNullOrWhiteSpace(local) ? null : local.Trim();
        Convocacao = string.IsNullOrWhiteSpace(convocacao) ? null : convocacao.Trim();
        ExigeAdimplencia = exigeAdimplencia;
    }

    public PautaAssembleia AdicionarPauta(string titulo, string? descricao, bool comVotacao, IReadOnlyList<string>? opcoes)
    {
        if (Situacao is not (SituacaoAssembleia.Rascunho or SituacaoAssembleia.Convocada))
            throw new DomainException("assembleia.situacao", "Pautas só podem ser incluídas antes da instalação.");
        var p = new PautaAssembleia(ClubeId, Id, _pautas.Count + 1, titulo, descricao, comVotacao, opcoes);
        _pautas.Add(p);
        return p;
    }

    public void RemoverPauta(Guid pautaId)
    {
        if (Situacao is not (SituacaoAssembleia.Rascunho or SituacaoAssembleia.Convocada))
            throw new DomainException("assembleia.situacao", "Pautas só podem ser removidas antes da instalação.");
        var p = Pauta(pautaId);
        _pautas.Remove(p);
        var ordem = 1;
        foreach (var x in _pautas.OrderBy(x => x.Ordem)) x.Ordem = ordem++;
    }

    public void Convocar(DateTimeOffset agora)
    {
        if (Situacao != SituacaoAssembleia.Rascunho) throw new DomainException("assembleia.situacao", "A assembleia já foi convocada.");
        if (_pautas.Count == 0) throw new DomainException("assembleia.sem_pauta", "Inclua ao menos uma pauta antes de convocar.");
        Situacao = SituacaoAssembleia.Convocada;
        ConvocadaEm = agora;
    }

    public void Instalar()
    {
        if (Situacao != SituacaoAssembleia.Convocada) throw new DomainException("assembleia.situacao", "Só uma assembleia convocada pode ser instalada.");
        Situacao = SituacaoAssembleia.EmAndamento;
    }

    public PautaAssembleia AbrirVotacao(Guid pautaId)
    {
        if (Situacao != SituacaoAssembleia.EmAndamento) throw new DomainException("assembleia.situacao", "A assembleia não está em andamento.");
        var p = Pauta(pautaId);
        p.AbrirVotacao();
        return p;
    }

    public PautaAssembleia EncerrarVotacao(Guid pautaId)
    {
        var p = Pauta(pautaId);
        p.EncerrarVotacao();
        return p;
    }

    public PautaAssembleia RegistrarDecisao(Guid pautaId, string decisao)
    {
        if (Situacao != SituacaoAssembleia.EmAndamento) throw new DomainException("assembleia.situacao", "A assembleia não está em andamento.");
        var p = Pauta(pautaId);
        p.RegistrarDecisao(decisao);
        return p;
    }

    public void Encerrar(DateTimeOffset agora)
    {
        if (Situacao != SituacaoAssembleia.EmAndamento) throw new DomainException("assembleia.situacao", "A assembleia não está em andamento.");
        if (_pautas.Any(p => p.Votacao == SituacaoVotacao.Aberta))
            throw new DomainException("assembleia.votacao_aberta", "Encerre as votações abertas antes de encerrar a assembleia.");
        Situacao = SituacaoAssembleia.Encerrada;
        EncerradaEm = agora;
    }

    /// <summary>Registra a ata (uma única vez, depois do encerramento). Depois de registrada, não muda.</summary>
    public void RegistrarAta(string texto, Guid? usuarioId, DateTimeOffset agora)
    {
        if (Situacao != SituacaoAssembleia.Encerrada) throw new DomainException("assembleia.situacao", "A ata é registrada depois do encerramento.");
        if (AtaRegistradaEm is not null) throw new DomainException("assembleia.ata_registrada", "A ata já foi registrada e não pode ser alterada.");
        if (string.IsNullOrWhiteSpace(texto)) throw new DomainException("assembleia.ata", "Escreva o texto da ata.");
        Ata = texto.Trim();
        AtaRegistradaEm = agora;
        AtaRegistradaPor = usuarioId;
    }

    public void Cancelar(string motivo)
    {
        if (Situacao is not (SituacaoAssembleia.Rascunho or SituacaoAssembleia.Convocada))
            throw new DomainException("assembleia.situacao", "Uma assembleia instalada não pode ser cancelada.");
        if (string.IsNullOrWhiteSpace(motivo)) throw new DomainException("assembleia.motivo", "Informe o motivo do cancelamento.");
        Situacao = SituacaoAssembleia.Cancelada;
        MotivoCancelamento = motivo.Trim();
    }

    private PautaAssembleia Pauta(Guid id) =>
        _pautas.FirstOrDefault(p => p.Id == id) ?? throw new DomainException("assembleia.pauta", "Pauta não encontrada.");
}

public class PautaAssembleia : Entity, ITenantScoped
{
    public static readonly IReadOnlyList<string> OpcoesPadrao = ["Sim", "Não", "Abstenção"];

    private PautaAssembleia() { }

    internal PautaAssembleia(Guid clubeId, Guid assembleiaId, int ordem, string titulo, string? descricao, bool comVotacao,
        IReadOnlyList<string>? opcoes)
    {
        if (string.IsNullOrWhiteSpace(titulo)) throw new DomainException("pauta.titulo", "Informe o título da pauta.");
        ClubeId = clubeId;
        AssembleiaId = assembleiaId;
        Ordem = ordem;
        Titulo = titulo.Trim();
        Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim();
        ComVotacao = comVotacao;
        if (comVotacao)
        {
            var lista = (opcoes is { Count: > 0 } ? opcoes : OpcoesPadrao)
                .Where(o => !string.IsNullOrWhiteSpace(o)).Select(o => o.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (lista.Count < 2) throw new DomainException("pauta.opcoes", "A votação precisa de pelo menos duas opções.");
            if (lista.Count > 10) throw new DomainException("pauta.opcoes", "Máximo de 10 opções.");
            Opcoes = lista;
            Votacao = SituacaoVotacao.NaoIniciada;
        }
    }

    public Guid ClubeId { get; private set; }
    public Guid AssembleiaId { get; private set; }
    public int Ordem { get; internal set; }
    public string Titulo { get; private set; } = null!;
    public string? Descricao { get; private set; }
    public bool ComVotacao { get; private set; }
    public List<string> Opcoes { get; private set; } = [];
    public SituacaoVotacao? Votacao { get; private set; }
    public string? Decisao { get; private set; }

    internal void AbrirVotacao()
    {
        if (!ComVotacao) throw new DomainException("pauta.sem_votacao", "Esta pauta não tem votação.");
        if (Votacao != SituacaoVotacao.NaoIniciada) throw new DomainException("pauta.votacao", "A votação desta pauta já foi aberta.");
        Votacao = SituacaoVotacao.Aberta;
    }

    internal void EncerrarVotacao()
    {
        if (Votacao != SituacaoVotacao.Aberta) throw new DomainException("pauta.votacao", "A votação desta pauta não está aberta.");
        Votacao = SituacaoVotacao.Encerrada;
    }

    internal void RegistrarDecisao(string decisao)
    {
        if (string.IsNullOrWhiteSpace(decisao)) throw new DomainException("pauta.decisao", "Descreva a decisão.");
        if (ComVotacao && Votacao != SituacaoVotacao.Encerrada)
            throw new DomainException("pauta.votacao", "A decisão de uma pauta com votação é registrada depois de encerrar a votação.");
        Decisao = decisao.Trim();
    }

    public void ExigirOpcaoValida(int opcao)
    {
        if (Votacao != SituacaoVotacao.Aberta) throw new DomainException("voto.fechado", "A votação desta pauta não está aberta.");
        if (opcao < 0 || opcao >= Opcoes.Count) throw new DomainException("voto.opcao", "Opção de voto inválida.");
    }
}

/// <summary>Presença de um associado titular na assembleia, com a aptidão para votar avaliada no momento do registro.</summary>
public class PresencaAssembleia : Entity, ITenantScoped, INaoAuditavel
{
    private PresencaAssembleia() { }

    public PresencaAssembleia(Guid clubeId, Guid assembleiaId, Guid associadoId, string? motivoInapto, Guid? registradaPor, DateTimeOffset agora,
        bool peloApp = false)
    {
        PeloApp = peloApp;
        ClubeId = clubeId;
        AssembleiaId = assembleiaId;
        AssociadoId = associadoId;
        AptoVotar = motivoInapto is null;
        MotivoInapto = motivoInapto;
        RegistradaPor = registradaPor;
        RegistradaEm = agora;
    }

    public Guid ClubeId { get; private set; }
    public Guid AssembleiaId { get; private set; }
    public Guid AssociadoId { get; private set; }
    public bool AptoVotar { get; private set; }
    public string? MotivoInapto { get; private set; }
    public Guid? RegistradaPor { get; private set; }
    public DateTimeOffset RegistradaEm { get; private set; }
    /// <summary>Presença confirmada pelo próprio associado no aplicativo (participação à distância).</summary>
    public bool PeloApp { get; private set; }
}

/// <summary>
/// Voto em uma pauta. O associado é guardado só para garantir um voto por pessoa (índice único);
/// a API divulga apenas a apuração, nunca quem votou em quê.
/// </summary>
public class VotoAssembleia : Entity, ITenantScoped, INaoAuditavel
{
    private VotoAssembleia() { }

    public VotoAssembleia(Guid clubeId, Guid pautaId, Guid associadoId, int opcao, DateTimeOffset agora)
    {
        ClubeId = clubeId;
        PautaId = pautaId;
        AssociadoId = associadoId;
        Opcao = opcao;
        VotadoEm = agora;
    }

    public Guid ClubeId { get; private set; }
    public Guid PautaId { get; private set; }
    public Guid AssociadoId { get; private set; }
    public int Opcao { get; private set; }
    public DateTimeOffset VotadoEm { get; private set; }
}

/// <summary>Quem pode votar: regras do estatuto aplicadas a um associado titular.</summary>
public static class RegrasVoto
{
    /// <summary>Retorna o motivo da inaptidão, ou nulo se o associado pode votar.</summary>
    public static string? MotivoInapto(bool associadoAtivo, bool categoriaComDireitoVoto, int mensalidadesVencidas, bool exigeAdimplencia)
    {
        if (!associadoAtivo) return "Cadastro não está ativo.";
        if (!categoriaComDireitoVoto) return "A categoria do associado não tem direito a voto.";
        if (exigeAdimplencia && mensalidadesVencidas > 0) return "Possui mensalidades vencidas.";
        return null;
    }
}
