using Microsoft.AspNetCore.Mvc;
using ProjetoClubes.Api.Autorizacao;
using ProjetoClubes.Application.Assembleias;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Application.Comunicacao;
using ProjetoClubes.Application.Operacional;
using ProjetoClubes.Application.Seguranca;

namespace ProjetoClubes.Api.Controllers;

/// <summary>Mural de comunicados (administração).</summary>
[ApiController]
[Route("api/v1/comunicados")]
[Produces("application/json")]
public sealed class ComunicadosController(IComunicacaoService comunicacao) : ControllerBase
{
    [HttpGet]
    [RequerPermissao(Permissoes.Comunicacao.Visualizar)]
    public Task<IReadOnlyList<ComunicadoDto>> Listar(CancellationToken ct) => comunicacao.ListarAsync(ct);

    /// <summary>Comunicado com quem leu/confirmou e quem ainda não leu.</summary>
    [HttpGet("{id:guid}")]
    [RequerPermissao(Permissoes.Comunicacao.Visualizar)]
    public Task<ComunicadoLeiturasDto> Obter(Guid id, CancellationToken ct) => comunicacao.ObterAsync(id, ct);

    /// <summary>Cria como rascunho, ou já publica com <c>publicar: true</c> (avisa os associados no app).</summary>
    [HttpPost]
    [RequerPermissao(Permissoes.Comunicacao.Gerenciar)]
    public async Task<IActionResult> Criar(SalvarComunicadoRequest r, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await comunicacao.CriarAsync(r, ct));

    [HttpPut("{id:guid}")]
    [RequerPermissao(Permissoes.Comunicacao.Gerenciar)]
    public Task<ComunicadoDto> Alterar(Guid id, SalvarComunicadoRequest r, CancellationToken ct) => comunicacao.AlterarAsync(id, r, ct);

    [HttpPost("{id:guid}/publicar")]
    [RequerPermissao(Permissoes.Comunicacao.Gerenciar)]
    public Task<ComunicadoDto> Publicar(Guid id, CancellationToken ct) => comunicacao.PublicarAsync(id, ct);

    [HttpDelete("{id:guid}")]
    [RequerPermissao(Permissoes.Comunicacao.Gerenciar)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct)
    {
        await comunicacao.ExcluirAsync(id, ct);
        return NoContent();
    }
}

/// <summary>Caixa de notificações do usuário logado no clube atual (app e Web).</summary>
[ApiController]
[Route("api/v1/notificacoes")]
[Produces("application/json")]
public sealed class NotificacoesController(IComunicacaoService comunicacao) : ControllerBase
{
    [HttpGet]
    public Task<CaixaNotificacoesDto> Minhas(CancellationToken ct) => comunicacao.MinhasNotificacoesAsync(ct);

    [HttpPost("{id:guid}/lida")]
    public async Task<IActionResult> MarcarLida(Guid id, CancellationToken ct)
    {
        await comunicacao.MarcarNotificacoesLidasAsync(id, ct);
        return NoContent();
    }

    [HttpPost("lidas")]
    public async Task<IActionResult> MarcarTodas(CancellationToken ct)
    {
        await comunicacao.MarcarNotificacoesLidasAsync(null, ct);
        return NoContent();
    }
}

/// <summary>Funcionários, férias/afastamentos e histórico.</summary>
[ApiController]
[Route("api/v1/funcionarios")]
[Produces("application/json")]
[RequerPermissao(Permissoes.Operacional.Funcionarios)]
public sealed class FuncionariosController(IFuncionarioService funcionarios) : ControllerBase
{
    /// <summary>Sem filtro de situação, lista os não desligados. A situação (férias/afastado) é a do dia.</summary>
    [HttpGet]
    public Task<Pagina<FuncionarioListaDto>> Listar([FromQuery] ConsultaFuncionarios c, CancellationToken ct) => funcionarios.ListarAsync(c, ct);

    [HttpGet("{id:guid}")]
    public Task<FuncionarioDto> Obter(Guid id, CancellationToken ct) => funcionarios.ObterAsync(id, ct);

    [HttpPost]
    public async Task<IActionResult> Criar(SalvarFuncionarioRequest r, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await funcionarios.CriarAsync(r, ct));

    [HttpPut("{id:guid}")]
    public Task<FuncionarioDto> Alterar(Guid id, SalvarFuncionarioRequest r, CancellationToken ct) => funcionarios.AlterarAsync(id, r, ct);

