using Microsoft.AspNetCore.Mvc;
using ProjetoClubes.API.Autorizacao;
using ProjetoClubes.Application.Arquivos;
using ProjetoClubes.Application.Cadastros;
using ProjetoClubes.Application.Carteirinhas;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Application.ControleAcesso;
using ProjetoClubes.Application.Portal;
using ProjetoClubes.Application.Seguranca;
using ProjetoClubes.Domain.Arquivos;

namespace ProjetoClubes.API.Controllers;

public sealed class EnvioDocumentoForm
{
    public IFormFile Arquivo { get; set; } = null!;
    public TipoDocumento Tipo { get; set; }
    public string? Descricao { get; set; }
}

public sealed class EnvioFotoForm
{
    public IFormFile Arquivo { get; set; } = null!;
}

/// <summary>
/// Foto, documentos e QR Code de uma pessoa (associado ou dependente). A autorização é feita no serviço:
/// administração com a permissão correspondente ou, no aplicativo, a própria pessoa / o titular dela.
/// </summary>
[ApiController]
[Route("api/v1/pessoas/{pessoaId:guid}")]
public sealed class PessoasController(IDocumentoService documentos, ICarteirinhaService carteirinhas) : ControllerBase
{
    private const long LimiteRequisicao = 11 * 1024 * 1024;

    [HttpGet("foto")]
    [Produces("image/jpeg", "image/png", "image/webp")]
    public async Task<IActionResult> Foto(Guid pessoaId, CancellationToken ct)
    {
        var foto = await documentos.BaixarFotoAsync(pessoaId, ct);
        Response.Headers.CacheControl = "private, no-store";
        return File(foto.Conteudo, foto.ContentType);
    }

    [HttpPut("foto")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(LimiteRequisicao)]
    public async Task<IActionResult> EnviarFoto(Guid pessoaId, [FromForm] EnvioFotoForm form, CancellationToken ct)
    {
        await using var s = form.Arquivo.OpenReadStream();
        await documentos.EnviarFotoAsync(pessoaId, new ArquivoEnviado(s, form.Arquivo.FileName, form.Arquivo.Length), ct);
        return NoContent();
    }

    [HttpGet("documentos")]
    [Produces("application/json")]
    public Task<IReadOnlyList<DocumentoDto>> Documentos(Guid pessoaId, CancellationToken ct) => documentos.ListarAsync(pessoaId, ct);

