using FluentValidation;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Domain.Common;
using ProjetoClubes.Domain.Dependentes;

namespace ProjetoClubes.Application.Cadastros;

public sealed record PermissoesDependenteDto(bool AcessoClube, bool ParticipacaoEventos, bool UsoEspacosEsportivos,
    bool InscricaoAtividades, bool SolicitacaoReservas, bool RecebimentoComunicados, bool UtilizacaoServicos)
{
    public static PermissoesDependenteDto De(PermissoesDependente p) => new(p.AcessoClube, p.ParticipacaoEventos,
        p.UsoEspacosEsportivos, p.InscricaoAtividades, p.SolicitacaoReservas, p.RecebimentoComunicados, p.UtilizacaoServicos);

    public PermissoesDependente ParaDominio() => new()
    {
        AcessoClube = AcessoClube, ParticipacaoEventos = ParticipacaoEventos, UsoEspacosEsportivos = UsoEspacosEsportivos,
        InscricaoAtividades = InscricaoAtividades, SolicitacaoReservas = SolicitacaoReservas,
        RecebimentoComunicados = RecebimentoComunicados, UtilizacaoServicos = UtilizacaoServicos,
    };
}

public sealed record TitularResumoDto(Guid Id, string Nome, string Matricula, SituacaoCadastral Situacao);

public sealed record VinculoDto(Guid TitularId, string TitularNome, DateTimeOffset Inicio, DateTimeOffset? Fim, string Motivo);

public sealed record DependenteListaDto(Guid Id, Guid PessoaId, string Nome, string? CpfMascarado, int? Idade,
    Parentesco Parentesco, string? ParentescoDescricao, string NumeroIdentificacao, SituacaoCadastral Situacao,
    Guid TitularId, string TitularNome, string TitularMatricula, bool TemFoto, DateOnly DataInclusao);

public sealed record DependenteDetalheDto(
    Guid Id,
    PessoaDto Pessoa,
    Parentesco Parentesco,
    string? ParentescoDescricao,
    string NumeroIdentificacao,
    SituacaoCadastral Situacao,
    string? MotivoSituacao,
    DateOnly DataInclusao,
    TitularResumoDto Titular,
    PermissoesDependenteDto Permissoes,
    bool MenorDeIdade,
    bool PodeTerLoginProprio,
    bool ExcecaoMultiplosTitulares,
    string? JustificativaExcecao,
    IReadOnlyList<VinculoDto> Vinculos,
    DateTimeOffset CriadoEm);

public sealed record CriarDependenteRequest(
    Guid AssociadoTitularId,
    DadosPessoaisRequest Pessoa,
    Parentesco Parentesco,
    string? ParentescoDescricao,
    // Somente administração com permissão de aprovação: autoriza vínculo a mais de um titular.
    string? JustificativaExcecaoMultiplosTitulares);

public sealed record AtualizarDependenteRequest(DadosPessoaisRequest Pessoa, Parentesco Parentesco, string? ParentescoDescricao);

public sealed record TransferirDependenteRequest(Guid NovoTitularId, Parentesco Parentesco, string? ParentescoDescricao, string Motivo);

public sealed record ConfiguracaoDependentesDto(
    int LimitePadraoDependentes,
    int MaximoConjuges,
    int? IdadeMaximaFilhos,
    int? IdadeMaximaIrmaos,
    int IdadeMaioridade,
    int IdadeMinimaLoginProprio,
    IReadOnlyList<Parentesco> ParentescosPermitidos,
    bool ExigeDocumentoComprobatorio,
    bool MenoresPodemSolicitarReservas,
    bool IncluidosNaMensalidadeDoTitular,
    decimal ValorAdicionalPorDependente,
    PermissoesDependenteDto PermissoesPadrao);

public sealed class ConsultaDependentes : ConsultaPaginada
{
    public SituacaoCadastral? Situacao { get; set; }
    public Parentesco? Parentesco { get; set; }
    public Guid? TitularId { get; set; }
}

public sealed class CriarDependenteRequestValidator : AbstractValidator<CriarDependenteRequest>
{
    public CriarDependenteRequestValidator()
    {
        RuleFor(x => x.AssociadoTitularId).NotEmpty().WithMessage("Selecione o titular.");
        RuleFor(x => x.Pessoa).NotNull().SetValidator(new DadosPessoaisValidator(exigeCpf: false));
        RuleFor(x => x.Parentesco).IsInEnum();
        RuleFor(x => x.ParentescoDescricao).NotEmpty().When(x => x.Parentesco == Parentesco.Outro)
            .WithMessage("Descreva o grau de parentesco.").MaximumLength(60);
        RuleFor(x => x.JustificativaExcecaoMultiplosTitulares).MaximumLength(300);
    }
}

public sealed class AtualizarDependenteRequestValidator : AbstractValidator<AtualizarDependenteRequest>
{
    public AtualizarDependenteRequestValidator()
    {
        RuleFor(x => x.Pessoa).NotNull().SetValidator(new DadosPessoaisValidator(exigeCpf: false));
        RuleFor(x => x.Parentesco).IsInEnum();
        RuleFor(x => x.ParentescoDescricao).NotEmpty().When(x => x.Parentesco == Parentesco.Outro)
            .WithMessage("Descreva o grau de parentesco.").MaximumLength(60);
    }
}

public sealed class TransferirDependenteRequestValidator : AbstractValidator<TransferirDependenteRequest>
{
    public TransferirDependenteRequestValidator()
    {
        RuleFor(x => x.NovoTitularId).NotEmpty();
        RuleFor(x => x.Parentesco).IsInEnum();
        RuleFor(x => x.Motivo).NotEmpty().WithMessage("Informe o motivo da transferência.").MaximumLength(300);
    }
}

public sealed class ConfiguracaoDependentesDtoValidator : AbstractValidator<ConfiguracaoDependentesDto>
{
    public ConfiguracaoDependentesDtoValidator()
    {
        RuleFor(x => x.LimitePadraoDependentes).InclusiveBetween(0, 50);
        RuleFor(x => x.MaximoConjuges).InclusiveBetween(0, 5);
        RuleFor(x => x.IdadeMaximaFilhos).InclusiveBetween(0, 120).When(x => x.IdadeMaximaFilhos.HasValue);
        RuleFor(x => x.IdadeMaximaIrmaos).InclusiveBetween(0, 120).When(x => x.IdadeMaximaIrmaos.HasValue);
        RuleFor(x => x.IdadeMaioridade).InclusiveBetween(14, 21);
        RuleFor(x => x.IdadeMinimaLoginProprio).InclusiveBetween(0, 21);
        RuleFor(x => x.ParentescosPermitidos).NotNull();
        RuleForEach(x => x.ParentescosPermitidos).IsInEnum();
        RuleFor(x => x.ValorAdicionalPorDependente).GreaterThanOrEqualTo(0).PrecisionScale(12, 2, true);
        RuleFor(x => x.PermissoesPadrao).NotNull();
    }
}
