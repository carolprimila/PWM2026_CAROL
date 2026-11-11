using Microsoft.AspNetCore.Mvc;
using ProjetoClubes.API.Autorizacao;
using ProjetoClubes.Application.Cadastros;
using ProjetoClubes.Application.Carteirinhas;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Application.Seguranca;

namespace ProjetoClubes.API.Controllers;

/// <summary>Categorias de associados do clube (direitos configuráveis).</summary>
[ApiController]
[Route("api/v1/categorias-associados")]
[Produces("application/json")]
public sealed class CategoriasAssociadosController(ICategoriaService categorias) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<CategoriaDto>> Listar([FromQuery] bool incluirInativas = false, CancellationToken ct = default) =>
        categorias.ListarAsync(incluirInativas, ct);

    [HttpPost]
    [RequerPermissao(Permissoes.Clubes.Configurar)]
    [ProducesResponseType<CategoriaDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(SalvarCategoriaRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await categorias.CriarAsync(request, ct));

    [HttpPut("{id:guid}")]
    [RequerPermissao(Permissoes.Clubes.Configurar)]
    public Task<CategoriaDto> Atualizar(Guid id, SalvarCategoriaRequest request, CancellationToken ct) =>
        categorias.AtualizarAsync(id, request, ct);

    [HttpDelete("{id:guid}")]
    [RequerPermissao(Permissoes.Clubes.Configurar)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct)
    {
        await categorias.ExcluirAsync(id, ct);
        return NoContent();
    }
}

/// <summary>Interesses dos associados (pesca, esportes…).</summary>
[ApiController]
[Route("api/v1/interesses")]
[Produces("application/json")]
public sealed class InteressesController(ICategoriaService categorias) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<InteresseDto>> Listar(CancellationToken ct) => categorias.ListarInteressesAsync(ct);

    [HttpPost]
    [RequerPermissao(Permissoes.Clubes.Configurar)]
    [ProducesResponseType<InteresseDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(SalvarInteresseRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await categorias.CriarInteresseAsync(request, ct));

    [HttpDelete("{id:guid}")]
    [RequerPermissao(Permissoes.Clubes.Configurar)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct)
    {
        await categorias.ExcluirInteresseAsync(id, ct);
        return NoContent();
    }
}

/// <summary>Associados titulares do clube selecionado.</summary>
[ApiController]
[Route("api/v1/associados")]
[Produces("application/json")]
public sealed class AssociadosController(IAssociadoService associados, ICarteirinhaService carteirinhas) : ControllerBase
{
    [HttpGet]
    [RequerPermissao(Permissoes.Associados.Visualizar)]
    public Task<Pagina<AssociadoListaDto>> Listar([FromQuery] ConsultaAssociados consulta, CancellationToken ct) =>
        associados.ListarAsync(consulta, ct);

    /// <summary>Totais por situação e categoria (indicadores do dashboard).</summary>
    [HttpGet("resumo")]
    [RequerPermissao(Permissoes.Associados.Visualizar)]
    public Task<ResumoCadastrosDto> Resumo(CancellationToken ct) => associados.ResumoAsync(ct);

    [HttpGet("{id:guid}")]
    [RequerPermissao(Permissoes.Associados.Visualizar)]
    public Task<AssociadoDetalheDto> Obter(Guid id, CancellationToken ct) => associados.ObterAsync(id, ct);

