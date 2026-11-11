using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProjetoClubes.API.Configuracao;
using ProjetoClubes.Application.Autenticacao;
using ProjetoClubes.Application.Setup;

namespace ProjetoClubes.API.Controllers;

/// <summary>Configuração inicial: criação segura do primeiro Superadmin.</summary>
[ApiController]
[Route("api/v1/setup")]
[Produces("application/json")]
public sealed class SetupController(ISetupService setup) : ControllerBase
{
    /// <summary>Indica se o sistema já possui um Superadmin.</summary>
    [HttpGet("status")]
    [AllowAnonymous]
    public Task<SetupStatusDto> Status(CancellationToken ct) => setup.ObterStatusAsync(ct);

    /// <summary>Cria o primeiro Superadmin usando o token de inicialização exibido no log da API.</summary>
    [HttpPost("inicializar")]
    [AllowAnonymous]
    [EnableRateLimiting(ApiServiceExtensions.LimiteAutenticacao)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Inicializar(InicializarSistemaRequest request, CancellationToken ct)
    {
        await setup.InicializarAsync(request, ct);
        return NoContent();
    }
}

/// <summary>Login, sessão, troca de clube e senhas.</summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AuthController(IAutenticacaoService auth) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(ApiServiceExtensions.LimiteAutenticacao)]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public Task<TokenResponse> Login(LoginRequest request, CancellationToken ct) => auth.LoginAsync(request, ct);

    /// <summary>Troca o refresh token por um novo par de tokens (rotação).</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(ApiServiceExtensions.LimiteAutenticacao)]
    public Task<TokenResponse> Refresh(RefreshRequest request, CancellationToken ct) => auth.RenovarAsync(request, ct);

    /// <summary>Encerra a sessão atual.</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await auth.LogoutAsync(ct);
        return NoContent();
    }

    /// <summary>Usuário autenticado, clube atual, clubes disponíveis e permissões efetivas.</summary>
    [HttpGet("me")]
    public Task<UsuarioLogadoDto> Me(CancellationToken ct) => auth.ObterUsuarioLogadoAsync(ct);

    /// <summary>Seleciona outro clube. O servidor valida o vínculo e emite novos tokens.</summary>
    [HttpPost("trocar-clube")]
    public Task<TokenResponse> TrocarClube(TrocarClubeRequest request, CancellationToken ct) =>
        auth.TrocarClubeAsync(request, ct);

    /// <summary>Envia o link de redefinição. Responde 202 mesmo se o e-mail não existir.</summary>
    [HttpPost("esqueci-senha")]
    [AllowAnonymous]
    [EnableRateLimiting(ApiServiceExtensions.LimiteAutenticacao)]
    public async Task<IActionResult> EsqueciSenha(EsqueciSenhaRequest request, CancellationToken ct)
    {
        await auth.SolicitarRecuperacaoSenhaAsync(request, ct);
        return Accepted();
    }

    [HttpPost("redefinir-senha")]
    [AllowAnonymous]
    [EnableRateLimiting(ApiServiceExtensions.LimiteAutenticacao)]
    public async Task<IActionResult> RedefinirSenha(RedefinirSenhaRequest request, CancellationToken ct)
    {
        await auth.RedefinirSenhaAsync(request, ct);
        return NoContent();
    }

    /// <summary>Altera a senha e encerra as demais sessões do usuário.</summary>
    [HttpPost("alterar-senha")]
    public async Task<IActionResult> AlterarSenha(AlterarSenhaRequest request, CancellationToken ct)
    {
        await auth.AlterarSenhaAsync(request, ct);
        return NoContent();
    }

    [HttpGet("sessoes")]
    public Task<IReadOnlyList<SessaoDto>> Sessoes(CancellationToken ct) => auth.ListarSessoesAsync(ct);

    [HttpDelete("sessoes/{id:guid}")]
    public async Task<IActionResult> RevogarSessao(Guid id, CancellationToken ct)
    {
        await auth.RevogarSessaoAsync(id, ct);
        return NoContent();
    }

    /// <summary>Histórico de autenticação do próprio usuário.</summary>
    [HttpGet("historico")]
    public Task<IReadOnlyList<HistoricoAcessoDto>> Historico([FromQuery] int limite = 50, CancellationToken ct = default) =>
        auth.ListarMeuHistoricoAsync(limite, ct);
}
