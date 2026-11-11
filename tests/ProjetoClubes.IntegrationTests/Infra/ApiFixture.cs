using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Data.Sqlite;
using ProjetoClubes.Application.Common;

namespace ProjetoClubes.IntegrationTests.Infra;

/// <summary>E-mail capturado em memória (para obter links de recuperação/convite nos testes).</summary>
public sealed class EmailCapturado : IEmailSender
{
    public ConcurrentQueue<MensagemEmail> Mensagens { get; } = new();

    public Task EnviarAsync(MensagemEmail mensagem, CancellationToken ct = default)
    {
        Mensagens.Enqueue(mensagem);
        return Task.CompletedTask;
    }

    public string UltimoToken(string para)
    {
        var msg = Mensagens.Last(m => m.Para.Equals(para, StringComparison.OrdinalIgnoreCase));
        var inicio = msg.CorpoTexto.IndexOf("token=", StringComparison.Ordinal) + "token=".Length;
        var fim = msg.CorpoTexto.IndexOfAny(['&', '\n', '\r', ' '], inicio);
        return Uri.UnescapeDataString(fim < 0 ? msg.CorpoTexto[inicio..] : msg.CorpoTexto[inicio..fim]);
    }
}

/// <summary>
/// Sobe a API real contra um arquivo SQLite exclusivo e descartável (na pasta temporária), aplicando as migrations reais.
/// Nada é simulado em memória e não há nada para configurar: roda em qualquer máquina com o .NET SDK.
/// </summary>
public sealed class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string TokenSetup = "token-de-inicializacao-dos-testes";
    public const string SenhaSuperAdmin = "SuperAdmin#Teste2026";
    public const string EmailSuperAdmin = "superadmin@testes.projetoclubes.local";

    private readonly string _arquivoBanco = Path.Combine(Path.GetTempPath(), "projetoclubes-testes", $"testes-{Guid.NewGuid():N}.db");
    private string Conexao => $"Data Source={_arquivoBanco};Pooling=False";

    public EmailCapturado Emails { get; } = new();

    public string PastaArquivos { get; } = Path.Combine(Path.GetTempPath(), "projetoclubes-testes", Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testes");
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_arquivoBanco}");
        builder.UseSetting("Jwt:Chave", "chave-jwt-exclusiva-dos-testes-de-integracao-0123456789");
        builder.UseSetting("Banco:AplicarMigrationsAoIniciar", "true");
        builder.UseSetting("Seed:Demo", "false");
        builder.UseSetting("Setup:Token", TokenSetup);
        builder.UseSetting("LimiteRequisicoes:AutenticacaoPorMinuto", "10000");
        builder.UseSetting("LimiteRequisicoes:GeralPorMinuto", "100000");
        builder.UseSetting("Serilog:MinimumLevel:Default", "Warning");
        builder.UseSetting("Arquivos:Pasta", PastaArquivos);
        builder.ConfigureServices(s =>
        {
            s.RemoveAll<IEmailSender>();
            s.AddSingleton<IEmailSender>(Emails);
        });
    }

    public async Task InitializeAsync()
    {
        // Cria o primeiro Superadmin pelo fluxo real de inicialização.
        var resposta = await CreateClient().PostAsJsonAsync("/api/v1/setup/inicializar",
            new { tokenInicializacao = TokenSetup, nome = "Superadmin Testes", email = EmailSuperAdmin, senha = SenhaSuperAdmin });
        resposta.EnsureSuccessStatusCode();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        if (Directory.Exists(PastaArquivos)) Directory.Delete(PastaArquivos, recursive: true);
        SqliteConnection.ClearAllPools();
        foreach (var arquivo in new[] { _arquivoBanco, _arquivoBanco + "-wal", _arquivoBanco + "-shm" })
            try { File.Delete(arquivo); } catch (IOException) { /* fica na pasta temporária */ }
    }

    /// <summary>Executa SQL direto no banco de teste (para provar garantias do banco sem passar pela API).</summary>
    public async Task<int> ExecutarSqlAsync(string sql)
    {
        await using var c = new SqliteConnection(Conexao);
        await c.OpenAsync();
        await using var cmd = new SqliteCommand(sql, c);
        return await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>Cliente HTTP autenticado com o access token informado.</summary>
    public HttpClient Cliente(string? accessToken = null)
    {
        var client = CreateClient();
        if (accessToken is not null) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }
}

[CollectionDefinition(Nome)]
public sealed class ColecaoApi : ICollectionFixture<ApiFixture>
{
    public const string Nome = "API";
}
