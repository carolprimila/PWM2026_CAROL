using FluentValidation;
using ProjetoClubes.Application.Common;

namespace ProjetoClubes.Application.Clubes;

public sealed record EnderecoDto(string? Logradouro, string? Numero, string? Complemento, string? Bairro,
    string? Cidade, string? Uf, string? Cep);

public sealed record ClubeListaDto(Guid Id, string Nome, string Sigla, string? Cidade, string? Uf, string Situacao,
    bool Demonstracao, string CorPrimaria, int TotalUsuarios);

public sealed record ConfiguracaoClubeDto(string CorPrimaria, string? NomeExibicao, string FusoHorario,
    bool ExigeAprovacaoAssociados, bool ExigeAprovacaoDependentes, string? PrefixoMatricula);

public sealed record ClubeDetalheDto(
    Guid Id,
    string Nome,
    string Sigla,
    string? Cnpj,
    EnderecoDto Endereco,
    string? Telefone,
    string? Email,
    string? LogotipoArquivo,
    DateOnly? DataFundacao,
    string Situacao,
    bool Demonstracao,
    DateTimeOffset CriadoEm,
    ConfiguracaoClubeDto Configuracao);

public sealed record SalvarClubeRequest(
    string Nome,
    string Sigla,
    string? Cnpj,
    EnderecoDto? Endereco,
    string? Telefone,
    string? Email,
    DateOnly? DataFundacao);

public sealed record AtualizarConfiguracaoClubeRequest(string CorPrimaria, string? NomeExibicao, string FusoHorario,
    bool ExigeAprovacaoAssociados, bool ExigeAprovacaoDependentes, string? PrefixoMatricula);

/// <summary>Cria (ou vincula, se o e-mail já existir) um usuário administrador para o clube.</summary>
public sealed record AdicionarAdministradorRequest(string Nome, string Email, string PerfilChave, string? SenhaInicial);

public sealed class ConsultaClubes : ConsultaPaginada
{
    public string? Situacao { get; set; }
}

public sealed class SalvarClubeRequestValidator : AbstractValidator<SalvarClubeRequest>
{
    public SalvarClubeRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().WithMessage("Informe o nome do clube.").MaximumLength(150);
        RuleFor(x => x.Sigla).NotEmpty().WithMessage("Informe a sigla.").Length(2, 20)
            .Matches("^[A-Za-z0-9_-]+$").WithMessage("A sigla deve conter apenas letras, números, '-' ou '_'.");
        RuleFor(x => x.Cnpj).Cnpj();
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("E-mail inválido.").MaximumLength(254);
        RuleFor(x => x.Telefone).Telefone();
        RuleFor(x => x.DataFundacao).LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .When(x => x.DataFundacao.HasValue).WithMessage("A data de fundação não pode ser futura.");
        When(x => x.Endereco is not null, () =>
        {
            RuleFor(x => x.Endereco!.Uf).Uf();
            RuleFor(x => x.Endereco!.Cep).Cep();
            RuleFor(x => x.Endereco!.Logradouro).MaximumLength(200);
            RuleFor(x => x.Endereco!.Numero).MaximumLength(20);
            RuleFor(x => x.Endereco!.Complemento).MaximumLength(100);
            RuleFor(x => x.Endereco!.Bairro).MaximumLength(100);
            RuleFor(x => x.Endereco!.Cidade).MaximumLength(100);
        });
    }
}

public sealed class AtualizarConfiguracaoClubeRequestValidator : AbstractValidator<AtualizarConfiguracaoClubeRequest>
{
    public AtualizarConfiguracaoClubeRequestValidator()
    {
        RuleFor(x => x.CorPrimaria).NotEmpty().Matches("^#[0-9A-Fa-f]{6}$")
            .WithMessage("Informe a cor no formato hexadecimal, ex.: #2F5D50.");
        RuleFor(x => x.NomeExibicao).MaximumLength(100);
        RuleFor(x => x.FusoHorario).NotEmpty()
            .Must(f => TimeZoneInfo.TryFindSystemTimeZoneById(f, out _)).WithMessage("Fuso horário inválido.");
        RuleFor(x => x.PrefixoMatricula).MaximumLength(10);
    }
}

public sealed class AdicionarAdministradorRequestValidator : AbstractValidator<AdicionarAdministradorRequest>
{
    public AdicionarAdministradorRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().WithMessage("Informe o nome.").MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("E-mail inválido.").MaximumLength(254);
        RuleFor(x => x.PerfilChave).NotEmpty();
        RuleFor(x => x.SenhaInicial!).SenhaForte().When(x => x.SenhaInicial is not null);
    }
}
