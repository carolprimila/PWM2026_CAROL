using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjetoClubes.Application.Cadastros;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Application.Seguranca;
using ProjetoClubes.Domain.Common;
using ProjetoClubes.Domain.Eventos;
using ProjetoClubes.Domain.Reservas;

namespace ProjetoClubes.Application.Reservas;

public sealed record EspacoDto(Guid Id, string Nome, TipoEspaco Tipo, string? Descricao, int? Capacidade, decimal ValorLocacao,
    decimal TaxaAdicional, bool ExigeAprovacao, string? Regras, bool Ativo);

public sealed record SalvarEspacoRequest(string Nome, TipoEspaco Tipo, string? Descricao, int? Capacidade, decimal ValorLocacao,
    decimal TaxaAdicional, bool ExigeAprovacao, string? Regras, bool Ativo = true);

public sealed record ReservaDto(Guid Id, Guid EspacoId, string Espaco, Guid? PessoaId, string? Solicitante, Guid? EventoId,
    DateTimeOffset Inicio, DateTimeOffset Fim, string? Finalidade, decimal Valor, SituacaoReserva Situacao, string? Motivo,
    DateTimeOffset CriadaEm);

public sealed record SolicitarReservaRequest(Guid EspacoId, DateTimeOffset Inicio, DateTimeOffset Fim, string? Finalidade, Guid? PessoaId);

public sealed record AlterarReservaRequest(DateTimeOffset Inicio, DateTimeOffset Fim);

public sealed record ItemCalendarioDto(string Tipo, Guid Id, string Titulo, Guid? EspacoId, string? Espaco, DateTimeOffset Inicio,
    DateTimeOffset Fim, string Situacao);

public sealed class ConsultaReservas : ConsultaPaginada
{
    public Guid? EspacoId { get; set; }
    public SituacaoReserva? Situacao { get; set; }
    public DateTimeOffset? De { get; set; }
    public DateTimeOffset? Ate { get; set; }
}

public sealed class SalvarEspacoRequestValidator : AbstractValidator<SalvarEspacoRequest>
{
    public SalvarEspacoRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().WithMessage("Informe o nome do espaço.").MaximumLength(100);
        RuleFor(x => x.Tipo).IsInEnum();
        RuleFor(x => x.Capacidade).GreaterThan(0).When(x => x.Capacidade.HasValue);
        RuleFor(x => x.ValorLocacao).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TaxaAdicional).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Descricao).MaximumLength(1000);
        RuleFor(x => x.Regras).MaximumLength(2000);
    }
}

public sealed class SolicitarReservaRequestValidator : AbstractValidator<SolicitarReservaRequest>
{
    public SolicitarReservaRequestValidator()
    {
        RuleFor(x => x.EspacoId).NotEmpty().WithMessage("Selecione o espaço.");
        RuleFor(x => x.Fim).GreaterThan(x => x.Inicio).WithMessage("O término deve ser depois do início.");
        RuleFor(x => x).Must(x => x.Fim - x.Inicio <= TimeSpan.FromDays(31))
            .WithMessage("Uma reserva pode ter no máximo 31 dias.").OverridePropertyName("Fim");
        RuleFor(x => x.Finalidade).MaximumLength(200);
    }
}

public interface IReservaService
{
    Task<IReadOnlyList<EspacoDto>> ListarEspacosAsync(bool incluirInativos, CancellationToken ct);
    Task<EspacoDto> SalvarEspacoAsync(Guid? id, SalvarEspacoRequest r, CancellationToken ct);
    Task ExcluirEspacoAsync(Guid id, CancellationToken ct);
    Task<Pagina<ReservaDto>> ListarAsync(ConsultaReservas c, CancellationToken ct);
    Task<IReadOnlyList<ItemCalendarioDto>> CalendarioAsync(DateTimeOffset de, DateTimeOffset ate, Guid? espacoId, CancellationToken ct);
    Task<ReservaDto> CriarAsync(SolicitarReservaRequest r, CancellationToken ct);
    Task<ReservaDto> AlterarAsync(Guid id, AlterarReservaRequest r, CancellationToken ct);
    Task<ReservaDto> AprovarAsync(Guid id, CancellationToken ct);
    Task<ReservaDto> RecusarAsync(Guid id, string motivo, CancellationToken ct);
    Task<ReservaDto> CancelarAsync(Guid id, string motivo, CancellationToken ct);
    Task<IReadOnlyList<ReservaDto>> MinhasAsync(CancellationToken ct);
    Task<ReservaDto> SolicitarNoPortalAsync(SolicitarReservaRequest r, CancellationToken ct);
}

