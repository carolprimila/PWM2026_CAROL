using Microsoft.AspNetCore.Mvc;
using ProjetoClubes.API.Autorizacao;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Application.Financeiro;
using ProjetoClubes.Application.Seguranca;

namespace ProjetoClubes.API.Controllers;

/// <summary>Financeiro do clube: planos, mensalidades, pagamentos, receitas, despesas e relatórios.</summary>
[ApiController]
[Route("api/v1/financeiro")]
[Produces("application/json")]
public sealed class FinanceiroController(IMensalidadeService mensalidades, ICaixaService caixa) : ControllerBase
{
    // ---------------------------------------------------------------- configuração e planos

    [HttpGet("configuracoes")]
    [RequerPermissao(Permissoes.Financeiro.Visualizar)]
    public Task<ConfiguracaoFinanceiraDto> Configuracoes(CancellationToken ct) => mensalidades.ObterConfiguracaoAsync(ct);

    [HttpPut("configuracoes")]
    [RequerPermissao(Permissoes.Financeiro.Gerenciar)]
    public Task<ConfiguracaoFinanceiraDto> SalvarConfiguracoes(ConfiguracaoFinanceiraDto r, CancellationToken ct) =>
        mensalidades.SalvarConfiguracaoAsync(r, ct);

    [HttpGet("planos")]
    [RequerPermissao(Permissoes.Financeiro.Visualizar)]
    public Task<IReadOnlyList<PlanoDto>> Planos(CancellationToken ct) => mensalidades.ListarPlanosAsync(ct);

    [HttpPost("planos")]
    [RequerPermissao(Permissoes.Financeiro.Gerenciar)]
    public async Task<IActionResult> CriarPlano(SalvarPlanoRequest r, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await mensalidades.SalvarPlanoAsync(null, r, ct));

    [HttpPut("planos/{id:guid}")]
    [RequerPermissao(Permissoes.Financeiro.Gerenciar)]
    public Task<PlanoDto> AtualizarPlano(Guid id, SalvarPlanoRequest r, CancellationToken ct) => mensalidades.SalvarPlanoAsync(id, r, ct);

    [HttpDelete("planos/{id:guid}")]
    [RequerPermissao(Permissoes.Financeiro.Gerenciar)]
    public async Task<IActionResult> ExcluirPlano(Guid id, CancellationToken ct)
    {
        await mensalidades.ExcluirPlanoAsync(id, ct);
        return NoContent();
    }

    // ---------------------------------------------------------------- mensalidades e pagamentos

    /// <summary>Gera as mensalidades da competência (AAAA-MM) para os associados ativos. Não duplica.</summary>
    [HttpPost("mensalidades/gerar")]
    [RequerPermissao(Permissoes.Financeiro.Gerenciar)]
    public Task<GerarMensalidadesResultado> Gerar(GerarMensalidadesRequest r, CancellationToken ct) => mensalidades.GerarAsync(r, ct);

    /// <summary>Filtros: competencia (AAAA-MM), situacao (Aberta, Vencida, Paga, Isenta, Cancelada), associadoId, busca.</summary>
    [HttpGet("mensalidades")]
    [RequerPermissao(Permissoes.Financeiro.Visualizar)]
    public Task<Pagina<MensalidadeDto>> Mensalidades([FromQuery] ConsultaMensalidades c, CancellationToken ct) =>
        mensalidades.ListarAsync(c, ct);

    /// <summary>Registra o pagamento (confirmação manual da tesouraria). Calcula multa e juros se vencida.</summary>
    [HttpPost("mensalidades/{id:guid}/pagamento")]
    [RequerPermissao(Permissoes.Financeiro.RegistrarPagamentos)]
    public Task<MensalidadeDto> Pagar(Guid id, RegistrarPagamentoRequest r, CancellationToken ct) => mensalidades.PagarAsync(id, r, ct);

    [HttpPost("mensalidades/{id:guid}/isentar")]
    [RequerPermissao(Permissoes.Financeiro.Gerenciar)]
    public Task<MensalidadeDto> Isentar(Guid id, MotivoRequest r, CancellationToken ct) => mensalidades.IsentarAsync(id, r, ct);

