using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjetoClubes.Application.Cadastros;
using ProjetoClubes.Application.Carteirinhas;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Application.Seguranca;
using ProjetoClubes.Domain.Common;
using ProjetoClubes.Domain.Eventos;
using ProjetoClubes.Domain.Reservas;

namespace ProjetoClubes.Application.Eventos;

public sealed record EventoDto(Guid Id, string Nome, string? Descricao, string? Local, Guid? EspacoId, string? Espaco,
    DateTimeOffset Inicio, DateTimeOffset Fim, PublicoEvento Publico, int? Capacidade, int LugaresOcupados, int NaListaEspera,
    decimal Valor, int MaximoAcompanhantes, SituacaoEvento Situacao, string? MotivoCancelamento,
    IReadOnlyList<LocalEventoDto> Locais, DateTimeOffset? DivulgadoEm);

/// <summary>Local (espaço) reservado para o evento.</summary>
public sealed record LocalEventoDto(Guid EspacoId, string Nome, int? Capacidade);

public sealed record LocalRequest(Guid EspacoId);

/// <summary>Resultado da divulgação: avisos/e-mails enviados e o texto pronto para WhatsApp e redes sociais.</summary>
public sealed record DivulgacaoDto(int AvisosNoApp, int Emails, string Texto, string LinkWhatsApp);

public sealed record InscricaoDto(Guid Id, Guid EventoId, Guid PessoaId, string Nome, int Acompanhantes, SituacaoInscricao Situacao,
    DateTimeOffset InscritoEm, DateTimeOffset? PresencaEm);

public sealed record EventoDetalheDto(EventoDto Evento, IReadOnlyList<InscricaoDto> Inscricoes);

public sealed record SalvarEventoRequest(string Nome, string? Descricao, string? Local, Guid? EspacoId, DateTimeOffset Inicio,
    DateTimeOffset Fim, PublicoEvento Publico, int? Capacidade, decimal Valor, int MaximoAcompanhantes);

public sealed record InscreverRequest(Guid? PessoaId, int Acompanhantes);

public sealed record PresencaRequest(string Codigo);

public sealed record PortalEventoDto(EventoDto Evento, IReadOnlyList<InscricaoDto> MinhasInscricoes);

public sealed class ConsultaEventos : ConsultaPaginada
{
    public bool? Futuros { get; set; }
    public SituacaoEvento? Situacao { get; set; }
}

public sealed class SalvarEventoRequestValidator : AbstractValidator<SalvarEventoRequest>
{
    public SalvarEventoRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().WithMessage("Informe o nome do evento.").MaximumLength(150);
        RuleFor(x => x.Descricao).MaximumLength(2000);
        RuleFor(x => x.Local).MaximumLength(150);
        RuleFor(x => x.Fim).GreaterThan(x => x.Inicio).WithMessage("O término deve ser depois do início.");
        RuleFor(x => x.Publico).IsInEnum();
        RuleFor(x => x.Capacidade).GreaterThan(0).When(x => x.Capacidade.HasValue).WithMessage("Capacidade deve ser maior que zero.");
        RuleFor(x => x.Valor).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaximoAcompanhantes).InclusiveBetween(0, 20);
    }
}

public interface IEventoService
{
    Task<Pagina<EventoDto>> ListarAsync(ConsultaEventos c, CancellationToken ct);
    Task<EventoDetalheDto> ObterAsync(Guid id, CancellationToken ct);
    Task<EventoDto> SalvarAsync(Guid? id, SalvarEventoRequest r, CancellationToken ct);
    Task<EventoDto> AdicionarLocalAsync(Guid id, Guid espacoId, CancellationToken ct);
    Task<EventoDto> RemoverLocalAsync(Guid id, Guid espacoId, CancellationToken ct);
    Task<DivulgacaoDto> DivulgarAsync(Guid id, CancellationToken ct);
    Task<EventoDto> AlterarInscricoesAsync(Guid id, bool abertas, CancellationToken ct);
    Task<EventoDto> CancelarAsync(Guid id, string motivo, CancellationToken ct);
    Task<InscricaoDto> InscreverAsync(Guid eventoId, InscreverRequest r, CancellationToken ct);
    Task CancelarInscricaoAsync(Guid inscricaoId, CancellationToken ct);
    Task<InscricaoDto> RegistrarPresencaAsync(Guid eventoId, PresencaRequest r, CancellationToken ct);
    Task<IReadOnlyList<PortalEventoDto>> PortalAsync(CancellationToken ct);
    Task<InscricaoDto> InscreverNoPortalAsync(Guid eventoId, InscreverRequest r, CancellationToken ct);
}

