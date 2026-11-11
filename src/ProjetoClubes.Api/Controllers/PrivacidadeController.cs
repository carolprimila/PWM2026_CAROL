using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using ProjetoClubes.Api.Autorizacao;
using ProjetoClubes.Application.Privacidade;
using ProjetoClubes.Application.Seguranca;

namespace ProjetoClubes.Api.Controllers;

/// <summary>LGPD: exportação dos dados pessoais e anonimização.</summary>
[ApiController]
[Produces("application/json")]
public sealed class PrivacidadeController(IPrivacidadeService privacidade) : ControllerBase
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>App: baixa um arquivo JSON com todos os dados que o clube guarda sobre o usuário.</summary>
    [HttpGet("api/v1/portal/meus-dados")]
    [RequerPermissao(Permissoes.Portal.Acessar)]
    public async Task<IActionResult> MeusDados(CancellationToken ct) =>
        Arquivo(await privacidade.MeusDadosAsync(ct), "meus-dados");

    /// <summary>Administração: dados de uma pessoa, para atender a um pedido do titular.</summary>
    [HttpGet("api/v1/pessoas/{pessoaId:guid}/dados-pessoais")]
    public async Task<IActionResult> DadosDaPessoa(Guid pessoaId, CancellationToken ct) =>
        Arquivo(await privacidade.DadosDaPessoaAsync(pessoaId, ct), $"dados-{pessoaId:N}");

    /// <summary>
    /// Apaga os dados pessoais (nome, CPF, contatos, endereço, foto, documentos e login) de quem não tem mais vínculo vigente.
    /// Irreversível. Exige permissão de excluir associados ou dependentes.
    /// </summary>
    [HttpPost("api/v1/pessoas/{pessoaId:guid}/anonimizar")]
    public Task<AnonimizacaoDto> Anonimizar(Guid pessoaId, AnonimizarRequest r, CancellationToken ct) =>
        privacidade.AnonimizarAsync(pessoaId, r, ct);

    private FileContentResult Arquivo(object dados, string nome) =>
        File(JsonSerializer.SerializeToUtf8Bytes(dados, Json), "application/json", $"{nome}-{DateTime.UtcNow:yyyyMMdd}.json");
}
