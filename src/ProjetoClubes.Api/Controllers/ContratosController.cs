using Microsoft.AspNetCore.Mvc;
using ProjetoClubes.Api.Autorizacao;
using ProjetoClubes.Application.Cadastros;
using ProjetoClubes.Application.Seguranca;

namespace ProjetoClubes.Api.Controllers;

/// <summary>Contrato anual do associado (gera as parcelas) e passagem de dependente a associado.</summary>
[ApiController]
[Produces("application/json")]
public sealed class ContratosController(IContratoService contratos, IAssociadoService associados) : ControllerBase
{
    [HttpGet("api/v1/associados/{associadoId:guid}/contratos")]
    [RequerPermissao(Permissoes.Associados.Visualizar)]
    public Task<IReadOnlyList<ContratoDto>> Listar(Guid associadoId, CancellationToken ct) => contratos.ListarAsync(associadoId, ct);

    /// <summary>Novo contrato (em análise). Sem valor/dia, usa o plano da categoria (+ adicional de dependentes).</summary>
    [HttpPost("api/v1/associados/{associadoId:guid}/contratos")]
    [RequerPermissao(Permissoes.Associados.Cadastrar)]
    public async Task<IActionResult> Criar(Guid associadoId, CriarContratoRequest r, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await contratos.CriarAsync(associadoId, r, ct));

    /// <summary>Aprova e gera as 12 parcelas (mensalidades) do contrato.</summary>
    [HttpPost("api/v1/contratos/{id:guid}/aprovar")]
    [RequerPermissao(Permissoes.Associados.Aprovar)]
    public Task<ContratoDto> Aprovar(Guid id, CancellationToken ct) => contratos.AprovarAsync(id, ct);

    [HttpPost("api/v1/contratos/{id:guid}/reprovar")]
    [RequerPermissao(Permissoes.Associados.Aprovar)]
    public Task<ContratoDto> Reprovar(Guid id, MotivoContratoRequest r, CancellationToken ct) => contratos.ReprovarAsync(id, r.Motivo, ct);

    /// <summary>Associado saiu do clube: cancela as parcelas futuras e inativa o cadastro.</summary>
    [HttpPost("api/v1/contratos/{id:guid}/desligar")]
    [RequerPermissao(Permissoes.Associados.Aprovar)]
    public Task<ContratoDto> Desligar(Guid id, MotivoContratoRequest r, CancellationToken ct) => contratos.DesligarAsync(id, r.Motivo, ct);

    /// <summary>Cria o contrato do ano seguinte com os dados atualizados.</summary>
    [HttpPost("api/v1/contratos/{id:guid}/renovar")]
    [RequerPermissao(Permissoes.Associados.Cadastrar)]
    public async Task<IActionResult> Renovar(Guid id, CriarContratoRequest r, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await contratos.RenovarAsync(id, r, ct));

    /// <summary>Dependente maior de idade vira associado titular (mesma pessoa e login).</summary>
    [HttpPost("api/v1/dependentes/{id:guid}/tornar-associado")]
    [RequerPermissao(Permissoes.Associados.Cadastrar)]
    public async Task<IActionResult> TornarAssociado(Guid id, TornarAssociadoRequest r, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await associados.TornarDependenteAssociadoAsync(id, r, ct));
}