/// <summary>Eventos, inscrições (com acompanhantes e lista de espera) e presença por QR Code da carteirinha.</summary>
public sealed class EventoService(
    IAppDbContext db,
    IUsuarioAtual atual,
    IPermissaoResolver permissoes,
    IAcessoPessoas acessoPessoas,
    ITransacoes transacoes,
    Comunicacao.INotificador notificador,
    IEmailSender email,
    TimeProvider relogio,
    IValidator<SalvarEventoRequest> validator) : IEventoService
{
    private Guid ClubeId => atual.ClubeIdObrigatorio;
    private DateTimeOffset Agora => relogio.GetUtcNow();

    public async Task<Pagina<EventoDto>> ListarAsync(ConsultaEventos c, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Eventos.Visualizar, ct);
        var q = db.Eventos.AsNoTracking().Where(e => e.ClubeId == ClubeId);
        if (c.Futuros == true) { var agora = Agora; q = q.Where(e => e.Fim >= agora); }
        if (c.Situacao is { } s) q = q.Where(e => e.Situacao == s);
        if (!string.IsNullOrWhiteSpace(c.Busca)) q = q.Where(e => e.Nome.ToLower().Contains(c.Busca.Trim().ToLower()));
        var total = await q.CountAsync(ct);
        var ids = await q.OrderBy(e => e.Inicio).Skip((c.Pagina - 1) * c.TamanhoPagina).Take(c.TamanhoPagina).Select(e => e.Id).ToListAsync(ct);
        return new Pagina<EventoDto>(await DtosAsync(ids, ct), c.Pagina, c.TamanhoPagina, total);
    }

    public async Task<EventoDetalheDto> ObterAsync(Guid id, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Eventos.Visualizar, ct);
        var dto = (await DtosAsync([id], ct)).SingleOrDefault() ?? throw new NaoEncontradoException("Evento");
        return new EventoDetalheDto(dto, await InscricoesAsync(i => i.EventoId == id, ct));
    }

    public async Task<EventoDto> SalvarAsync(Guid? id, SalvarEventoRequest r, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Eventos.Gerenciar, ct);
        await validator.ValidarAsync(r, ct);
        Espaco? espaco = null;
        if (r.EspacoId is { } eid)
            espaco = await db.Espacos.FirstOrDefaultAsync(e => e.Id == eid && e.ClubeId == ClubeId && e.Ativo, ct)
                     ?? throw new NaoEncontradoException("Espaço");

        // O evento ocupa o espaço no período: a reserva do evento passa pela mesma proteção contra conflitos.
        var chave = espaco is null ? $"evento:{id ?? Guid.NewGuid()}" : $"espaco:{espaco.Id}";
        var eventoId = await transacoes.ComBloqueioAsync(chave, async () =>
        {
            Evento e;
            if (id is { } existente)
            {
                e = await db.Eventos.FirstOrDefaultAsync(x => x.Id == existente && x.ClubeId == ClubeId, ct) ?? throw new NaoEncontradoException("Evento");
                e.Nome = r.Nome.Trim();
                e.DefinirPeriodo(r.Inicio, r.Fim);
            }
            else
            {
                e = new Evento(ClubeId, r.Nome, r.Inicio, r.Fim, r.Publico);
                db.Eventos.Add(e);
            }
            var principalAnterior = e.EspacoId;
            e.Descricao = r.Descricao?.Trim();
            e.Local = r.Local?.Trim() ?? espaco?.Nome;
            e.Publico = r.Publico;
            e.Capacidade = r.Capacidade;
            e.Valor = r.Valor;
            e.MaximoAcompanhantes = r.MaximoAcompanhantes;
            e.EspacoId = espaco?.Id;

            // O evento pode ocupar vários locais (um principal + os adicionados depois): todos acompanham o período.
            var reservas = await db.Reservas.Where(x => x.EventoId == e.Id
                && (x.Situacao == SituacaoReserva.Aprovada || x.Situacao == SituacaoReserva.Pendente)).ToListAsync(ct);
            var trocado = reservas.FirstOrDefault(x => x.EspacoId == principalAnterior && x.EspacoId != espaco?.Id);
            if (trocado is not null) { trocado.Cancelar("Evento alterado"); reservas.Remove(trocado); }
            foreach (var x in reservas)
            {
                await ExigirSemConflitoAsync(x.EspacoId, r.Inicio, r.Fim, x.Id, ct);
                x.AlterarPeriodo(r.Inicio, r.Fim);
            }
            if (espaco is not null && reservas.All(x => x.EspacoId != espaco.Id))
            {
                await ExigirSemConflitoAsync(espaco.Id, r.Inicio, r.Fim, null, ct);
                db.Reservas.Add(new Reserva(ClubeId, espaco.Id, null, e.Id, r.Inicio, r.Fim, $"Evento: {e.Nome}", 0, exigeAprovacao: false));
            }

            // Os locais precisam comportar o público do evento.
            var locais = reservas.Select(x => x.EspacoId).Append(espaco?.Id ?? Guid.Empty).Where(x => x != Guid.Empty).Distinct().ToList();
            var capacidades = await db.Espacos.Where(x => locais.Contains(x.Id)).Select(x => x.Capacidade).ToListAsync(ct);
            if (r.Capacidade is { } publico && capacidades.Count > 0 && capacidades.All(c => c is not null) && publico > capacidades.Sum(c => c!.Value))
                throw new DomainException("evento.capacidade",
                    $"Os locais escolhidos comportam {capacidades.Sum(c => c!.Value)} pessoas; o evento prevê {publico}.");
            await db.SaveChangesAsync(ct);
            return e.Id;
        }, ct);
        return (await DtosAsync([eventoId], ct)).Single();
    }

    public async Task<EventoDto> AlterarInscricoesAsync(Guid id, bool abertas, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Eventos.Gerenciar, ct);
        var e = await db.Eventos.FirstOrDefaultAsync(x => x.Id == id && x.ClubeId == ClubeId, ct) ?? throw new NaoEncontradoException("Evento");
        if (abertas) e.ReabrirInscricoes(); else e.EncerrarInscricoes();
        await db.SaveChangesAsync(ct);
        return (await DtosAsync([id], ct)).Single();
    }

    public async Task<EventoDto> CancelarAsync(Guid id, string motivo, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Eventos.Gerenciar, ct);
        var e = await db.Eventos.FirstOrDefaultAsync(x => x.Id == id && x.ClubeId == ClubeId, ct) ?? throw new NaoEncontradoException("Evento");
        e.Cancelar(motivo);
        foreach (var r in await db.Reservas.Where(x => x.EventoId == id && (x.Situacao == SituacaoReserva.Aprovada || x.Situacao == SituacaoReserva.Pendente)).ToListAsync(ct))
            r.Cancelar("Evento cancelado");
        await db.SaveChangesAsync(ct);
        return (await DtosAsync([id], ct)).Single();
    }

    public async Task<InscricaoDto> InscreverAsync(Guid eventoId, InscreverRequest r, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Eventos.Gerenciar, ct);
        var pessoaId = r.PessoaId ?? throw new NaoEncontradoException("Pessoa");
        return await InscreverInternoAsync(eventoId, pessoaId, r.Acompanhantes, ct);
    }

    public async Task<InscricaoDto> InscreverNoPortalAsync(Guid eventoId, InscreverRequest r, CancellationToken ct)
    {
        var portal = await acessoPessoas.ObterPortalAsync(ct);
        var pessoaId = r.PessoaId ?? portal.Pessoa.Id;
        if (pessoaId != portal.Pessoa.Id) await acessoPessoas.ExigirPortalSobreAsync(pessoaId, ct); // titular → próprio dependente
        return await InscreverInternoAsync(eventoId, pessoaId, r.Acompanhantes, ct);
    }

    public async Task CancelarInscricaoAsync(Guid inscricaoId, CancellationToken ct)
    {
        var i = await db.InscricoesEventos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == inscricaoId && x.ClubeId == ClubeId, ct)
                ?? throw new NaoEncontradoException("Inscrição");
        if (!await permissoes.PossuiAsync(Permissoes.Eventos.Gerenciar, ct))
            await acessoPessoas.ExigirPortalSobreAsync(i.PessoaId, ct); // a própria pessoa ou o titular dela

        await transacoes.ComBloqueioAsync($"evento:{i.EventoId}", async () =>
        {
            var inscricao = await db.InscricoesEventos.FirstAsync(x => x.Id == inscricaoId, ct);
            if (inscricao.Situacao == SituacaoInscricao.Cancelada) return true;
            inscricao.Cancelar();
            await db.SaveChangesAsync(ct);

            // Libera vagas para a lista de espera, por ordem de chegada.
            var evento = await db.Eventos.FirstAsync(x => x.Id == inscricao.EventoId, ct);
            var ocupados = await LugaresOcupadosAsync(evento.Id, ct);
            var espera = await db.InscricoesEventos.Where(x => x.EventoId == evento.Id && x.Situacao == SituacaoInscricao.ListaEspera)
                .OrderBy(x => x.CriadoEm).ToListAsync(ct);
            foreach (var p in RegrasEventos.Promover(evento.Capacidade, ocupados, espera).ToList()) p.Promover();
            await db.SaveChangesAsync(ct);
            return true;
        }, ct);
    }

    public async Task<InscricaoDto> RegistrarPresencaAsync(Guid eventoId, PresencaRequest r, CancellationToken ct)
    {
        var minhas = await permissoes.ObterDoUsuarioAtualAsync(ct);
        if (!minhas.Contains(Permissoes.Eventos.Gerenciar) && !minhas.Contains(Permissoes.ControleAcesso.Validar))
            throw new AcessoNegadoException();
        var token = CodigoCarteirinha.Extrair(r.Codigo);
        var pessoa = token is null ? null : await db.Pessoas.AsNoTracking().FirstOrDefaultAsync(p => p.TokenCarteirinha == token && p.ClubeId == ClubeId, ct);
        if (pessoa is null) throw new DomainException("evento.carteirinha", "Carteirinha não reconhecida neste clube.");
        var i = await db.InscricoesEventos.FirstOrDefaultAsync(x => x.EventoId == eventoId && x.PessoaId == pessoa.Id && x.Situacao != SituacaoInscricao.Cancelada, ct)
                ?? throw new DomainException("evento.sem_inscricao", $"{pessoa.NomeCompleto} não está inscrito(a) neste evento.");
        i.RegistrarPresenca(Agora);
        await db.SaveChangesAsync(ct);
        return (await InscricoesAsync(x => x.Id == i.Id, ct)).Single();
    }

    /// <summary>Próximos eventos que o associado/dependente pode ver, com as inscrições dele e dos dependentes.</summary>
    public async Task<IReadOnlyList<PortalEventoDto>> PortalAsync(CancellationToken ct)
    {
        var portal = await acessoPessoas.ObterPortalAsync(ct);
        var agora = Agora;
        var dependente = portal.Dependente is not null;
        var ids = await db.Eventos.AsNoTracking()
            .Where(e => e.ClubeId == ClubeId && e.Fim >= agora && e.Situacao != SituacaoEvento.Cancelado
                        && (!dependente || e.Publico != PublicoEvento.Associados))
            .OrderBy(e => e.Inicio).Take(50).Select(e => e.Id).ToListAsync(ct);
        var eventos = await DtosAsync(ids, ct);

        var pessoas = new List<Guid> { portal.Pessoa.Id };
        if (portal.Associado is { } a)
            pessoas.AddRange(await db.Dependentes.Where(d => d.AssociadoTitularId == a.Id && d.Situacao == SituacaoCadastral.Ativo)
                .Select(d => d.PessoaId).ToListAsync(ct));
        var inscricoes = await InscricoesAsync(i => ids.Contains(i.EventoId) && pessoas.Contains(i.PessoaId) && i.Situacao != SituacaoInscricao.Cancelada, ct);
        return eventos.Select(e => new PortalEventoDto(e, inscricoes.Where(i => i.EventoId == e.Id).ToList())).ToList();
    }

    // ---------------------------------------------------------------- auxiliares

    private async Task<InscricaoDto> InscreverInternoAsync(Guid eventoId, Guid pessoaId, int acompanhantes, CancellationToken ct)
    {
        var id = await transacoes.ComBloqueioAsync($"evento:{eventoId}", async () =>
        {
            var evento = await db.Eventos.FirstOrDefaultAsync(x => x.Id == eventoId && x.ClubeId == ClubeId, ct)
                         ?? throw new NaoEncontradoException("Evento");
            evento.ExigirInscricoesAbertas(Agora);
            if (acompanhantes < 0 || acompanhantes > evento.MaximoAcompanhantes)
                throw new DomainException("evento.acompanhantes", evento.MaximoAcompanhantes == 0
                    ? "Este evento não permite acompanhantes."
                    : $"Este evento permite até {evento.MaximoAcompanhantes} acompanhante(s) por inscrito.");
            await ExigirElegivelAsync(evento, pessoaId, ct);
            if (await db.InscricoesEventos.AnyAsync(x => x.EventoId == eventoId && x.PessoaId == pessoaId && x.Situacao != SituacaoInscricao.Cancelada, ct))
                throw new ConflitoException("evento.ja_inscrito", "Esta pessoa já está inscrita neste evento.");

            var situacao = RegrasEventos.SituacaoParaNovaInscricao(evento.Capacidade, await LugaresOcupadosAsync(eventoId, ct), 1 + acompanhantes);
            var i = new InscricaoEvento(ClubeId, eventoId, pessoaId, acompanhantes, situacao);
            db.InscricoesEventos.Add(i);
            await db.SaveChangesAsync(ct);
            return i.Id;
        }, ct);
        return (await InscricoesAsync(x => x.Id == id, ct)).Single();
    }

    /// <summary>Quem pode se inscrever: associado ativo; dependente ativo, com titular ativo e permissão de eventos.</summary>
    private async Task ExigirElegivelAsync(Evento evento, Guid pessoaId, CancellationToken ct)
    {
        if (await db.Associados.AnyAsync(a => a.PessoaId == pessoaId && a.Situacao == SituacaoCadastral.Ativo, ct)) return;

        var dependente = await db.Dependentes.AnyAsync(d => d.PessoaId == pessoaId && d.Situacao == SituacaoCadastral.Ativo
                                                           && d.AssociadoTitular.Situacao == SituacaoCadastral.Ativo
                                                           && d.Permissoes.ParticipacaoEventos, ct);
        if (!dependente) throw new DomainException("evento.nao_elegivel", "Somente associados e dependentes ativos podem se inscrever.");
        if (evento.Publico == PublicoEvento.Associados)
            throw new DomainException("evento.somente_associados", "Este evento é exclusivo para associados titulares.");
    }

    private async Task<int> LugaresOcupadosAsync(Guid eventoId, CancellationToken ct) =>
        await db.InscricoesEventos.Where(x => x.EventoId == eventoId && x.Situacao == SituacaoInscricao.Confirmada)
            .SumAsync(x => (int?)(1 + x.Acompanhantes), ct) ?? 0;

    /// <summary>Reserva mais um local (quadra, quiosque...) para o evento, no mesmo período, se estiver livre.</summary>
    public async Task<EventoDto> AdicionarLocalAsync(Guid id, Guid espacoId, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Eventos.Gerenciar, ct);
        await transacoes.ComBloqueioAsync($"espaco:{espacoId}", async () =>
        {
            var e = await db.Eventos.FirstOrDefaultAsync(x => x.Id == id && x.ClubeId == ClubeId, ct) ?? throw new NaoEncontradoException("Evento");
            if (e.Situacao == SituacaoEvento.Cancelado) throw new DomainException("evento.cancelado", "O evento está cancelado.");
            var espaco = await db.Espacos.FirstOrDefaultAsync(x => x.Id == espacoId && x.ClubeId == ClubeId && x.Ativo, ct)
                         ?? throw new NaoEncontradoException("Espaço");
            if (await db.Reservas.AnyAsync(x => x.EventoId == id && x.EspacoId == espacoId
                    && (x.Situacao == SituacaoReserva.Aprovada || x.Situacao == SituacaoReserva.Pendente), ct))
                throw new ConflitoException("evento.local_repetido", "Este local já faz parte do evento.");
            await ExigirSemConflitoAsync(espacoId, e.Inicio, e.Fim, null, ct);
            db.Reservas.Add(new Reserva(ClubeId, espacoId, null, e.Id, e.Inicio, e.Fim, $"Evento: {e.Nome}", 0, exigeAprovacao: false));
            e.Local ??= espaco.Nome;
            await db.SaveChangesAsync(ct);
            return true;
        }, ct);
        return await ObterDtoAsync(id, ct);
    }

    public async Task<EventoDto> RemoverLocalAsync(Guid id, Guid espacoId, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Eventos.Gerenciar, ct);
        var e = await db.Eventos.FirstOrDefaultAsync(x => x.Id == id && x.ClubeId == ClubeId, ct) ?? throw new NaoEncontradoException("Evento");
        var reserva = await db.Reservas.FirstOrDefaultAsync(x => x.EventoId == id && x.EspacoId == espacoId
                          && (x.Situacao == SituacaoReserva.Aprovada || x.Situacao == SituacaoReserva.Pendente), ct)
                      ?? throw new NaoEncontradoException("Local do evento");
        reserva.Cancelar("Local removido do evento");
        if (e.EspacoId == espacoId) e.EspacoId = null;
        await db.SaveChangesAsync(ct);
        return await ObterDtoAsync(id, ct);
    }

    /// <summary>
    /// Divulga o evento: aviso no app e e-mail para o público do evento. Devolve o texto pronto e o link de
    /// compartilhamento do WhatsApp (para grupos ou redes sociais, enviados por quem divulga).
    /// </summary>
    public async Task<DivulgacaoDto> DivulgarAsync(Guid id, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Eventos.Gerenciar, ct);
        var e = await db.Eventos.FirstOrDefaultAsync(x => x.Id == id && x.ClubeId == ClubeId, ct) ?? throw new NaoEncontradoException("Evento");
        if (e.Situacao == SituacaoEvento.Cancelado) throw new DomainException("evento.cancelado", "O evento está cancelado.");
        var clube = await db.Clubes.Where(c => c.Id == ClubeId).Select(c => c.Nome).FirstAsync(ct);
        var quando = await notificador.FormatarAsync(e.Inicio, ct, "dd/MM/yyyy 'às' HH:mm");
        var texto = $"{clube} convida: {e.Nome} — {quando}{(e.Local is null ? "" : $", {e.Local}")}." +
                    $"{(e.Valor > 0 ? $" Valor: {e.Valor:C} por pessoa." : " Entrada gratuita.")}" +
                    $"{(e.Descricao is null ? "" : $" {e.Descricao}")} Inscrições pelo aplicativo do clube.";

        var titulares = db.Associados.Where(a => a.ClubeId == ClubeId && a.Situacao == SituacaoCadastral.Ativo).Select(a => a.PessoaId);
        var pessoas = await (e.Publico == PublicoEvento.Associados ? titulares
            : titulares.Union(db.Dependentes.Where(d => d.ClubeId == ClubeId && d.Situacao == SituacaoCadastral.Ativo
                                                        && d.Permissoes.ParticipacaoEventos).Select(d => d.PessoaId))).ToListAsync(ct);
        await notificador.NotificarPessoasAsync(pessoas, Domain.Comunicacao.TipoNotificacao.Evento, $"Novo evento: {e.Nome}", texto, e.Id, ct);
        var avisos = await db.Notificacoes.CountAsync(n => n.ReferenciaId == e.Id && n.Tipo == Domain.Comunicacao.TipoNotificacao.Evento, ct);

        var emails = await db.Pessoas.Where(p => pessoas.Contains(p.Id) && p.Email != null).Select(p => p.Email!).Distinct().ToListAsync(ct);
        foreach (var destino in emails)
            await email.EnviarAsync(new MensagemEmail(destino, $"{clube}: {e.Nome}", texto), ct);

        e.DivulgadoEm = Agora;
        await db.SaveChangesAsync(ct);
        return new DivulgacaoDto(avisos, emails.Count, texto, "https://wa.me/?text=" + Uri.EscapeDataString(texto));
    }

    private async Task<EventoDto> ObterDtoAsync(Guid id, CancellationToken ct) => (await DtosAsync([id], ct)).Single();

    private async Task ExigirSemConflitoAsync(Guid espacoId, DateTimeOffset inicio, DateTimeOffset fim, Guid? ignorar, CancellationToken ct)
    {
        var conflito = await db.Reservas.AnyAsync(x => x.EspacoId == espacoId && x.Id != ignorar
            && (x.Situacao == SituacaoReserva.Aprovada || x.Situacao == SituacaoReserva.Pendente)
            && x.Inicio < fim && inicio < x.Fim, ct);
        if (conflito) throw new ConflitoException("conflito_horario", "O espaço já está reservado em parte deste período.");
    }

    private async Task<List<EventoDto>> DtosAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return [];
        var lista = await db.Eventos.AsNoTracking().Where(e => ids.Contains(e.Id) && e.ClubeId == ClubeId)
            .Select(e => new EventoDto(e.Id, e.Nome, e.Descricao, e.Local, e.EspacoId,
                db.Espacos.Where(s => s.Id == e.EspacoId).Select(s => s.Nome).FirstOrDefault(),
                e.Inicio, e.Fim, e.Publico, e.Capacidade,
                db.InscricoesEventos.Where(i => i.EventoId == e.Id && i.Situacao == SituacaoInscricao.Confirmada).Sum(i => 1 + i.Acompanhantes),
                db.InscricoesEventos.Count(i => i.EventoId == e.Id && i.Situacao == SituacaoInscricao.ListaEspera),
                e.Valor, e.MaximoAcompanhantes, e.Situacao, e.MotivoCancelamento, new List<LocalEventoDto>(), e.DivulgadoEm))
            .ToListAsync(ct);
        var locais = await db.Reservas.AsNoTracking()
            .Where(r => r.EventoId != null && ids.Contains(r.EventoId.Value)
                        && (r.Situacao == SituacaoReserva.Aprovada || r.Situacao == SituacaoReserva.Pendente))
            .Select(r => new { EventoId = r.EventoId!.Value, Local = new LocalEventoDto(r.EspacoId, r.Espaco.Nome, r.Espaco.Capacidade) })
            .ToListAsync(ct);
        return ids.Select(id => lista.FirstOrDefault(e => e.Id == id)).OfType<EventoDto>()
            .Select(e => e with { Locais = locais.Where(l => l.EventoId == e.Id).Select(l => l.Local).ToList() }).ToList();
    }

    private async Task<List<InscricaoDto>> InscricoesAsync(System.Linq.Expressions.Expression<Func<InscricaoEvento, bool>> filtro, CancellationToken ct) =>
        await db.InscricoesEventos.AsNoTracking().Where(i => i.ClubeId == ClubeId).Where(filtro)
            .OrderBy(i => i.Situacao).ThenBy(i => i.CriadoEm)
            .Select(i => new InscricaoDto(i.Id, i.EventoId, i.PessoaId,
                db.Pessoas.Where(p => p.Id == i.PessoaId).Select(p => p.NomeCompleto).FirstOrDefault() ?? "—",
                i.Acompanhantes, i.Situacao, i.CriadoEm, i.PresencaEm))
            .ToListAsync(ct);
}
