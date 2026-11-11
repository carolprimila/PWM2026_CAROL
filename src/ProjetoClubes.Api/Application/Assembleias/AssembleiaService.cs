using System.Text;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjetoClubes.Application.Cadastros;
using ProjetoClubes.Application.Carteirinhas;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Application.Comunicacao;
using ProjetoClubes.Application.Seguranca;
using ProjetoClubes.Domain.Assembleias;
using ProjetoClubes.Domain.Common;
using ProjetoClubes.Domain.Comunicacao;
using ProjetoClubes.Domain.Financeiro;

namespace ProjetoClubes.Application.Assembleias;

public sealed record AssembleiaListaDto(Guid Id, string Titulo, TipoAssembleia Tipo, DateTimeOffset DataHora, string? Local,
    SituacaoAssembleia Situacao, int Pautas, int Presentes);

public sealed record ApuracaoDto(string Opcao, int Votos);

public sealed record PautaDto(Guid Id, int Ordem, string Titulo, string? Descricao, bool ComVotacao, IReadOnlyList<string> Opcoes,
    SituacaoVotacao? Votacao, int TotalVotos, IReadOnlyList<ApuracaoDto>? Apuracao, string? Decisao);

public sealed record PresencaDto(Guid AssociadoId, string Nome, string Matricula, bool AptoVotar, string? MotivoInapto, DateTimeOffset RegistradaEm,
    bool PeloApp = false);

public sealed record AssembleiaDto(Guid Id, string Titulo, TipoAssembleia Tipo, DateTimeOffset DataHora, string? Local, string? Convocacao,
    bool ExigeAdimplencia, SituacaoAssembleia Situacao, DateTimeOffset? ConvocadaEm, DateTimeOffset? EncerradaEm, string? MotivoCancelamento,
    string? Ata, DateTimeOffset? AtaRegistradaEm, string? AtaSugerida, int Presentes, int Aptos, IReadOnlyList<PautaDto> Pautas,
    IReadOnlyList<PresencaDto> Presencas, bool ParticipacaoPeloApp = false);

public sealed record SalvarAssembleiaRequest(string Titulo, TipoAssembleia Tipo, DateTimeOffset DataHora, string? Local, string? Convocacao,
    bool ExigeAdimplencia, bool ParticipacaoPeloApp = false);

public sealed record PautaRequest(string Titulo, string? Descricao, bool ComVotacao, IReadOnlyList<string>? Opcoes);

/// <summary>Presença pelo QR da carteirinha (<see cref="Codigo"/>) ou escolhendo o associado (<see cref="AssociadoId"/>).</summary>
public sealed record RegistrarPresencaAssembleiaRequest(string? Codigo, Guid? AssociadoId);

public sealed record TextoRequest(string Texto);

public sealed record VotarRequest(int Opcao);

public sealed record PautaPortalDto(Guid Id, int Ordem, string Titulo, string? Descricao, bool ComVotacao, IReadOnlyList<string> Opcoes,
    SituacaoVotacao? Votacao, bool Votei, IReadOnlyList<ApuracaoDto>? Apuracao, string? Decisao);

public sealed record AssembleiaPortalDto(Guid Id, string Titulo, TipoAssembleia Tipo, DateTimeOffset DataHora, string? Local,
    string? Convocacao, SituacaoAssembleia Situacao, bool Presente, bool AptoVotar, string? MotivoInapto, IReadOnlyList<PautaPortalDto> Pautas,
    bool ParticipacaoPeloApp = false, bool PresentePeloApp = false);

public sealed class SalvarAssembleiaRequestValidator : AbstractValidator<SalvarAssembleiaRequest>
{
    public SalvarAssembleiaRequestValidator()
    {
        RuleFor(x => x.Titulo).NotEmpty().WithMessage("Informe o título.").MaximumLength(150);
        RuleFor(x => x.Tipo).IsInEnum();
        RuleFor(x => x.Local).MaximumLength(150);
        RuleFor(x => x.Convocacao).MaximumLength(8000);
    }
}