/// <summary>
/// Espaços e reservas. Conflitos são impedidos em duas camadas: verificação na API dentro de um bloqueio por espaço
/// (mensagem amigável) e restrição de exclusão no PostgreSQL (garantia final contra reservas simultâneas).
/// </summary>
public sealed class ReservaService(
    IAppDbContext db,
    IUsuarioAtual atual,
    IPermissaoResolver permissoes,
    IAcessoPessoas acessoPessoas,
    ITransacoes transacoes,
    Comunicacao.INotificador notificador,
    TimeProvider relogio,
    IValidator<SalvarEspacoRequest> espacoValidator,
    IValidator<SolicitarReservaRequest> reservaValidator) : IReservaService
{
    private Guid ClubeId => atual.ClubeIdObrigatorio;

    public async Task<IReadOnlyList<EspacoDto>> ListarEspacosAsync(bool incluirInativos, CancellationToken ct)
    {
        if (!await permissoes.PossuiAsync(Permissoes.Reservas.Visualizar, ct)) await acessoPessoas.ObterPortalAsync(ct);
        return await db.Espacos.AsNoTracking().Where(e => e.ClubeId == ClubeId && (incluirInativos || e.Ativo)).OrderBy(e => e.Nome)
            .Select(e => new EspacoDto(e.Id, e.Nome, e.Tipo, e.Descricao, e.Capacidade, e.ValorLocacao, e.TaxaAdicional, e.ExigeAprovacao, e.Regras, e.Ativo))
            .ToListAsync(ct);
    }

    public async Task<EspacoDto> SalvarEspacoAsync(Guid? id, SalvarEspacoRequest r, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Reservas.GerenciarEspacos, ct);
        await espacoValidator.ValidarAsync(r, ct);
        Espaco e;
        if (id is { } existente)
        {
            e = await db.Espacos.FirstOrDefaultAsync(x => x.Id == existente && x.ClubeId == ClubeId, ct) ?? throw new NaoEncontradoException("Espaço");
            e.Nome = r.Nome.Trim();
            e.Tipo = r.Tipo;
        }
        else
        {
            e = new Espaco(ClubeId, r.Nome, r.Tipo);
            db.Espacos.Add(e);
        }
        e.Descricao = r.Descricao?.Trim();
        e.Capacidade = r.Capacidade;
        e.ValorLocacao = r.ValorLocacao;
        e.TaxaAdicional = r.TaxaAdicional;
        e.ExigeAprovacao = r.ExigeAprovacao;
        e.Regras = r.Regras?.Trim();
        e.Ativo = r.Ativo;
        await db.SaveChangesAsync(ct);
        return new EspacoDto(e.Id, e.Nome, e.Tipo, e.Descricao, e.Capacidade, e.ValorLocacao, e.TaxaAdicional, e.ExigeAprovacao, e.Regras, e.Ativo);
    }

    public async Task ExcluirEspacoAsync(Guid id, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Reservas.GerenciarEspacos, ct);
        var e = await db.Espacos.FirstOrDefaultAsync(x => x.Id == id && x.ClubeId == ClubeId, ct) ?? throw new NaoEncontradoException("Espaço");
        var agora = relogio.GetUtcNow();
        if (await db.Reservas.AnyAsync(x => x.EspacoId == id && x.Fim > agora && (x.Situacao == SituacaoReserva.Aprovada || x.Situacao == SituacaoReserva.Pendente), ct))
            throw new ConflitoException("espaco.com_reservas", "Há reservas futuras neste espaço. Cancele-as ou desative o espaço.");
        db.Espacos.Remove(e);
        await db.SaveChangesAsync(ct);
    }

    public async Task<Pagina<ReservaDto>> ListarAsync(ConsultaReservas c, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Reservas.Visualizar, ct);
        var q = db.Reservas.AsNoTracking().Where(r => r.ClubeId == ClubeId);
        if (c.EspacoId is { } e) q = q.Where(r => r.EspacoId == e);
        if (c.Situacao is { } s) q = q.Where(r => r.Situacao == s);
        if (c.De is { } de) q = q.Where(r => r.Fim >= de);
        if (c.Ate is { } ate) q = q.Where(r => r.Inicio <= ate);
        return await Projetar(q.OrderBy(r => r.Inicio)).PaginarAsync(c, ct);
    }

    public async Task<IReadOnlyList<ItemCalendarioDto>> CalendarioAsync(DateTimeOffset de, DateTimeOffset ate, Guid? espacoId, CancellationToken ct)
    {
        if (!await permissoes.PossuiAsync(Permissoes.Reservas.Visualizar, ct)) await acessoPessoas.ObterPortalAsync(ct);
        var veDetalhes = await permissoes.PossuiAsync(Permissoes.Reservas.Visualizar, ct);
        var reservas = await db.Reservas.AsNoTracking()
            .Where(r => r.ClubeId == ClubeId && r.Inicio < ate && r.Fim > de && (r.Situacao == SituacaoReserva.Aprovada || r.Situacao == SituacaoReserva.Pendente)
                        && (espacoId == null || r.EspacoId == espacoId))
            .Select(r => new { r.Id, r.EventoId, r.Finalidade, r.EspacoId, Espaco = r.Espaco.Nome, r.Inicio, r.Fim, r.Situacao })
            .ToListAsync(ct);
        var eventos = await db.Eventos.AsNoTracking()
            .Where(e => e.ClubeId == ClubeId && e.Inicio < ate && e.Fim > de && e.Situacao != SituacaoEvento.Cancelado
                        && e.EspacoId == null && espacoId == null)
            .Select(e => new ItemCalendarioDto("Evento", e.Id, e.Nome, null, e.Local, e.Inicio, e.Fim, e.Situacao.ToString()))
            .ToListAsync(ct);

        // No portal mostramos só que o horário está ocupado, sem dados de quem reservou.
        return reservas.Select(r => new ItemCalendarioDto(r.EventoId is null ? "Reserva" : "Evento", r.EventoId ?? r.Id,
                veDetalhes || r.EventoId is not null ? r.Finalidade ?? "Reserva" : "Ocupado", r.EspacoId, r.Espaco, r.Inicio, r.Fim, r.Situacao.ToString()))
            .Concat(eventos).OrderBy(i => i.Inicio).ToList();
    }

    public async Task<ReservaDto> CriarAsync(SolicitarReservaRequest r, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Reservas.Aprovar, ct);
        await reservaValidator.ValidarAsync(r, ct);
        if (r.PessoaId is { } p && !await db.Pessoas.AnyAsync(x => x.Id == p && x.ClubeId == ClubeId, ct)) throw new NaoEncontradoException("Pessoa");
        return await ReservarAsync(r, r.PessoaId, aprovadaDireto: true, ct);
    }

    public async Task<ReservaDto> SolicitarNoPortalAsync(SolicitarReservaRequest r, CancellationToken ct)
    {
        await reservaValidator.ValidarAsync(r, ct);
        var portal = await acessoPessoas.ObterPortalAsync(ct);
        if (portal.Associado is { } a && a.Situacao != SituacaoCadastral.Ativo)
            throw new DomainException("reserva.nao_ativo", "Somente associados ativos podem solicitar reservas.");
        if (portal.Dependente is { } d && (d.Situacao != SituacaoCadastral.Ativo || !d.Permissoes.SolicitacaoReservas))
            throw new AcessoNegadoException("Você não tem permissão para solicitar reservas. Fale com o titular ou com a secretaria.");
        if (r.Inicio < relogio.GetUtcNow()) throw new DomainException("reserva.passado", "Não é possível reservar um horário que já passou.");
        return await ReservarAsync(r, portal.Pessoa.Id, aprovadaDireto: false, ct);
    }

    public async Task<ReservaDto> AlterarAsync(Guid id, AlterarReservaRequest r, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Reservas.Aprovar, ct);
        if (r.Fim <= r.Inicio) throw new DomainException("periodo.invalido", "O término deve ser depois do início.");
        var reserva = await db.Reservas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.ClubeId == ClubeId, ct) ?? throw new NaoEncontradoException("Reserva");
        await transacoes.ComBloqueioAsync($"espaco:{reserva.EspacoId}", async () =>
        {
            var x = await db.Reservas.FirstAsync(y => y.Id == id, ct);
            await ExigirSemConflitoAsync(x.EspacoId, r.Inicio, r.Fim, x.Id, ct);
            x.AlterarPeriodo(r.Inicio, r.Fim);
            await db.SaveChangesAsync(ct);
            return true;
        }, ct);
        return await ObterAsync(id, ct);
    }

    public async Task<ReservaDto> AprovarAsync(Guid id, CancellationToken ct)
    {
        var dto = await AlterarSituacaoAsync(id, r => r.Aprovar(), Permissoes.Reservas.Aprovar, ct);
        await AvisarSolicitanteAsync(id, "Reserva aprovada", "foi aprovada", ct);
        return dto;
    }

    public async Task<ReservaDto> RecusarAsync(Guid id, string motivo, CancellationToken ct)
    {
        var dto = await AlterarSituacaoAsync(id, r => r.Recusar(motivo), Permissoes.Reservas.Aprovar, ct);
        await AvisarSolicitanteAsync(id, "Reserva recusada", $"foi recusada. Motivo: {motivo.Trim()}", ct);
        return dto;
    }

    /// <summary>Avisa quem pediu a reserva pelo app (e o titular, se foi um dependente).</summary>
    private async Task AvisarSolicitanteAsync(Guid id, string titulo, string resultado, CancellationToken ct)
    {
        var r = await db.Reservas.AsNoTracking().Where(x => x.Id == id && x.PessoaId != null)
            .Select(x => new { x.PessoaId, x.Inicio, Espaco = x.Espaco.Nome }).FirstOrDefaultAsync(ct);
        if (r is null) return;
        await notificador.NotificarPessoasAsync([r.PessoaId!.Value], Domain.Comunicacao.TipoNotificacao.Reserva, titulo,
            $"Sua reserva de {r.Espaco} em {await notificador.FormatarAsync(r.Inicio, ct)} {resultado}", id, ct, incluirTitular: true);
    }

    public async Task<ReservaDto> CancelarAsync(Guid id, string motivo, CancellationToken ct)
    {
        var reserva = await db.Reservas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.ClubeId == ClubeId, ct) ?? throw new NaoEncontradoException("Reserva");
        if (!await permissoes.PossuiAsync(Permissoes.Reservas.Aprovar, ct))
        {
            if (reserva.PessoaId is not { } dono) throw new NaoEncontradoException("Reserva");
            await acessoPessoas.ExigirPortalSobreAsync(dono, ct);
        }
        return await AlterarSituacaoAsync(id, r => r.Cancelar(string.IsNullOrWhiteSpace(motivo) ? "Cancelada pelo solicitante" : motivo), null, ct);
    }

    public async Task<IReadOnlyList<ReservaDto>> MinhasAsync(CancellationToken ct)
    {
        var portal = await acessoPessoas.ObterPortalAsync(ct);
        var pessoas = new List<Guid> { portal.Pessoa.Id };
        if (portal.Associado is { } a)
            pessoas.AddRange(await db.Dependentes.Where(d => d.AssociadoTitularId == a.Id).Select(d => d.PessoaId).ToListAsync(ct));
        return await Projetar(db.Reservas.AsNoTracking()
                .Where(r => r.ClubeId == ClubeId && r.PessoaId != null && pessoas.Contains(r.PessoaId.Value))
                .OrderByDescending(r => r.Inicio)).Take(50).ToListAsync(ct);
    }

    // ---------------------------------------------------------------- auxiliares

    private async Task<ReservaDto> ReservarAsync(SolicitarReservaRequest r, Guid? pessoaId, bool aprovadaDireto, CancellationToken ct)
    {
        var espaco = await db.Espacos.AsNoTracking().FirstOrDefaultAsync(e => e.Id == r.EspacoId && e.ClubeId == ClubeId && e.Ativo, ct)
                     ?? throw new NaoEncontradoException("Espaço");
        var id = await transacoes.ComBloqueioAsync($"espaco:{espaco.Id}", async () =>
        {
            await ExigirSemConflitoAsync(espaco.Id, r.Inicio, r.Fim, null, ct);
            var reserva = new Reserva(ClubeId, espaco.Id, pessoaId, null, r.Inicio, r.Fim, r.Finalidade,
                espaco.ValorLocacao + espaco.TaxaAdicional, exigeAprovacao: !aprovadaDireto && espaco.ExigeAprovacao);
            db.Reservas.Add(reserva);
            await db.SaveChangesAsync(ct);
            return reserva.Id;
        }, ct);
        return await ObterAsync(id, ct);
    }

    private async Task ExigirSemConflitoAsync(Guid espacoId, DateTimeOffset inicio, DateTimeOffset fim, Guid? ignorar, CancellationToken ct)
    {
        var conflito = await db.Reservas.AsNoTracking()
            .Where(x => x.EspacoId == espacoId && x.Id != ignorar && (x.Situacao == SituacaoReserva.Aprovada || x.Situacao == SituacaoReserva.Pendente)
                        && x.Inicio < fim && inicio < x.Fim)
            .OrderBy(x => x.Inicio).Select(x => new { x.Inicio, x.Fim }).FirstOrDefaultAsync(ct);
        if (conflito is not null)
            throw new ConflitoException("conflito_horario",
                $"O espaço já está reservado entre {conflito.Inicio.ToLocalTime():dd/MM HH:mm} e {conflito.Fim.ToLocalTime():dd/MM HH:mm}. Escolha outro horário.");
    }

    private async Task<ReservaDto> AlterarSituacaoAsync(Guid id, Action<Reserva> acao, string? permissao, CancellationToken ct)
    {
        if (permissao is not null) await permissoes.ExigirAsync(permissao, ct);
        var r = await db.Reservas.FirstOrDefaultAsync(x => x.Id == id && x.ClubeId == ClubeId, ct) ?? throw new NaoEncontradoException("Reserva");
        acao(r);
        await db.SaveChangesAsync(ct);
        return await ObterAsync(id, ct);
    }

    private async Task<ReservaDto> ObterAsync(Guid id, CancellationToken ct) =>
        await Projetar(db.Reservas.AsNoTracking().Where(r => r.Id == id)).FirstAsync(ct);

    private IQueryable<ReservaDto> Projetar(IQueryable<Reserva> q) =>
        q.Select(r => new ReservaDto(r.Id, r.EspacoId, r.Espaco.Nome, r.PessoaId,
            db.Pessoas.Where(p => p.Id == r.PessoaId).Select(p => p.NomeCompleto).FirstOrDefault(),
            r.EventoId, r.Inicio, r.Fim, r.Finalidade, r.Valor, r.Situacao, r.Motivo, r.CriadoEm));
}
