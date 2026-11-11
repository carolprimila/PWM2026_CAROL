using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace ProjetoClubes.Web.Servicos;

/// <summary>
/// Sessão do usuário na conexão (circuito) do Blazor. Os tokens ficam no servidor do Web; só o refresh token
/// vai para o sessionStorage do navegador, criptografado (ProtectedSessionStorage), para sobreviver ao F5.
/// </summary>
public sealed class Sessao(ProtectedSessionStorage armazenamento)
{
    private const string Chave = "pc.refresh";
    private bool _restaurada;

    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public UsuarioLogado? Usuario { get; private set; }
    public bool Autenticado => Usuario is not null;

    public event Action? Mudou;

    public async Task DefinirAsync(TokenResponse t)
    {
        AccessToken = t.AccessToken;
        RefreshToken = t.RefreshToken;
        Usuario = t.Usuario;
        _restaurada = true;
        try { await armazenamento.SetAsync(Chave, t.RefreshToken); } catch { /* navegador indisponível */ }
        Mudou?.Invoke();
    }

    public async Task LimparAsync()
    {
        AccessToken = RefreshToken = null;
        Usuario = null;
        try { await armazenamento.DeleteAsync(Chave); } catch { /* ignorado */ }
        Mudou?.Invoke();
    }

    /// <summary>Na primeira renderização, retoma a sessão da aba a partir do refresh token salvo.</summary>
    public async Task RestaurarAsync(Func<string, Task<TokenResponse?>> renovar)
    {
        if (_restaurada) return;
        _restaurada = true;
        try
        {
            var salvo = await armazenamento.GetAsync<string>(Chave);
            if (salvo is { Success: true, Value: { } refresh } && await renovar(refresh) is { } t) await DefinirAsync(t);
        }
        catch
        {
            // armazenamento ilegível (ex.: chave de proteção trocada): segue sem sessão
        }
    }

    /// <summary>Permissão no clube atual (a API valida de novo em cada chamada).</summary>
    public bool Possui(string permissao) =>
        Usuario?.ClubeAtual is not null && (Usuario.SuperAdmin || Usuario.Permissoes.Contains(permissao));
}