    [HttpPost]
    [RequerPermissao(Permissoes.Associados.Cadastrar)]
    [ProducesResponseType<AssociadoDetalheDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(CriarAssociadoRequest request, CancellationToken ct)
    {
        var a = await associados.CriarAsync(request, ct);
        return CreatedAtAction(nameof(Obter), new { id = a.Id }, a);
    }

    [HttpPut("{id:guid}")]
    [RequerPermissao(Permissoes.Associados.Editar)]
    public Task<AssociadoDetalheDto> Atualizar(Guid id, AtualizarAssociadoRequest request, CancellationToken ct) =>
        associados.AtualizarAsync(id, request, ct);

    [HttpPost("{id:guid}/aprovar")]
    [RequerPermissao(Permissoes.Associados.Aprovar)]
    public Task<AssociadoDetalheDto> Aprovar(Guid id, CancellationToken ct) =>
        associados.AlterarSituacaoAsync(id, AcaoSituacao.Aprovar, null, ct);

    [HttpPost("{id:guid}/rejeitar")]
    [RequerPermissao(Permissoes.Associados.Aprovar)]
    public Task<AssociadoDetalheDto> Rejeitar(Guid id, AlterarSituacaoRequest request, CancellationToken ct) =>
        associados.AlterarSituacaoAsync(id, AcaoSituacao.Rejeitar, request.Motivo, ct);

    [HttpPost("{id:guid}/suspender")]
    [RequerPermissao(Permissoes.Associados.Aprovar)]
    public Task<AssociadoDetalheDto> Suspender(Guid id, AlterarSituacaoRequest request, CancellationToken ct) =>
        associados.AlterarSituacaoAsync(id, AcaoSituacao.Suspender, request.Motivo, ct);

    [HttpPost("{id:guid}/reativar")]
    [RequerPermissao(Permissoes.Associados.Aprovar)]
    public Task<AssociadoDetalheDto> Reativar(Guid id, CancellationToken ct) =>
        associados.AlterarSituacaoAsync(id, AcaoSituacao.Reativar, null, ct);

    [HttpPost("{id:guid}/inativar")]
    [RequerPermissao(Permissoes.Associados.Aprovar)]
    public Task<AssociadoDetalheDto> Inativar(Guid id, AlterarSituacaoRequest request, CancellationToken ct) =>
        associados.AlterarSituacaoAsync(id, AcaoSituacao.Inativar, request.Motivo, ct);

    [HttpDelete("{id:guid}")]
    [RequerPermissao(Permissoes.Associados.Excluir)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct)
    {
        await associados.ExcluirAsync(id, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/historico")]
    [RequerPermissao(Permissoes.Associados.Visualizar)]
    public Task<IReadOnlyList<HistoricoCadastroDto>> Historico(Guid id, CancellationToken ct) => associados.HistoricoAsync(id, ct);

    [HttpGet("{id:guid}/carteirinha")]
    public Task<CarteirinhaDto> Carteirinha(Guid id, CancellationToken ct) => carteirinhas.DoAssociadoAsync(id, ct);

    /// <summary>Cria o login do associado no aplicativo (convite por e-mail se não houver senha inicial).</summary>
    [HttpPost("{id:guid}/acesso-app")]
    [RequerPermissao(Permissoes.Usuarios.Gerenciar)]
    public Task<CriarAcessoAppResponse> CriarAcessoApp(Guid id, CriarAcessoAppRequest request, CancellationToken ct) =>
        associados.CriarAcessoAppAsync(id, request, ct);
}

/// <summary>Dependentes dos associados do clube selecionado.</summary>
[ApiController]
[Route("api/v1/dependentes")]
[Produces("application/json")]
public sealed class DependentesController(IDependenteService dependentes, ICarteirinhaService carteirinhas) : ControllerBase
{
    [HttpGet]
    [RequerPermissao(Permissoes.Dependentes.Visualizar)]
    public Task<Pagina<DependenteListaDto>> Listar([FromQuery] ConsultaDependentes consulta, CancellationToken ct) =>
        dependentes.ListarAsync(consulta, ct);

    [HttpGet("{id:guid}")]
    [RequerPermissao(Permissoes.Dependentes.Visualizar)]
    public Task<DependenteDetalheDto> Obter(Guid id, CancellationToken ct) => dependentes.ObterAsync(id, ct);

    [HttpPost]
    [RequerPermissao(Permissoes.Dependentes.Cadastrar)]
    [ProducesResponseType<DependenteDetalheDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(CriarDependenteRequest request, CancellationToken ct)
    {
        var d = await dependentes.CriarAsync(request, ct);
        return CreatedAtAction(nameof(Obter), new { id = d.Id }, d);
    }

    [HttpPut("{id:guid}")]
    [RequerPermissao(Permissoes.Dependentes.Editar)]
    public Task<DependenteDetalheDto> Atualizar(Guid id, AtualizarDependenteRequest request, CancellationToken ct) =>
        dependentes.AtualizarAsync(id, request, ct);

    [HttpPost("{id:guid}/aprovar")]
    [RequerPermissao(Permissoes.Dependentes.Aprovar)]
    public Task<DependenteDetalheDto> Aprovar(Guid id, CancellationToken ct) =>
        dependentes.AlterarSituacaoAsync(id, AcaoSituacao.Aprovar, null, ct);

    [HttpPost("{id:guid}/rejeitar")]
    [RequerPermissao(Permissoes.Dependentes.Aprovar)]
    public Task<DependenteDetalheDto> Rejeitar(Guid id, AlterarSituacaoRequest request, CancellationToken ct) =>
        dependentes.AlterarSituacaoAsync(id, AcaoSituacao.Rejeitar, request.Motivo, ct);

    [HttpPost("{id:guid}/suspender")]
    [RequerPermissao(Permissoes.Dependentes.Aprovar)]
    public Task<DependenteDetalheDto> Suspender(Guid id, AlterarSituacaoRequest request, CancellationToken ct) =>
        dependentes.AlterarSituacaoAsync(id, AcaoSituacao.Suspender, request.Motivo, ct);

    [HttpPost("{id:guid}/reativar")]
    [RequerPermissao(Permissoes.Dependentes.Aprovar)]
    public Task<DependenteDetalheDto> Reativar(Guid id, CancellationToken ct) =>
        dependentes.AlterarSituacaoAsync(id, AcaoSituacao.Reativar, null, ct);

    [HttpPost("{id:guid}/inativar")]
    [RequerPermissao(Permissoes.Dependentes.Aprovar)]
    public Task<DependenteDetalheDto> Inativar(Guid id, AlterarSituacaoRequest request, CancellationToken ct) =>
        dependentes.AlterarSituacaoAsync(id, AcaoSituacao.Inativar, request.Motivo, ct);

    /// <summary>Transfere o dependente para outro titular (mediante autorização), mantendo o histórico de vínculos.</summary>
    [HttpPost("{id:guid}/transferir")]
    [RequerPermissao(Permissoes.Dependentes.Aprovar)]
    public Task<DependenteDetalheDto> Transferir(Guid id, TransferirDependenteRequest request, CancellationToken ct) =>
        dependentes.TransferirAsync(id, request, ct);

    [HttpPut("{id:guid}/permissoes")]
    [RequerPermissao(Permissoes.Dependentes.Editar)]
    public Task<DependenteDetalheDto> AtualizarPermissoes(Guid id, PermissoesDependenteDto request, CancellationToken ct) =>
        dependentes.AtualizarPermissoesAsync(id, request, ct);

    [HttpDelete("{id:guid}")]
    [RequerPermissao(Permissoes.Dependentes.Excluir)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct)
    {
        await dependentes.ExcluirAsync(id, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/historico")]
    [RequerPermissao(Permissoes.Dependentes.Visualizar)]
    public Task<IReadOnlyList<HistoricoCadastroDto>> Historico(Guid id, CancellationToken ct) => dependentes.HistoricoAsync(id, ct);

    [HttpGet("{id:guid}/carteirinha")]
    public Task<CarteirinhaDto> Carteirinha(Guid id, CancellationToken ct) => carteirinhas.DoDependenteAsync(id, ct);

    [HttpPost("{id:guid}/acesso-app")]
    [RequerPermissao(Permissoes.Usuarios.Gerenciar)]
    public Task<CriarAcessoAppResponse> CriarAcessoApp(Guid id, CriarAcessoAppRequest request, CancellationToken ct) =>
        dependentes.CriarAcessoAppAsync(id, request, ct);

    /// <summary>Regras de dependentes do clube (limites, idades, parentescos, cobrança, permissões padrão).</summary>
    [HttpGet("configuracoes")]
    public Task<ConfiguracaoDependentesDto> ObterConfiguracoes(CancellationToken ct) => dependentes.ObterConfiguracaoAsync(ct);

    [HttpPut("configuracoes")]
    [RequerPermissao(Permissoes.Clubes.Configurar)]
    public Task<ConfiguracaoDependentesDto> AtualizarConfiguracoes(ConfiguracaoDependentesDto request, CancellationToken ct) =>
        dependentes.AtualizarConfiguracaoAsync(request, ct);
}
