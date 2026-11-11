using System.Linq.Expressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Application.Seguranca;
using ProjetoClubes.Domain.Common;
using ProjetoClubes.Domain.Operacional;

namespace ProjetoClubes.Application.Operacional;

public sealed record FuncionarioListaDto(Guid Id, string Nome, string Cargo, string? Departamento, string? Telefone,
    SituacaoFuncionario Situacao, DateOnly Admissao);

public sealed record AusenciaDto(Guid Id, TipoAusencia Tipo, DateOnly Inicio, DateOnly Fim, int Dias, string? Observacao, bool Cancelada);

public sealed record HistoricoDto(string Evento, string Descricao, string? Usuario, DateTimeOffset OcorridoEm);

public sealed record FuncionarioDto(Guid Id, string Nome, string? Cpf, string Cargo, string? Departamento, string? Telefone, string? Email,
    string? Escala, string? Observacoes, DateOnly Admissao, DateOnly? Desligamento, SituacaoFuncionario Situacao, Guid? UsuarioId,
    string? UsuarioEmail, IReadOnlyList<AusenciaDto> Ausencias, IReadOnlyList<HistoricoDto> Historico);

public sealed record SalvarFuncionarioRequest(string Nome, string? Cpf, string Cargo, string? Departamento, string? Telefone,
    string? Email, string? Escala, string? Observacoes, DateOnly Admissao, Guid? UsuarioId);

public sealed record RegistrarAusenciaRequest(TipoAusencia Tipo, DateOnly Inicio, DateOnly Fim, string? Observacao);

public sealed record DataMotivoRequest(DateOnly Data, string? Motivo);

public sealed class ConsultaFuncionarios : ConsultaPaginada
{
    public SituacaoFuncionario? Situacao { get; set; }
    public string? Departamento { get; set; }
}

public sealed class SalvarFuncionarioRequestValidator : AbstractValidator<SalvarFuncionarioRequest>
{
    public SalvarFuncionarioRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().WithMessage("Informe o nome.").MaximumLength(150);
        RuleFor(x => x.Cargo).NotEmpty().WithMessage("Informe o cargo.").MaximumLength(80);
        RuleFor(x => x.Departamento).MaximumLength(80);
        RuleFor(x => x.Telefone).MaximumLength(30);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email)).WithMessage("E-mail inválido.").MaximumLength(150);
        RuleFor(x => x.Escala).MaximumLength(200);
        RuleFor(x => x.Observacoes).MaximumLength(2000);
    }
}

public sealed class RegistrarAusenciaRequestValidator : AbstractValidator<RegistrarAusenciaRequest>
{
    public RegistrarAusenciaRequestValidator()
    {
        RuleFor(x => x.Tipo).IsInEnum();
        RuleFor(x => x.Fim).GreaterThanOrEqualTo(x => x.Inicio).WithMessage("O fim deve ser igual ou posterior ao início.");
        RuleFor(x => x).Must(x => x.Fim.DayNumber - x.Inicio.DayNumber < 366).WithMessage("Período máximo de um ano.");
        RuleFor(x => x.Observacao).MaximumLength(300);
    }
}

public interface IFuncionarioService
{
    Task<Pagina<FuncionarioListaDto>> ListarAsync(ConsultaFuncionarios c, CancellationToken ct);
    Task<FuncionarioDto> ObterAsync(Guid id, CancellationToken ct);
    Task<FuncionarioDto> CriarAsync(SalvarFuncionarioRequest r, CancellationToken ct);
    Task<FuncionarioDto> AlterarAsync(Guid id, SalvarFuncionarioRequest r, CancellationToken ct);
    Task<FuncionarioDto> RegistrarAusenciaAsync(Guid id, RegistrarAusenciaRequest r, CancellationToken ct);
    Task<FuncionarioDto> CancelarAusenciaAsync(Guid id, Guid ausenciaId, CancellationToken ct);
    Task<FuncionarioDto> DesligarAsync(Guid id, DataMotivoRequest r, CancellationToken ct);
    Task<FuncionarioDto> ReadmitirAsync(Guid id, DataMotivoRequest r, CancellationToken ct);
    Task ExcluirAsync(Guid id, CancellationToken ct);
}

