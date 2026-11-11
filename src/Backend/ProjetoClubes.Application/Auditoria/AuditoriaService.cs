using Microsoft.EntityFrameworkCore;
using ProjetoClubes.Application.Autenticacao;
using ProjetoClubes.Application.Clubes;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Application.Seguranca;
using ProjetoClubes.Domain.Identidade;

namespace ProjetoClubes.Application.Auditoria;

public sealed class ConsultaAuditoria : ConsultaPaginada
{
    public string? Entidade { get; set; }
    public string? EntidadeId { get; set; }
    public Guid? UsuarioId { get; set; }
    public DateTimeOffset? De { get; set; }
    public DateTimeOffset? Ate { get; set; }
}

public sealed class ConsultaHistoricoAcessos : ConsultaPaginada
{
    public Guid? UsuarioId { get; set; }
    public string? Evento { get; set; }
    public bool? Sucesso { get; set; }
    public DateTimeOffset? De { get; set; }
    public DateTimeOffset? Ate { get; set; }
}

public sealed record HistoricoAcessoClubeDto(Guid Id, Guid? UsuarioId, string? UsuarioNome, string Evento, bool Sucesso,
    DateTimeOffset OcorridoEm, string? Ip, string? Detalhe);

public interface IAuditoriaService
{
    Task<Pagina<LogAuditoriaDto>> ListarAsync(ConsultaAuditoria consulta, CancellationToken ct);
    Task<Pagina<HistoricoAcessoClubeDto>> ListarAcessosAsync(ConsultaHistoricoAcessos consulta, CancellationToken ct);
}

/// <summary>Consulta de logs de auditoria e do histórico de autenticação — sempre restrita ao clube atual.</summary>
public sealed class AuditoriaService(IAppDbContext db, IUsuarioAtual atual, IPermissaoResolver permissoes) : IAuditoriaService
{
    public async Task<Pagina<LogAuditoriaDto>> ListarAsync(ConsultaAuditoria consulta, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Auditoria.Visualizar, ct);
        var clubeId = atual.ClubeIdObrigatorio;
        var query = db.LogsAuditoria.AsNoTracking().Where(l => l.ClubeId == clubeId);
        if (!string.IsNullOrWhiteSpace(consulta.Entidade)) query = query.Where(l => l.Entidade == consulta.Entidade);
        if (!string.IsNullOrWhiteSpace(consulta.EntidadeId)) query = query.Where(l => l.EntidadeId == consulta.EntidadeId);
        if (consulta.UsuarioId is { } u) query = query.Where(l => l.UsuarioId == u);
        if (consulta.De is { } de) query = query.Where(l => l.OcorridoEm >= de);
        if (consulta.Ate is { } ate) query = query.Where(l => l.OcorridoEm <= ate);

        return await query.OrderByDescending(l => l.OcorridoEm)
            .Select(l => new LogAuditoriaDto(l.Id, l.Entidade, l.EntidadeId, l.Acao.ToString(), l.Alteracoes, l.UsuarioId,
                db.Usuarios.IgnoreQueryFilters().Where(x => x.Id == l.UsuarioId).Select(x => x.Nome).FirstOrDefault(),
                l.OcorridoEm))
            .PaginarAsync(consulta, ct);
    }

    public async Task<Pagina<HistoricoAcessoClubeDto>> ListarAcessosAsync(ConsultaHistoricoAcessos consulta, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Auditoria.Visualizar, ct);
        var clubeId = atual.ClubeIdObrigatorio;
        var query = db.HistoricoAcessos.AsNoTracking().Where(h => h.ClubeId == clubeId);
        if (consulta.UsuarioId is { } u) query = query.Where(h => h.UsuarioId == u);
        if (Enum.TryParse<EventoAcesso>(consulta.Evento, true, out var evento)) query = query.Where(h => h.Evento == evento);
        if (consulta.Sucesso is { } s) query = query.Where(h => h.Sucesso == s);
        if (consulta.De is { } de) query = query.Where(h => h.OcorridoEm >= de);
        if (consulta.Ate is { } ate) query = query.Where(h => h.OcorridoEm <= ate);

        var pagina = await query.OrderByDescending(h => h.OcorridoEm)
            .Select(h => new
            {
                h.Id, h.UsuarioId,
                Nome = db.Usuarios.IgnoreQueryFilters().Where(x => x.Id == h.UsuarioId).Select(x => x.Nome).FirstOrDefault(),
                h.Evento, h.Sucesso, h.OcorridoEm, h.Ip, h.Detalhe,
            })
            .PaginarAsync(consulta, ct);

        return new Pagina<HistoricoAcessoClubeDto>(
            pagina.Itens.Select(h => new HistoricoAcessoClubeDto(h.Id, h.UsuarioId, h.Nome, h.Evento.ToString(), h.Sucesso,
                h.OcorridoEm, h.Ip, h.Detalhe)).ToList(),
            pagina.PaginaAtual, pagina.TamanhoPagina, pagina.TotalItens);
    }
}
