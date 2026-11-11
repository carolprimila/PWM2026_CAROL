using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjetoClubes.Application.Cadastros;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Application.Seguranca;
using ProjetoClubes.Domain.Common;
using ProjetoClubes.Domain.Comunicacao;

namespace ProjetoClubes.Application.Comunicacao;

public sealed record ComunicadoDto(Guid Id, string Titulo, string Texto, PublicoComunicado Publico, bool ExigeConfirmacao, bool Fixado,
    DateTimeOffset? PublicadoEm, DateTimeOffset? ExpiraEm, DateTimeOffset CriadoEm, int Destinatarios, int Leituras, int Confirmacoes);

public sealed record LeituraComunicadoDto(Guid PessoaId, string Nome, DateTimeOffset LidoEm, DateTimeOffset? ConfirmadoEm);

public sealed record ComunicadoLeiturasDto(ComunicadoDto Comunicado, IReadOnlyList<LeituraComunicadoDto> Leituras,
    IReadOnlyList<string> Pendentes);

public sealed record SalvarComunicadoRequest(string Titulo, string Texto, PublicoComunicado Publico, bool ExigeConfirmacao, bool Fixado,
    DateTimeOffset? ExpiraEm, bool Publicar = false);

public sealed record ComunicadoPortalDto(Guid Id, string Titulo, string Texto, bool Fixado, DateTimeOffset PublicadoEm,
    bool ExigeConfirmacao, bool Lido, bool Confirmado);

public sealed record NotificacaoDto(Guid Id, TipoNotificacao Tipo, string Titulo, string Texto, Guid? ReferenciaId,
    DateTimeOffset CriadaEm, bool Lida);

public sealed record CaixaNotificacoesDto(int NaoLidas, IReadOnlyList<NotificacaoDto> Itens);

public sealed class SalvarComunicadoRequestValidator : AbstractValidator<SalvarComunicadoRequest>
{
    public SalvarComunicadoRequestValidator()
    {
        RuleFor(x => x.Titulo).NotEmpty().WithMessage("Informe o título.").MaximumLength(150);
        RuleFor(x => x.Texto).NotEmpty().WithMessage("Escreva o comunicado.").MaximumLength(8000);
        RuleFor(x => x.Publico).IsInEnum();
    }
}

public interface IComunicacaoService
{
    Task<IReadOnlyList<ComunicadoDto>> ListarAsync(CancellationToken ct);
    Task<ComunicadoLeiturasDto> ObterAsync(Guid id, CancellationToken ct);
    Task<ComunicadoDto> CriarAsync(SalvarComunicadoRequest r, CancellationToken ct);
    Task<ComunicadoDto> AlterarAsync(Guid id, SalvarComunicadoRequest r, CancellationToken ct);
    Task<ComunicadoDto> PublicarAsync(Guid id, CancellationToken ct);
    Task ExcluirAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<ComunicadoPortalDto>> ComunicadosPortalAsync(CancellationToken ct);
    Task<ComunicadoPortalDto> MarcarLidoAsync(Guid id, bool confirmar, CancellationToken ct);

    Task<CaixaNotificacoesDto> MinhasNotificacoesAsync(CancellationToken ct);
    Task MarcarNotificacoesLidasAsync(Guid? id, CancellationToken ct);
}

