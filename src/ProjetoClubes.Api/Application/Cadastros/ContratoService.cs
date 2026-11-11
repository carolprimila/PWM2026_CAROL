using Microsoft.EntityFrameworkCore;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Application.Financeiro;
using ProjetoClubes.Application.Seguranca;
using ProjetoClubes.Domain.Associados;
using ProjetoClubes.Domain.Common;
using ProjetoClubes.Domain.Financeiro;

namespace ProjetoClubes.Application.Cadastros;

public sealed record ContratoDto(Guid Id, string Numero, Guid AssociadoId, string Associado, string Matricula, SituacaoContrato Situacao,
    DateOnly Inicio, DateOnly Fim, decimal ValorMensal, int DiaVencimento, IReadOnlyList<string> Dependentes, DateTimeOffset DataContrato,
    DateTimeOffset? AprovadoEm, string? Motivo, bool Vigente, IReadOnlyList<MensalidadeDto> Parcelas);

/// <summary>Valor e dia são opcionais: sem eles, vêm do plano da categoria do associado (+ adicional de dependentes).</summary>
public sealed record CriarContratoRequest(decimal? ValorMensal, int? DiaVencimento, DateOnly? Inicio);

public sealed record MotivoContratoRequest(string Motivo);

public interface IContratoService
{
    Task<IReadOnlyList<ContratoDto>> ListarAsync(Guid associadoId, CancellationToken ct);
    Task<ContratoDto> CriarAsync(Guid associadoId, CriarContratoRequest r, CancellationToken ct);
    Task<ContratoDto> AprovarAsync(Guid id, CancellationToken ct);
    Task<ContratoDto> ReprovarAsync(Guid id, string motivo, CancellationToken ct);
    Task<ContratoDto> DesligarAsync(Guid id, string motivo, CancellationToken ct);
    Task<ContratoDto> RenovarAsync(Guid id, CriarContratoRequest r, CancellationToken ct);
}

