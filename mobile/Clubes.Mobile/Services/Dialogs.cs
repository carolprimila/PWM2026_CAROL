namespace Clubes.Mobile.Services;

/// <summary>Diálogos e navegação a partir dos view models.</summary>
public static class Dialogs
{
    private static Page? Host => Shell.Current ?? Application.Current?.Windows.FirstOrDefault()?.Page;

    public static Task AlertAsync(string title, string message) =>
        Host is { } page ? MainThread.InvokeOnMainThreadAsync(() => page.DisplayAlertAsync(title, message, "OK")) : Task.CompletedTask;

    public static Task<bool> ConfirmAsync(string title, string message, string accept = "Confirmar", string cancel = "Voltar") =>
        Host is { } page ? MainThread.InvokeOnMainThreadAsync(() => page.DisplayAlertAsync(title, message, accept, cancel)) : Task.FromResult(false);

    /// <summary>Devolve o texto digitado, ou null se o usuário cancelar.</summary>
    public static Task<string?> PromptAsync(string title, string message, string placeholder = "", int maxLength = 500, Keyboard? keyboard = null, string initialValue = "") =>
        Host is { } page
            ? MainThread.InvokeOnMainThreadAsync(() => page.DisplayPromptAsync(title, message, "Confirmar", "Voltar", placeholder, maxLength, keyboard ?? Keyboard.Text, initialValue))
            : Task.FromResult<string?>(null);

    /// <summary>Devolve a opção escolhida, ou null se o usuário cancelar.</summary>
    public static async Task<string?> ActionSheetAsync(string title, params string[] options)
    {
        if (Host is not { } page) return null;
        var choice = await MainThread.InvokeOnMainThreadAsync(() => page.DisplayActionSheetAsync(title, "Cancelar", null, options));
        return options.Contains(choice) ? choice : null;
    }
}

public static class Nav
{
    public static Task GoToAsync(string route) => Shell.Current is { } shell ? shell.GoToAsync(route) : Task.CompletedTask;

    public static Task GoToAsync(string route, Guid id) =>
        Shell.Current is { } shell ? shell.GoToAsync(route, new Dictionary<string, object> { ["id"] = id }) : Task.CompletedTask;

    public static Task GoToAsync(string route, IDictionary<string, object> parameters) =>
        Shell.Current is { } shell ? shell.GoToAsync(route, parameters) : Task.CompletedTask;

    public static Task BackAsync() => Shell.Current is { } shell ? shell.GoToAsync("..") : Task.CompletedTask;

    /// <summary>Volta até a raiz da aba atual.</summary>
    public static Task ToRootAsync() => Shell.Current is { } shell ? shell.Navigation.PopToRootAsync() : Task.CompletedTask;
}

/// <summary>Máscaras simples de digitação.</summary>
public enum MaskKind { None, Cpf, Phone, Cep, Digits, Uf }

public static class Masks
{
    public static string Digits(string? text) => new((text ?? "").Where(char.IsDigit).ToArray());

    public static string Apply(MaskKind kind, string? text)
    {
        if (kind == MaskKind.None || text is null) return text ?? "";
        if (kind == MaskKind.Uf) return new string(text.Where(char.IsLetter).Take(2).ToArray()).ToUpperInvariant();
        var d = Digits(text);
        switch (kind)
        {
            case MaskKind.Digits:
                return d;
            case MaskKind.Cpf:
                d = d.Length > 11 ? d[..11] : d;
                return d.Length switch
                {
                    <= 3 => d,
                    <= 6 => $"{d[..3]}.{d[3..]}",
                    <= 9 => $"{d[..3]}.{d[3..6]}.{d[6..]}",
                    _ => $"{d[..3]}.{d[3..6]}.{d[6..9]}-{d[9..]}",
                };
            case MaskKind.Cep:
                d = d.Length > 8 ? d[..8] : d;
                return d.Length <= 5 ? d : $"{d[..5]}-{d[5..]}";
            case MaskKind.Phone:
                d = d.Length > 11 ? d[..11] : d;
                return d.Length switch
                {
                    0 => "",
                    <= 2 => $"({d}",
                    <= 6 => $"({d[..2]}) {d[2..]}",
                    <= 10 => $"({d[..2]}) {d[2..6]}-{d[6..]}",
                    _ => $"({d[..2]}) {d[2..7]}-{d[7..]}",
                };
            default:
                return text;
        }
    }
}