/// <summary>Mural de comunicados (com confirmação de leitura) e caixa de notificações de cada usuário no clube.</summary>
public sealed class ComunicacaoService(
    IAppDbContext db,
    IUsuarioAtual atual,
    IPermissaoResolver permissoes,
    IAcessoPessoas acessoPessoas,
    INotificador notificador,
    TimeProvider relogio,
    IValidator<SalvarComunicadoRequest> validator) : IComunicacaoService
{
    private Guid ClubeId => atual.ClubeIdObrigatorio;
    private DateTimeOffset Agora => relogio.GetUtcNow();

    public async Task<IReadOnlyList<ComunicadoDto>> ListarAsync(CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Comunicacao.Visualizar, ct);
        var lista = await db.Comunicados.AsNoTracking().Where(c => c.ClubeId == ClubeId)
            .OrderByDescending(c => c.PublicadoEm == null).ThenByDescending(c => c.Fixado).ThenByDescending(c => c.PublicadoEm ?? c.CriadoEm)
            .Take(200).ToListAsync(ct);
        var ids = lista.Select(c => c.Id).ToList();
        var leituras = await db.LeiturasComunicados.Where(l => ids.Contains(l.ComunicadoId))
            .GroupBy(l => l.ComunicadoId)
            .Select(g => new { g.Key, Lidos = g.Count(), Confirmados = g.Count(l => l.ConfirmadoEm != null) })
            .ToDictionaryAsync(x => x.Key, ct);
        var todos = await Destinatarios(PublicoComunicado.Todos).CountAsync(ct);
        var titulares = await Destinatarios(PublicoComunicado.Titulares).CountAsync(ct);
        return lista.Select(c => Dto(c, c.Publico == PublicoComunicado.Todos ? todos : titulares,
            leituras.GetValueOrDefault(c.Id)?.Lidos ?? 0, leituras.GetValueOrDefault(c.Id)?.Confirmados ?? 0)).ToList();
    }

    public async Task<ComunicadoLeiturasDto> ObterAsync(Guid id, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Comunicacao.Visualizar, ct);
        var c = await CarregarAsync(id, ct);
        var leituras = await (from l in db.LeiturasComunicados
                              join p in db.Pessoas on l.PessoaId equals p.Id
                              where l.ComunicadoId == id
                              orderby l.LidoEm
                              select new LeituraComunicadoDto(p.Id, p.NomeCompleto, l.LidoEm, l.ConfirmadoEm)).ToListAsync(ct);
        var leram = leituras.Where(l => !c.ExigeConfirmacao || l.ConfirmadoEm != null).Select(l => l.PessoaId).ToHashSet();
        var destinatarios = await (from pid in Destinatarios(c.Publico)
                                   join p in db.Pessoas on pid equals p.Id
                                   select new { p.Id, p.NomeCompleto }).ToListAsync(ct);
        var pendentes = c.Publicado
            ? destinatarios.Where(d => !leram.Contains(d.Id)).Select(d => d.NomeCompleto).Order().ToList()
            : [];
        return new ComunicadoLeiturasDto(Dto(c, destinatarios.Count, leituras.Count, leituras.Count(l => l.ConfirmadoEm != null)),
            leituras, pendentes);
    }

    public async Task<ComunicadoDto> CriarAsync(SalvarComunicadoRequest r, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Comunicacao.Gerenciar, ct);
        await validator.ValidarAsync(r, ct);
        var c = new Comunicado(ClubeId, r.Titulo, r.Texto, r.Publico, r.ExigeConfirmacao, r.Fixado, r.ExpiraEm);
        db.Comunicados.Add(c);
        if (r.Publicar) c.Publicar(Agora);
        await db.SaveChangesAsync(ct);
        if (r.Publicar) await NotificarPublicacaoAsync(c, ct);
        return await ResumoAsync(c.Id, ct);
    }

    public async Task<ComunicadoDto> AlterarAsync(Guid id, SalvarComunicadoRequest r, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Comunicacao.Gerenciar, ct);
        await validator.ValidarAsync(r, ct);
        var c = await CarregarAsync(id, ct);
        c.Alterar(r.Titulo, r.Texto, r.Publico, r.ExigeConfirmacao, r.Fixado, r.ExpiraEm);
        var publicar = r.Publicar && !c.Publicado;
        if (publicar) c.Publicar(Agora);
        await db.SaveChangesAsync(ct);
        if (publicar) await NotificarPublicacaoAsync(c, ct);
        return await ResumoAsync(id, ct);
    }

    public async Task<ComunicadoDto> PublicarAsync(Guid id, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Comunicacao.Gerenciar, ct);
        var c = await CarregarAsync(id, ct);
        c.Publicar(Agora);
        await db.SaveChangesAsync(ct);
        await NotificarPublicacaoAsync(c, ct);
        return await ResumoAsync(id, ct);
    }

    public async Task ExcluirAsync(Guid id, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Comunicacao.Gerenciar, ct);
        db.Comunicados.Remove(await CarregarAsync(id, ct)); // exclusão lógica pelo interceptor
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ComunicadoPortalDto>> ComunicadosPortalAsync(CancellationToken ct)
    {
        var portal = await acessoPessoas.ObterPortalAsync(ct);
        if (portal.Dependente is { Permissoes.RecebimentoComunicados: false }) return [];
        var agora = Agora;
        var pessoaId = portal.Pessoa.Id;
        return await PortalQuery(portal.Titular)
            .Where(c => c.ExpiraEm == null || c.ExpiraEm > agora)
            .OrderByDescending(c => c.Fixado).ThenByDescending(c => c.PublicadoEm)
            .Take(30)
            .Select(c => new ComunicadoPortalDto(c.Id, c.Titulo, c.Texto, c.Fixado, c.PublicadoEm!.Value, c.ExigeConfirmacao,
                db.LeiturasComunicados.Any(l => l.ComunicadoId == c.Id && l.PessoaId == pessoaId),
                db.LeiturasComunicados.Any(l => l.ComunicadoId == c.Id && l.PessoaId == pessoaId && l.ConfirmadoEm != null)))
            .ToListAsync(ct);
    }

    public async Task<ComunicadoPortalDto> MarcarLidoAsync(Guid id, bool confirmar, CancellationToken ct)
    {
        var portal = await acessoPessoas.ObterPortalAsync(ct);
        if (portal.Dependente is { Permissoes.RecebimentoComunicados: false }) throw new NaoEncontradoException("Comunicado");
        var c = await PortalQuery(portal.Titular).FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NaoEncontradoException("Comunicado");
        var leitura = await db.LeiturasComunicados.FirstOrDefaultAsync(l => l.ComunicadoId == id && l.PessoaId == portal.Pessoa.Id, ct);
        if (leitura is null)
        {
            leitura = new LeituraComunicado(ClubeId, id, portal.Pessoa.Id, Agora);
            db.LeiturasComunicados.Add(leitura);
        }
        if (confirmar) leitura.Confirmar(Agora);
        await db.SaveChangesAsync(ct);
        return new ComunicadoPortalDto(c.Id, c.Titulo, c.Texto, c.Fixado, c.PublicadoEm!.Value, c.ExigeConfirmacao, true, leitura.ConfirmadoEm != null);
    }

    public async Task<CaixaNotificacoesDto> MinhasNotificacoesAsync(CancellationToken ct)
    {
        var usuario = atual.UsuarioIdObrigatorio;
        var q = db.Notificacoes.AsNoTracking().Where(n => n.ClubeId == ClubeId && n.UsuarioId == usuario);
        var naoLidas = await q.CountAsync(n => n.LidaEm == null, ct);
        var itens = await q.OrderByDescending(n => n.CriadaEm).Take(50)
            .Select(n => new NotificacaoDto(n.Id, n.Tipo, n.Titulo, n.Texto, n.ReferenciaId, n.CriadaEm, n.LidaEm != null))
            .ToListAsync(ct);
        return new CaixaNotificacoesDto(naoLidas, itens);
    }

    public async Task MarcarNotificacoesLidasAsync(Guid? id, CancellationToken ct)
    {
        var usuario = atual.UsuarioIdObrigatorio;
        var q = db.Notificacoes.Where(n => n.ClubeId == ClubeId && n.UsuarioId == usuario && n.LidaEm == null);
        if (id is { } i) q = q.Where(n => n.Id == i);
        foreach (var n in await q.Take(500).ToListAsync(ct)) n.MarcarLida(Agora);
        await db.SaveChangesAsync(ct);
    }

    // ---------------------------------------------------------------- auxiliares

    /// <summary>Pessoas que recebem o comunicado: titulares ativos e, para "Todos", dependentes ativos autorizados.</summary>
    private IQueryable<Guid> Destinatarios(PublicoComunicado publico)
    {
        var titulares = db.Associados.Where(a => a.ClubeId == ClubeId && a.Situacao == SituacaoCadastral.Ativo).Select(a => a.PessoaId);
        if (publico == PublicoComunicado.Titulares) return titulares;
        var dependentes = db.Dependentes.Where(d => d.ClubeId == ClubeId && d.Situacao == SituacaoCadastral.Ativo
                                                    && d.Permissoes.RecebimentoComunicados).Select(d => d.PessoaId);
        return titulares.Union(dependentes);
    }

    private IQueryable<Comunicado> PortalQuery(bool titular) =>
        db.Comunicados.AsNoTracking().Where(c => c.ClubeId == ClubeId && c.PublicadoEm != null
                                                 && (titular || c.Publico == PublicoComunicado.Todos));

    private async Task NotificarPublicacaoAsync(Comunicado c, CancellationToken ct) =>
        await notificador.NotificarPessoasAsync(await Destinatarios(c.Publico).ToListAsync(ct), TipoNotificacao.Comunicado, c.Titulo,
            c.ExigeConfirmacao ? "Novo comunicado do clube. Leia e confirme a leitura." : "Novo comunicado do clube.", c.Id, ct);

    private async Task<Comunicado> CarregarAsync(Guid id, CancellationToken ct) =>
        await db.Comunicados.FirstOrDefaultAsync(c => c.Id == id && c.ClubeId == ClubeId, ct) ?? throw new NaoEncontradoException("Comunicado");

    private async Task<ComunicadoDto> ResumoAsync(Guid id, CancellationToken ct) => (await ObterAsync(id, ct)).Comunicado;

    private static ComunicadoDto Dto(Comunicado c, int destinatarios, int leituras, int confirmacoes) =>
        new(c.Id, c.Titulo, c.Texto, c.Publico, c.ExigeConfirmacao, c.Fixado, c.PublicadoEm, c.ExpiraEm, c.CriadoEm,
            destinatarios, leituras, confirmacoes);
}
