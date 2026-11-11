using FluentValidation;
using ProjetoClubes.Domain.Common;

namespace ProjetoClubes.Application.Common;

public static class ValidacaoExtensions
{
    /// <summary>Valida e lança <see cref="ValidationException"/> (HTTP 400 com erros por campo).</summary>
    public static async Task ValidarAsync<T>(this IValidator<T> validator, T instancia, CancellationToken ct)
    {
        var resultado = await validator.ValidateAsync(instancia, ct);
        if (!resultado.IsValid) throw new ValidationException(resultado.Errors);
    }

    public static IRuleBuilderOptions<T, string?> Cpf<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(v => string.IsNullOrWhiteSpace(v) || Documentos.CpfValido(v)).WithMessage("CPF inválido.");

    public static IRuleBuilderOptions<T, string?> Cnpj<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(v => string.IsNullOrWhiteSpace(v) || Documentos.CnpjValido(v)).WithMessage("CNPJ inválido.");

    public static IRuleBuilderOptions<T, string?> Uf<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(v => string.IsNullOrWhiteSpace(v) || UfsValidas.Contains(v.ToUpperInvariant()))
            .WithMessage("UF inválida.");

    public static IRuleBuilderOptions<T, string?> Cep<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(v => string.IsNullOrWhiteSpace(v) || Documentos.SomenteDigitos(v).Length == 8)
            .WithMessage("CEP deve ter 8 dígitos.");

    public static IRuleBuilderOptions<T, string?> Telefone<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(v => string.IsNullOrWhiteSpace(v) || Documentos.SomenteDigitos(v).Length is 10 or 11)
            .WithMessage("Telefone deve ter DDD e 8 ou 9 dígitos.");

    public static IRuleBuilderOptions<T, string> SenhaForte<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Informe a senha.")
            .MinimumLength(10).WithMessage("A senha deve ter pelo menos 10 caracteres.")
            .MaximumLength(128).WithMessage("A senha deve ter no máximo 128 caracteres.")
            .Must(s => s.Any(char.IsLetter) && s.Any(char.IsDigit))
            .WithMessage("A senha deve conter letras e números.");

    private static readonly HashSet<string> UfsValidas =
    [
        "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG", "PA", "PB", "PR", "PE", "PI",
        "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO",
    ];
}
