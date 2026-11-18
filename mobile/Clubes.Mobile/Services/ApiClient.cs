using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Clubes.Mobile.Models;

namespace Clubes.Mobile.Services;

/// <summary>Falha devolvida pela API (ou de conexão, com Status 0). A mensagem já vem em português do servidor.</summary>
public class ApiException(string code, string message, int status, Dictionary<string, string[]>? errors = null) : Exception(message)
{
    public string Code { get; } = code;
    public int Status { get; } = status;
    public Dictionary<string, string[]>? Errors { get; } = errors;
    public bool IsNetwork => Status == 0;

    /// <summary>Mensagem do servidor acrescida dos erros de validação por campo, quando houver.</summary>
    public string FullMessage
    {
        get
        {
            if (Errors is null || Errors.Count == 0) return Message;
            var details = Errors.SelectMany(e => e.Value).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct().Take(5);
            return Message + "\n" + string.Join("\n", details.Select(d => "• " + d));
        }
    }
}

/// <summary>Cliente HTTP único da API: anexa o token, renova a sessão em 401 (com rotação do token de renovação) e padroniza erros.</summary>
public class ApiClient
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(40) };
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly SessionStore _session;
    private readonly AppConfig _config;

    public ApiClient(SessionStore session, AppConfig config)
    {
        _session = session;
        _config = config;
    }

    /// <summary>Disparado quando a sessão não pôde ser renovada: o app deve voltar ao login.</summary>
    public event Action? SessionExpired;

    public string BaseUrl => _config.BaseUrl;

    // ---------- Operações tipadas ----------
    public Task<T> GetAsync<T>(string path, CancellationToken ct = default) => SendJsonAsync<T>(HttpMethod.Get, path, null, true, ct);
    public Task<T> PostAsync<T>(string path, object? body = null, CancellationToken ct = default) => SendJsonAsync<T>(HttpMethod.Post, path, body, true, ct);
    public Task<T> PutAsync<T>(string path, object? body, CancellationToken ct = default) => SendJsonAsync<T>(HttpMethod.Put, path, body, true, ct);
    public Task<T> DeleteAsync<T>(string path, CancellationToken ct = default) => SendJsonAsync<T>(HttpMethod.Delete, path, null, true, ct);
    public Task PostAsync(string path, object? body = null, CancellationToken ct = default) => SendNoContentAsync(HttpMethod.Post, path, body, true, ct);
    public Task DeleteAsync(string path, CancellationToken ct = default) => SendNoContentAsync(HttpMethod.Delete, path, null, true, ct);

    /// <summary>Chamadas sem autenticação (login, recuperação de senha).</summary>
    public Task<T> PostAnonymousAsync<T>(string path, object? body, CancellationToken ct = default) => SendJsonAsync<T>(HttpMethod.Post, path, body, false, ct);
    public Task PostAnonymousAsync(string path, object? body, CancellationToken ct = default) => SendNoContentAsync(HttpMethod.Post, path, body, false, ct);

    public async Task<T> GetAsync<T>(string path, IDictionary<string, string> headers, CancellationToken ct = default)
    {
        using var response = await SendAsync(() =>
        {
            var request = new HttpRequestMessage(HttpMethod.Get, Url(path));
            foreach (var h in headers) request.Headers.TryAddWithoutValidation(h.Key, h.Value);
            return request;
        }, true, ct);
        return await ReadAsync<T>(response, ct);
    }

    /// <summary>Baixa um conteúdo binário autenticado (imagens, PDFs).</summary>
    public async Task<byte[]> GetBytesAsync(string path, CancellationToken ct = default)
    {
        using var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Get, Url(path)), true, ct);
        try { return await response.Content.ReadAsByteArrayAsync(ct); }
        catch (Exception ex) when (ex is HttpRequestException or IOException) { throw Network(); }
    }

    /// <summary>Envia um arquivo (multipart: File, Scope) e devolve o identificador gerado.</summary>
    public async Task<FileDto> UploadAsync(byte[] content, string fileName, string contentType, FileScope scope, CancellationToken ct = default)
    {
        using var response = await SendAsync(() =>
        {
            var file = new ByteArrayContent(content);
            file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            var form = new MultipartFormDataContent
            {
                { file, "File", fileName },
                { new StringContent(scope.ToString()), "Scope" },
            };
            return new HttpRequestMessage(HttpMethod.Post, Url("/api/arquivos")) { Content = form };
        }, true, ct);
        return await ReadAsync<FileDto>(response, ct);
    }

    /// <summary>Token de acesso válido para o canal em tempo real (renova se estiver para expirar).</summary>
    public async Task<string?> GetValidAccessTokenAsync()
    {
        if (!_session.HasSession) return null;
        if (_session.AccessToken is null || _session.AccessTokenExpiresAt <= DateTime.UtcNow.AddSeconds(20))
        {
            try { await RefreshAsync(_session.AccessToken, CancellationToken.None); }
            catch (ApiException) { /* sem conexão: o hub tentará de novo */ }
        }
        return _session.AccessToken;
    }

    // ---------- Infraestrutura ----------
    private string Url(string path) => _config.BaseUrl + path;

    private async Task<T> SendJsonAsync<T>(HttpMethod method, string path, object? body, bool auth, CancellationToken ct)
    {
        using var response = await SendAsync(() => Build(method, path, body), auth, ct);
        return await ReadAsync<T>(response, ct);
    }

    private async Task SendNoContentAsync(HttpMethod method, string path, object? body, bool auth, CancellationToken ct)
    {
        using var response = await SendAsync(() => Build(method, path, body), auth, ct);
    }

    private HttpRequestMessage Build(HttpMethod method, string path, object? body)
    {
        var request = new HttpRequestMessage(method, Url(path));
        if (body is not null) request.Content = JsonContent.Create(body, body.GetType(), options: Json);
        else if (method == HttpMethod.Post || method == HttpMethod.Put) request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        return request;
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var value = await response.Content.ReadFromJsonAsync<T>(Json, ct);
            return value ?? throw new ApiException("resposta_invalida", "O servidor devolveu uma resposta vazia.", (int)response.StatusCode);
        }
        catch (JsonException)
        {
            throw new ApiException("resposta_invalida", "Não foi possível interpretar a resposta do servidor.", (int)response.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException) { throw Network(); }
    }

    /// <summary>Envia a requisição; em 401 renova a sessão uma vez e repete. Devolve somente respostas de sucesso.</summary>
    private async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> factory, bool auth, CancellationToken ct)
    {
        if (auth && _session.HasSession && _session.AccessTokenExpiresAt <= DateTime.UtcNow.AddSeconds(10))
            await RefreshOrExpireAsync(_session.AccessToken, ct);

        var usedToken = _session.AccessToken;
        var response = await SendOnceAsync(factory, auth ? usedToken : null, ct);
        if (auth && response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            await RefreshOrExpireAsync(usedToken, ct);
            response = await SendOnceAsync(factory, _session.AccessToken, ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                response.Dispose();
                throw Expire();
            }
        }
        if (response.IsSuccessStatusCode) return response;
        using (response) throw await ToExceptionAsync(response, ct);
    }

    private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> factory, string? token, CancellationToken ct)
    {
        using var request = factory();
        if (!string.IsNullOrEmpty(token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Accept.ParseAdd("*/*;q=0.8");
        try
        {
            return await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException or InvalidOperationException)
        {
            throw Network();
        }
    }

    private async Task RefreshOrExpireAsync(string? staleAccessToken, CancellationToken ct)
    {
        if (!await RefreshAsync(staleAccessToken, ct)) throw Expire();
    }

    /// <summary>
    /// Renova a sessão. O token de renovação é de uso único: as renovações são serializadas e, se outra chamada já renovou
    /// enquanto esta aguardava, o novo token é reaproveitado em vez de reutilizar o token antigo.
    /// </summary>
    private async Task<bool> RefreshAsync(string? staleAccessToken, CancellationToken ct)
    {
        await _refreshLock.WaitAsync(ct);
        try
        {
            if (!_session.HasSession) return false;
            if (_session.AccessToken is not null && _session.AccessToken != staleAccessToken && _session.AccessTokenExpiresAt > DateTime.UtcNow.AddSeconds(10))
                return true;

            var refreshToken = _session.RefreshToken!;
            using var response = await SendOnceAsync(() => Build(HttpMethod.Post, "/api/auth/refresh", new RefreshRequest(refreshToken)), null, ct);
            if (response.IsSuccessStatusCode)
            {
                var auth = await ReadAsync<AuthResponse>(response, ct);
                await _session.SaveAsync(auth);
                return true;
            }
            if ((int)response.StatusCode is >= 400 and < 500) return false; // token inválido, expirado ou revogado
            throw await ToExceptionAsync(response, ct);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private ApiException Expire()
    {
        var hadSession = _session.HasSession || _session.User is not null;
        _session.Clear();
        if (hadSession) SessionExpired?.Invoke();
        return new ApiException("sessao_expirada", "Sessão expirada. Entre novamente.", 401);
    }

    private static ApiException Network() =>
        new("sem_conexao", "Não foi possível conectar ao servidor. Verifique sua conexão e tente novamente.", 0);

    private static async Task<ApiException> ToExceptionAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var status = (int)response.StatusCode;
        ApiError? error = null;
        try
        {
            var text = await response.Content.ReadAsStringAsync(ct);
            if (!string.IsNullOrWhiteSpace(text) && text.TrimStart().StartsWith('{')) error = JsonSerializer.Deserialize<ApiError>(text, Json);
        }
        catch (Exception) { /* corpo ilegível: usa a mensagem padrão */ }

        var message = !string.IsNullOrWhiteSpace(error?.Message) ? error!.Message! : status switch
        {
            400 or 422 => "Os dados enviados são inválidos.",
            401 => "Autenticação necessária.",
            403 => "Você não tem permissão para esta operação.",
            404 => "Registro não encontrado.",
            409 => "A operação não pôde ser concluída por um conflito. Atualize e tente novamente.",
            429 => "Muitas tentativas. Aguarde um instante e tente novamente.",
            >= 500 => "O servidor está indisponível no momento. Tente novamente em instantes.",
            _ => "Não foi possível concluir a operação.",
        };
        return new ApiException(error?.Code ?? $"http_{status}", message, status, error?.Errors);
    }
}
