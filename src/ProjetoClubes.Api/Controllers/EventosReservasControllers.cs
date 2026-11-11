using Microsoft.AspNetCore.Mvc;
using ProjetoClubes.Api.Autorizacao;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Application.Eventos;
using ProjetoClubes.Application.Financeiro;
using ProjetoClubes.Application.Reservas;
using ProjetoClubes.Application.Seguranca;
using ProjetoClubes.Application.Solicitacoes;

namespace ProjetoClubes.Api.Controllers;

/// <summary>Eventos do clube, inscrições e presença.</summary>
[ApiController]
[Route("api/v1/eventos")]
[Produces("application/json")]
public sealed class EventosController(IEventoService eventos) : ControllerBase
{
    /// <summary>Filtros: futuros, situacao, busca.</summary>
    [HttpGet]
    [RequerPermissao(Permissoes.Eventos.Visualizar)]
    public Task<Pagina<EventoDto>> Listar([FromQuery] ConsultaEventos c, CancellationToken ct) => eventos.ListarAsync(c, ct);

    [HttpGet("{id:guid}")]
    [RequerPermissao(Permissoes.Eventos.Visualizar)]
    public Task<EventoDetalheDto> Obter(Guid id, CancellationToken ct) => eventos.ObterAsync(id, ct);

    /// <summary>Cria o evento. Com espaço informado, o período fica reservado (sem conflito com outras reservas).</summary>
    [HttpPost]
    [RequerPermissao(Permissoes.Eventos.Gerenciar)]
    public async Task<IActionResult> Criar(SalvarEventoRequest r, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await eventos.SalvarAsync(null, r, ct));

    [HttpPut("{id:guid}")]
    [RequerPermissao(Permissoes.Eventos.Gerenciar)]
    public Task<EventoDto> Atualizar(Guid id, SalvarEventoRequest r, CancellationToken ct) => eventos.SalvarAsync(id, r, ct);

    [HttpPost("{id:guid}/encerrar-inscricoes")]
    [RequerPermissao(Permissoes.Eventos.Gerenciar)]
    public Task<EventoDto> EncerrarInscricoes(Guid id, CancellationToken ct) => eventos.AlterarInscricoesAsync(id, false, ct);

    [HttpPost("{id:guid}/reabrir-inscricoes")]
    [RequerPermissao(Permissoes.Eventos.Gerenciar)]
    public Task<EventoDto> ReabrirInscricoes(Guid id, CancellationToken ct) => eventos.AlterarInscricoesAsync(id, true, ct);

    [HttpPost("{id:guid}/cancelar")]
    [RequerPermissao(Permissoes.Eventos.Gerenciar)]
    public Task<EventoDto> Cancelar(Guid id, MotivoRequest r, CancellationToken ct) => eventos.CancelarAsync(id, r.Motivo, ct);

    /// <summary>Inscreve um associado ou dependente (confirmada ou lista de espera, conforme as vagas).</summary>
    [HttpPost("{id:guid}/inscricoes")]
    [RequerPermissao(Permissoes.Eventos.Gerenciar)]
    public async Task<IActionResult> Inscrever(Guid id, InscreverRequest r, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await eventos.InscreverAsync(id, r, ct));

    /// <summary>Cancela uma inscrição (administração, a própria pessoa ou o titular) e promove a lista de espera.</summary>
    [HttpDelete("inscricoes/{inscricaoId:guid}")]
    public async Task<IActionResult> CancelarInscricao(Guid inscricaoId, CancellationToken ct)
    {
        await eventos.CancelarInscricaoAsync(inscricaoId, ct);
        return NoContent();
    }

    /// <summary>Confirma presença lendo o QR Code da carteirinha na entrada do evento.</summary>
    [HttpPost("{id:guid}/presenca")]
    public Task<InscricaoDto> Presenca(Guid id, PresencaRequest r, CancellationToken ct) => eventos.RegistrarPresencaAsync(id, r, ct);
}