/// <summary>
/// Contrato anual do associado: em análise → aprovado (gera as 12 parcelas) ou reprovado; desligado se ele sair do clube;
/// renovado a cada ano com os dados atualizados (valor do plano e dependentes do momento).
/// </summary>
public sealed class ContratoService(
    IAppDbContext db,
    IUsuarioAtual atual,
    IPermissaoResolver permissoes,
    IGeradorSequencia sequencias,
    IMensalidadeService mensalidades,
    IAssociadoService associados,
    TimeProvider relogio) : IContratoService
{
    private Guid ClubeId => atual.ClubeIdObrigatorio;
    private DateTimeOffset Agora => relogio.GetUtcNow();
    private DateOnly Hoje => DateOnly.FromDateTime(Agora.UtcDateTime);

    public async Task<IReadOnlyList<ContratoDto>> ListarAsync(Guid associadoId, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Associados.Visualizar, ct);
        var ids = await db.ContratosAssociados.Where(c => c.ClubeId == ClubeId && c.AssociadoId == associadoId)
            .OrderByDescending(c => c.Inicio).ThenByDescending(c => c.CriadoEm).Select(c => c.Id).ToListAsync(ct);
        var lista = new List<ContratoDto>();
        foreach (var id in ids) lista.Add(await DetalheAsync(id, ct));
        return lista;
    }

    public async Task<ContratoDto> CriarAsync(Guid associadoId, CriarContratoRequest r, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Associados.Cadastrar, ct);
        var a = await CarregarAssociadoAsync(associadoId, ct);
        if (await db.ContratosAssociados.AnyAsync(c => c.AssociadoId == a.Id && c.Situacao == SituacaoContrato.EmAnalise, ct))
            throw new ConflitoException("contrato.em_analise", "Este associado já tem um contrato em análise.");
        var hoje = Hoje;
        if (await db.ContratosAssociados.AnyAsync(c => c.AssociadoId == a.Id && c.Situacao == SituacaoContrato.Aprovado && c.Fim >= hoje, ct))
            throw new ConflitoException("contrato.vigente", "Este associado já tem contrato vigente. Use a renovação.");
        var inicio = r.Inicio ?? new DateOnly(hoje.Year, hoje.Month, 1).AddMonths(1);
        return await NovoAsync(a, inicio, r, anterior: null, ct);
    }

    public async Task<ContratoDto> AprovarAsync(Guid id, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Associados.Aprovar, ct);
        var c = await CarregarAsync(id, ct);
        var a = await CarregarAssociadoAsync(c.AssociadoId, ct);
        if (a.Situacao != SituacaoCadastral.Ativo) throw new DomainException("contrato.associado", "O associado precisa estar ativo.");
        var plano = await db.PlanosMensalidade.FirstOrDefaultAsync(p => p.ClubeId == ClubeId && p.CategoriaId == a.CategoriaId && p.Ativo, ct)
                    ?? throw new DomainException("contrato.sem_plano",
                        "Cadastre o plano de mensalidade da categoria do associado (Financeiro → Planos) antes de aprovar.");

        c.Aprovar(Agora);
        // Renovação: o contrato anterior se encerra quando o novo é aprovado.
        if (c.ContratoAnteriorId is { } anteriorId &&
            await db.ContratosAssociados.FirstOrDefaultAsync(x => x.Id == anteriorId && x.Situacao == SituacaoContrato.Aprovado, ct) is { } anterior)
            anterior.Encerrar($"Renovado pelo contrato {c.Numero}.");

        var competencias = c.Competencias().ToList();
        var existentes = await db.Mensalidades
            .Where(m => m.AssociadoId == a.Id && competencias.Contains(m.Competencia) && m.Situacao != SituacaoMensalidade.Cancelada)
            .Select(m => m.Competencia).ToListAsync(ct);
        foreach (var comp in competencias.Except(existentes))
        {
            var m = new Mensalidade(ClubeId, a.Id, plano.Id, comp, CalculoFinanceiro.Vencimento(comp, c.DiaVencimento), c.ValorMensal, 0m,
                c.Dependentes.Count);
            m.VincularContrato(c.Id);
            db.Mensalidades.Add(m);
        }
        Historico(a.Id, "Contrato", $"Contrato {c.Numero} aprovado: {competencias.Count - existentes.Count} parcela(s) de " +
                                    $"{c.ValorMensal:C} geradas ({c.Inicio:MM/yyyy} a {c.Fim:MM/yyyy}).");
        await db.SaveChangesAsync(ct);
        return await DetalheAsync(id, ct);
    }

    public async Task<ContratoDto> ReprovarAsync(Guid id, string motivo, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Associados.Aprovar, ct);
        var c = await CarregarAsync(id, ct);
        c.Reprovar(motivo);
        Historico(c.AssociadoId, "Contrato", $"Contrato {c.Numero} reprovado: {c.Motivo}");
        await db.SaveChangesAsync(ct);
        return await DetalheAsync(id, ct);
    }

    /// <summary>Associado saiu do clube: contrato desligado, parcelas futuras em aberto canceladas e cadastro inativado.</summary>
    public async Task<ContratoDto> DesligarAsync(Guid id, string motivo, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Associados.Aprovar, ct);
        var c = await CarregarAsync(id, ct);
        c.Desligar(motivo);
        var mesAtual = new DateOnly(Hoje.Year, Hoje.Month, 1);
        var futuras = await db.Mensalidades
            .Where(m => m.ContratoId == c.Id && m.Situacao == SituacaoMensalidade.Aberta && m.Competencia > mesAtual).ToListAsync(ct);
        foreach (var m in futuras) m.Cancelar($"Contrato {c.Numero} desligado.");
        Historico(c.AssociadoId, "Contrato", $"Contrato {c.Numero} desligado ({futuras.Count} parcela(s) futura(s) cancelada(s)): {c.Motivo}");
        await db.SaveChangesAsync(ct);
        await associados.AlterarSituacaoAsync(c.AssociadoId, AcaoSituacao.Inativar, $"Desligado do clube: {c.Motivo}", ct);
        return await DetalheAsync(id, ct);
    }

    /// <summary>Cria o contrato do ano seguinte (em análise) com os dados atuais; o atual se encerra quando o novo for aprovado.</summary>
    public async Task<ContratoDto> RenovarAsync(Guid id, CriarContratoRequest r, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Associados.Cadastrar, ct);
        var c = await CarregarAsync(id, ct);
        if (c.Situacao != SituacaoContrato.Aprovado) throw new DomainException("contrato.situacao", "Só um contrato aprovado pode ser renovado.");
        if (await db.ContratosAssociados.AnyAsync(x => x.ContratoAnteriorId == c.Id && x.Situacao != SituacaoContrato.Reprovado, ct))
            throw new ConflitoException("contrato.ja_renovado", "Este contrato já tem uma renovação.");
        var a = await CarregarAssociadoAsync(c.AssociadoId, ct);
        return await NovoAsync(a, c.Fim.AddDays(1), r, c, ct);
    }

    // ---------------------------------------------------------------- auxiliares

    private async Task<ContratoDto> NovoAsync(Associado a, DateOnly inicio, CriarContratoRequest r, ContratoAssociado? anterior, CancellationToken ct)
    {
        var dependentes = await db.Dependentes.Where(d => d.AssociadoTitularId == a.Id && d.Situacao == SituacaoCadastral.Ativo)
            .OrderBy(d => d.Pessoa.NomeCompleto)
            .Select(d => new { d.Pessoa.NomeCompleto, d.Parentesco, d.ParentescoDescricao }).ToListAsync(ct);
        var (valorPadrao, diaPadrao) = await ValorDoPlanoAsync(a, dependentes.Count, ct);
        var valor = r.ValorMensal ?? valorPadrao
                    ?? throw new DomainException("contrato.sem_valor", "A categoria não tem plano de mensalidade: informe o valor do contrato.");
        var ano = inicio.Year;
        var numero = $"{ano}-{await sequencias.ProximoAsync(ClubeId, $"contrato-{ano}", ct):0000}";
        var c = new ContratoAssociado(ClubeId, a.Id, numero, inicio, valor, r.DiaVencimento ?? diaPadrao ?? 10,
            dependentes.Select(d => $"{d.NomeCompleto} ({d.ParentescoDescricao ?? NomeParentesco(d.Parentesco)})"), anterior?.Id);
        db.ContratosAssociados.Add(c);
        Historico(a.Id, "Contrato", anterior is null
            ? $"Contrato {numero} criado (em análise): {valor:C} por mês, {c.Inicio:MM/yyyy} a {c.Fim:MM/yyyy}."
            : $"Renovação do contrato {anterior.Numero}: contrato {numero} criado (em análise), {valor:C} por mês.");
        await db.SaveChangesAsync(ct);
        return await DetalheAsync(c.Id, ct);
    }

    private static string NomeParentesco(Domain.Dependentes.Parentesco p) => p switch
    {
        Domain.Dependentes.Parentesco.Conjuge => "Cônjuge",
        Domain.Dependentes.Parentesco.Filho => "Filho(a)",
        Domain.Dependentes.Parentesco.Mae => "Mãe",
        Domain.Dependentes.Parentesco.Irmao => "Irmão(ã)",
        _ => p.ToString(),
    };

    private async Task<(decimal? Valor, int? Dia)> ValorDoPlanoAsync(Associado a, int dependentes, CancellationToken ct)
    {
        var plano = await db.PlanosMensalidade.Where(p => p.ClubeId == ClubeId && p.CategoriaId == a.CategoriaId && p.Ativo)
            .Select(p => new { p.ValorMensal, p.DiaVencimento }).FirstOrDefaultAsync(ct);
        if (plano is null) return (null, null);
        var cfg = await db.ConfiguracoesDependentes.FirstOrDefaultAsync(c => c.ClubeId == ClubeId, ct);
        var adicional = cfg is null || cfg.IncluidosNaMensalidadeDoTitular ? 0m : dependentes * cfg.ValorAdicionalPorDependente;
        return (plano.ValorMensal + adicional, plano.DiaVencimento);
    }

    private async Task<Associado> CarregarAssociadoAsync(Guid id, CancellationToken ct) =>
        await db.Associados.FirstOrDefaultAsync(x => x.Id == id && x.ClubeId == ClubeId, ct) ?? throw new NaoEncontradoException("Associado");

    private async Task<ContratoAssociado> CarregarAsync(Guid id, CancellationToken ct) =>
        await db.ContratosAssociados.FirstOrDefaultAsync(x => x.Id == id && x.ClubeId == ClubeId, ct) ?? throw new NaoEncontradoException("Contrato");

    private void Historico(Guid associadoId, string evento, string descricao) =>
        db.HistoricoAssociados.Add(new HistoricoAssociado(ClubeId, associadoId, evento, descricao, atual.UsuarioId, Agora));

    private async Task<ContratoDto> DetalheAsync(Guid id, CancellationToken ct)
    {
        var c = await db.ContratosAssociados.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.ClubeId == ClubeId, ct)
                ?? throw new NaoEncontradoException("Contrato");
        var a = await db.Associados.AsNoTracking().Where(x => x.Id == c.AssociadoId)
            .Select(x => new { x.Matricula, x.Pessoa.NomeCompleto }).FirstAsync(ct);
        IReadOnlyList<MensalidadeDto> parcelas = [];
        if (await permissoes.PossuiAsync(Permissoes.Financeiro.Visualizar, ct))
            parcelas = (await mensalidades.ListarAsync(new ConsultaMensalidades { ContratoId = c.Id, TamanhoPagina = 100 }, ct)).Itens
                .OrderBy(m => m.Competencia).ToList();
        return new ContratoDto(c.Id, c.Numero, c.AssociadoId, a.NomeCompleto, a.Matricula, c.Situacao, c.Inicio, c.Fim, c.ValorMensal,
            c.DiaVencimento, c.Dependentes, c.CriadoEm, c.AprovadoEm, c.Motivo, c.Vigente(Hoje), parcelas);
    }
}
