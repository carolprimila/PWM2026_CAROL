using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjetoClubes.Web.Servicos;

/// <summary>Erro devolvido pela API, com a mensagem pronta para mostrar ao usuário.</summary>
public sealed class ErroApi(HttpStatusCode status, string mensagem, string? codigo = null) : Exception(mensagem)
{
    public HttpStatusCode Status { get; } = status;
    public string? Codigo { get; } = codigo;
}

/// <summary>
/// Acesso à API REST do Projeto Clubes. O Web nunca fala com o banco: tudo passa por aqui,
/// com o token do usuário e renovação automática quando ele expira.
/// </summary>
public sealed class Api(IHttpClientFactory fabrica, Sessao sessao)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    private HttpClient Cliente => fabrica.CreateClient("api");

    public Task<T> GetAsync<T>(string url) => EnviarAsync<T>(HttpMethod.Get, url, null);
    public Task<T> PostAsync<T>(string url, object? corpo = null) => EnviarAsync<T>(HttpMethod.Post, url, corpo ?? new { });
    public Task PostAsync(string url, object? corpo = null) => EnviarAsync<object?>(HttpMethod.Post, url, corpo ?? new { });
    public Task<T> PutAsync<T>(string url, object corpo) => EnviarAsync<T>(HttpMethod.Put, url, corpo);
    public Task DeleteAsync(string url) => EnviarAsync<object?>(HttpMethod.Delete, url, null);
    public Task<T> DeleteAsync<T>(string url) => EnviarAsync<T>(HttpMethod.Delete, url, null);

    /// <summary>Resposta como texto (ex.: exportação LGPD em JSON), para download.</summary>
    public async Task<string> TextoAsync(string url)
    {
        using var r = await ExecutarComRenovacaoAsync(HttpMethod.Get, url, null);
        if (!r.IsSuccessStatusCode) throw await LerErroAsync(r);
        return await r.Content.ReadAsStringAsync();
    }

    /// <summary>Conteúdo binário (QR Code, foto) para exibir como data URI.</summary>
    public async Task<string?> ImagemAsync(string url)
    {
        using var r = await ExecutarComRenovacaoAsync(HttpMethod.Get, url, null);
        if (!r.IsSuccessStatusCode) return null;
        var tipo = r.Content.Headers.ContentType?.MediaType ?? "image/png";
        return $"data:{tipo};base64,{Convert.ToBase64String(await r.Content.ReadAsByteArrayAsync())}";
    }

    public async Task<TokenResponse> EntrarAsync(string email, string senha)
    {
        using var r = await Cliente.PostAsJsonAsync("api/v1/auth/login", new { email, senha, dispositivo = "Web administrativo" }, Json);
        if (!r.IsSuccessStatusCode) throw await LerErroAsync(r);
        var t = (await r.Content.ReadFromJsonAsync<TokenResponse>(Json))!;
        await sessao.DefinirAsync(t);
        return t;
    }

    public async Task SairAsync()
    {
        try { await PostAsync("api/v1/auth/logout"); } catch { /* encerra localmente de qualquer forma */ }
        await sessao.LimparAsync();
    }

    public async Task TrocarClubeAsync(Guid clubeId) =>
        await sessao.DefinirAsync(await PostAsync<TokenResponse>("api/v1/auth/trocar-clube", new { clubeId }));

    public Task RestaurarSessaoAsync() => sessao.RestaurarAsync(RenovarAsync);

    private async Task<TokenResponse?> RenovarAsync(string refresh)
    {
        using var r = await Cliente.PostAsJsonAsync("api/v1/auth/refresh", new { refreshToken = refresh }, Json);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<TokenResponse>(Json) : null;
    }

    private async Task<T> EnviarAsync<T>(HttpMethod metodo, string url, object? corpo)
    {
        using var r = await ExecutarComRenovacaoAsync(metodo, url, corpo);
        if (!r.IsSuccessStatusCode) throw await LerErroAsync(r);
        if (r.StatusCode == HttpStatusCode.NoContent || r.Content.Headers.ContentLength == 0) return default!;
        return (await r.Content.ReadFromJsonAsync<T>(Json))!;
    }

    private async Task<HttpResponseMessage> ExecutarComRenovacaoAsync(HttpMethod metodo, string url, object? corpo)
    {
        HttpRequestMessage Montar()
        {
            var m = new HttpRequestMessage(metodo, url);
            if (sessao.AccessToken is { } t) m.Headers.Authorization = new AuthenticationHeaderValue("Bearer", t);
            if (corpo is not null) m.Content = JsonContent.Create(corpo, options: Json);
            return m;
        }

        HttpResponseMessage r;
        try
        {
            r = await Cliente.SendAsync(Montar());
        }
        catch (HttpRequestException)
        {
            throw new ErroApi(0, "Não foi possível conectar à API. Verifique se ela está em execução.");
        }

        if (r.StatusCode == HttpStatusCode.Unauthorized && sessao.RefreshToken is { } refresh)
        {
            r.Dispose();
            if (await RenovarAsync(refresh) is { } novo)
            {
                await sessao.DefinirAsync(novo);
                return await Cliente.SendAsync(Montar());
            }
            await sessao.LimparAsync();
            throw new ErroApi(HttpStatusCode.Unauthorized, "Sua sessão expirou. Entre novamente.");
        }
        return r;
    }

    private static async Task<ErroApi> LerErroAsync(HttpResponseMessage r)
    {
        try
        {
            var p = await r.Content.ReadFromJsonAsync<JsonElement>();
            var titulo = p.TryGetProperty("title", out var t) ? t.GetString() : null;
            var codigo = p.TryGetProperty("codigo", out var c) ? c.GetString() : null;
            // Em erros de validação, mostra a primeira mensagem de campo (é a mais útil para o usuário).
            if (p.TryGetProperty("errors", out var erros) && erros.ValueKind == JsonValueKind.Object)
                foreach (var campo in erros.EnumerateObject())
                    if (campo.Value.ValueKind == JsonValueKind.Array && campo.Value.GetArrayLength() > 0)
                        return new ErroApi(r.StatusCode, campo.Value[0].GetString() ?? titulo ?? "Dados inválidos.", codigo);
            return new ErroApi(r.StatusCode, titulo ?? Padrao(r.StatusCode), codigo);
        }
        catch
        {
            return new ErroApi(r.StatusCode, Padrao(r.StatusCode));
        }
    }

    private static string Padrao(HttpStatusCode s) => s switch
    {
        HttpStatusCode.Forbidden => "Você não tem permissão para esta operação.",
        HttpStatusCode.NotFound => "Registro não encontrado.",
        _ => "Não foi possível concluir a operação.",
    };
}
