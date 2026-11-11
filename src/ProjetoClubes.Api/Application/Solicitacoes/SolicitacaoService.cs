using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjetoClubes.Application.Cadastros;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Application.Seguranca;
using ProjetoClubes.Domain.Solicitacoes;

namespace ProjetoClubes.Application.Solicitacoes;

public sealed record RespostaDto(Guid Id, string Texto, bool DaAdministracao, bool Interna, string? Autor, DateTimeOffset CriadaEm);

public sealed record SolicitacaoListaDto(Guid Id, string Protocolo, TipoSolicitacao Tipo, string Assunto, SituacaoSolicitacao Situacao,
    string? Solicitante, DateOnly? Prazo, bool Atrasada, DateTimeOffset AbertaEm, int Respostas);

public sealed record SolicitacaoDetalheDto(Guid Id, string Protocolo, TipoSolicitacao Tipo, string Assunto, string Descricao,
    SituacaoSolicitacao Situacao, Guid? PessoaId, string? Solicitante, string? Responsavel, DateOnly? Prazo, DateTimeOffset AbertaEm,
    IReadOnlyList<RespostaDto> Respostas);

public sealed record AbrirSolicitacaoRequest(TipoSolicitacao Tipo, string Assunto, string Descricao, Guid? PessoaId, DateOnly? Prazo);

public sealed record ResponderRequest(string Texto, bool Interna = false);

public sealed record AlterarSituacaoSolicitacaoRequest(SituacaoSolicitacao Situacao, DateOnly? Prazo);

public sealed class ConsultaSolicitacoes : ConsultaPaginada
{
    public SituacaoSolicitacao? Situacao { get; set; }
    public TipoSolicitacao? Tipo { get; set; }
    public bool? SomenteAbertas { get; set; }
}

public sealed class AbrirSolicitacaoRequestValidator : AbstractValidator<AbrirSolicitacaoRequest>
{
    public AbrirSolicitacaoRequestValidator()
    {
        RuleFor(x => x.Tipo).IsInEnum();
        RuleFor(x => x.Assunto).NotEmpty().WithMessage("Informe o assunto.").MaximumLength(150);
        RuleFor(x => x.Descricao).NotEmpty().WithMessage("Descreva a solicitação.").MaximumLength(4000);
    }
}

public sealed class ResponderRequestValidator : AbstractValidator<ResponderRequest>
{
    public ResponderRequestValidator() => RuleFor(x => x.Texto).NotEmpty().WithMessage("Escreva a mensagem.").MaximumLength(4000);
}

public interface ISolicitacaoService
{
    Task<Pagina<SolicitacaoListaDto>> ListarAsync(ConsultaSolicitacoes c, CancellationToken ct);
    Task<SolicitacaoDetalheDto> ObterAsync(Guid id, CancellationToken ct);
    Task<SolicitacaoDetalheDto> AbrirAsync(AbrirSolicitacaoRequest r, CancellationToken ct);
    Task<SolicitacaoDetalheDto> ResponderAsync(Guid id, ResponderRequest r, CancellationToken ct);
    Task<SolicitacaoDetalheDto> AlterarSituacaoAsync(Guid id, AlterarSituacaoSolicitacaoRequest r, CancellationToken ct);
    Task<IReadOnlyList<SolicitacaoListaDto>> MinhasAsync(CancellationToken ct);
    Task<SolicitacaoDetalheDto> ObterNoPortalAsync(Guid id, CancellationToken ct);
    Task<SolicitacaoDetalheDto> AbrirNoPortalAsync(AbrirSolicitacaoRequest r, CancellationToken ct);
    Task<SolicitacaoDetalheDto> ResponderNoPortalAsync(Guid id, ResponderRequest r, CancellationToken ct);
    Task<SolicitacaoDetalheDto> CancelarNoPortalAsync(Guid id, CancellationToken ct);
}

