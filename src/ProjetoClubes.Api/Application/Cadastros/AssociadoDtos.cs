using FluentValidation;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Domain.Common;
using ProjetoClubes.Domain.Dependentes;

namespace ProjetoClubes.Application.Cadastros;

public sealed record CategoriaResumoDto(Guid Id, string Nome, bool DireitoVoto);

public sealed record DependenteResumoDto(Guid Id, Guid PessoaId, string Nome, Parentesco Parentesco, string? ParentescoDescricao,
    string NumeroIdentificacao, SituacaoCadastral Situacao, int? Idade);

public sealed record AssociadoListaDto(Guid Id, Guid PessoaId, string Matricula, string Nome, string? CpfMascarado,
    Guid CategoriaId, string Categoria, SituacaoCadastral Situacao, DateOnly DataAssociacao, int TotalDependentes, bool TemFoto);

public sealed record AssociadoDetalheDto(
    Guid Id,
    PessoaDto Pessoa,
    string Matricula,
    CategoriaResumoDto Categoria,
    DateOnly DataAssociacao,
    SituacaoCadastral Situacao,
    string? MotivoSituacao,
    // Calculado: categoria com direito a voto e associado ativo.
    bool AptoAVotar,
    IReadOnlyList<InteresseDto> Interesses,
    IReadOnlyList<DependenteResumoDto> Dependentes,
    DateTimeOffset CriadoEm);

public sealed record CriarAssociadoRequest(
    DadosPessoaisRequest Pessoa,
    Guid CategoriaId,
    DateOnly? DataAssociacao,
    IReadOnlyList<Guid>? InteresseIds,
    // Opcional: matrícula informada manualmente (migração de cadastros antigos). Vazio = gerada.
    string? Matricula);

public sealed record AtualizarAssociadoRequest(
    DadosPessoaisRequest Pessoa,
    Guid CategoriaId,
    DateOnly DataAssociacao,
    IReadOnlyList<Guid>? InteresseIds);

public sealed record CriarAcessoAppRequest(string? SenhaInicial);

public sealed record CriarAcessoAppResponse(Guid UsuarioId, bool ConviteEnviado);

public sealed record ResumoCadastrosDto(
    int TotalAssociados,
    IReadOnlyDictionary<string, int> AssociadosPorSituacao,
    IReadOnlyDictionary<string, int> AssociadosPorCategoria,
    int TotalDependentes,
    IReadOnlyDictionary<string, int> DependentesPorSituacao);

public sealed class ConsultaAssociados : ConsultaPaginada
{
    public SituacaoCadastral? Situacao { get; set; }
    public Guid? CategoriaId { get; set; }
    public Guid? InteresseId { get; set; }
}

public sealed class CriarAssociadoRequestValidator : AbstractValidator<CriarAssociadoRequest>
{
    public CriarAssociadoRequestValidator()
    {
        RuleFor(x => x.Pessoa).NotNull().SetValidator(new DadosPessoaisValidator(exigeCpf: true));
        RuleFor(x => x.CategoriaId).NotEmpty().WithMessage("Selecione a categoria.");
        RuleFor(x => x.Matricula).MaximumLength(30).Matches("^[A-Za-z0-9-]*$").WithMessage("Matrícula inválida.");
        RuleFor(x => x.DataAssociacao).LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1))
            .WithMessage("A data de associação não pode ser futura.");
    }
}

public sealed class AtualizarAssociadoRequestValidator : AbstractValidator<AtualizarAssociadoRequest>
{
    public AtualizarAssociadoRequestValidator()
    {
        RuleFor(x => x.Pessoa).NotNull().SetValidator(new DadosPessoaisValidator(exigeCpf: true));
        RuleFor(x => x.CategoriaId).NotEmpty().WithMessage("Selecione a categoria.");
    }
}

public sealed class CriarAcessoAppRequestValidator : AbstractValidator<CriarAcessoAppRequest>
{
    public CriarAcessoAppRequestValidator() => RuleFor(x => x.SenhaInicial!).SenhaForte().When(x => x.SenhaInicial is not null);
}