/// <summary>Espaços físicos do clube.</summary>
[ApiController]
[Route("api/v1/espacos")]
[Produces("application/json")]
public sealed class EspacosController(IReservaService reservas) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<EspacoDto>> Listar([FromQuery] bool incluirInativos = false, CancellationToken ct = default) =>
        reservas.ListarEspacosAsync(incluirInativos, ct);

    [HttpPost]
    [RequerPermissao(Permissoes.Reservas.GerenciarEspacos)]
    public async Task<IActionResult> Criar(SalvarEspacoRequest r, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await reservas.SalvarEspacoAsync(null, r, ct));

    [HttpPut("{id:guid}")]
    [RequerPermissao(Permissoes.Reservas.GerenciarEspacos)]
    public Task<EspacoDto> Atualizar(Guid id, SalvarEspacoRequest r, CancellationToken ct) => reservas.SalvarEspacoAsync(id, r, ct);

    [HttpDelete("{id:guid}")]
    [RequerPermissao(Permissoes.Reservas.GerenciarEspacos)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct)
    {
        await reservas.ExcluirEspacoAsync(id, ct);
        return NoContent();
    }
}

/// <summary>Reservas e calendário de locações.</summary>
[ApiController]
[Route("api/v1/reservas")]
[Produces("application/json")]
public sealed class ReservasController(IReservaService reservas) : ControllerBase
{
    [HttpGet]
    [RequerPermissao(Permissoes.Reservas.Visualizar)]
    public Task<Pagina<ReservaDto>> Listar([FromQuery] ConsultaReservas c, CancellationToken ct) => reservas.ListarAsync(c, ct);

    /// <summary>Ocupação dos espaços no período (reservas e eventos). No app, sem dados de quem reservou.</summary>
    [HttpGet("calendario")]
    public Task<IReadOnlyList<ItemCalendarioDto>> Calendario([FromQuery] DateTimeOffset de, [FromQuery] DateTimeOffset ate,
        [FromQuery] Guid? espacoId, CancellationToken ct) => reservas.CalendarioAsync(de, ate, espacoId, ct);

    /// <summary>Reserva feita pela administração (já aprovada).</summary>
    [HttpPost]
    [RequerPermissao(Permissoes.Reservas.Aprovar)]
    public async Task<IActionResult> Criar(SolicitarReservaRequest r, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await reservas.CriarAsync(r, ct));

    [HttpPut("{id:guid}")]
    [RequerPermissao(Permissoes.Reservas.Aprovar)]
    public Task<ReservaDto> Alterar(Guid id, AlterarReservaRequest r, CancellationToken ct) => reservas.AlterarAsync(id, r, ct);

    [HttpPost("{id:guid}/aprovar")]
    [RequerPermissao(Permissoes.Reservas.Aprovar)]
    public Task<ReservaDto> Aprovar(Guid id, CancellationToken ct) => reservas.AprovarAsync(id, ct);

    [HttpPost("{id:guid}/recusar")]
    [RequerPermissao(Permissoes.Reservas.Aprovar)]
    public Task<ReservaDto> Recusar(Guid id, MotivoRequest r, CancellationToken ct) => reservas.RecusarAsync(id, r.Motivo, ct);

    /// <summary>Cancela (administração ou o próprio solicitante pelo app).</summary>
    [HttpPost("{id:guid}/cancelar")]
    public Task<ReservaDto> Cancelar(Guid id, MotivoRequest r, CancellationToken ct) => reservas.CancelarAsync(id, r.Motivo, ct);
}

/// <summary>Solicitações, reclamações e sugestões (atendimento pela secretaria).</summary>
[ApiController]
[Route("api/v1/solicitacoes")]
[Produces("application/json")]
public sealed class SolicitacoesController(ISolicitacaoService solicitacoes) : ControllerBase
{
    [HttpGet]
    [RequerPermissao(Permissoes.Solicitacoes.Visualizar)]
    public Task<Pagina<SolicitacaoListaDto>> Listar([FromQuery] ConsultaSolicitacoes c, CancellationToken ct) => solicitacoes.ListarAsync(c, ct);

