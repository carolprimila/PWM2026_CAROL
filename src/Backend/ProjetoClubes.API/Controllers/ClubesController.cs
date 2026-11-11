using Microsoft.AspNetCore.Mvc;
using ProjetoClubes.Application.Clubes;
using ProjetoClubes.Application.Common;

namespace ProjetoClubes.API.Controllers;

/// <summary>
/// Clubes. Criar, ativar, desativar e adicionar administradores: somente Superadmin.
/// Consultar/editar: usuários do próprio clube conforme permissões (validado no serviço).
/// </summary>
[ApiController]
[Route("api/v1/clubes")]
[Produces("application/json")]
public sealed class ClubesController(IClubeService clubes) : ControllerBase
{
    [HttpGet]
    public Task<Pagina<ClubeListaDto>> Listar([FromQuery] ConsultaClubes consulta, CancellationToken ct) =>
        clubes.ListarAsync(consulta, ct);

    [HttpGet("{id:guid}")]
    public Task<ClubeDetalheDto> Obter(Guid id, CancellationToken ct) => clubes.ObterAsync(id, ct);

    [HttpPost]
    [ProducesResponseType<ClubeDetalheDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(SalvarClubeRequest request, CancellationToken ct)
    {
        var clube = await clubes.CriarAsync(request, ct);
        return CreatedAtAction(nameof(Obter), new { id = clube.Id }, clube);
    }

    [HttpPut("{id:guid}")]
    public Task<ClubeDetalheDto> Atualizar(Guid id, SalvarClubeRequest request, CancellationToken ct) =>
        clubes.AtualizarAsync(id, request, ct);

    [HttpPost("{id:guid}/ativar")]
    public async Task<IActionResult> Ativar(Guid id, CancellationToken ct)
    {
        await clubes.AlterarSituacaoAsync(id, true, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/desativar")]
    public async Task<IActionResult> Desativar(Guid id, CancellationToken ct)
    {
        await clubes.AlterarSituacaoAsync(id, false, ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/configuracoes")]
    public Task<ConfiguracaoClubeDto> AtualizarConfiguracoes(Guid id, AtualizarConfiguracaoClubeRequest request,
        CancellationToken ct) => clubes.AtualizarConfiguracaoAsync(id, request, ct);

    /// <summary>Cria (ou vincula) um administrador do clube com um perfil padrão (ex.: "presidente").</summary>
    [HttpPost("{id:guid}/administradores")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> AdicionarAdministrador(Guid id, AdicionarAdministradorRequest request, CancellationToken ct)
    {
        var usuarioId = await clubes.AdicionarAdministradorAsync(id, request, ct);
        return StatusCode(StatusCodes.Status201Created, new { usuarioId });
    }

    /// <summary>Histórico de alterações do cadastro e das configurações do clube.</summary>
    [HttpGet("{id:guid}/historico")]
    public Task<Pagina<LogAuditoriaDto>> Historico(Guid id, [FromQuery] ConsultaPaginada consulta, CancellationToken ct) =>
        clubes.ListarHistoricoAsync(id, consulta, ct);
}