    [HttpPost("{id:guid}/ausencias")]
    public Task<FuncionarioDto> RegistrarAusencia(Guid id, RegistrarAusenciaRequest r, CancellationToken ct) =>
        funcionarios.RegistrarAusenciaAsync(id, r, ct);

    [HttpDelete("{id:guid}/ausencias/{ausenciaId:guid}")]
    public Task<FuncionarioDto> CancelarAusencia(Guid id, Guid ausenciaId, CancellationToken ct) =>
        funcionarios.CancelarAusenciaAsync(id, ausenciaId, ct);

    [HttpPost("{id:guid}/desligar")]
    public Task<FuncionarioDto> Desligar(Guid id, DataMotivoRequest r, CancellationToken ct) => funcionarios.DesligarAsync(id, r, ct);

    [HttpPost("{id:guid}/readmitir")]
    public Task<FuncionarioDto> Readmitir(Guid id, DataMotivoRequest r, CancellationToken ct) => funcionarios.ReadmitirAsync(id, r, ct);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct)
    {
        await funcionarios.ExcluirAsync(id, ct);
        return NoContent();
    }
}

/// <summary>Patrimônio: bens, localização, estado, manutenções e baixa.</summary>
[ApiController]
[Route("api/v1/patrimonio")]
[Produces("application/json")]
[RequerPermissao(Permissoes.Operacional.Patrimonio)]
public sealed class PatrimonioController(IPatrimonioService patrimonio) : ControllerBase
{
    [HttpGet]
    public Task<Pagina<BemListaDto>> Listar([FromQuery] ConsultaBens c, CancellationToken ct) => patrimonio.ListarAsync(c, ct);

    [HttpGet("resumo")]
    public Task<ResumoPatrimonioDto> Resumo(CancellationToken ct) => patrimonio.ResumoAsync(ct);

    [HttpGet("{id:guid}")]
    public Task<BemDto> Obter(Guid id, CancellationToken ct) => patrimonio.ObterAsync(id, ct);

    [HttpPost]
    public async Task<IActionResult> Criar(SalvarBemRequest r, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await patrimonio.CriarAsync(r, ct));

    [HttpPut("{id:guid}")]
    public Task<BemDto> Alterar(Guid id, SalvarBemRequest r, CancellationToken ct) => patrimonio.AlterarAsync(id, r, ct);

    [HttpPost("{id:guid}/movimentar")]
    public Task<BemDto> Movimentar(Guid id, MovimentarBemRequest r, CancellationToken ct) => patrimonio.MovimentarAsync(id, r, ct);

    [HttpPost("{id:guid}/manutencoes")]
    public Task<BemDto> AbrirManutencao(Guid id, AbrirManutencaoRequest r, CancellationToken ct) => patrimonio.AbrirManutencaoAsync(id, r, ct);

    [HttpPost("{id:guid}/manutencoes/{manutencaoId:guid}/concluir")]
    public Task<BemDto> ConcluirManutencao(Guid id, Guid manutencaoId, ConcluirManutencaoRequest r, CancellationToken ct) =>
        patrimonio.ConcluirManutencaoAsync(id, manutencaoId, r, ct);

    [HttpPost("{id:guid}/baixar")]
    public Task<BemDto> Baixar(Guid id, MotivoTextoRequest r, CancellationToken ct) => patrimonio.BaixarAsync(id, r.Motivo, ct);
}

/// <summary>Estoque: produtos, movimentações e solicitações de compra.</summary>
[ApiController]
[Route("api/v1/estoque")]
[Produces("application/json")]
[RequerPermissao(Permissoes.Operacional.Estoque)]
public sealed class EstoqueController(IEstoqueService estoque) : ControllerBase
{
    [HttpGet("produtos")]
    public Task<IReadOnlyList<ProdutoDto>> Produtos([FromQuery] string? busca, [FromQuery] bool abaixoDoMinimo = false,
        [FromQuery] bool incluirInativos = false, CancellationToken ct = default) => estoque.ListarAsync(busca, abaixoDoMinimo, incluirInativos, ct);

    [HttpPost("produtos")]
    public async Task<IActionResult> CriarProduto(SalvarProdutoRequest r, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await estoque.CriarAsync(r, ct));

    [HttpPut("produtos/{id:guid}")]
    public Task<ProdutoDto> AlterarProduto(Guid id, SalvarProdutoRequest r, CancellationToken ct) => estoque.AlterarAsync(id, r, ct);