    [HttpGet("{id:guid}")]
    [RequerPermissao(Permissoes.Solicitacoes.Visualizar)]
    public Task<SolicitacaoDetalheDto> Obter(Guid id, CancellationToken ct) => solicitacoes.ObterAsync(id, ct);

    [HttpPost]
    [RequerPermissao(Permissoes.Solicitacoes.Atender)]
    public async Task<IActionResult> Abrir(AbrirSolicitacaoRequest r, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await solicitacoes.AbrirAsync(r, ct));

    /// <summary>Resposta ao associado ou anotação interna (interna: true).</summary>
    [HttpPost("{id:guid}/respostas")]
    [RequerPermissao(Permissoes.Solicitacoes.Atender)]
    public Task<SolicitacaoDetalheDto> Responder(Guid id, ResponderRequest r, CancellationToken ct) => solicitacoes.ResponderAsync(id, r, ct);

    [HttpPut("{id:guid}/situacao")]
    [RequerPermissao(Permissoes.Solicitacoes.Atender)]
    public Task<SolicitacaoDetalheDto> Situacao(Guid id, AlterarSituacaoSolicitacaoRequest r, CancellationToken ct) =>
        solicitacoes.AlterarSituacaoAsync(id, r, ct);
}

/// <summary>Eventos, reservas e solicitações no aplicativo do associado/dependente.</summary>
[ApiController]
[Route("api/v1/portal")]
[Produces("application/json")]
[RequerPermissao(Permissoes.Portal.Acessar)]
public sealed class PortalAtividadesController(IEventoService eventos, IReservaService reservas, ISolicitacaoService solicitacoes)
    : ControllerBase
{
    [HttpGet("eventos")]
    public Task<IReadOnlyList<PortalEventoDto>> Eventos(CancellationToken ct) => eventos.PortalAsync(ct);

    /// <summary>Inscreve a si mesmo ou um dependente (pessoaId) no evento.</summary>
    [HttpPost("eventos/{id:guid}/inscricoes")]
    public async Task<IActionResult> Inscrever(Guid id, InscreverRequest r, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await eventos.InscreverNoPortalAsync(id, r, ct));

    [HttpGet("reservas")]
    public Task<IReadOnlyList<ReservaDto>> Reservas(CancellationToken ct) => reservas.MinhasAsync(ct);

    [HttpPost("reservas")]
    public async Task<IActionResult> Reservar(SolicitarReservaRequest r, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await reservas.SolicitarNoPortalAsync(r, ct));

    [HttpGet("solicitacoes")]
    public Task<IReadOnlyList<SolicitacaoListaDto>> Solicitacoes(CancellationToken ct) => solicitacoes.MinhasAsync(ct);

    [HttpGet("solicitacoes/{id:guid}")]
    public Task<SolicitacaoDetalheDto> Solicitacao(Guid id, CancellationToken ct) => solicitacoes.ObterNoPortalAsync(id, ct);

    [HttpPost("solicitacoes")]
    public async Task<IActionResult> Abrir(AbrirSolicitacaoRequest r, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await solicitacoes.AbrirNoPortalAsync(r, ct));

    [HttpPost("solicitacoes/{id:guid}/respostas")]
    public Task<SolicitacaoDetalheDto> Responder(Guid id, ResponderRequest r, CancellationToken ct) =>
        solicitacoes.ResponderNoPortalAsync(id, r, ct);

    [HttpPost("solicitacoes/{id:guid}/cancelar")]
    public Task<SolicitacaoDetalheDto> CancelarSolicitacao(Guid id, CancellationToken ct) => solicitacoes.CancelarNoPortalAsync(id, ct);
}
