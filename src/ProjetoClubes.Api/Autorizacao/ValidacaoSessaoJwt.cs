using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Domain.Clubes;
using ProjetoClubes.Domain.Identidade;
using ProjetoClubes.Infrastructure.Persistence;
using ProjetoClubes.Infrastructure.Seguranca;

namespace ProjetoClubes.Api.Autorizacao;

/// <summary>
/// Executado após a validação criptográfica do JWT. Confirma no banco que a sessão continua ativa, que o usuário
/// não foi bloqueado e que o vínculo com o clube do token continua válido. Só então preenche <see cref="UsuarioAtual"/>,
/// que alimenta o filtro de tenant do EF Core e os serviços. Assim, revogação de sessão, bloqueio ou remoção do
/// acesso ao clube têm efeito imediato, sem esperar o access token expirar.
/// </summary>
public static class ValidacaoSessaoJwt
{
    public static async Task OnTokenValidated(TokenValidatedContext context)
    {
        var principal = context.Principal!;
        if (!Guid.TryParse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var usuarioId)
            || !Guid.TryParse(principal.FindFirst(ClaimsProjeto.Sessao)?.Value, out var sessaoId))
        {
            context.Fail("Token sem identificação de usuário/sessão.");
            return;
        }

        Guid? clubeToken = Guid.TryParse(principal.FindFirst(ClaimsProjeto.Clube)?.Value, out var c) ? c : null;
        var sp = context.HttpContext.RequestServices;
        var db = sp.GetRequiredService<AppDbContext>();
        var agora = sp.GetRequiredService<TimeProvider>().GetUtcNow();
        var ct = context.HttpContext.RequestAborted;

        var sessao = await db.SessoesUsuarios.AsNoTracking()
            .Where(s => s.Id == sessaoId && s.UsuarioId == usuarioId)
            .Select(s => new
            {
                s.RevogadaEm, s.ExpiraEm, s.ClubeId,
                s.Usuario.Situacao, s.Usuario.SuperAdmin, s.Usuario.BloqueadoAte,
            })
            .FirstOrDefaultAsync(ct);

        if (sessao is null || sessao.RevogadaEm is not null || sessao.ExpiraEm <= agora
            || sessao.Situacao != SituacaoUsuario.Ativo)
        {
            context.Fail("Sessão encerrada.");
            return;
        }

        // O clube do token precisa ser o clube atual da sessão (após trocar de clube, tokens antigos deixam de valer).
        if (sessao.ClubeId != clubeToken)
        {
            context.Fail("Clube do token difere do clube da sessão.");
            return;
        }

        if (clubeToken is { } clubeId)
        {
            var clubeValido = sessao.SuperAdmin
                ? await db.Clubes.AnyAsync(x => x.Id == clubeId, ct)
                : await db.Clubes.AnyAsync(x => x.Id == clubeId && x.Situacao == SituacaoClube.Ativo, ct)
                  && await db.UsuarioClubes.IgnoreQueryFilters(FiltrosConsulta.SomenteTenant)
                      .AnyAsync(v => v.UsuarioId == usuarioId && v.ClubeId == clubeId && v.Situacao == SituacaoVinculo.Ativo, ct);
            if (!clubeValido)
            {
                context.Fail("Acesso ao clube não autorizado.");
                return;
            }
        }

        sp.GetRequiredService<UsuarioAtual>().DefinirAutenticado(usuarioId, sessaoId, clubeToken, sessao.SuperAdmin);
    }
}
