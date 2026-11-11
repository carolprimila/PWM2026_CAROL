using System.Linq.Expressions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Application.Seguranca;
using ProjetoClubes.Domain.Identidade;

namespace ProjetoClubes.Application.Usuarios;

public interface IUsuarioService
{
    Task<Pagina<UsuarioListaDto>> ListarAsync(ConsultaUsuarios consulta, CancellationToken ct);
    Task<UsuarioDetalheDto> ObterAsync(Guid id, CancellationToken ct);
    Task<CriarUsuarioResponse> CriarAsync(CriarUsuarioRequest request, CancellationToken ct);
    Task<UsuarioDetalheDto> AtualizarAsync(Guid id, AtualizarUsuarioRequest request, CancellationToken ct);
    Task<UsuarioDetalheDto> DefinirAcessoAsync(Guid id, DefinirAcessoRequest request, CancellationToken ct);
    Task AlterarSituacaoNoClubeAsync(Guid id, bool ativo, CancellationToken ct);
    Task AlterarBloqueioContaAsync(Guid id, bool bloquear, CancellationToken ct);
    Task<int> RevogarSessoesAsync(Guid id, CancellationToken ct);
}

/// <summary>
/// Gestão dos usuários do clube selecionado. Toda consulta parte de UsuarioClubes filtrado pelo clube atual,
/// então um administrador nunca enxerga usuários de outro clube.
/// </summary>
public sealed class UsuarioService(
    IAppDbContext db,
    IUsuarioAtual atual,
    IPermissaoResolver permissoes,
    IProvisionamentoUsuarios provisionamento,
    TimeProvider relogio,
    IValidator<CriarUsuarioRequest> criarValidator,
    IValidator<AtualizarUsuarioRequest> atualizarValidator,
    IValidator<DefinirAcessoRequest> acessoValidator) : IUsuarioService
{
    private static readonly Dictionary<string, Expression<Func<UsuarioClube, object?>>> Ordenacoes = new()
    {
        ["nome"] = v => v.Usuario.Nome,
        ["email"] = v => v.Usuario.EmailNormalizado,
        ["ultimoacessoem"] = v => v.Usuario.UltimoAcessoEm,
        ["criadoem"] = v => v.CriadoEm,
    };

    private Guid ClubeId => atual.ClubeIdObrigatorio;

    /// <summary>Vínculos do clube atual (filtro explícito, além do filtro global de tenant).</summary>
    private IQueryable<UsuarioClube> Vinculos() => db.UsuarioClubes.Where(v => v.ClubeId == ClubeId);

    public async Task<Pagina<UsuarioListaDto>> ListarAsync(ConsultaUsuarios consulta, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Usuarios.Visualizar, ct);
        var query = Vinculos().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(consulta.Busca))
        {
            var termo = consulta.Busca.Trim().ToLower();
            query = query.Where(v => v.Usuario.Nome.ToLower().Contains(termo) || v.Usuario.EmailNormalizado.Contains(termo));
        }
        if (consulta.PerfilId is { } perfilId) query = query.Where(v => v.Perfis.Any(p => p.PerfilId == perfilId));
        if (Enum.TryParse<SituacaoVinculo>(consulta.Situacao, true, out var situacao))
            query = query.Where(v => v.Situacao == situacao);

        var pagina = await query.Ordenar(consulta, Ordenacoes, "nome")
            .Select(v => new
            {
                v.UsuarioId, v.Usuario.Nome, v.Usuario.Email, ContaSituacao = v.Usuario.Situacao, v.Situacao,
                v.Usuario.UltimoAcessoEm,
                Perfis = v.Perfis.Select(p => new PerfilResumoDto(p.PerfilId, p.Perfil.Nome)).ToList(),
            })
            .PaginarAsync(consulta, ct);

        return new Pagina<UsuarioListaDto>(pagina.Itens.Select(i => new UsuarioListaDto(i.UsuarioId, i.Nome, i.Email,
                i.ContaSituacao.ToString(), i.Situacao.ToString(), i.Perfis, i.UltimoAcessoEm)).ToList(),
            pagina.PaginaAtual, pagina.TamanhoPagina, pagina.TotalItens);
    }

    public async Task<UsuarioDetalheDto> ObterAsync(Guid id, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Usuarios.Visualizar, ct);
        return await MontarDetalheAsync(id, ct);
    }

    public async Task<CriarUsuarioResponse> CriarAsync(CriarUsuarioRequest request, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Usuarios.Gerenciar, ct);
        await criarValidator.ValidarAsync(request, ct);
        var perfilIds = await ValidarPerfisAsync(request.PerfilIds, ct);
        await ImpedirEscalonamentoAsync(perfilIds, [], ct);

        var resultado = await provisionamento.CriarOuVincularAsync(request.Nome, request.Email, ClubeId, perfilIds,
            request.SenhaInicial, ct);
        await db.SaveChangesAsync(ct);
        permissoes.Invalidar();
        return new CriarUsuarioResponse(resultado.Usuario.Id, resultado.UsuarioCriado, resultado.ConviteEnviado);
    }

    public async Task<UsuarioDetalheDto> AtualizarAsync(Guid id, AtualizarUsuarioRequest request, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Usuarios.Gerenciar, ct);
        await atualizarValidator.ValidarAsync(request, ct);
        var vinculo = await ObterVinculoAsync(id, ct);
        vinculo.Usuario.Nome = request.Nome.Trim();
        await db.SaveChangesAsync(ct);
        return await MontarDetalheAsync(id, ct);
    }

    public async Task<UsuarioDetalheDto> DefinirAcessoAsync(Guid id, DefinirAcessoRequest request, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Usuarios.Gerenciar, ct);
        await acessoValidator.ValidarAsync(request, ct);
        var vinculo = await ObterVinculoAsync(id, ct);

        var perfilIds = await ValidarPerfisAsync(request.PerfilIds, ct);
        var individuais = request.PermissoesIndividuais ?? [];
        await ImpedirEscalonamentoAsync(perfilIds, individuais.Where(i => i.Concedida).Select(i => i.Codigo), ct);

        var catalogo = await db.Permissoes.ToDictionaryAsync(p => p.Codigo, p => p.Id, ct);
        vinculo.DefinirPerfis(perfilIds);
        vinculo.DefinirPermissoesIndividuais(individuais.Select(i => (catalogo[i.Codigo], i.Concedida)));
        await db.SaveChangesAsync(ct);
        permissoes.Invalidar();
        return await MontarDetalheAsync(id, ct);
    }

    public async Task AlterarSituacaoNoClubeAsync(Guid id, bool ativo, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Usuarios.Gerenciar, ct);
        if (!ativo && id == atual.UsuarioId)
            throw new ConflitoException("usuario.auto_inativacao", "Você não pode remover o seu próprio acesso.");

        var vinculo = await ObterVinculoAsync(id, ct);
        if (ativo) vinculo.Ativar();
        else
        {
            vinculo.Inativar();
            await RevogarSessoesDoClubeAsync(id, "Acesso ao clube removido", ct);
        }
        await db.SaveChangesAsync(ct);
        permissoes.Invalidar();
    }

    /// <summary>Bloqueio da conta inteira (afeta todos os clubes): exclusivo do Superadmin.</summary>
    public async Task AlterarBloqueioContaAsync(Guid id, bool bloquear, CancellationToken ct)
    {
        if (!atual.SuperAdmin)
            throw new AcessoNegadoException("Somente o Superadmin pode bloquear contas. Use a remoção de acesso ao clube.");
        if (bloquear && id == atual.UsuarioId)
            throw new ConflitoException("usuario.auto_bloqueio", "Você não pode bloquear a sua própria conta.");

        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct) ?? throw new NaoEncontradoException("Usuário");
        var agora = relogio.GetUtcNow();
        if (bloquear)
        {
            usuario.Bloquear();
            var sessoes = await db.SessoesUsuarios.Where(s => s.UsuarioId == id && s.RevogadaEm == null).ToListAsync(ct);
            foreach (var s in sessoes) s.Revogar(agora, "Conta bloqueada");
        }
        else
        {
            usuario.Desbloquear();
        }
        db.HistoricoAcessos.Add(new HistoricoAcesso(
            bloquear ? EventoAcesso.UsuarioBloqueado : EventoAcesso.UsuarioDesbloqueado, true, agora, id, null,
            atual.ClubeId, null, atual.Ip, atual.UserAgent, $"Por {atual.UsuarioId}"));
        await db.SaveChangesAsync(ct);
    }

    public async Task<int> RevogarSessoesAsync(Guid id, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Usuarios.Gerenciar, ct);
        await ObterVinculoAsync(id, ct);
        var total = await RevogarSessoesDoClubeAsync(id, "Revogada pela administração do clube", ct);
        await db.SaveChangesAsync(ct);
        return total;
    }

    // ---------------------------------------------------------------- auxiliares

    private async Task<int> RevogarSessoesDoClubeAsync(Guid usuarioId, string motivo, CancellationToken ct)
    {
        var agora = relogio.GetUtcNow();
        // A administração de um clube só encerra sessões abertas no próprio clube.
        var sessoes = await db.SessoesUsuarios
            .Where(s => s.UsuarioId == usuarioId && s.ClubeId == ClubeId && s.RevogadaEm == null)
            .ToListAsync(ct);
        foreach (var s in sessoes)
        {
            s.Revogar(agora, motivo);
            db.HistoricoAcessos.Add(new HistoricoAcesso(EventoAcesso.SessaoRevogada, true, agora, usuarioId, null,
                ClubeId, s.Id, atual.Ip, atual.UserAgent, motivo));
        }
        return sessoes.Count;
    }

    private async Task<UsuarioClube> ObterVinculoAsync(Guid usuarioId, CancellationToken ct) =>
        await Vinculos().Include(v => v.Usuario).Include(v => v.Perfis).Include(v => v.Permissoes)
            .FirstOrDefaultAsync(v => v.UsuarioId == usuarioId, ct)
        ?? throw new NaoEncontradoException("Usuário");

    private async Task<List<Guid>> ValidarPerfisAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        var distintos = ids.Distinct().ToList();
        var existentes = await db.Perfis.Where(p => p.ClubeId == ClubeId && distintos.Contains(p.Id))
            .Select(p => p.Id).ToListAsync(ct);
        if (existentes.Count != distintos.Count)
            throw new ValidationException([new ValidationFailure("PerfilIds", "Um ou mais perfis não pertencem a este clube.")]);
        return existentes;
    }

    /// <summary>Impede que alguém conceda permissões que ele mesmo não possui.</summary>
    private async Task ImpedirEscalonamentoAsync(IReadOnlyCollection<Guid> perfilIds, IEnumerable<string> concessoes,
        CancellationToken ct)
    {
        if (atual.SuperAdmin) return;
        var minhas = await permissoes.ObterDoUsuarioAtualAsync(ct);
        var dosPerfis = await db.PerfilPermissoes.Where(pp => perfilIds.Contains(pp.PerfilId))
            .Select(pp => pp.Permissao.Codigo).Distinct().ToListAsync(ct);
        var excedentes = dosPerfis.Concat(concessoes).Where(c => !minhas.Contains(c)).Distinct().ToList();
        if (excedentes.Count > 0)
            throw new AcessoNegadoException(
                $"Você não pode conceder permissões que não possui: {string.Join(", ", excedentes)}.");
    }

    private async Task<UsuarioDetalheDto> MontarDetalheAsync(Guid usuarioId, CancellationToken ct)
    {
        var v = await Vinculos().AsNoTracking()
                    .Where(x => x.UsuarioId == usuarioId)
                    .Select(x => new
                    {
                        x.Usuario.Id, x.Usuario.Nome, x.Usuario.Email, Conta = x.Usuario.Situacao, x.Situacao,
                        x.Usuario.SuperAdmin, x.Usuario.DeveTrocarSenha, x.Usuario.UltimoAcessoEm, x.Usuario.CriadoEm,
                        Perfis = x.Perfis.Select(p => new PerfilResumoDto(p.PerfilId, p.Perfil.Nome)).ToList(),
                        Individuais = x.Permissoes.Select(p => new PermissaoIndividualDto(p.Permissao.Codigo, p.Concedida)).ToList(),
                    })
                    .FirstOrDefaultAsync(ct)
                ?? throw new NaoEncontradoException("Usuário");

        var efetivas = await permissoes.ObterAsync(usuarioId, ClubeId, ct);
        return new UsuarioDetalheDto(v.Id, v.Nome, v.Email, v.Conta.ToString(), v.Situacao.ToString(), v.SuperAdmin,
            v.DeveTrocarSenha, v.UltimoAcessoEm, v.CriadoEm, v.Perfis, v.Individuais, efetivas.Order().ToList());
    }
}
