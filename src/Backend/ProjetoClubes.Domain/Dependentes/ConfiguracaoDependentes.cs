using ProjetoClubes.Domain.Associados;
using ProjetoClubes.Domain.Common;
using ProjetoClubes.Domain.Pessoas;

namespace ProjetoClubes.Domain.Dependentes;

/// <summary>Regras de dependentes de cada clube (limites, idades, parentescos, cobrança e permissões padrão).</summary>
public class ConfiguracaoDependentes : AuditableEntity, ITenantScoped
{
    private ConfiguracaoDependentes() { }

    public ConfiguracaoDependentes(Guid clubeId) => ClubeId = clubeId;

    public Guid ClubeId { get; private set; }
    public int LimitePadraoDependentes { get; set; } = 5;
    public int MaximoConjuges { get; set; } = 1;
    /// <summary>Idade máxima para inclusão/permanência como filho(a). Nulo = sem limite.</summary>
    public int? IdadeMaximaFilhos { get; set; } = 24;
    public int? IdadeMaximaIrmaos { get; set; } = 18;
    public int IdadeMaioridade { get; set; } = 18;
    /// <summary>Idade mínima para o dependente ter login próprio no aplicativo.</summary>
    public int IdadeMinimaLoginProprio { get; set; } = 12;
    public List<Parentesco> ParentescosPermitidos { get; set; } = [.. Enum.GetValues<Parentesco>()];
    public bool ExigeDocumentoComprobatorio { get; set; }
    public bool MenoresPodemSolicitarReservas { get; set; }
    // Regras financeiras (usadas pelo módulo financeiro).
    public bool IncluidosNaMensalidadeDoTitular { get; set; } = true;
    public decimal ValorAdicionalPorDependente { get; set; }
    public PermissoesDependente PermissoesPadrao { get; set; } = new();
}

/// <summary>Regras de inclusão de dependentes — puras, sem acesso a dados, para teste unitário.</summary>
public static class RegrasDependentes
{
    public static void ValidarInclusao(ConfiguracaoDependentes cfg, CategoriaAssociado categoria, Parentesco parentesco,
        DateOnly? nascimento, DateOnly hoje, int dependentesVigentes, int conjugesVigentes)
    {
        if (!categoria.PermiteDependentes)
            throw new DomainException("dependente.categoria_sem_dependentes",
                $"A categoria \"{categoria.Nome}\" não permite dependentes.");

        if (!cfg.ParentescosPermitidos.Contains(parentesco))
            throw new DomainException("dependente.parentesco_nao_permitido",
                $"O clube não permite dependentes com parentesco \"{parentesco}\".");

        var limite = categoria.LimiteDependentes ?? cfg.LimitePadraoDependentes;
        if (dependentesVigentes >= limite)
            throw new DomainException("dependente.limite_atingido",
                $"O titular já atingiu o limite de {limite} dependente(s).");

        if (parentesco == Parentesco.Conjuge && conjugesVigentes >= cfg.MaximoConjuges)
            throw new DomainException("dependente.limite_conjuge", "O titular já possui cônjuge cadastrado.");

        ValidarIdade(cfg, parentesco, nascimento, hoje);
    }

    public static void ValidarIdade(ConfiguracaoDependentes cfg, Parentesco parentesco, DateOnly? nascimento, DateOnly hoje)
    {
        if (nascimento is not { } n) return;
        if (n > hoje) throw new DomainException("pessoa.nascimento_futuro", "A data de nascimento não pode ser futura.");

        var idade = Pessoa.CalcularIdade(n, hoje);
        var maxima = parentesco switch
        {
            Parentesco.Filho => cfg.IdadeMaximaFilhos,
            Parentesco.Irmao => cfg.IdadeMaximaIrmaos,
            _ => null,
        };
        if (maxima is { } m && idade > m)
            throw new DomainException("dependente.idade_excedida",
                $"Pelas regras do clube, {(parentesco == Parentesco.Filho ? "filhos" : "irmãos")} podem ser dependentes até {m} anos.");
    }

    /// <summary>Permissões iniciais a partir do padrão do clube, com restrições para menores de idade.</summary>
    public static PermissoesDependente PermissoesIniciais(ConfiguracaoDependentes cfg, DateOnly? nascimento, DateOnly hoje)
    {
        var p = cfg.PermissoesPadrao.Copiar();
        var menor = nascimento is { } n && Pessoa.CalcularIdade(n, hoje) < cfg.IdadeMaioridade;
        if (menor && !cfg.MenoresPodemSolicitarReservas) p.SolicitacaoReservas = false;
        return p;
    }

    public static bool PodeTerLoginProprio(ConfiguracaoDependentes cfg, DateOnly? nascimento, DateOnly hoje) =>
        nascimento is { } n && Pessoa.CalcularIdade(n, hoje) >= cfg.IdadeMinimaLoginProprio;
}
