using FluentValidation;
using ProjetoClubes.Application.Common;

namespace ProjetoClubes.Application.Autenticacao;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Informe o e-mail.").MaximumLength(254);
        RuleFor(x => x.Senha).NotEmpty().WithMessage("Informe a senha.").MaximumLength(128);
        RuleFor(x => x.Dispositivo).MaximumLength(100);
    }
}

public sealed class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator() => RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(200);
}

public sealed class EsqueciSenhaRequestValidator : AbstractValidator<EsqueciSenhaRequest>
{
    public EsqueciSenhaRequestValidator() =>
        RuleFor(x => x.Email).NotEmpty().WithMessage("Informe o e-mail.").EmailAddress().WithMessage("E-mail inválido.");
}

public sealed class RedefinirSenhaRequestValidator : AbstractValidator<RedefinirSenhaRequest>
{
    public RedefinirSenhaRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NovaSenha).SenhaForte();
    }
}

public sealed class AlterarSenhaRequestValidator : AbstractValidator<AlterarSenhaRequest>
{
    public AlterarSenhaRequestValidator()
    {
        RuleFor(x => x.SenhaAtual).NotEmpty().WithMessage("Informe a senha atual.");
        RuleFor(x => x.NovaSenha).SenhaForte()
            .NotEqual(x => x.SenhaAtual).WithMessage("A nova senha deve ser diferente da atual.");
    }
}
