using FluentValidation;
using ProjetoClubes.Application.Common;

namespace ProjetoClubes.Application.Usuarios;

public sealed record PerfilResumoDto(Guid Id, string Nome);

public sealed record UsuarioListaDto(Guid Id, string Nome, string Email, string SituacaoConta, string SituacaoNoClube,
    IReadOnlyList<PerfilResumoDto> Perfis, DateTimeOffset? UltimoAcessoEm);

public sealed record PermissaoIndividualDto(string Codigo, bool Concedida);

public sealed record UsuarioDetalheDto(
    Guid Id,
    string Nome,
    string Email,
    string SituacaoConta,
    string SituacaoNoClube,
    bool SuperAdmin,
    bool DeveTrocarSenha,
    DateTimeOffset? UltimoAcessoEm,
    DateTimeOffset CriadoEm,
    IReadOnlyList<PerfilResumoDto> Perfis,
    IReadOnlyList<PermissaoIndividualDto> PermissoesIndividuais,
    IReadOnlyList<string> PermissoesEfetivas);

public sealed record CriarUsuarioRequest(string Nome, string Email, IReadOnlyList<Guid> PerfilIds, string? SenhaInicial);

public sealed record CriarUsuarioResponse(Guid Id, bool UsuarioCriado, bool ConviteEnviado);

public sealed record AtualizarUsuarioRequest(string Nome);

public sealed record DefinirAcessoRequest(IReadOnlyList<Guid> PerfilIds, IReadOnlyList<PermissaoIndividualDto>? PermissoesIndividuais);

public sealed class ConsultaUsuarios : ConsultaPaginada
{
    public Guid? PerfilId { get; set; }
    /// <summary>Ativo | Inativo (situação do vínculo no clube).</summary>
    public string? Situacao { get; set; }
}

public sealed class CriarUsuarioRequestValidator : AbstractValidator<CriarUsuarioRequest>
{
    public CriarUsuarioRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().WithMessage("Informe o nome.").MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("E-mail inválido.").MaximumLength(254);
        RuleFor(x => x.PerfilIds).NotEmpty().WithMessage("Selecione pelo menos um perfil.");
        RuleFor(x => x.SenhaInicial!).SenhaForte().When(x => x.SenhaInicial is not null);
    }
}

public sealed class AtualizarUsuarioRequestValidator : AbstractValidator<AtualizarUsuarioRequest>
{
    public AtualizarUsuarioRequestValidator() =>
        RuleFor(x => x.Nome).NotEmpty().WithMessage("Informe o nome.").MaximumLength(150);
}

public sealed class DefinirAcessoRequestValidator : AbstractValidator<DefinirAcessoRequest>
{
    public DefinirAcessoRequestValidator()
    {
        RuleFor(x => x.PerfilIds).NotNull();
        RuleForEach(x => x.PermissoesIndividuais).ChildRules(p =>
            p.RuleFor(i => i.Codigo).Must(c => Seguranca.Permissoes.Codigos.Contains(c))
                .WithMessage("Permissão desconhecida: {PropertyValue}."));
    }
}