public sealed class PautaRequestValidator : AbstractValidator<PautaRequest>
{
    public PautaRequestValidator()
    {
        RuleFor(x => x.Titulo).NotEmpty().WithMessage("Informe o título da pauta.").MaximumLength(200);
        RuleFor(x => x.Descricao).MaximumLength(4000);
        RuleForEach(x => x.Opcoes).MaximumLength(60);
    }
}

public interface IAssembleiaService
{
    Task<IReadOnlyList<AssembleiaListaDto>> ListarAsync(CancellationToken ct);
    Task<AssembleiaDto> ObterAsync(Guid id, CancellationToken ct);
    Task<AssembleiaDto> CriarAsync(SalvarAssembleiaRequest r, CancellationToken ct);
    Task<AssembleiaDto> AlterarAsync(Guid id, SalvarAssembleiaRequest r, CancellationToken ct);
    Task<AssembleiaDto> AdicionarPautaAsync(Guid id, PautaRequest r, CancellationToken ct);
    Task<AssembleiaDto> RemoverPautaAsync(Guid id, Guid pautaId, CancellationToken ct);
    Task<AssembleiaDto> ConvocarAsync(Guid id, CancellationToken ct);
    Task<AssembleiaDto> InstalarAsync(Guid id, CancellationToken ct);
    Task<PresencaDto> RegistrarPresencaAsync(Guid id, RegistrarPresencaAssembleiaRequest r, CancellationToken ct);
    Task<AssembleiaDto> AbrirVotacaoAsync(Guid id, Guid pautaId, CancellationToken ct);
    Task<AssembleiaDto> EncerrarVotacaoAsync(Guid id, Guid pautaId, CancellationToken ct);
    Task<AssembleiaDto> RegistrarDecisaoAsync(Guid id, Guid pautaId, string decisao, CancellationToken ct);
    Task<AssembleiaDto> EncerrarAsync(Guid id, CancellationToken ct);
    Task<AssembleiaDto> RegistrarAtaAsync(Guid id, string texto, CancellationToken ct);
    Task<AssembleiaDto> CancelarAsync(Guid id, string motivo, CancellationToken ct);

    Task<IReadOnlyList<AssembleiaPortalDto>> PortalAsync(CancellationToken ct);
    Task<AssembleiaPortalDto> VotarAsync(Guid id, Guid pautaId, int opcao, CancellationToken ct);
    /// <summary>O associado confirma presença pelo app (assembleia em andamento que permite participação pelo app).</summary>
    Task<AssembleiaPortalDto> ParticiparPeloAppAsync(Guid id, CancellationToken ct);
}

