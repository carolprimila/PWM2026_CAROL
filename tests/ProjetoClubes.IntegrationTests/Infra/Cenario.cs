using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjetoClubes.IntegrationTests.Infra;

public sealed record Sessao(string AccessToken, string RefreshToken, JsonElement Usuario)
{
    public Guid UsuarioId => Usuario.GetProperty("id").GetGuid();
    public Guid? ClubeAtualId => Usuario.GetProperty("clubeAtual").ValueKind == JsonValueKind.Null
        ? null
        : Usuario.GetProperty("clubeAtual").GetProperty("id").GetGuid();
    public IReadOnlyList<string> Permissoes => Usuario.GetProperty("permissoes").EnumerateArray().Select(p => p.GetString()!).ToList();
}

public sealed record ClubeCriado(Guid Id, string Sigla, string EmailPresidente, Guid PresidenteId);

/// <summary>Atalhos para montar cenários pelos endpoints reais da API.</summary>
public sealed class Cenario(ApiFixture api)
{
    public const string SenhaPadrao = "SenhaDeTeste2026";
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private static string Sufixo() => Guid.NewGuid().ToString("N")[..8];

    public async Task<Sessao> LoginAsync(string email, string senha = SenhaPadrao, Guid? clubeId = null)
    {
        var r = await api.Cliente().PostAsJsonAsync("/api/v1/auth/login", new { email, senha, clubeId });
        Assert.True(r.StatusCode == HttpStatusCode.OK, $"Login de {email} falhou: {(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        return await LerSessaoAsync(r);
    }

    public static async Task<Sessao> LerSessaoAsync(HttpResponseMessage r)
    {
        var json = await r.Content.ReadFromJsonAsync<JsonElement>(Json);
        return new Sessao(json.GetProperty("accessToken").GetString()!, json.GetProperty("refreshToken").GetString()!,
            json.GetProperty("usuario"));
    }

    public Task<Sessao> SuperAdminAsync() => LoginAsync(ApiFixture.EmailSuperAdmin, ApiFixture.SenhaSuperAdmin);

    /// <summary>Superadmin cria um clube e o presidente dele (com senha inicial).</summary>
    public async Task<ClubeCriado> CriarClubeComPresidenteAsync(string? nome = null)
    {
        var sa = await SuperAdminAsync();
        var sufixo = Sufixo();
        var cliente = api.Cliente(sa.AccessToken);

        var r = await cliente.PostAsJsonAsync("/api/v1/clubes", new
        {
            nome = nome ?? $"Clube Teste {sufixo}",
            sigla = $"T{sufixo}".ToUpperInvariant(),
            endereco = new { cidade = "Foz do Iguaçu", uf = "PR" },
        });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var clube = await r.Content.ReadFromJsonAsync<JsonElement>(Json);
        var clubeId = clube.GetProperty("id").GetGuid();

        var email = $"presidente.{sufixo}@testes.local";
        var adm = await cliente.PostAsJsonAsync($"/api/v1/clubes/{clubeId}/administradores",
            new { nome = $"Presidente {sufixo}", email, perfilChave = "presidente", senhaInicial = SenhaPadrao });
        Assert.Equal(HttpStatusCode.Created, adm.StatusCode);
        var usuarioId = (await adm.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("usuarioId").GetGuid();
        return new ClubeCriado(clubeId, clube.GetProperty("sigla").GetString()!, email, usuarioId);
    }

    public async Task<Guid> PerfilIdAsync(string accessToken, string nome)
    {
        var perfis = await api.Cliente(accessToken).GetFromJsonAsync<JsonElement>("/api/v1/perfis", Json);
        return perfis.EnumerateArray().First(p => p.GetProperty("nome").GetString() == nome).GetProperty("id").GetGuid();
    }

    /// <summary>Cria um usuário no clube da sessão informada, com senha inicial, e retorna (id, email).</summary>
    public async Task<(Guid Id, string Email)> CriarUsuarioAsync(Sessao admin, string perfilNome)
    {
        var email = $"usuario.{Sufixo()}@testes.local";
        var perfilId = await PerfilIdAsync(admin.AccessToken, perfilNome);
        var r = await api.Cliente(admin.AccessToken).PostAsJsonAsync("/api/v1/usuarios",
            new { nome = $"Usuário {perfilNome}", email, perfilIds = new[] { perfilId }, senhaInicial = SenhaPadrao });
        Assert.True(r.StatusCode == HttpStatusCode.Created, $"{(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        return ((await r.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("id").GetGuid(), email);
    }

    // ------------------------------------------------------------------ Etapa 2

    /// <summary>CPF válido aleatório.</summary>
    public static string NovoCpf()
    {
        var d = Enumerable.Range(0, 9).Select(_ => Random.Shared.Next(10)).ToList();
        if (d.Distinct().Count() == 1) d[0] = (d[0] + 1) % 10;
        for (var t = 9; t <= 10; t++)
        {
            var soma = 0;
            for (var i = 0; i < t; i++) soma += d[i] * (t + 1 - i);
            var r = soma % 11;
            d.Add(r < 2 ? 0 : 11 - r);
        }
        return string.Concat(d);
    }

    public static object DadosPessoa(string? nome = null, string? cpf = null, int idade = 35, string? email = null) => new
    {
        nomeCompleto = nome ?? $"Pessoa Teste {Sufixo()}",
        cpf,
        dataNascimento = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-idade).AddDays(-10).ToString("yyyy-MM-dd"),
        sexo = "NaoInformado",
        email,
        endereco = new { cidade = "Foz do Iguaçu", uf = "PR", cep = "85851-000" },
        telefone = "45999990000",
    };

    public async Task<Guid> CategoriaIdAsync(string token, string nome)
    {
        var lista = await api.Cliente(token).GetFromJsonAsync<JsonElement>("/api/v1/categorias-associados", Json);
        return lista.EnumerateArray().First(c => c.GetProperty("nome").GetString() == nome).GetProperty("id").GetGuid();
    }

    /// <summary>Cria associado (ativo quando quem cria pode aprovar) e retorna o JSON de detalhe.</summary>
    public async Task<JsonElement> CriarAssociadoAsync(string token, string categoria = "Sócio associado", string? email = null)
    {
        var r = await api.Cliente(token).PostAsJsonAsync("/api/v1/associados", new
        {
            pessoa = DadosPessoa(cpf: NovoCpf(), email: email),
            categoriaId = await CategoriaIdAsync(token, categoria),
        });
        Assert.True(r.StatusCode == HttpStatusCode.Created, $"{(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        return await r.Content.ReadFromJsonAsync<JsonElement>(Json);
    }

    public async Task<HttpResponseMessage> PostDependenteAsync(string token, Guid titularId, string parentesco = "Filho", int idade = 10,
        string? cpf = null, string? justificativa = null, string? email = null) =>
        await api.Cliente(token).PostAsJsonAsync("/api/v1/dependentes", new
        {
            associadoTitularId = titularId,
            pessoa = DadosPessoa(cpf: cpf, idade: idade, email: email),
            parentesco,
            justificativaExcecaoMultiplosTitulares = justificativa,
        });

    public async Task<JsonElement> CriarDependenteAsync(string token, Guid titularId, string parentesco = "Filho", int idade = 10,
        string? cpf = null, string? email = null)
    {
        var r = await PostDependenteAsync(token, titularId, parentesco, idade, cpf, email: email);
        Assert.True(r.StatusCode == HttpStatusCode.Created, $"{(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        return await r.Content.ReadFromJsonAsync<JsonElement>(Json);
    }

    public static async Task<string> CodigoProblemaAsync(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("codigo").GetString()!;

    /// <summary>Cria associado com login no app e retorna (sessão do presidente, sessão do associado, id do associado).</summary>
    public async Task<(Sessao Associado, Guid AssociadoId, Guid PessoaId)> AssociadoComAppAsync(Sessao presidente, string categoria = "Sócio associado")
    {
        var email = $"app.{Sufixo()}@testes.local";
        var a = await CriarAssociadoAsync(presidente.AccessToken, categoria, email);
        var r = await api.Cliente(presidente.AccessToken).PostAsJsonAsync($"/api/v1/associados/{a.GetProperty("id")}/acesso-app",
            new { senhaInicial = SenhaPadrao });
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        return (await LoginAsync(email), a.GetProperty("id").GetGuid(), a.GetProperty("pessoa").GetProperty("id").GetGuid());
    }
}