/// <summary>Solicitações, reclamações e sugestões com protocolo; o associado acompanha pelo app e a secretaria responde pelo Web.</summary>
public sealed class SolicitacaoService(
    IAppDbContext db,
    IUsuarioAtual atual,
    IPermissaoResolver permissoes,
    IAcessoPessoas acessoPessoas,
    IGeradorSequencia sequencias,
    Comunicacao.INotificador notificador,
    TimeProvider relogio,
    IValidator<AbrirSolicitacaoRequest> abrirValidator,
    IValidator<ResponderRequest> responderValidator) : ISolicitacaoService
{
    private Guid ClubeId => atual.ClubeIdObrigatorio;
    private DateTimeOffset Agora => relogio.GetUtcNow();

    public async Task<Pagina<SolicitacaoListaDto>> ListarAsync(ConsultaSolicitacoes c, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Solicitacoes.Visualizar, ct);
        var q = db.Solicitacoes.AsNoTracking().Where(s => s.ClubeId == ClubeId);
        if (c.Situacao is { } sit) q = q.Where(s => s.Situacao == sit);
        if (c.SomenteAbertas == true) q = q.Where(s => s.Situacao == SituacaoSolicitacao.Aberta || s.Situacao == SituacaoSolicitacao.EmAndamento);
        if (c.Tipo is { } t) q = q.Where(s => s.Tipo == t);
        if (!string.IsNullOrWhiteSpace(c.Busca))
        {
            var termo = c.Busca.Trim().ToLower();
            q = q.Where(s => s.Protocolo.Contains(termo) || s.Assunto.ToLower().Contains(termo));
        }
        return await Lista(q.OrderBy(s => s.Situacao).ThenByDescending(s => s.CriadoEm)).PaginarAsync(c, ct);
    }

    public async Task<SolicitacaoDetalheDto> ObterAsync(Guid id, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Solicitacoes.Visualizar, ct);
        return await DetalheAsync(id, incluirInternas: true, ct);
    }

    public async Task<SolicitacaoDetalheDto> AbrirAsync(AbrirSolicitacaoRequest r, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Solicitacoes.Atender, ct);
        await abrirValidator.ValidarAsync(r, ct);
        if (r.PessoaId is { } p && !await db.Pessoas.AnyAsync(x => x.Id == p && x.ClubeId == ClubeId, ct)) throw new NaoEncontradoException("Pessoa");
        return await CriarAsync(r, r.PessoaId, incluirInternas: true, ct);
    }

    public async Task<SolicitacaoDetalheDto> ResponderAsync(Guid id, ResponderRequest r, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Solicitacoes.Atender, ct);
        await responderValidator.ValidarAsync(r, ct);
        var s = await CarregarAsync(id, ct);
        s.Responder(r.Texto, atual.UsuarioId, daAdministracao: true, r.Interna, Agora);
        if (s.ResponsavelId is null) s.AtribuirResponsavel(atual.UsuarioId);
        await SalvarRespostasAsync(s, ct);
        if (!r.Interna && s.PessoaId is { } pessoa)
            await notificador.NotificarPessoasAsync([pessoa], Domain.Comunicacao.TipoNotificacao.Solicitacao, $"Resposta ao protocolo {s.Protocolo}",
                r.Texto.Length > 140 ? r.Texto[..140] + "…" : r.Texto, s.Id, ct);
        return await DetalheAsync(id, incluirInternas: true, ct);
    }

    public async Task<SolicitacaoDetalheDto> AlterarSituacaoAsync(Guid id, AlterarSituacaoSolicitacaoRequest r, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Solicitacoes.Atender, ct);
        var s = await CarregarAsync(id, ct);
        if (r.Prazo is not null) s.Prazo = r.Prazo;
        s.AlterarSituacao(r.Situacao, atual.UsuarioId, Agora);
        if (s.ResponsavelId is null) s.AtribuirResponsavel(atual.UsuarioId);
        await SalvarRespostasAsync(s, ct);
        if (s.PessoaId is { } pessoa && s.Encerrada)
            await notificador.NotificarPessoasAsync([pessoa], Domain.Comunicacao.TipoNotificacao.Solicitacao, $"Protocolo {s.Protocolo}",
                $"Sua solicitação \"{s.Assunto}\" foi {Solicitacao.Rotulo(s.Situacao).ToLowerInvariant()}.", s.Id, ct);
        return await DetalheAsync(id, incluirInternas: true, ct);
    }

    public async Task<IReadOnlyList<SolicitacaoListaDto>> MinhasAsync(CancellationToken ct)
    {
        var portal = await acessoPessoas.ObterPortalAsync(ct);
        return await Lista(db.Solicitacoes.AsNoTracking().Where(s => s.ClubeId == ClubeId && s.PessoaId == portal.Pessoa.Id)
            .OrderByDescending(s => s.CriadoEm)).Take(50).ToListAsync(ct);
    }

    public async Task<SolicitacaoDetalheDto> ObterNoPortalAsync(Guid id, CancellationToken ct)
    {
        await ExigirDonoAsync(id, ct);
        return await DetalheAsync(id, incluirInternas: false, ct);
    }

    public async Task<SolicitacaoDetalheDto> AbrirNoPortalAsync(AbrirSolicitacaoRequest r, CancellationToken ct)
    {
        await abrirValidator.ValidarAsync(r, ct);
        var portal = await acessoPessoas.ObterPortalAsync(ct);
        return await CriarAsync(r with { Prazo = null }, portal.Pessoa.Id, incluirInternas: false, ct);
    }

    public async Task<SolicitacaoDetalheDto> ResponderNoPortalAsync(Guid id, ResponderRequest r, CancellationToken ct)
    {
        await responderValidator.ValidarAsync(r, ct);
        await ExigirDonoAsync(id, ct);
        var s = await CarregarAsync(id, ct);
        s.Responder(r.Texto, atual.UsuarioId, daAdministracao: false, interna: false, Agora);
        await SalvarRespostasAsync(s, ct);
        return await DetalheAsync(id, incluirInternas: false, ct);
    }

    public async Task<SolicitacaoDetalheDto> CancelarNoPortalAsync(Guid id, CancellationToken ct)
    {
        await ExigirDonoAsync(id, ct);
        var s = await CarregarAsync(id, ct);
        s.AlterarSituacao(SituacaoSolicitacao.Cancelada, atual.UsuarioId, Agora);
        await SalvarRespostasAsync(s, ct);
        return await DetalheAsync(id, incluirInternas: false, ct);
    }

    // ---------------------------------------------------------------- auxiliares

    private async Task<SolicitacaoDetalheDto> CriarAsync(AbrirSolicitacaoRequest r, Guid? pessoaId, bool incluirInternas, CancellationToken ct)
    {
        var ano = Agora.Year;
        var protocolo = $"{ano}-{await sequencias.ProximoAsync(ClubeId, $"protocolo-{ano}", ct):000000}";
        var s = new Solicitacao(ClubeId, protocolo, r.Tipo, r.Assunto, r.Descricao, pessoaId, r.Prazo);
        db.Solicitacoes.Add(s);
        await db.SaveChangesAsync(ct);
        return await DetalheAsync(s.Id, incluirInternas, ct);
    }

    /// <summary>Respostas novas da coleção são gravadas como inclusão (Ids gerados pela aplicação — DT-016).</summary>
    private Task SalvarRespostasAsync(Solicitacao _, CancellationToken ct) => db.SaveChangesAsync(ct);

    private async Task ExigirDonoAsync(Guid id, CancellationToken ct)
    {
        var portal = await acessoPessoas.ObterPortalAsync(ct);
        if (!await db.Solicitacoes.AnyAsync(s => s.Id == id && s.ClubeId == ClubeId && s.PessoaId == portal.Pessoa.Id, ct))
            throw new NaoEncontradoException("Solicitação");
    }

    private async Task<Solicitacao> CarregarAsync(Guid id, CancellationToken ct) =>
        await db.Solicitacoes.Include(s => s.Respostas).FirstOrDefaultAsync(s => s.Id == id && s.ClubeId == ClubeId, ct)
        ?? throw new NaoEncontradoException("Solicitação");

    private IQueryable<SolicitacaoListaDto> Lista(IQueryable<Solicitacao> q)
    {
        var hoje = DateOnly.FromDateTime(Agora.UtcDateTime);
        return q.Select(s => new SolicitacaoListaDto(s.Id, s.Protocolo, s.Tipo, s.Assunto, s.Situacao,
            db.Pessoas.Where(p => p.Id == s.PessoaId).Select(p => p.NomeCompleto).FirstOrDefault(),
            s.Prazo, s.Prazo != null && s.Prazo < hoje && (s.Situacao == SituacaoSolicitacao.Aberta || s.Situacao == SituacaoSolicitacao.EmAndamento),
            s.CriadoEm, s.Respostas.Count(r => !r.Interna)));
    }

    private async Task<SolicitacaoDetalheDto> DetalheAsync(Guid id, bool incluirInternas, CancellationToken ct)
    {
        var s = await db.Solicitacoes.AsNoTracking().Include(x => x.Respostas).FirstOrDefaultAsync(x => x.Id == id && x.ClubeId == ClubeId, ct)
                ?? throw new NaoEncontradoException("Solicitação");
        var usuarios = s.Respostas.Select(r => r.AutorUsuarioId).Append(s.ResponsavelId).OfType<Guid>().Distinct().ToList();
        var nomes = await db.Usuarios.IgnoreQueryFilters().Where(u => usuarios.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Nome, ct);
        var solicitante = s.PessoaId is { } p ? await db.Pessoas.Where(x => x.Id == p).Select(x => x.NomeCompleto).FirstOrDefaultAsync(ct) : null;
        return new SolicitacaoDetalheDto(s.Id, s.Protocolo, s.Tipo, s.Assunto, s.Descricao, s.Situacao, s.PessoaId, solicitante,
            s.ResponsavelId is { } r ? nomes.GetValueOrDefault(r) : null, s.Prazo, s.CriadoEm,
            s.Respostas.Where(x => incluirInternas || !x.Interna).OrderBy(x => x.CriadaEm)
                .Select(x => new RespostaDto(x.Id, x.Texto, x.DaAdministracao, x.Interna,
                    x.DaAdministracao ? (x.AutorUsuarioId is { } a ? nomes.GetValueOrDefault(a) ?? "Secretaria" : "Secretaria") : solicitante,
                    x.CriadaEm)).ToList());
    }
}
