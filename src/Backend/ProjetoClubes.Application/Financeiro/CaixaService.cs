using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Application.Seguranca;
using ProjetoClubes.Domain.Financeiro;

namespace ProjetoClubes.Application.Financeiro;

public interface ICaixaService
{
    Task<IReadOnlyList<CategoriaFinanceiraDto>> ListarCategoriasAsync(CancellationToken ct);
    Task<Pagina<LancamentoDto>> ListarLancamentosAsync(ConsultaLancamentos c, CancellationToken ct);
    Task<LancamentoDto> SalvarLancamentoAsync(Guid? id, SalvarLancamentoRequest r, CancellationToken ct);
    Task<LancamentoDto> MarcarPagoAsync(Guid id, DateOnly? data, CancellationToken ct);
    Task ExcluirLancamentoAsync(Guid id, CancellationToken ct);
    Task<ResumoFinanceiroDto> ResumoAsync(DateOnly? de, DateOnly? ate, CancellationToken ct);
    Task<FluxoCaixaDto> FluxoCaixaAsync(DateOnly? de, DateOnly? ate, CancellationToken ct);
    Task<IReadOnlyList<InadimplenteDto>> InadimplentesAsync(CancellationToken ct);
}

/// <summary>Receitas e despesas avulsas, contas a pagar/receber, resumo, fluxo de caixa e inadimplência.</summary>
public sealed class CaixaService(
    IAppDbContext db,
    IUsuarioAtual atual,
    IPermissaoResolver permissoes,
    TimeProvider relogio,
    IValidator<SalvarLancamentoRequest> validator) : ICaixaService
{
    private Guid ClubeId => atual.ClubeIdObrigatorio;
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetUtcNow().UtcDateTime);

    public async Task<IReadOnlyList<CategoriaFinanceiraDto>> ListarCategoriasAsync(CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Financeiro.Visualizar, ct);
        return await db.CategoriasFinanceiras.AsNoTracking().Where(c => c.ClubeId == ClubeId)
            .OrderBy(c => c.Tipo).ThenBy(c => c.Nome).Select(c => new CategoriaFinanceiraDto(c.Id, c.Nome, c.Tipo)).ToListAsync(ct);
    }

    public async Task<Pagina<LancamentoDto>> ListarLancamentosAsync(ConsultaLancamentos c, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Financeiro.Visualizar, ct);
        var q = db.LancamentosFinanceiros.AsNoTracking().Where(l => l.ClubeId == ClubeId);
        if (c.Tipo is { } t) q = q.Where(l => l.Tipo == t);
        if (c.Pago is { } pago) q = q.Where(l => (l.PagoEm != null) == pago);
        if (c.De is { } de) q = q.Where(l => l.Vencimento >= de);
        if (c.Ate is { } ate) q = q.Where(l => l.Vencimento <= ate);
        if (!string.IsNullOrWhiteSpace(c.Busca)) q = q.Where(l => l.Descricao.ToLower().Contains(c.Busca.Trim().ToLower()));

        var pagina = await q.OrderByDescending(l => l.Vencimento).ThenBy(l => l.Descricao)
            .Select(l => new { l.Id, l.Tipo, l.CategoriaId, Categoria = l.Categoria.Nome, l.Descricao, l.Valor, l.Vencimento, l.PagoEm })
            .PaginarAsync(c, ct);
        var hoje = Hoje;
        return new Pagina<LancamentoDto>(pagina.Itens.Select(l => new LancamentoDto(l.Id, l.Tipo, l.CategoriaId, l.Categoria, l.Descricao,
            l.Valor, l.Vencimento, l.PagoEm, Situacao(l.PagoEm, l.Vencimento, hoje))).ToList(), pagina.PaginaAtual, pagina.TamanhoPagina, pagina.TotalItens);
    }

    public async Task<LancamentoDto> SalvarLancamentoAsync(Guid? id, SalvarLancamentoRequest r, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Financeiro.Gerenciar, ct);
        await validator.ValidarAsync(r, ct);
        var categoria = await db.CategoriasFinanceiras.FirstOrDefaultAsync(c => c.Id == r.CategoriaId && c.ClubeId == ClubeId, ct)
                        ?? throw new ValidationException([new ValidationFailure("CategoriaId", "Categoria inexistente.")]);
        if (categoria.Tipo != r.Tipo)
            throw new ValidationException([new ValidationFailure("CategoriaId", $"A categoria escolhida é de {categoria.Tipo.ToString().ToLower()}.")]);

        LancamentoFinanceiro l;
        if (id is { } existente)
        {
            l = await db.LancamentosFinanceiros.FirstOrDefaultAsync(x => x.Id == existente && x.ClubeId == ClubeId, ct)
                ?? throw new NaoEncontradoException("Lançamento");
            l.CategoriaId = r.CategoriaId;
            l.Atualizar(r.Descricao, r.Valor, r.Vencimento);
        }
        else
        {
            l = new LancamentoFinanceiro(ClubeId, r.Tipo, r.CategoriaId, r.Descricao, r.Valor, r.Vencimento);
            db.LancamentosFinanceiros.Add(l);
        }
        l.MarcarPago(r.PagoEm);
        await db.SaveChangesAsync(ct);
        return Dto(l, categoria.Nome);
    }

    public async Task<LancamentoDto> MarcarPagoAsync(Guid id, DateOnly? data, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Financeiro.Gerenciar, ct);
        var l = await db.LancamentosFinanceiros.Include(x => x.Categoria).FirstOrDefaultAsync(x => x.Id == id && x.ClubeId == ClubeId, ct)
                ?? throw new NaoEncontradoException("Lançamento");
        l.MarcarPago(data ?? Hoje);
        await db.SaveChangesAsync(ct);
        return Dto(l, l.Categoria.Nome);
    }

    public async Task ExcluirLancamentoAsync(Guid id, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Financeiro.Gerenciar, ct);
        var l = await db.LancamentosFinanceiros.FirstOrDefaultAsync(x => x.Id == id && x.ClubeId == ClubeId, ct)
                ?? throw new NaoEncontradoException("Lançamento");
        db.LancamentosFinanceiros.Remove(l);
        await db.SaveChangesAsync(ct);
    }

    public async Task<ResumoFinanceiroDto> ResumoAsync(DateOnly? de, DateOnly? ate, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Financeiro.Visualizar, ct);
        var (ini, fim) = Periodo(de, ate);
        var hoje = Hoje;

        var mensalidadesPagas = await db.Pagamentos.Where(p => p.ClubeId == ClubeId && !p.Estornado && p.Data >= ini && p.Data <= fim)
            .SumAsync(p => (decimal?)p.Valor, ct) ?? 0;
        var lancPagos = db.LancamentosFinanceiros.Where(l => l.ClubeId == ClubeId && l.PagoEm >= ini && l.PagoEm <= fim);
        var outras = await lancPagos.Where(l => l.Tipo == TipoLancamento.Receita).SumAsync(l => (decimal?)l.Valor, ct) ?? 0;
        var despesas = await lancPagos.Where(l => l.Tipo == TipoLancamento.Despesa).SumAsync(l => (decimal?)l.Valor, ct) ?? 0;

        var abertas = db.Mensalidades.Where(m => m.ClubeId == ClubeId && m.Situacao == SituacaoMensalidade.Aberta);
        var aReceberMens = await abertas.SumAsync(m => (decimal?)(m.ValorPlano + m.ValorDependentes), ct) ?? 0;
        var pendentes = db.LancamentosFinanceiros.Where(l => l.ClubeId == ClubeId && l.PagoEm == null);
        var aReceberOutros = await pendentes.Where(l => l.Tipo == TipoLancamento.Receita).SumAsync(l => (decimal?)l.Valor, ct) ?? 0;
        var aPagar = await pendentes.Where(l => l.Tipo == TipoLancamento.Despesa).SumAsync(l => (decimal?)l.Valor, ct) ?? 0;

        var vencidas = abertas.Where(m => m.Vencimento < hoje);
        var inad = new InadimplenciaDto(
            await vencidas.Select(m => m.AssociadoId).Distinct().CountAsync(ct),
            await vencidas.CountAsync(ct),
            await vencidas.SumAsync(m => (decimal?)(m.ValorPlano + m.ValorDependentes), ct) ?? 0);

        var receitas = mensalidadesPagas + outras;
        return new ResumoFinanceiroDto(ini, fim, mensalidadesPagas, outras, despesas, receitas - despesas, aReceberMens, aReceberOutros, aPagar, inad);
    }

    public async Task<FluxoCaixaDto> FluxoCaixaAsync(DateOnly? de, DateOnly? ate, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Financeiro.Visualizar, ct);
        var (ini, fim) = Periodo(de, ate);

        var pagamentos = await db.Pagamentos.AsNoTracking()
            .Where(p => p.ClubeId == ClubeId && !p.Estornado && p.Data >= ini && p.Data <= fim)
            .Select(p => new
            {
                p.Data, p.Valor, p.NumeroRecibo, p.Mensalidade.Competencia,
                Nome = db.Associados.Where(a => a.Id == p.Mensalidade.AssociadoId).Select(a => a.Pessoa.NomeCompleto).FirstOrDefault(),
            })
            .ToListAsync(ct);
        var lancamentos = await db.LancamentosFinanceiros.AsNoTracking()
            .Where(l => l.ClubeId == ClubeId && l.PagoEm >= ini && l.PagoEm <= fim)
            .Select(l => new { Data = l.PagoEm!.Value, l.Tipo, l.Valor, l.Descricao, Categoria = l.Categoria.Nome })
            .ToListAsync(ct);

        var movimentos = pagamentos
            .Select(p => (p.Data, Descricao: $"Mensalidade {p.Competencia:MM/yyyy} — {p.Nome} (recibo {p.NumeroRecibo})", Tipo: TipoLancamento.Receita, p.Valor))
            .Concat(lancamentos.Select(l => (l.Data, Descricao: $"{l.Descricao} ({l.Categoria})", l.Tipo, l.Valor)))
            .OrderBy(m => m.Data).ThenBy(m => m.Tipo)
            .ToList();

        decimal saldo = 0;
        var lista = movimentos.Select(m =>
        {
            saldo += m.Tipo == TipoLancamento.Receita ? m.Valor : -m.Valor;
            return new MovimentoCaixaDto(m.Data, m.Descricao, m.Tipo, m.Valor, saldo);
        }).ToList();

        var entradas = movimentos.Where(m => m.Tipo == TipoLancamento.Receita).Sum(m => m.Valor);
        var saidas = movimentos.Where(m => m.Tipo == TipoLancamento.Despesa).Sum(m => m.Valor);
        return new FluxoCaixaDto(ini, fim, entradas, saidas, entradas - saidas, lista);
    }

    public async Task<IReadOnlyList<InadimplenteDto>> InadimplentesAsync(CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Financeiro.Visualizar, ct);
        var hoje = Hoje;
        var cfg = await db.ConfiguracoesFinanceiras.FirstAsync(c => c.ClubeId == ClubeId, ct);
        var vencidas = await db.Mensalidades.AsNoTracking()
            .Where(m => m.ClubeId == ClubeId && m.Situacao == SituacaoMensalidade.Aberta && m.Vencimento < hoje)
            .ToListAsync(ct);
        var ids = vencidas.Select(m => m.AssociadoId).Distinct().ToList();
        var associados = await db.Associados.AsNoTracking().Where(a => ids.Contains(a.Id))
            .Select(a => new { a.Id, a.Pessoa.NomeCompleto, a.Matricula }).ToDictionaryAsync(a => a.Id, ct);

        return vencidas.GroupBy(m => m.AssociadoId)
            .Where(g => associados.ContainsKey(g.Key))
            .Select(g => new InadimplenteDto(g.Key, associados[g.Key].NomeCompleto, associados[g.Key].Matricula, g.Count(),
                g.Sum(m => m.Valor + CalculoFinanceiro.Encargos(m.Valor, m.Vencimento, hoje, cfg)), g.Min(m => m.Vencimento)))
            .OrderByDescending(i => i.ValorDevido).ToList();
    }

    private (DateOnly De, DateOnly Ate) Periodo(DateOnly? de, DateOnly? ate)
    {
        var inicioMes = new DateOnly(Hoje.Year, Hoje.Month, 1);
        var ini = de ?? inicioMes;
        var fim = ate ?? inicioMes.AddMonths(1).AddDays(-1);
        if (fim < ini) throw new ValidationException([new ValidationFailure("Ate", "A data final deve ser posterior à inicial.")]);
        return (ini, fim);
    }

    private static string Situacao(DateOnly? pagoEm, DateOnly vencimento, DateOnly hoje) =>
        pagoEm is not null ? "Pago" : vencimento < hoje ? "Atrasado" : "Em aberto";

    private LancamentoDto Dto(LancamentoFinanceiro l, string categoria) =>
        new(l.Id, l.Tipo, l.CategoriaId, categoria, l.Descricao, l.Valor, l.Vencimento, l.PagoEm, Situacao(l.PagoEm, l.Vencimento, Hoje));
}
