using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using ProjetoClubes.Application.Seguranca;

namespace ProjetoClubes.API.Autorizacao;

/// <summary>
/// Exige uma permissão do catálogo no clube atual: <c>[RequerPermissao(Permissoes.Associados.Cadastrar)]</c>.
/// A verificação consulta as permissões efetivas no servidor (perfis + exceções), nunca o nome do perfil.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequerPermissaoAttribute(string permissao) : AuthorizeAttribute(PoliticaPermissao.Prefixo + permissao)
{
    public string Permissao { get; } = permissao;
}

public static class PoliticaPermissao
{
    public const string Prefixo = "permissao:";
}

public sealed class PermissaoRequirement(string permissao) : IAuthorizationRequirement
{
    public string Permissao { get; } = permissao;
}

public sealed class PermissaoHandler(IPermissaoResolver resolver) : AuthorizationHandler<PermissaoRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissaoRequirement requirement)
    {
        if (await resolver.PossuiAsync(requirement.Permissao)) context.Succeed(requirement);
    }
}

/// <summary>Cria dinamicamente uma política por permissão ("permissao:codigo").</summary>
public sealed class PermissaoPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(PoliticaPermissao.Prefixo, StringComparison.Ordinal))
            return await base.GetPolicyAsync(policyName);

        var permissao = policyName[PoliticaPermissao.Prefixo.Length..];
        if (!Permissoes.Codigos.Contains(permissao))
            throw new InvalidOperationException($"Permissão desconhecida na política: {permissao}");

        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissaoRequirement(permissao))
            .Build();
    }
}