    [HttpPost("documentos")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(LimiteRequisicao)]
    [ProducesResponseType<DocumentoDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> EnviarDocumento(Guid pessoaId, [FromForm] EnvioDocumentoForm form, CancellationToken ct)
    {
        await using var s = form.Arquivo.OpenReadStream();
        var doc = await documentos.EnviarAsync(pessoaId, form.Tipo, form.Descricao,
            new ArquivoEnviado(s, form.Arquivo.FileName, form.Arquivo.Length), ct);
        return StatusCode(StatusCodes.Status201Created, doc);
    }

    /// <summary>Imagem PNG do QR Code da carteirinha (conteúdo opaco, sem dados pessoais).</summary>
    [HttpGet("qrcode")]
    [Produces("image/png")]
    public async Task<IActionResult> QrCode(Guid pessoaId, CancellationToken ct)
    {
        Response.Headers.CacheControl = "private, no-store";
        return File(await carteirinhas.QrCodePngAsync(pessoaId, ct), "image/png");
    }

    /// <summary>Gera um novo código de carteirinha; o anterior deixa de ser aceito na portaria.</summary>
    [HttpPost("carteirinha/regenerar")]
    public async Task<IActionResult> RegenerarCarteirinha(Guid pessoaId, CancellationToken ct)
    {
        await carteirinhas.RegenerarAsync(pessoaId, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/v1/documentos")]
public sealed class DocumentosController(IDocumentoService documentos) : ControllerBase
{
    [HttpGet("{id:guid}/conteudo")]
    public async Task<IActionResult> Baixar(Guid id, CancellationToken ct)
    {
        var arq = await documentos.BaixarAsync(id, ct);
        Response.Headers.CacheControl = "private, no-store";
        return File(arq.Conteudo, arq.ContentType, arq.NomeArquivo);
    }

    [HttpPost("{id:guid}/analisar")]
    [Produces("application/json")]
    [RequerPermissao(Permissoes.Documentos.Gerenciar)]
    public Task<DocumentoDto> Analisar(Guid id, AnalisarDocumentoRequest request, CancellationToken ct) =>
        documentos.AnalisarAsync(id, request, ct);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct)
    {
        await documentos.ExcluirAsync(id, ct);
        return NoContent();
    }
}

/// <summary>Portaria: validação da carteirinha e registro de entradas e saídas.</summary>
[ApiController]
[Route("api/v1/controle-acesso")]
[Produces("application/json")]
public sealed class ControleAcessoController(IControleAcessoService acesso) : ControllerBase
{
    /// <summary>Consulta se a carteirinha está autorizada, sem registrar movimento.</summary>
    [HttpPost("validar")]
    [RequerPermissao(Permissoes.ControleAcesso.Validar)]
    public Task<ResultadoValidacaoDto> Validar(ValidarCodigoRequest request, CancellationToken ct) => acesso.ValidarAsync(request, ct);

    /// <summary>Valida e registra entrada ou saída (tentativas recusadas também são registradas).</summary>
    [HttpPost("registrar")]
    [RequerPermissao(Permissoes.ControleAcesso.Validar)]
    public Task<ResultadoValidacaoDto> Registrar(RegistrarAcessoRequest request, CancellationToken ct) =>
        acesso.RegistrarAsync(request, ct);

    [HttpGet]
    public Task<Pagina<AcessoDto>> Listar([FromQuery] ConsultaAcessos consulta, CancellationToken ct) => acesso.ListarAsync(consulta, ct);

    /// <summary>Pessoas cuja última movimentação nas últimas 24 horas foi uma entrada.</summary>
    [HttpGet("presentes")]
    [RequerPermissao(Permissoes.ControleAcesso.Validar)]
    public Task<IReadOnlyList<PresenteDto>> Presentes(CancellationToken ct) => acesso.PresentesAsync(ct);
}

/// <summary>Área do associado/dependente (aplicativo). Tudo é resolvido a partir do usuário autenticado.</summary>
[ApiController]
[Route("api/v1/portal")]
[Produces("application/json")]
[RequerPermissao(Permissoes.Portal.Acessar)]
public sealed class PortalController(IPortalService portal, ICarteirinhaService carteirinhas,
    Application.Financeiro.IMensalidadeService mensalidades) : ControllerBase
{
    /// <summary>Mensalidades e situação financeira do titular (inclui a cobrança dos dependentes).</summary>
    [HttpGet("financeiro")]
    public Task<Application.Financeiro.SituacaoFinanceiraDto> Financeiro(CancellationToken ct) => mensalidades.SituacaoNoPortalAsync(ct);

    [HttpGet("perfil")]
    public Task<PortalPerfilDto> Perfil(CancellationToken ct) => portal.PerfilAsync(ct);

    [HttpPut("perfil/contato")]
    public Task<PortalPerfilDto> AtualizarContato(AtualizarContatoRequest request, CancellationToken ct) =>
        portal.AtualizarContatoAsync(request, ct);

    [HttpGet("carteirinhas")]
    public Task<IReadOnlyList<CarteirinhaDto>> Carteirinhas(CancellationToken ct) => carteirinhas.DoPortalAsync(ct);

    [HttpGet("dependentes")]
    public Task<IReadOnlyList<DependenteListaDto>> Dependentes(CancellationToken ct) => portal.ListarDependentesAsync(ct);

    [HttpGet("dependentes/{id:guid}")]
    public Task<DependenteDetalheDto> Dependente(Guid id, CancellationToken ct) => portal.ObterDependenteAsync(id, ct);

    [HttpPost("dependentes")]
    [ProducesResponseType<DependenteDetalheDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CriarDependente(CriarDependentePortalRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await portal.CriarDependenteAsync(request, ct));

    [HttpPut("dependentes/{id:guid}")]
    public Task<DependenteDetalheDto> AtualizarDependente(Guid id, AtualizarDependenteRequest request, CancellationToken ct) =>
        portal.AtualizarDependenteAsync(id, request, ct);

    /// <summary>O titular autoriza o login próprio do dependente (respeitando a idade mínima do clube).</summary>
    [HttpPost("dependentes/{id:guid}/acesso-app")]
    public Task<CriarAcessoAppResponse> AutorizarAcessoApp(Guid id, CancellationToken ct) =>
        portal.AutorizarAcessoAppDependenteAsync(id, ct);
}
