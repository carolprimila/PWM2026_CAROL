using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
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
/// Sobe a API real contra um banco PostgreSQL exclusivo e descartável (criado a partir da mesma conexão de
/// desenvolvimento, com outro nome de banco). Aplica as migrations reais. Nada é simulado em memória.
/// Conexão base: variável PROJETOCLUBES_TESTES_POSTGRES ou user-secrets da API (ConnectionStrings:Postgres).
/// </summary>
public sealed class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string TokenSetup = "token-de-inicializacao-dos-testes";
    public const string SenhaSuperAdmin = "SuperAdmin#Teste2026";
    public const string EmailSuperAdmin = "superadmin@testes.projetoclubes.local";

    private readonly string _conexao;
    private readonly string _banco = $"projetoclubes_testes_{Guid.NewGuid():N}"[..34];

    public EmailCapturado Emails { get; } = new();

    public string PastaArquivos { get; } = Path.Combine(Path.GetTempPath(), "projetoclubes-testes", Guid.NewGuid().ToString("N"));

    public ApiFixture()
    {
        var config = new ConfigurationBuilder()
            .AddUserSecrets<Program>(optional: true)
            .AddEnvironmentVariables()
            .Build();
        var baseConexao = config["PROJETOCLUBES_TESTES_POSTGRES"] ?? config.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Configure PROJETOCLUBES_TESTES_POSTGRES ou o user-secret ConnectionStrings:Postgres da API para rodar os testes de integração.");
        _conexao = new NpgsqlConnectionStringBuilder(baseConexao) { Database = _banco, Pooling = false }.ConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testes");
        builder.UseSetting("ConnectionStrings:Postgres", _conexao);
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
        await using (var c = new NpgsqlConnection(ConexaoAdministrativa()))
        {
            await c.OpenAsync();
            await using var cmd = new NpgsqlCommand($"CREATE DATABASE \"{_banco}\"", c);
            await cmd.ExecuteNonQueryAsync();
        }

        // Cria o primeiro Superadmin pelo fluxo real de inicialização.
        var resposta = await CreateClient().PostAsJsonAsync("/api/v1/setup/inicializar",
            new { tokenInicializacao = TokenSetup, nome = "Superadmin Testes", email = EmailSuperAdmin, senha = SenhaSuperAdmin });
        resposta.EnsureSuccessStatusCode();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        if (Directory.Exists(PastaArquivos)) Directory.Delete(PastaArquivos, recursive: true);
        NpgsqlConnection.ClearAllPools();
        await using var c = new NpgsqlConnection(ConexaoAdministrativa());
        await c.OpenAsync();
        await using var cmd = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_banco}\" WITH (FORCE)", c);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>Executa SQL direto no banco de teste (para provar garantias do banco sem passar pela API).</summary>
    public async Task<int> ExecutarSqlAsync(string sql)
    {
        await using var c = new NpgsqlConnection(_conexao);
        await c.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, c);
        return await cmd.ExecuteNonQueryAsync();
    }

    private string ConexaoAdministrativa() =>
        new NpgsqlConnectionStringBuilder(_conexao) { Database = "postgres" }.ConnectionString;

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
