using ProjetoClubes.Domain.Common;
using ProjetoClubes.Domain.Dependentes;
using ProjetoClubes.Domain.Pessoas;

namespace ProjetoClubes.Domain.Associados;

/// <summary>
/// Categoria de associado configurável por clube. Direitos (voto, dependentes) vêm das configurações,
/// nunca do nome da categoria.
/// </summary>
public class CategoriaAssociado : AuditableEntity, ITenantScoped, ISoftDelete
{
    private CategoriaAssociado() { }

    public CategoriaAssociado(Guid clubeId, string nome, string? chaveModelo = null)
    {
        ClubeId = clubeId;
        Nome = nome.Trim();
        ChaveModelo = chaveModelo;
        Ativa = true;
    }

    public Guid ClubeId { get; private set; }
    public string Nome { get; set; } = null!;
    public string? Descricao { get; set; }
    public bool DireitoVoto { get; set; }
    public bool PodeSerVotado { get; set; }
    public bool PermiteDependentes { get; set; } = true;
    /// <summary>Limite próprio da categoria; nulo usa o limite padrão das configurações de dependentes.</summary>
    public int? LimiteDependentes { get; set; }
    public int Ordem { get; set; }
    public bool Ativa { get; set; }
    public string? ChaveModelo { get; private set; }

    public bool Excluido { get; private set; }
    public DateTimeOffset? ExcluidoEm { get; private set; }
    public Guid? ExcluidoPor { get; private set; }

    public void MarcarExcluido(DateTimeOffset quando, Guid? por)
    {
        Excluido = true;
        ExcluidoEm = quando;
        ExcluidoPor = por;
    }
}

/// <summary>Interesse do associado (pesca, esportes…), configurável por clube.</summary>
public class Interesse : AuditableEntity, ITenantScoped, ISoftDelete
{
    private Interesse() { }

    public Interesse(Guid clubeId, string nome)
    {
        ClubeId = clubeId;
        Nome = nome.Trim();
    }

    public Guid ClubeId { get; private set; }
    public string Nome { get; set; } = null!;

    public bool Excluido { get; private set; }
    public DateTimeOffset? ExcluidoEm { get; private set; }
    public Guid? ExcluidoPor { get; private set; }

    public void MarcarExcluido(DateTimeOffset quando, Guid? por)
    {
        Excluido = true;
        ExcluidoEm = quando;
        ExcluidoPor = por;
    }
}

public class Associado : RegistroCadastral
{
    private readonly List<AssociadoInteresse> _interesses = [];
    private readonly List<Dependente> _dependentes = [];

    private Associado() { }

    public Associado(Guid clubeId, Pessoa pessoa, string matricula, Guid categoriaId, DateOnly dataAssociacao,
        bool exigeAprovacao, DateTimeOffset agora)
    {
        ClubeId = clubeId;
        PessoaId = pessoa.Id;
        Pessoa = pessoa;
        Matricula = matricula;
        CategoriaId = categoriaId;
        DataAssociacao = dataAssociacao;
        DefinirSituacaoInicial(exigeAprovacao, agora);
    }

    public Guid PessoaId { get; private set; }
    public Pessoa Pessoa { get; private set; } = null!;
    public string Matricula { get; private set; } = null!;
    public Guid CategoriaId { get; private set; }
    public CategoriaAssociado Categoria { get; private set; } = null!;
    public DateOnly DataAssociacao { get; set; }

    public IReadOnlyCollection<AssociadoInteresse> Interesses => _interesses;
    public IReadOnlyCollection<Dependente> Dependentes => _dependentes;

    public void AlterarCategoria(Guid categoriaId) => CategoriaId = categoriaId;

    public void DefinirInteresses(IEnumerable<Guid> interesseIds)
    {
        var novos = interesseIds.ToHashSet();
        _interesses.RemoveAll(i => !novos.Contains(i.InteresseId));
        foreach (var id in novos.Where(id => _interesses.All(i => i.InteresseId != id)))
            _interesses.Add(new AssociadoInteresse(Id, id));
    }
}

public class AssociadoInteresse
{
    private AssociadoInteresse() { }

    public AssociadoInteresse(Guid associadoId, Guid interesseId)
    {
        AssociadoId = associadoId;
        InteresseId = interesseId;
    }

    public Guid AssociadoId { get; private set; }
    public Guid InteresseId { get; private set; }
    public Interesse Interesse { get; private set; } = null!;
}

public class HistoricoAssociado : RegistroHistorico
{
    private HistoricoAssociado() { }

    public HistoricoAssociado(Guid clubeId, Guid associadoId, string evento, string descricao, Guid? usuarioId,
        DateTimeOffset agora)
    {
        ClubeId = clubeId;
        AssociadoId = associadoId;
        Evento = evento;
        Descricao = descricao;
        UsuarioId = usuarioId;
        OcorridoEm = agora;
    }

    public Guid AssociadoId { get; private set; }
}