    /// <summary>Entrada, saída ou ajuste (saldo contado). Saída maior que o saldo é recusada.</summary>
    [HttpPost("produtos/{id:guid}/movimentacoes")]
    public Task<ProdutoDto> Movimentar(Guid id, MovimentarEstoqueRequest r, CancellationToken ct) => estoque.MovimentarAsync(id, r, ct);

    [HttpGet("movimentacoes")]
    public Task<IReadOnlyList<MovimentacaoEstoqueDto>> Movimentacoes([FromQuery] Guid? produtoId, [FromQuery] DateOnly? de,
        [FromQuery] DateOnly? ate, CancellationToken ct) => estoque.MovimentacoesAsync(produtoId, de, ate, ct);

    [HttpGet("compras")]
    public Task<IReadOnlyList<SolicitacaoCompraDto>> Compras([FromQuery] bool somenteAbertas = false, CancellationToken ct = default) =>
        estoque.ComprasAsync(somenteAbertas, ct);

    [HttpPost("compras")]
    public async Task<IActionResult> SolicitarCompra(CriarSolicitacaoCompraRequest r, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await estoque.SolicitarCompraAsync(r, ct));

    /// <summary>Gera pedidos de compra para todos os produtos abaixo do mínimo sem pedido aberto.</summary>
    [HttpPost("compras/abaixo-do-minimo")]
    public Task<IReadOnlyList<SolicitacaoCompraDto>> GerarCompras(CancellationToken ct) => estoque.SolicitarComprasAbaixoDoMinimoAsync(ct);

    /// <summary>Recebe a compra e dá entrada no estoque.</summary>
    [HttpPost("compras/{id:guid}/atender")]
    public Task<SolicitacaoCompraDto> Atender(Guid id, AtenderSolicitacaoCompraRequest r, CancellationToken ct) => estoque.AtenderCompraAsync(id, r, ct);

    [HttpPost("compras/{id:guid}/cancelar")]
    public Task<SolicitacaoCompraDto> CancelarCompra(Guid id, CancellationToken ct) => estoque.CancelarCompraAsync(id, ct);
}

/// <summary>Assembleias: convocação, pautas, presença, votações, decisões e ata.</summary>
[ApiController]
[Route("api/v1/assembleias")]
[Produces("application/json")]
public sealed class AssembleiasController(IAssembleiaService assembleias) : ControllerBase
{
    [HttpGet]
    [RequerPermissao(Permissoes.Assembleias.Visualizar)]
    public Task<IReadOnlyList<AssembleiaListaDto>> Listar(CancellationToken ct) => assembleias.ListarAsync(ct);

    [HttpGet("{id:guid}")]
    [RequerPermissao(Permissoes.Assembleias.Visualizar)]
    public Task<AssembleiaDto> Obter(Guid id, CancellationToken ct) => assembleias.ObterAsync(id, ct);

    [HttpPost]
    [RequerPermissao(Permissoes.Assembleias.Gerenciar)]
    public async Task<IActionResult> Criar(SalvarAssembleiaRequest r, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await assembleias.CriarAsync(r, ct));

    [HttpPut("{id:guid}")]
    [RequerPermissao(Permissoes.Assembleias.Gerenciar)]
    public Task<AssembleiaDto> Alterar(Guid id, SalvarAssembleiaRequest r, CancellationToken ct) => assembleias.AlterarAsync(id, r, ct);

    [HttpPost("{id:guid}/pautas")]
    [RequerPermissao(Permissoes.Assembleias.Gerenciar)]
    public Task<AssembleiaDto> AdicionarPauta(Guid id, PautaRequest r, CancellationToken ct) => assembleias.AdicionarPautaAsync(id, r, ct);

    [HttpDelete("{id:guid}/pautas/{pautaId:guid}")]
    [RequerPermissao(Permissoes.Assembleias.Gerenciar)]
    public Task<AssembleiaDto> RemoverPauta(Guid id, Guid pautaId, CancellationToken ct) => assembleias.RemoverPautaAsync(id, pautaId, ct);

    /// <summary>Publica a convocação e avisa os associados titulares no app.</summary>
    [HttpPost("{id:guid}/convocar")]
    [RequerPermissao(Permissoes.Assembleias.Gerenciar)]
    public Task<AssembleiaDto> Convocar(Guid id, CancellationToken ct) => assembleias.ConvocarAsync(id, ct);

    [HttpPost("{id:guid}/instalar")]
    [RequerPermissao(Permissoes.Assembleias.Gerenciar)]
    public Task<AssembleiaDto> Instalar(Guid id, CancellationToken ct) => assembleias.InstalarAsync(id, ct);

