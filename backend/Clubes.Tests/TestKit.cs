using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Clubes.Application.Access;
using Clubes.Application.Clubs;
using Clubes.Application.Common;
using Clubes.Application.People;
using Clubes.Domain;
using Clubes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Clubes.Tests;

/// <summary>
/// Sobe a API real contra um PostgreSQL real (contêiner descartável). As migrations são aplicadas na
/// inicialização, então os testes exercitam também as restrições do banco (chaves compostas por clube,
/// exclusão de sobreposição de reservas, imutabilidade da auditoria).
/// </summary>
public class ApiFixture : IAsyncLifetime
{
    public const string SuperAdminEmail = "super@teste.local";
    public const string SuperAdminPassword = "Super1234teste";
    public const string SandboxSecret = "segredo-sandbox-para-testes-1234";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().WithImage("postgres:17-alpine").Build();
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        // O Program lê parte da configuração antes de o host ser construído; variáveis de ambiente garantem os valores de teste.
        var settings = new Dictionary<string, string>
        {
            ["ConnectionStrings__Default"] = _postgres.GetConnectionString(),
            ["Jwt__Key"] = "chave-de-teste-com-mais-de-trinta-e-dois-caracteres-0123456789",
            ["Bootstrap__SuperAdminEmail"] = SuperAdminEmail,
            ["Bootstrap__SuperAdminPassword"] = SuperAdminPassword,
            ["Routines__Enabled"] = "false",
            ["Payments__Provider"] = "Sandbox",
            ["Payments__SandboxSecret"] = SandboxSecret,
            ["RateLimit__AuthPerMinute"] = "100000",
            ["RateLimit__PerMinute"] = "1000000",
            ["Storage__RootPath"] = Path.Combine(Path.GetTempPath(), "clubes-tests-" + Guid.NewGuid().ToString("N")),
            ["Logging__Directory"] = Path.Combine(Path.GetTempPath(), "clubes-tests-logs"),
        };
        foreach (var (key, value) in settings) Environment.SetEnvironmentVariable(key, value);
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment("Testing"));
        _ = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    public Api Anonymous() => new(Factory.CreateClient());

    public async Task<Api> LoginAsync(string email, string password)
    {
        var api = Anonymous();
        var auth = await api.PostAsync<AuthResponse>("/api/auth/login", new LoginRequest(email, password, "testes"));
        api.Authorize(auth.AccessToken);
        api.Auth = auth;
        return api;
    }

    public Task<Api> SuperAdminAsync() => LoginAsync(SuperAdminEmail, SuperAdminPassword);

    /// <summary>Cria um clube novo (com a estrutura física padrão) e devolve o administrador autenticado.</summary>
    public async Task<ClubScope> NewClubAsync(Action<SettingsDto>? configure = null)
    {
        var super = await SuperAdminAsync();
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var created = await super.PostAsync<CreateClubResult>("/api/clubes",
            new CreateClubRequest(new SaveClubRequest($"Clube {suffix}", null, null, null, null, null, null, null, null, null, null, null, null, null, null), "Admin Teste", $"admin-{suffix}@teste.local", true));
        var admin = await LoginAsync(created.Admin.Email, created.Admin.TemporaryPassword);
        var scope = new ClubScope(this, created.Club.Id, admin);
        if (configure is not null) await scope.ConfigureAsync(configure);
        return scope;
    }

    /// <summary>Executa código com acesso direto ao banco (para preparar cenários e verificar restrições).</summary>
    public async Task<T> DbAsync<T>(Guid? clubId, Func<AppDbContext, Task<T>> action)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<RequestContext>().ClubId = clubId;
        return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public async Task<T> ServiceAsync<TService, T>(Guid clubId, Func<TService, Task<T>> action) where TService : notnull
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<RequestContext>();
        (ctx.ClubId, ctx.UserName) = (clubId, "Teste");
        return await action(scope.ServiceProvider.GetRequiredService<TService>());
    }
}

[CollectionDefinition("api")]
public class ApiCollection : ICollectionFixture<ApiFixture>;

/// <summary>Cliente HTTP de teste: serializa como os clientes reais e expõe o erro padronizado da API.</summary>
public class Api(HttpClient http)
{
    public AuthResponse? Auth { get; set; }
    public HttpClient Http => http;

