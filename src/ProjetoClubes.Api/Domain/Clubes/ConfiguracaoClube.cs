using ProjetoClubes.Domain.Common;

namespace ProjetoClubes.Domain.Clubes;

/// <summary>
/// Configurações gerais e de identidade visual do clube (1:1 com Clube, parte do agregado Clube —
/// o acesso é controlado pelo serviço de clubes, por isso não usa o filtro de tenant).
/// Regras específicas de cada módulo (dependentes, financeiro, reservas) ficam em tabelas
/// próprias de configuração criadas nas respectivas etapas — nada fixo em código.
/// </summary>
public class ConfiguracaoClube : AuditableEntity
{
    private ConfiguracaoClube() { }

    internal ConfiguracaoClube(Guid clubeId)
    {
        ClubeId = clubeId;
    }

    public Guid ClubeId { get; private set; }

    /// <summary>Cor de destaque (hex, ex.: #2F5D50) usada no Web e no Mobile.</summary>
    public string CorPrimaria { get; set; } = "#2F5D50";
    public string? NomeExibicao { get; set; }
    public string FusoHorario { get; set; } = "America/Sao_Paulo";
    /// <summary>Novos cadastros de associados exigem aprovação administrativa.</summary>
    public bool ExigeAprovacaoAssociados { get; set; } = true;
    /// <summary>Novos dependentes exigem aprovação administrativa.</summary>
    public bool ExigeAprovacaoDependentes { get; set; } = true;
    /// <summary>Prefixo usado na geração de números de matrícula.</summary>
    public string? PrefixoMatricula { get; set; }
}