    [HttpPost("mensalidades/{id:guid}/cancelar")]
    [RequerPermissao(Permissoes.Financeiro.Gerenciar)]
    public Task<MensalidadeDto> Cancelar(Guid id, MotivoRequest r, CancellationToken ct) => mensalidades.CancelarAsync(id, r, ct);

    [HttpPost("pagamentos/{id:guid}/estornar")]
    [RequerPermissao(Permissoes.Financeiro.Gerenciar)]
    public Task<MensalidadeDto> Estornar(Guid id, MotivoRequest r, CancellationToken ct) => mensalidades.EstornarPagamentoAsync(id, r, ct);

    /// <summary>Recibo do pagamento (tesouraria ou o próprio associado).</summary>
    [HttpGet("pagamentos/{id:guid}/recibo")]
    public Task<ReciboDto> Recibo(Guid id, CancellationToken ct) => mensalidades.ReciboAsync(id, ct);

    [HttpGet("associados/{associadoId:guid}")]
    [RequerPermissao(Permissoes.Financeiro.Visualizar)]
    public Task<SituacaoFinanceiraDto> SituacaoAssociado(Guid associadoId, CancellationToken ct) =>
        mensalidades.SituacaoDoAssociadoAsync(associadoId, ct);

    // ---------------------------------------------------------------- receitas, despesas e relatórios

    [HttpGet("categorias")]
    [RequerPermissao(Permissoes.Financeiro.Visualizar)]
    public Task<IReadOnlyList<CategoriaFinanceiraDto>> Categorias(CancellationToken ct) => caixa.ListarCategoriasAsync(ct);

    [HttpGet("lancamentos")]
    [RequerPermissao(Permissoes.Financeiro.Visualizar)]
    public Task<Pagina<LancamentoDto>> Lancamentos([FromQuery] ConsultaLancamentos c, CancellationToken ct) => caixa.ListarLancamentosAsync(c, ct);

    [HttpPost("lancamentos")]
    [RequerPermissao(Permissoes.Financeiro.Gerenciar)]
    public async Task<IActionResult> CriarLancamento(SalvarLancamentoRequest r, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await caixa.SalvarLancamentoAsync(null, r, ct));

    [HttpPut("lancamentos/{id:guid}")]
    [RequerPermissao(Permissoes.Financeiro.Gerenciar)]
    public Task<LancamentoDto> AtualizarLancamento(Guid id, SalvarLancamentoRequest r, CancellationToken ct) =>
        caixa.SalvarLancamentoAsync(id, r, ct);

    [HttpPost("lancamentos/{id:guid}/pagar")]
    [RequerPermissao(Permissoes.Financeiro.Gerenciar)]
    public Task<LancamentoDto> PagarLancamento(Guid id, [FromQuery] DateOnly? data, CancellationToken ct) => caixa.MarcarPagoAsync(id, data, ct);

    [HttpDelete("lancamentos/{id:guid}")]
    [RequerPermissao(Permissoes.Financeiro.Gerenciar)]
    public async Task<IActionResult> ExcluirLancamento(Guid id, CancellationToken ct)
    {
        await caixa.ExcluirLancamentoAsync(id, ct);
        return NoContent();
    }

    /// <summary>Receitas, despesas e saldo do período (padrão: mês atual), a receber, a pagar e inadimplência.</summary>
    [HttpGet("resumo")]
    [RequerPermissao(Permissoes.Financeiro.Visualizar)]
    public Task<ResumoFinanceiroDto> Resumo([FromQuery] DateOnly? de, [FromQuery] DateOnly? ate, CancellationToken ct) =>
        caixa.ResumoAsync(de, ate, ct);

    [HttpGet("fluxo-caixa")]
    [RequerPermissao(Permissoes.Financeiro.Visualizar)]
    public Task<FluxoCaixaDto> FluxoCaixa([FromQuery] DateOnly? de, [FromQuery] DateOnly? ate, CancellationToken ct) =>
        caixa.FluxoCaixaAsync(de, ate, ct);

    [HttpGet("inadimplentes")]
    [RequerPermissao(Permissoes.Financeiro.Visualizar)]
    public Task<IReadOnlyList<InadimplenteDto>> Inadimplentes(CancellationToken ct) => caixa.InadimplentesAsync(ct);
}
