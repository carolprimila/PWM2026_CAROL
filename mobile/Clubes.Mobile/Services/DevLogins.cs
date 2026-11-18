namespace Clubes.Mobile.Services;

public record DevLogin(string Label, string Email, string Password);

/// <summary>
/// Contas de teste para entrada rápida na tela de login — SOMENTE em builds Debug.
/// A lista vem do arquivo local não versionado <c>DevLogins.local.cs</c>, gerado por
/// <c>database/scripts/contas-dev.py</c>. Sem esse arquivo (ou em Release) a lista fica vazia e nada é exibido.
/// </summary>
public static partial class DevLogins
{
    public static List<DevLogin> All { get; } = [];

    static DevLogins() => Fill();

    static partial void Fill();
}