/// <summary>
/// Assembleias e votações. Só associados titulares ativos, de categoria com direito a voto (e adimplentes, quando a
/// assembleia exigir), presentes, votam — uma vez por pauta. Dependentes nunca participam.
/// </summary>
public sealed class AssembleiaService(
    IAppDbContext db,
    IUsuarioAtual atual,
    IPermissaoResolver permissoes,
    IAcessoPessoas acessoPessoas,
    INotificador notificador,
    TimeProvider relogio,
    IValidator<SalvarAssembleiaRequest> validator,
    IValidator<PautaRequest> pautaValidator) : IAssembleiaService
{
    private Guid ClubeId => atual.ClubeIdObrigatorio;
    private DateTimeOffset Agora => relogio.GetUtcNow();
    private DateOnly Hoje => DateOnly.FromDateTime(Agora.UtcDateTime);

    public async Task<IReadOnlyList<AssembleiaListaDto>> ListarAsync(CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Assembleias.Visualizar, ct);
        return await db.Assembleias.AsNoTracking().Where(a => a.ClubeId == ClubeId)
            .OrderByDescending(a => a.DataHora).Take(100)
            .Select(a => new AssembleiaListaDto(a.Id, a.Titulo, a.Tipo, a.DataHora, a.Local, a.Situacao, a.Pautas.Count,
                db.PresencasAssembleias.Count(p => p.AssembleiaId == a.Id)))
            .ToListAsync(ct);
    }

    public async Task<AssembleiaDto> ObterAsync(Guid id, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Assembleias.Visualizar, ct);
        return await DetalheAsync(id, ct);
    }

    public async Task<AssembleiaDto> CriarAsync(SalvarAssembleiaRequest r, CancellationToken ct)
    {
        await Gerenciar(ct);
        await validator.ValidarAsync(r, ct);
        var a = new Assembleia(ClubeId, r.Titulo, r.Tipo, r.DataHora, r.Local, r.Convocacao, r.ExigeAdimplencia);
        a.DefinirParticipacaoPeloApp(r.ParticipacaoPeloApp);
        db.Assembleias.Add(a);
        await db.SaveChangesAsync(ct);
        return await DetalheAsync(a.Id, ct);
    }

    public Task<AssembleiaDto> AlterarAsync(Guid id, SalvarAssembleiaRequest r, CancellationToken ct) =>
        AlterarComAsync(id, async a =>
        {
            await validator.ValidarAsync(r, ct);
            a.Alterar(r.Titulo, r.Tipo, r.DataHora, r.Local, r.Convocacao, r.ExigeAdimplencia);
            a.DefinirParticipacaoPeloApp(r.ParticipacaoPeloApp);
        }, ct);

    public Task<AssembleiaDto> AdicionarPautaAsync(Guid id, PautaRequest r, CancellationToken ct) =>
        AlterarComAsync(id, async a =>
        {
            await pautaValidator.ValidarAsync(r, ct);
            a.AdicionarPauta(r.Titulo, r.Descricao, r.ComVotacao, r.Opcoes);
        }, ct);

    public Task<AssembleiaDto> RemoverPautaAsync(Guid id, Guid pautaId, CancellationToken ct) =>
        AlterarComAsync(id, a => { a.RemoverPauta(pautaId); return Task.CompletedTask; }, ct);

    public async Task<AssembleiaDto> ConvocarAsync(Guid id, CancellationToken ct)
    {
        var dto = await AlterarComAsync(id, a => { a.Convocar(Agora); return Task.CompletedTask; }, ct);
        var titulares = await db.Associados.Where(x => x.ClubeId == ClubeId && x.Situacao == SituacaoCadastral.Ativo).Select(x => x.PessoaId).ToListAsync(ct);
        await notificador.NotificarPessoasAsync(titulares, TipoNotificacao.Assembleia, $"Convocação: {dto.Titulo}",
            $"Assembleia {(dto.Tipo == TipoAssembleia.Ordinaria ? "ordinária" : "extraordinária")} em {await notificador.FormatarAsync(dto.DataHora, ct)}"
            + (dto.Local is null ? "." : $", {dto.Local}."), id, ct);
        return dto;
    }

    public async Task<AssembleiaDto> InstalarAsync(Guid id, CancellationToken ct)
    {
        var dto = await AlterarComAsync(id, a => { a.Instalar(); return Task.CompletedTask; }, ct);
        if (dto.ParticipacaoPeloApp)
        {
            var titulares = await db.Associados.Where(x => x.ClubeId == ClubeId && x.Situacao == SituacaoCadastral.Ativo).Select(x => x.PessoaId).ToListAsync(ct);
            await notificador.NotificarPessoasAsync(titulares, TipoNotificacao.Assembleia, $"Começou: {dto.Titulo}",
                "A assembleia está em andamento. Você pode participar e votar pelo aplicativo.", id, ct);
        }
        return dto;
    }

    public async Task<PresencaDto> RegistrarPresencaAsync(Guid id, RegistrarPresencaAssembleiaRequest r, CancellationToken ct)
    {
        await Gerenciar(ct);
        var a = await db.Assembleias.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.ClubeId == ClubeId, ct)
                ?? throw new NaoEncontradoException("Assembleia");
        if (!a.RecebePresencas) throw new DomainException("assembleia.situacao", "A assembleia não está recebendo presenças.");

        Guid associadoId;
        if (!string.IsNullOrWhiteSpace(r.Codigo))
        {
            var pessoaId = await CodigoCarteirinha.Buscar(db.Pessoas, r.Codigo).Where(p => p.ClubeId == ClubeId)
                .Select(p => (Guid?)p.Id).FirstOrDefaultAsync(ct);
            if (pessoaId is null) throw new DomainException("assembleia.carteirinha", "Carteirinha não reconhecida neste clube.");
            var assoc = await db.Associados.Where(x => x.PessoaId == pessoaId && x.ClubeId == ClubeId).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct);
            if (assoc is null) throw new DomainException("assembleia.dependente", "Dependentes não participam de assembleias nem votações.");
            associadoId = assoc.Value;
        }
        else if (r.AssociadoId is { } informado) associadoId = informado;
        else throw new DomainException("assembleia.presenca", "Leia a carteirinha ou escolha o associado.");

        var associado = await db.Associados.AsNoTracking().Include(x => x.Pessoa).Include(x => x.Categoria)
                            .FirstOrDefaultAsync(x => x.Id == associadoId && x.ClubeId == ClubeId, ct)
                        ?? throw new NaoEncontradoException("Associado");
        if (await db.PresencasAssembleias.AnyAsync(p => p.AssembleiaId == id && p.AssociadoId == associadoId, ct))
            throw new ConflitoException("assembleia.presenca_duplicada", $"{associado.Pessoa.NomeCompleto} já está com presença registrada.");

        var motivo = await MotivoInaptoAsync(a, associado, ct);
        var presenca = new PresencaAssembleia(ClubeId, id, associadoId, motivo, atual.UsuarioId, Agora);
        db.PresencasAssembleias.Add(presenca);
        await db.SaveChangesAsync(ct);
        return new PresencaDto(associadoId, associado.Pessoa.NomeCompleto, associado.Matricula, presenca.AptoVotar, motivo, presenca.RegistradaEm);
    }

    public async Task<AssembleiaDto> AbrirVotacaoAsync(Guid id, Guid pautaId, CancellationToken ct)
    {
        var dto = await AlterarComAsync(id, a => { a.AbrirVotacao(pautaId); return Task.CompletedTask; }, ct);
        var aptos = await (from p in db.PresencasAssembleias
                           join x in db.Associados on p.AssociadoId equals x.Id
                           where p.AssembleiaId == id && p.AptoVotar
                           select x.PessoaId).ToListAsync(ct);
        var pauta = dto.Pautas.First(p => p.Id == pautaId);
        await notificador.NotificarPessoasAsync(aptos, TipoNotificacao.Votacao, "Votação aberta",
            $"{dto.Titulo}: \"{pauta.Titulo}\". Vote pelo aplicativo.", id, ct);
        return dto;
    }

    public Task<AssembleiaDto> EncerrarVotacaoAsync(Guid id, Guid pautaId, CancellationToken ct) =>
        AlterarComAsync(id, a => { a.EncerrarVotacao(pautaId); return Task.CompletedTask; }, ct);

    public Task<AssembleiaDto> RegistrarDecisaoAsync(Guid id, Guid pautaId, string decisao, CancellationToken ct) =>
        AlterarComAsync(id, a => { a.RegistrarDecisao(pautaId, decisao); return Task.CompletedTask; }, ct);

    public Task<AssembleiaDto> EncerrarAsync(Guid id, CancellationToken ct) =>
        AlterarComAsync(id, a => { a.Encerrar(Agora); return Task.CompletedTask; }, ct);

    public Task<AssembleiaDto> RegistrarAtaAsync(Guid id, string texto, CancellationToken ct) =>
        AlterarComAsync(id, a => { a.RegistrarAta(texto, atual.UsuarioId, Agora); return Task.CompletedTask; }, ct);

    public async Task<AssembleiaDto> CancelarAsync(Guid id, string motivo, CancellationToken ct)
    {
        var convocada = await db.Assembleias.AnyAsync(a => a.Id == id && a.ClubeId == ClubeId && a.Situacao == SituacaoAssembleia.Convocada, ct);
        var dto = await AlterarComAsync(id, a => { a.Cancelar(motivo); return Task.CompletedTask; }, ct);
        if (convocada)
        {
            var titulares = await db.Associados.Where(x => x.ClubeId == ClubeId && x.Situacao == SituacaoCadastral.Ativo).Select(x => x.PessoaId).ToListAsync(ct);
            await notificador.NotificarPessoasAsync(titulares, TipoNotificacao.Assembleia, $"Assembleia cancelada: {dto.Titulo}",
                dto.MotivoCancelamento ?? "", id, ct);
        }
        return dto;
    }

    // ---------------------------------------------------------------- portal (app)

    public async Task<IReadOnlyList<AssembleiaPortalDto>> PortalAsync(CancellationToken ct)
    {
        var portal = await acessoPessoas.ObterPortalAsync(ct);
        if (portal.Associado is not { } associado) return []; // dependentes não participam
        var assembleias = await db.Assembleias.AsNoTracking().Include(a => a.Pautas)
            .Where(a => a.ClubeId == ClubeId && (a.Situacao == SituacaoAssembleia.Convocada || a.Situacao == SituacaoAssembleia.EmAndamento
                                                 || a.Situacao == SituacaoAssembleia.Encerrada))
            .OrderByDescending(a => a.DataHora).Take(10).ToListAsync(ct);
        var resultado = new List<AssembleiaPortalDto>();
        foreach (var a in assembleias) resultado.Add(await PortalDtoAsync(a, associado.Id, ct));
        return resultado;
    }

    private async Task<string?> MotivoInaptoAsync(Assembleia a, Domain.Associados.Associado associado, CancellationToken ct)
    {
        var hoje = Hoje;
        var vencidas = await db.Mensalidades.CountAsync(m => m.AssociadoId == associado.Id && m.Situacao == SituacaoMensalidade.Aberta
                                                             && m.Vencimento < hoje, ct);
        return RegrasVoto.MotivoInapto(associado.Situacao == SituacaoCadastral.Ativo, associado.Categoria.DireitoVoto, vencidas, a.ExigeAdimplencia);
    }

    public async Task<AssembleiaPortalDto> ParticiparPeloAppAsync(Guid id, CancellationToken ct)
    {
        var portal = await acessoPessoas.ObterPortalAsync(ct);
        if (portal.Associado is not { } titular) throw new DomainException("assembleia.dependente", "Dependentes não participam de assembleias nem votações.");
        var a = await db.Assembleias.AsNoTracking().Include(x => x.Pautas).FirstOrDefaultAsync(x => x.Id == id && x.ClubeId == ClubeId, ct)
                ?? throw new NaoEncontradoException("Assembleia");
        if (!a.ParticipacaoPeloApp) throw new DomainException("assembleia.sem_app", "Esta assembleia é só presencial: registre a presença na mesa.");
        if (a.Situacao != SituacaoAssembleia.EmAndamento) throw new DomainException("assembleia.situacao", "A assembleia ainda não começou (ou já terminou).");
        if (await db.PresencasAssembleias.AnyAsync(p => p.AssembleiaId == id && p.AssociadoId == titular.Id, ct))
            throw new ConflitoException("assembleia.presenca_duplicada", "Sua presença já está registrada.");
        var associado = await db.Associados.AsNoTracking().Include(x => x.Categoria).FirstAsync(x => x.Id == titular.Id, ct);
        db.PresencasAssembleias.Add(new PresencaAssembleia(ClubeId, id, associado.Id, await MotivoInaptoAsync(a, associado, ct), atual.UsuarioId, Agora, peloApp: true));
        await db.SaveChangesAsync(ct);
        return await PortalDtoAsync(a, associado.Id, ct);
    }

    public async Task<AssembleiaPortalDto> VotarAsync(Guid id, Guid pautaId, int opcao, CancellationToken ct)
    {
        var portal = await acessoPessoas.ObterPortalAsync(ct);
        if (portal.Associado is not { } associado) throw new DomainException("voto.dependente", "Dependentes não participam de votações.");
        var a = await db.Assembleias.AsNoTracking().Include(x => x.Pautas)
                    .FirstOrDefaultAsync(x => x.Id == id && x.ClubeId == ClubeId && x.Situacao != SituacaoAssembleia.Rascunho, ct)
                ?? throw new NaoEncontradoException("Assembleia");
        var pauta = a.Pautas.FirstOrDefault(p => p.Id == pautaId) ?? throw new NaoEncontradoException("Pauta");
        if (a.Situacao != SituacaoAssembleia.EmAndamento) throw new DomainException("voto.fechado", "A assembleia não está em andamento.");
        pauta.ExigirOpcaoValida(opcao);

        var presenca = await db.PresencasAssembleias.AsNoTracking().FirstOrDefaultAsync(p => p.AssembleiaId == id && p.AssociadoId == associado.Id, ct);
        if (presenca is null)
            throw new DomainException("voto.sem_presenca", a.ParticipacaoPeloApp
                ? "Toque em \"Participar pelo app\" para registrar sua presença antes de votar."
                : "Registre sua presença na assembleia para votar.");
        if (!presenca.AptoVotar) throw new DomainException("voto.inapto", $"Você não está apto a votar: {presenca.MotivoInapto}");
        if (await db.VotosAssembleias.AnyAsync(v => v.PautaId == pautaId && v.AssociadoId == associado.Id, ct))
            throw new ConflitoException("voto.duplicado", "Você já votou nesta pauta.");

        // Um voto por associado e pauta também é garantido pelo índice único (UX_Votos_PautaAssociado).
        db.VotosAssembleias.Add(new VotoAssembleia(ClubeId, pautaId, associado.Id, opcao, Agora));
        await db.SaveChangesAsync(ct);
        return await PortalDtoAsync(a, associado.Id, ct);
    }

    // ---------------------------------------------------------------- auxiliares

    private Task Gerenciar(CancellationToken ct) => permissoes.ExigirAsync(Permissoes.Assembleias.Gerenciar, ct);

    private async Task<AssembleiaDto> AlterarComAsync(Guid id, Func<Assembleia, Task> acao, CancellationToken ct)
    {
        await Gerenciar(ct);
        var a = await db.Assembleias.Include(x => x.Pautas).FirstOrDefaultAsync(x => x.Id == id && x.ClubeId == ClubeId, ct)
                ?? throw new NaoEncontradoException("Assembleia");
        await acao(a);
        await db.SaveChangesAsync(ct);
        return await DetalheAsync(id, ct);
    }

    private async Task<Dictionary<Guid, int[]>> ContagemAsync(IEnumerable<PautaAssembleia> pautas, CancellationToken ct)
    {
        var ids = pautas.Where(p => p.ComVotacao).Select(p => p.Id).ToList();
        var votos = await db.VotosAssembleias.Where(v => ids.Contains(v.PautaId))
            .GroupBy(v => new { v.PautaId, v.Opcao }).Select(g => new { g.Key.PautaId, g.Key.Opcao, Total = g.Count() }).ToListAsync(ct);
        return pautas.Where(p => p.ComVotacao).ToDictionary(p => p.Id, p =>
        {
            var c = new int[p.Opcoes.Count];
            foreach (var v in votos.Where(v => v.PautaId == p.Id && v.Opcao < c.Length)) c[v.Opcao] = v.Total;
            return c;
        });
    }

    /// <summary>A apuração por opção só aparece depois do encerramento da votação (não influencia quem ainda vai votar).</summary>
    private static IReadOnlyList<ApuracaoDto>? Apuracao(PautaAssembleia p, Dictionary<Guid, int[]> contagem) =>
        p.Votacao == SituacaoVotacao.Encerrada ? p.Opcoes.Select((o, i) => new ApuracaoDto(o, contagem[p.Id][i])).ToList() : null;

    private async Task<AssembleiaDto> DetalheAsync(Guid id, CancellationToken ct)
    {
        var a = await db.Assembleias.AsNoTracking().Include(x => x.Pautas).FirstOrDefaultAsync(x => x.Id == id && x.ClubeId == ClubeId, ct)
                ?? throw new NaoEncontradoException("Assembleia");
        var pautas = a.Pautas.OrderBy(p => p.Ordem).ToList();
        var contagem = await ContagemAsync(pautas, ct);
        var presencas = await (from p in db.PresencasAssembleias
                               join x in db.Associados on p.AssociadoId equals x.Id
                               join pe in db.Pessoas on x.PessoaId equals pe.Id
                               where p.AssembleiaId == id
                               orderby pe.NomeCompleto
                               select new PresencaDto(x.Id, pe.NomeCompleto, x.Matricula, p.AptoVotar, p.MotivoInapto, p.RegistradaEm, p.PeloApp)).ToListAsync(ct);
        var pautasDto = pautas.Select(p => new PautaDto(p.Id, p.Ordem, p.Titulo, p.Descricao, p.ComVotacao, p.Opcoes, p.Votacao,
            p.ComVotacao ? contagem[p.Id].Sum() : 0, p.ComVotacao ? Apuracao(p, contagem) : null, p.Decisao)).ToList();
        var sugerida = a.Situacao == SituacaoAssembleia.Encerrada && a.Ata is null ? await AtaSugeridaAsync(a, pautasDto, presencas, ct) : null;
        return new AssembleiaDto(a.Id, a.Titulo, a.Tipo, a.DataHora, a.Local, a.Convocacao, a.ExigeAdimplencia, a.Situacao, a.ConvocadaEm,
            a.EncerradaEm, a.MotivoCancelamento, a.Ata, a.AtaRegistradaEm, sugerida, presencas.Count, presencas.Count(p => p.AptoVotar),
            pautasDto, presencas, a.ParticipacaoPeloApp);
    }

    /// <summary>Rascunho de ata montado com os dados registrados (presenças, apuração e decisões), para a secretaria revisar.</summary>
    private async Task<string> AtaSugeridaAsync(Assembleia a, IReadOnlyList<PautaDto> pautas, IReadOnlyList<PresencaDto> presencas, CancellationToken ct)
    {
        var clube = await db.Clubes.Where(c => c.Id == ClubeId).Select(c => c.Nome).FirstOrDefaultAsync(ct);
        var sb = new StringBuilder();
        sb.AppendLine($"ATA DA ASSEMBLEIA GERAL {(a.Tipo == TipoAssembleia.Ordinaria ? "ORDINÁRIA" : "EXTRAORDINÁRIA")} — {clube}");
        sb.AppendLine();
        sb.AppendLine($"Realizada em {await notificador.FormatarAsync(a.DataHora, ct, "dd/MM/yyyy 'às' HH:mm")}{(a.Local is null ? "" : $", em {a.Local}")}.");
        var peloApp = presencas.Count(p => p.PeloApp);
        sb.AppendLine($"Presentes: {presencas.Count} associado(s){(peloApp > 0 ? $" ({peloApp} pelo aplicativo)" : "")}, dos quais " +
                      $"{presencas.Count(p => p.AptoVotar)} apto(s) a votar.");
        sb.AppendLine();
        foreach (var p in pautas)
        {
            sb.AppendLine($"{p.Ordem}. {p.Titulo}");
            if (p.Apuracao is { } ap) sb.AppendLine("   Votação: " + string.Join("; ", ap.Select(x => $"{x.Opcao}: {x.Votos}")) + $" (total {p.TotalVotos}).");
            sb.AppendLine($"   Decisão: {p.Decisao ?? "(não registrada)"}");
        }
        sb.AppendLine();
        sb.AppendLine("Lista de presença: " + string.Join(", ", presencas.Select(p => $"{p.Nome} ({p.Matricula})")) + ".");
        return sb.ToString();
    }

    private async Task<AssembleiaPortalDto> PortalDtoAsync(Assembleia a, Guid associadoId, CancellationToken ct)
    {
        var pautas = a.Pautas.OrderBy(p => p.Ordem).ToList();
        var contagem = await ContagemAsync(pautas, ct);
        var ids = pautas.Select(p => p.Id).ToList();
        var votei = (await db.VotosAssembleias.Where(v => v.AssociadoId == associadoId && ids.Contains(v.PautaId)).Select(v => v.PautaId)
            .ToListAsync(ct)).ToHashSet();
        var presenca = await db.PresencasAssembleias.AsNoTracking().FirstOrDefaultAsync(p => p.AssembleiaId == a.Id && p.AssociadoId == associadoId, ct);
        return new AssembleiaPortalDto(a.Id, a.Titulo, a.Tipo, a.DataHora, a.Local, a.Convocacao, a.Situacao, presenca is not null,
            presenca?.AptoVotar ?? false, presenca?.MotivoInapto,
            pautas.Select(p => new PautaPortalDto(p.Id, p.Ordem, p.Titulo, p.Descricao, p.ComVotacao, p.Opcoes, p.Votacao, votei.Contains(p.Id),
                p.ComVotacao ? Apuracao(p, contagem) : null, p.Decisao)).ToList(), a.ParticipacaoPeloApp, presenca?.PeloApp ?? false);
    }
}