    /// <summary>Presença pelo QR da carteirinha (<c>codigo</c>) ou pelo associado (<c>associadoId</c>). Dependentes são recusados.</summary>
    [HttpPost("{id:guid}/presencas")]
    [RequerPermissao(Permissoes.Assembleias.Gerenciar)]
    public async Task<IActionResult> Presenca(Guid id, RegistrarPresencaAssembleiaRequest r, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await assembleias.RegistrarPresencaAsync(id, r, ct));

    [HttpPost("{id:guid}/pautas/{pautaId:guid}/abrir-votacao")]
    [RequerPermissao(Permissoes.Assembleias.Gerenciar)]
    public Task<AssembleiaDto> AbrirVotacao(Guid id, Guid pautaId, CancellationToken ct) => assembleias.AbrirVotacaoAsync(id, pautaId, ct);

    [HttpPost("{id:guid}/pautas/{pautaId:guid}/encerrar-votacao")]
    [RequerPermissao(Permissoes.Assembleias.Gerenciar)]
    public Task<AssembleiaDto> EncerrarVotacao(Guid id, Guid pautaId, CancellationToken ct) => assembleias.EncerrarVotacaoAsync(id, pautaId, ct);

    [HttpPost("{id:guid}/pautas/{pautaId:guid}/decisao")]
    [RequerPermissao(Permissoes.Assembleias.Gerenciar)]
    public Task<AssembleiaDto> Decisao(Guid id, Guid pautaId, TextoRequest r, CancellationToken ct) =>
        assembleias.RegistrarDecisaoAsync(id, pautaId, r.Texto, ct);

    [HttpPost("{id:guid}/encerrar")]
    [RequerPermissao(Permissoes.Assembleias.Gerenciar)]
    public Task<AssembleiaDto> Encerrar(Guid id, CancellationToken ct) => assembleias.EncerrarAsync(id, ct);

    /// <summary>Registra a ata (uma vez; depois não muda).</summary>
    [HttpPost("{id:guid}/ata")]
    [RequerPermissao(Permissoes.Assembleias.Gerenciar)]
    public Task<AssembleiaDto> Ata(Guid id, TextoRequest r, CancellationToken ct) => assembleias.RegistrarAtaAsync(id, r.Texto, ct);

    [HttpPost("{id:guid}/cancelar")]
    [RequerPermissao(Permissoes.Assembleias.Gerenciar)]
    public Task<AssembleiaDto> Cancelar(Guid id, MotivoTextoRequest r, CancellationToken ct) => assembleias.CancelarAsync(id, r.Motivo, ct);
}

/// <summary>Comunicados e assembleias no aplicativo do associado/dependente.</summary>
[ApiController]
[Route("api/v1/portal")]
[Produces("application/json")]
[RequerPermissao(Permissoes.Portal.Acessar)]
public sealed class PortalComunicacaoController(IComunicacaoService comunicacao, IAssembleiaService assembleias) : ControllerBase
{
    [HttpGet("comunicados")]
    public Task<IReadOnlyList<ComunicadoPortalDto>> Comunicados(CancellationToken ct) => comunicacao.ComunicadosPortalAsync(ct);

    /// <summary>Marca como lido; com <c>confirmar=true</c>, registra a confirmação de leitura.</summary>
    [HttpPost("comunicados/{id:guid}/leitura")]
    public Task<ComunicadoPortalDto> Ler(Guid id, [FromQuery] bool confirmar = false, CancellationToken ct = default) =>
        comunicacao.MarcarLidoAsync(id, confirmar, ct);

    /// <summary>Assembleias para o titular (dependentes recebem lista vazia).</summary>
    [HttpGet("assembleias")]
    public Task<IReadOnlyList<AssembleiaPortalDto>> Assembleias(CancellationToken ct) => assembleias.PortalAsync(ct);

    /// <summary>Presença pelo aplicativo (assembleia em andamento que permite participação pelo app).</summary>
    [HttpPost("assembleias/{id:guid}/participar")]
    public Task<AssembleiaPortalDto> Participar(Guid id, CancellationToken ct) => assembleias.ParticiparPeloAppAsync(id, ct);

    [HttpPost("assembleias/{id:guid}/pautas/{pautaId:guid}/voto")]
    public Task<AssembleiaPortalDto> Votar(Guid id, Guid pautaId, VotarRequest r, CancellationToken ct) =>
        assembleias.VotarAsync(id, pautaId, r.Opcao, ct);
}