    public void Authorize(string token) => http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (body is not null) request.Content = JsonContent.Create(body, body.GetType(), options: ApiFixture.Json);
        else if (method != HttpMethod.Get && method != HttpMethod.Delete) request.Content = JsonContent.Create(new { });
        return await http.SendAsync(request);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, string url)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{url} → {(int)response.StatusCode}: {text}");
        return JsonSerializer.Deserialize<T>(text, ApiFixture.Json)!;
    }

    public async Task<T> GetAsync<T>(string url) => await ReadAsync<T>(await SendAsync(HttpMethod.Get, url), url);
    public async Task<T> PostAsync<T>(string url, object? body = null) => await ReadAsync<T>(await SendAsync(HttpMethod.Post, url, body), url);
    public async Task<T> PutAsync<T>(string url, object? body = null) => await ReadAsync<T>(await SendAsync(HttpMethod.Put, url, body), url);

    public async Task OkAsync(HttpMethod method, string url, object? body = null)
    {
        var response = await SendAsync(method, url, body);
        Assert.True(response.IsSuccessStatusCode, $"{url} → {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }

    /// <summary>Espera uma falha com o status (e, opcionalmente, o código de erro) informado.</summary>
    public async Task<ApiError> FailAsync(HttpMethod method, string url, HttpStatusCode status, object? body = null, string? code = null)
    {
        var response = await SendAsync(method, url, body);
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == status, $"{method} {url}: esperado {(int)status}, recebido {(int)response.StatusCode}: {text}");
        var error = text.Length > 0 ? JsonSerializer.Deserialize<ApiError>(text, ApiFixture.Json)! : new ApiError("", "");
        if (code is not null) Assert.Equal(code, error.Code);
        return error;
    }
}

public record ApiError(string Code, string Message, Dictionary<string, string[]>? Errors = null);

/// <summary>Um clube isolado para o teste, com atalhos para montar cenários pelos mesmos endpoints usados pelos clientes.</summary>
public class ClubScope(ApiFixture fixture, Guid clubId, Api admin)
{
    private static int _cpfSeed = 100_000_000;
    public Guid ClubId => clubId;
    public Api Admin => admin;
    public ApiFixture Fixture => fixture;

    public async Task<SettingsDto> ConfigureAsync(Action<SettingsDto> configure)
    {
        var settings = await admin.GetAsync<SettingsDto>("/api/configuracoes");
        configure(settings);
        return await admin.PutAsync<SettingsDto>("/api/configuracoes", new { settings, justification = "teste" });
    }

    public static string NewCpf()
    {
        var digits = Interlocked.Increment(ref _cpfSeed).ToString("D9").Select(c => c - '0').ToList();
        for (var j = 9; j < 11; j++)
        {
            var sum = 0;
            for (var i = 0; i < j; i++) sum += digits[i] * (j + 1 - i);
            digits.Add(sum * 10 % 11 % 10);
        }
        return string.Concat(digits);
    }

    public async Task<MemberDto> NewMemberAsync(bool approve = true, string? name = null)
    {
        var member = await admin.PostAsync<MemberDto>("/api/associados", new SaveMemberRequest { FullName = name ?? "Associado " + Guid.NewGuid().ToString("N")[..6], Cpf = NewCpf() });
        return approve ? await admin.PostAsync<MemberDto>($"/api/associados/{member.Id}/aprovar") : member;
    }

    /// <summary>Cria o associado, aprova, gera o acesso e devolve o cliente autenticado como o associado.</summary>
    public async Task<(MemberDto Member, Api Api)> NewMemberWithLoginAsync()
    {
        var member = await NewMemberAsync();
        var access = await admin.PostAsync<TemporaryPasswordDto>($"/api/associados/{member.Id}/acesso", new CreateAccessRequest($"socio-{Guid.NewGuid():N}@teste.local"));
        return (member, await fixture.LoginAsync(access.Email, access.TemporaryPassword));
    }

    public async Task<DependentDto> NewDependentAsync(Guid memberId, string? name = null)
        => await admin.PostAsync<DependentDto>($"/api/dependentes?memberId={memberId}", new SaveDependentRequest { FullName = name ?? "Dependente " + Guid.NewGuid().ToString("N")[..6], Relationship = "Filho(a)" });

    public async Task<(DependentDto Dependent, Api Api)> NewDependentWithLoginAsync(Guid memberId)
    {
        var dependent = await NewDependentAsync(memberId);
        var access = await admin.PostAsync<TemporaryPasswordDto>($"/api/dependentes/{dependent.Id}/acesso", new CreateAccessRequest($"dep-{Guid.NewGuid():N}@teste.local"));
        return (dependent, await fixture.LoginAsync(access.Email, access.TemporaryPassword));
    }

    /// <summary>Cria um funcionário (usuário Staff) com a função padrão informada: "Secretaria", "Financeiro" ou "Portaria".</summary>
    public async Task<Api> NewStaffAsync(string roleName)
    {
        var roles = await admin.GetAsync<List<RoleDto>>("/api/funcoes");
        var access = await admin.PostAsync<TemporaryPasswordDto>("/api/usuarios",
            new CreateUserRequest("Funcionário " + roleName, $"staff-{Guid.NewGuid():N}@teste.local", UserKind.Staff, null, [roles.First(r => r.Name == roleName).Id]));
        return await fixture.LoginAsync(access.Email, access.TemporaryPassword);
    }

    public async Task<Application.Spaces.SpaceDto> SpaceAsync(string name)
        => (await admin.GetAsync<List<Application.Spaces.SpaceDto>>("/api/espacos")).First(s => s.Name == name);

    /// <summary>Próxima data (a partir de amanhã) que cai no dia da semana informado.</summary>
    public static DateOnly Next(DayOfWeek day)
    {
        var date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"))).AddDays(1);
        while (date.DayOfWeek != day) date = date.AddDays(1);
        return date;
    }

    public static DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")));
}