/// <summary>Funcionários: dados, cargo, departamento, escala, férias/afastamentos e histórico.</summary>
public sealed class FuncionarioService(
    IAppDbContext db,
    IUsuarioAtual atual,
    IPermissaoResolver permissoes,
    TimeProvider relogio,
    IValidator<SalvarFuncionarioRequest> validator,
    IValidator<RegistrarAusenciaRequest> ausenciaValidator) : IFuncionarioService
{
    private Guid ClubeId => atual.ClubeIdObrigatorio;
    private DateTimeOffset Agora => relogio.GetUtcNow();
    private DateOnly Hoje => DateOnly.FromDateTime(Agora.UtcDateTime);

    public async Task<Pagina<FuncionarioListaDto>> ListarAsync(ConsultaFuncionarios c, CancellationToken ct)
    {
        await Exigir(ct);
        var hoje = Hoje;
        // Projeção intermediária anônima: o EF filtra e ordena por ela; o DTO é montado no fim.
        var q = db.Funcionarios.AsNoTracking().Where(f => f.ClubeId == ClubeId).Select(f => new
        {
            f.Id, f.Nome, f.Cargo, f.Departamento, f.Telefone, f.Admissao,
            Situacao = f.Situacao == SituacaoFuncionario.Desligado ? SituacaoFuncionario.Desligado
                : f.Ausencias.Any(a => !a.Cancelada && a.Inicio <= hoje && hoje <= a.Fim && a.Tipo == TipoAusencia.Ferias) ? SituacaoFuncionario.Ferias
                : f.Ausencias.Any(a => !a.Cancelada && a.Inicio <= hoje && hoje <= a.Fim) ? SituacaoFuncionario.Afastado
                : SituacaoFuncionario.Ativo,
        });
        if (c.Situacao is { } s) q = q.Where(x => x.Situacao == s);
        else q = q.Where(x => x.Situacao != SituacaoFuncionario.Desligado);
        if (!string.IsNullOrWhiteSpace(c.Departamento)) q = q.Where(x => x.Departamento == c.Departamento);
        if (!string.IsNullOrWhiteSpace(c.Busca))
        {
            var termo = c.Busca.Trim().ToLower();
            q = q.Where(x => x.Nome.ToLower().Contains(termo) || x.Cargo.ToLower().Contains(termo));
        }
        return await q.OrderBy(x => x.Nome)
            .Select(x => new FuncionarioListaDto(x.Id, x.Nome, x.Cargo, x.Departamento, x.Telefone, x.Situacao, x.Admissao))
            .PaginarAsync(c, ct);
    }

    public async Task<FuncionarioDto> ObterAsync(Guid id, CancellationToken ct)
    {
        await Exigir(ct);
        return await DetalheAsync(id, ct);
    }

    public async Task<FuncionarioDto> CriarAsync(SalvarFuncionarioRequest r, CancellationToken ct)
    {
        await Exigir(ct);
        await validator.ValidarAsync(r, ct);
        var f = new Funcionario(ClubeId, r.Nome, r.Cpf, r.Cargo, r.Departamento, r.Admissao);
        await AplicarAsync(f, r, ct);
        db.Funcionarios.Add(f);
        Historico(f.Id, "Admissao", $"Admitido como {f.Cargo}{(f.Departamento is null ? "" : $" ({f.Departamento})")} em {f.Admissao:dd/MM/yyyy}.");
        await db.SaveChangesAsync(ct);
        return await DetalheAsync(f.Id, ct);
    }

    public async Task<FuncionarioDto> AlterarAsync(Guid id, SalvarFuncionarioRequest r, CancellationToken ct)
    {
        await Exigir(ct);
        await validator.ValidarAsync(r, ct);
        var f = await CarregarAsync(id, ct);
        var antes = (f.Cargo, f.Departamento);
        f.AlterarDados(r.Nome, r.Cpf, r.Cargo, r.Departamento, r.Admissao);
        await AplicarAsync(f, r, ct);
        if (antes != (f.Cargo, f.Departamento))
            Historico(f.Id, "Cargo", $"Cargo: {antes.Cargo}{(antes.Departamento is null ? "" : $" ({antes.Departamento})")} → {f.Cargo}{(f.Departamento is null ? "" : $" ({f.Departamento})")}.");
        else Historico(f.Id, "Edicao", "Dados atualizados.");
        await db.SaveChangesAsync(ct);
        return await DetalheAsync(id, ct);
    }

    public async Task<FuncionarioDto> RegistrarAusenciaAsync(Guid id, RegistrarAusenciaRequest r, CancellationToken ct)
    {
        await Exigir(ct);
        await ausenciaValidator.ValidarAsync(r, ct);
        var f = await CarregarAsync(id, ct);
        var a = f.RegistrarAusencia(r.Tipo, r.Inicio, r.Fim, r.Observacao);
        f.AtualizarSituacao(Hoje);
        Historico(f.Id, r.Tipo.ToString(), $"{(r.Tipo == TipoAusencia.Ferias ? "Férias" : "Afastamento")} de {r.Inicio:dd/MM/yyyy} a {r.Fim:dd/MM/yyyy} ({a.Dias} dia(s)).");
        await db.SaveChangesAsync(ct);
        return await DetalheAsync(id, ct);
    }

    public async Task<FuncionarioDto> CancelarAusenciaAsync(Guid id, Guid ausenciaId, CancellationToken ct)
    {
        await Exigir(ct);
        var f = await CarregarAsync(id, ct);
        var a = f.Ausencias.FirstOrDefault(x => x.Id == ausenciaId && !x.Cancelada) ?? throw new NaoEncontradoException("Período");
        a.Cancelar();
        f.AtualizarSituacao(Hoje);
        Historico(f.Id, "CancelamentoAusencia", $"Cancelado período de {a.Inicio:dd/MM/yyyy} a {a.Fim:dd/MM/yyyy}.");
        await db.SaveChangesAsync(ct);
        return await DetalheAsync(id, ct);
    }

    public async Task<FuncionarioDto> DesligarAsync(Guid id, DataMotivoRequest r, CancellationToken ct)
    {
        await Exigir(ct);
        var f = await CarregarAsync(id, ct);
        f.Desligar(r.Data);
        Historico(f.Id, "Desligamento", $"Desligado em {r.Data:dd/MM/yyyy}.{(string.IsNullOrWhiteSpace(r.Motivo) ? "" : $" Motivo: {r.Motivo.Trim()}")}");
        await db.SaveChangesAsync(ct);
        return await DetalheAsync(id, ct);
    }

    public async Task<FuncionarioDto> ReadmitirAsync(Guid id, DataMotivoRequest r, CancellationToken ct)
    {
        await Exigir(ct);
        var f = await CarregarAsync(id, ct);
        f.Readmitir(r.Data);
        f.AtualizarSituacao(Hoje);
        Historico(f.Id, "Readmissao", $"Readmitido em {r.Data:dd/MM/yyyy}.");
        await db.SaveChangesAsync(ct);
        return await DetalheAsync(id, ct);
    }

    public async Task ExcluirAsync(Guid id, CancellationToken ct)
    {
        await Exigir(ct);
        db.Funcionarios.Remove(await CarregarAsync(id, ct));
        await db.SaveChangesAsync(ct);
    }

    // ---------------------------------------------------------------- auxiliares

    private Task Exigir(CancellationToken ct) => permissoes.ExigirAsync(Permissoes.Operacional.Funcionarios, ct);

    /// <summary>Férias/afastamento valem pelo período: a situação do dia é calculada na leitura.</summary>
    private static Expression<Func<Funcionario, FuncionarioListaDto>> SituacaoDoDia(DateOnly hoje) => f => new FuncionarioListaDto(
        f.Id, f.Nome, f.Cargo, f.Departamento, f.Telefone,
        f.Situacao == SituacaoFuncionario.Desligado ? SituacaoFuncionario.Desligado
        : f.Ausencias.Any(a => !a.Cancelada && a.Inicio <= hoje && hoje <= a.Fim && a.Tipo == TipoAusencia.Ferias) ? SituacaoFuncionario.Ferias
        : f.Ausencias.Any(a => !a.Cancelada && a.Inicio <= hoje && hoje <= a.Fim) ? SituacaoFuncionario.Afastado
        : SituacaoFuncionario.Ativo,
        f.Admissao);

    private async Task AplicarAsync(Funcionario f, SalvarFuncionarioRequest r, CancellationToken ct)
    {
        if (f.Cpf is { } cpf && await db.Funcionarios.AnyAsync(x => x.ClubeId == ClubeId && x.Cpf == cpf && x.Id != f.Id, ct))
            throw new ConflitoException("funcionario.cpf_duplicado", "Já existe um funcionário com este CPF.");
        if (r.UsuarioId is { } u && !await db.UsuarioClubes.AnyAsync(x => x.UsuarioId == u && x.ClubeId == ClubeId, ct))
            throw new NaoEncontradoException("Usuário");
        f.Telefone = Limpar(r.Telefone);
        f.Email = Limpar(r.Email);
        f.Escala = Limpar(r.Escala);
        f.Observacoes = Limpar(r.Observacoes);
        f.UsuarioId = r.UsuarioId;
    }

    private void Historico(Guid funcionarioId, string evento, string descricao) =>
        db.HistoricoFuncionarios.Add(new HistoricoFuncionario(ClubeId, funcionarioId, evento, descricao, atual.UsuarioId, Agora));

    private async Task<Funcionario> CarregarAsync(Guid id, CancellationToken ct) =>
        await db.Funcionarios.Include(f => f.Ausencias).FirstOrDefaultAsync(f => f.Id == id && f.ClubeId == ClubeId, ct)
        ?? throw new NaoEncontradoException("Funcionário");

    private async Task<FuncionarioDto> DetalheAsync(Guid id, CancellationToken ct)
    {
        var f = await db.Funcionarios.AsNoTracking().Include(x => x.Ausencias).FirstOrDefaultAsync(x => x.Id == id && x.ClubeId == ClubeId, ct)
                ?? throw new NaoEncontradoException("Funcionário");
        var situacao = await db.Funcionarios.Where(x => x.Id == id).Select(SituacaoDoDia(Hoje)).Select(x => x.Situacao).FirstAsync(ct);
        var email = f.UsuarioId is { } u ? await db.Usuarios.IgnoreQueryFilters().Where(x => x.Id == u).Select(x => x.Email).FirstOrDefaultAsync(ct) : null;
        var historico = await HistoricoOperacional.ListarAsync(db, db.HistoricoFuncionarios.Where(h => h.FuncionarioId == id), ct);
        return new FuncionarioDto(f.Id, f.Nome, f.Cpf, f.Cargo, f.Departamento, f.Telefone, f.Email, f.Escala, f.Observacoes, f.Admissao,
            f.Desligamento, situacao, f.UsuarioId, email,
            f.Ausencias.OrderByDescending(a => a.Inicio).Select(a => new AusenciaDto(a.Id, a.Tipo, a.Inicio, a.Fim, a.Dias, a.Observacao, a.Cancelada)).ToList(),
            historico);
    }

    private static string? Limpar(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

internal static class HistoricoOperacional
{
    public static async Task<IReadOnlyList<HistoricoDto>> ListarAsync<T>(IAppDbContext db, IQueryable<T> q, CancellationToken ct)
        where T : RegistroHistorico
    {
        var itens = await q.AsNoTracking().OrderByDescending(h => h.OcorridoEm).Take(100).ToListAsync(ct);
        var ids = itens.Select(h => h.UsuarioId).OfType<Guid>().Distinct().ToList();
        var nomes = await db.Usuarios.IgnoreQueryFilters().Where(u => ids.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Nome, ct);
        return itens.Select(h => new HistoricoDto(h.Evento, h.Descricao, h.UsuarioId is { } u ? nomes.GetValueOrDefault(u) : null, h.OcorridoEm)).ToList();
    }
}
