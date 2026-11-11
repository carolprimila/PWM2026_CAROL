using Microsoft.AspNetCore.Mvc;
using ProjetoClubes.API.Autorizacao;
using ProjetoClubes.Application.Auditoria;
using ProjetoClubes.Application.Clubes;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Application.Perfis;
using ProjetoClubes.Application.Seguranca;
using ProjetoClubes.Application.Usuarios;

namespace ProjetoClubes.API.Controllers;

/// <summary>Usuários do clube selecionado.</summary>
[ApiController]
[Route("api/v1/usuarios")]
[Produces("application/json")]
public sealed class UsuariosController(IUsuarioService usuarios) : ControllerBase
{
    [HttpGet]
    [RequerPermissao(Permissoes.Usuarios.Visualizar)]
    public Task<Pagina<UsuarioListaDto>> Listar([FromQuery] ConsultaUsuarios consulta, CancellationToken ct) =>
        usuarios.ListarAsync(consulta, ct);

    [HttpGet("{id:guid}")]
    [RequerPermissao(Permissoes.Usuarios.Visualizar)]
    public Task<UsuarioDetalheDto> Obter(Guid id, CancellationToken ct) => usuarios.ObterAsync(id, ct);

    /// <summary>Cria o usuário (ou vincula conta existente) com os perfis informados. Sem senha inicial, envia convite.</summary>
    [HttpPost]
    [RequerPermissao(Permissoes.Usuarios.Gerenciar)]
    [ProducesResponseType<CriarUsuarioResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(CriarUsuarioRequest request, CancellationToken ct)
    {
        var resposta = await usuarios.CriarAsync(request, ct);
        return CreatedAtAction(nameof(Obter), new { id = resposta.Id }, resposta);
    }

    [HttpPut("{id:guid}")]
    [RequerPermissao(Permissoes.Usuarios.Gerenciar)]
    public Task<UsuarioDetalheDto> Atualizar(Guid id, AtualizarUsuarioRequest request, CancellationToken ct) =>
        usuarios.AtualizarAsync(id, request, ct);

    /// <summary>Define perfis e exceções individuais (conceder/negar) do usuário neste clube.</summary>
    [HttpPut("{id:guid}/acesso")]
    [RequerPermissao(Permissoes.Usuarios.Gerenciar)]
    public Task<UsuarioDetalheDto> DefinirAcesso(Guid id, DefinirAcessoRequest request, CancellationToken ct) =>
        usuarios.DefinirAcessoAsync(id, request, ct);

    /// <summary>Remove o acesso do usuário a este clube (não afeta outros clubes).</summary>
    [HttpPost("{id:guid}/inativar")]
    [RequerPermissao(Permissoes.Usuarios.Gerenciar)]
    public async Task<IActionResult> Inativar(Guid id, CancellationToken ct)
    {
        await usuarios.AlterarSituacaoNoClubeAsync(id, false, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/reativar")]
    [RequerPermissao(Permissoes.Usuarios.Gerenciar)]
    public async Task<IActionResult> Reativar(Guid id, CancellationToken ct)
    {
        await usuarios.AlterarSituacaoNoClubeAsync(id, true, ct);
        return NoContent();
    }

    /// <summary>Bloqueia a conta em toda a plataforma (somente Superadmin).</summary>
    [HttpPost("{id:guid}/bloquear")]
    public async Task<IActionResult> Bloquear(Guid id, CancellationToken ct)
    {
        await usuarios.AlterarBloqueioContaAsync(id, true, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/desbloquear")]
    public async Task<IActionResult> Desbloquear(Guid id, CancellationToken ct)
    {
        await usuarios.AlterarBloqueioContaAsync(id, false, ct);
        return NoContent();
    }

    /// <summary>Encerra as sessões do usuário abertas neste clube.</summary>
    [HttpPost("{id:guid}/revogar-sessoes")]
    [RequerPermissao(Permissoes.Usuarios.Gerenciar)]
    public async Task<IActionResult> RevogarSessoes(Guid id, CancellationToken ct) =>
        Ok(new { sessoesRevogadas = await usuarios.RevogarSessoesAsync(id, ct) });
}

/// <summary>Perfis de acesso do clube selecionado.</summary>
[ApiController]
[Route("api/v1/perfis")]
[Produces("application/json")]
public sealed class PerfisController(IPerfilService perfis) : ControllerBase
{
    [HttpGet]
    [RequerPermissao(Permissoes.Usuarios.Visualizar)]
    public Task<IReadOnlyList<PerfilDto>> Listar(CancellationToken ct) => perfis.ListarAsync(ct);

    [HttpGet("{id:guid}")]
    [RequerPermissao(Permissoes.Usuarios.Visualizar)]
    public Task<PerfilDto> Obter(Guid id, CancellationToken ct) => perfis.ObterAsync(id, ct);

    [HttpPost]
    [RequerPermissao(Permissoes.Usuarios.GerenciarPerfis)]
    [ProducesResponseType<PerfilDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(SalvarPerfilRequest request, CancellationToken ct)
    {
        var perfil = await perfis.CriarAsync(request, ct);
        return CreatedAtAction(nameof(Obter), new { id = perfil.Id }, perfil);
    }

    [HttpPut("{id:guid}")]
    [RequerPermissao(Permissoes.Usuarios.GerenciarPerfis)]
    public Task<PerfilDto> Atualizar(Guid id, SalvarPerfilRequest request, CancellationToken ct) =>
        perfis.AtualizarAsync(id, request, ct);

    [HttpDelete("{id:guid}")]
    [RequerPermissao(Permissoes.Usuarios.GerenciarPerfis)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct)
    {
        await perfis.ExcluirAsync(id, ct);
        return NoContent();
    }
}

/// <summary>Catálogo de permissões disponíveis para montar perfis.</summary>
[ApiController]
[Route("api/v1/permissoes")]
[Produces("application/json")]
public sealed class PermissoesController(IPerfilService perfis) : ControllerBase
{
    [HttpGet]
    public IReadOnlyList<ModuloPermissoesDto> Listar() => perfis.ListarCatalogo();
}

/// <summary>Logs de auditoria e histórico de acessos do clube selecionado.</summary>
[ApiController]
[Route("api/v1/auditoria")]
[Produces("application/json")]
[RequerPermissao(Permissoes.Auditoria.Visualizar)]
public sealed class AuditoriaController(IAuditoriaService auditoria) : ControllerBase
{
    [HttpGet]
    public Task<Pagina<LogAuditoriaDto>> Listar([FromQuery] ConsultaAuditoria consulta, CancellationToken ct) =>
        auditoria.ListarAsync(consulta, ct);

    [HttpGet("acessos")]
    public Task<Pagina<HistoricoAcessoClubeDto>> Acessos([FromQuery] ConsultaHistoricoAcessos consulta, CancellationToken ct) =>
        auditoria.ListarAcessosAsync(consulta, ct);
}
