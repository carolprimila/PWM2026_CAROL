using ProjetoClubes.Domain.Associados;
using ProjetoClubes.Domain.Common;
using ProjetoClubes.Domain.Pessoas;

namespace ProjetoClubes.Domain.Dependentes;

public enum Parentesco
{
    Conjuge = 1,
    Filho = 2,
    Pai = 3,
    Mae = 4,
    Irmao = 5,
    Outro = 6,
}

/// <summary>Permissões individuais do dependente. Não incluem direitos políticos/administrativos do titular.</summary>
public sealed class PermissoesDependente
{
    public bool AcessoClube { get; set; } = true;
    public bool ParticipacaoEventos { get; set; } = true;
    public bool UsoEspacosEsportivos { get; set; } = true;
    public bool InscricaoAtividades { get; set; } = true;
    public bool SolicitacaoReservas { get; set; } = true;
    public bool RecebimentoComunicados { get; set; } = true;
    public bool UtilizacaoServicos { get; set; } = true;

    public PermissoesDependente Copiar() => (PermissoesDependente)MemberwiseClone();
}

/// <summary>
/// Dependente de um associado titular. Tem identificação, situação, permissões, documentos e histórico próprios.
/// Uma pessoa só pode ser dependente vigente de um titular por clube, salvo exceção autorizada e registrada.
/// </summary>
public class Dependente : RegistroCadastral
{
    private readonly List<VinculoDependente> _vinculos = [];

    private Dependente() { }

    public Dependente(Guid clubeId, Pessoa pessoa, Associado titular, Parentesco parentesco, string? parentescoDescricao,
        string numeroIdentificacao, PermissoesDependente permissoes, bool exigeAprovacao, Guid? usuarioId, DateTimeOffset agora)
    {
        ClubeId = clubeId;
        PessoaId = pessoa.Id;
        Pessoa = pessoa;
        AssociadoTitularId = titular.Id;
        DefinirParentesco(parentesco, parentescoDescricao);
        NumeroIdentificacao = numeroIdentificacao;
        Permissoes = permissoes;
        DataInclusao = DateOnly.FromDateTime(agora.UtcDateTime);
        DefinirSituacaoInicial(exigeAprovacao, agora);
        _vinculos.Add(new VinculoDependente(clubeId, Id, titular.Id, agora, "Inclusão", usuarioId));
    }

    public Guid PessoaId { get; private set; }
    public Pessoa Pessoa { get; private set; } = null!;
    public Guid AssociadoTitularId { get; private set; }
    public Associado AssociadoTitular { get; private set; } = null!;
    public Parentesco Parentesco { get; private set; }
    public string? ParentescoDescricao { get; private set; }
    /// <summary>Número da carteirinha: matrícula do titular + sequencial (ex.: FOZ000123-02).</summary>
    public string NumeroIdentificacao { get; private set; } = null!;
    public DateOnly DataInclusao { get; private set; }
    public PermissoesDependente Permissoes { get; private set; } = new();

    /// <summary>Exceção autorizada para a mesma pessoa ser dependente de mais de um titular no clube.</summary>
    public bool ExcecaoMultiplosTitulares { get; private set; }
    public string? JustificativaExcecao { get; private set; }
    public Guid? ExcecaoAutorizadaPor { get; private set; }

    public IReadOnlyCollection<VinculoDependente> Vinculos => _vinculos;

    public void DefinirParentesco(Parentesco parentesco, string? descricao)
    {
        if (parentesco == Parentesco.Outro && string.IsNullOrWhiteSpace(descricao))
            throw new DomainException("dependente.parentesco_descricao", "Descreva o grau de parentesco.");
        Parentesco = parentesco;
        ParentescoDescricao = parentesco == Parentesco.Outro ? descricao!.Trim() : null;
    }

    public void AutorizarExcecaoMultiplosTitulares(string justificativa, Guid autorizadoPor)
    {
        if (string.IsNullOrWhiteSpace(justificativa))
            throw new DomainException("dependente.excecao_justificativa", "Informe a justificativa da exceção.");
        ExcecaoMultiplosTitulares = true;
        JustificativaExcecao = justificativa.Trim();
        ExcecaoAutorizadaPor = autorizadoPor;
    }

    public void AtualizarPermissoes(PermissoesDependente permissoes) => Permissoes = permissoes.Copiar();

    /// <summary>Transfere para outro titular, encerrando o vínculo atual e abrindo um novo.</summary>
    public void TransferirTitular(Associado novoTitular, string novoNumero, string motivo, Guid? usuarioId, DateTimeOffset agora)
    {
        if (novoTitular.Id == AssociadoTitularId)
            throw new DomainException("dependente.mesmo_titular", "O dependente já está vinculado a este titular.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new DomainException("situacao.motivo_obrigatorio", "Informe o motivo da transferência.");

        foreach (var v in _vinculos.Where(v => v.Fim is null)) v.Encerrar(agora);
        _vinculos.Add(new VinculoDependente(ClubeId, Id, novoTitular.Id, agora, motivo.Trim(), usuarioId));
        AssociadoTitularId = novoTitular.Id;
        AssociadoTitular = novoTitular;
        NumeroIdentificacao = novoNumero;
    }

    public override void MarcarExcluido(DateTimeOffset quando, Guid? por)
    {
        base.MarcarExcluido(quando, por);
        foreach (var v in _vinculos.Where(v => v.Fim is null)) v.Encerrar(quando);
    }
}

/// <summary>Período em que o dependente esteve vinculado a cada titular (histórico de titularidade).</summary>
public class VinculoDependente : Entity, ITenantScoped
{
    private VinculoDependente() { }

    internal VinculoDependente(Guid clubeId, Guid dependenteId, Guid titularId, DateTimeOffset inicio, string motivo, Guid? registradoPor)
    {
        ClubeId = clubeId;
        DependenteId = dependenteId;
        AssociadoTitularId = titularId;
        Inicio = inicio;
        Motivo = motivo;
        RegistradoPor = registradoPor;
    }

    public Guid ClubeId { get; private set; }
    public Guid DependenteId { get; private set; }
    public Guid AssociadoTitularId { get; private set; }
    public DateTimeOffset Inicio { get; private set; }
    public DateTimeOffset? Fim { get; private set; }
    public string Motivo { get; private set; } = null!;
    public Guid? RegistradoPor { get; private set; }

    internal void Encerrar(DateTimeOffset quando) => Fim ??= quando;
}

public class HistoricoDependente : RegistroHistorico
{
    private HistoricoDependente() { }

    public HistoricoDependente(Guid clubeId, Guid dependenteId, string evento, string descricao, Guid? usuarioId,
        DateTimeOffset agora)
    {
        ClubeId = clubeId;
        DependenteId = dependenteId;
        Evento = evento;
        Descricao = descricao;
        UsuarioId = usuarioId;
        OcorridoEm = agora;
    }

    public Guid DependenteId { get; private set; }
}
