namespace ProjetoClubes.Application.Common;

/// <summary>Nomes dos filtros globais do EF Core (permite desligar apenas um deles quando necessário).</summary>
public static class FiltrosConsulta
{
    /// <summary>Restringe entidades <c>ITenantScoped</c> ao clube do usuário atual.</summary>
    public const string Tenant = "Tenant";

    /// <summary>Oculta registros com exclusão lógica.</summary>
    public const string ExclusaoLogica = "ExclusaoLogica";

    public static readonly string[] SomenteTenant = [Tenant];
}
