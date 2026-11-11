using Microsoft.AspNetCore.Mvc;
using ProjetoClubes.Api.Autorizacao;
using ProjetoClubes.Application.Clubes;
using ProjetoClubes.Application.Seguranca;

namespace ProjetoClubes.Api.Controllers;

/// <summary>Diretoria do clube selecionado (presidente com mandato de 2 anos, diretores e demais cargos).</summary>
[ApiController]
[Route("api/v1/diretoria")]
[Produces("application/json")]
public sealed class DiretoriaController(IDiretoriaService diretoria) : ControllerBase
{
    /// <summary>Diretoria atual, anteriores e aviso sobre o mandato do presidente.</summary>
    [HttpGet]
    [RequerPermissao(Permissoes.Clubes.Visualizar)]
    public Task<DiretoriaDto> Obter(CancellationToken ct) => diretoria.ObterAsync(ct);

    [HttpPost]
    [RequerPermissao(Permissoes.Clubes.Editar)]
    public async Task<IActionResult> Adicionar(SalvarMembroDiretoriaRequest r, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await diretoria.AdicionarAsync(r, ct));

    /// <summary>Encerra o mandato hoje (renúncia ou substituição).</summary>
    [HttpPost("{id:guid}/encerrar")]
    [RequerPermissao(Permissoes.Clubes.Editar)]
    public Task<DiretoriaDto> Encerrar(Guid id, CancellationToken ct) => diretoria.EncerrarAsync(id, ct);
}
