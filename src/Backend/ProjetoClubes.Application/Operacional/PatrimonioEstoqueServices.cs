using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Application.Seguranca;
using ProjetoClubes.Domain.Operacional;

namespace ProjetoClubes.Application.Operacional;

// ============================================================================================== patrimônio

public sealed record BemListaDto(Guid Id, string Codigo, string Nome, string? Localizacao, EstadoConservacao Estado, SituacaoBem Situacao,
    decimal? ValorAquisicao, DateOnly? DataAquisicao);

public sealed record ManutencaoDto(Guid Id, string Descricao, string? Fornecedor, DateOnly Inicio, DateOnly? Fim, decimal? Custo);

public sealed record BemDto(Guid Id, string Codigo, string Nome, string? Descricao, string? Localizacao, EstadoConservacao Estado,
    SituacaoBem Situacao, decimal? ValorAquisicao, DateOnly? DataAquisicao, string? MotivoBaixa, decimal CustoManutencoes,
    IReadOnlyList<ManutencaoDto> Manutencoes, IReadOnlyList<HistoricoDto> Historico);

public sealed record ResumoPatrimonioDto(int Bens, int EmManutencao, int Baixados, decimal ValorEmUso);

public sealed record SalvarBemRequest(string Nome, string? Descricao, string? Localizacao, decimal? ValorAquisicao, DateOnly? DataAquisicao,
    EstadoConservacao Estado);

public sealed record MovimentarBemRequest(string? Localizacao, EstadoConservacao Estado, string? Observacao);

public sealed record AbrirManutencaoRequest(string Descricao, DateOnly Inicio, string? Fornecedor);

public sealed record ConcluirManutencaoRequest(DateOnly Fim, decimal? Custo, EstadoConservacao Estado);

public sealed record MotivoTextoRequest(string Motivo);

public sealed class ConsultaBens : ConsultaPaginada
{
    public SituacaoBem? Situacao { get; set; }
    public string? Localizacao { get; set; }
}

public sealed class SalvarBemRequestValidator : AbstractValidator<SalvarBemRequest>
{
    public SalvarBemRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().WithMessage("Informe o nome do bem.").MaximumLength(150);
        RuleFor(x => x.Descricao).MaximumLength(1000);
        RuleFor(x => x.Localizacao).MaximumLength(120);
        RuleFor(x => x.ValorAquisicao).GreaterThanOrEqualTo(0).When(x => x.ValorAquisicao is not null);
        RuleFor(x => x.Estado).IsInEnum();
    }
}

public interface IPatrimonioService
{
    Task<Pagina<BemListaDto>> ListarAsync(ConsultaBens c, CancellationToken ct);
    Task<ResumoPatrimonioDto> ResumoAsync(CancellationToken ct);
    Task<BemDto> ObterAsync(Guid id, CancellationToken ct);
    Task<BemDto> CriarAsync(SalvarBemRequest r, CancellationToken ct);
    Task<BemDto> AlterarAsync(Guid id, SalvarBemRequest r, CancellationToken ct);
    Task<BemDto> MovimentarAsync(Guid id, MovimentarBemRequest r, CancellationToken ct);
    Task<BemDto> AbrirManutencaoAsync(Guid id, AbrirManutencaoRequest r, CancellationToken ct);
    Task<BemDto> ConcluirManutencaoAsync(Guid id, Guid manutencaoId, ConcluirManutencaoRequest r, CancellationToken ct);
    Task<BemDto> BaixarAsync(Guid id, string motivo, CancellationToken ct);
}

/// <summary>Bens patrimoniais com código sequencial, localização, estado, manutenções e histórico.</summary>
public sealed class PatrimonioService(
    IAppDbContext db,
    IUsuarioAtual atual,
    IPermissaoResolver permissoes,
    IGeradorSequencia sequencias,
    TimeProvider relogio,
    IValidator<SalvarBemRequest> validator) : IPatrimonioService
{
    private Guid ClubeId => atual.ClubeIdObrigatorio;
    private DateTimeOffset Agora => relogio.GetUtcNow();

    public async Task<Pagina<BemListaDto>> ListarAsync(ConsultaBens c, CancellationToken ct)
    {
        await Exigir(ct);
        var q = db.Bens.AsNoTracking().Where(b => b.ClubeId == ClubeId);
        q = c.Situacao is { } s ? q.Where(b => b.Situacao == s) : q.Where(b => b.Situacao != SituacaoBem.Baixado);
        if (!string.IsNullOrWhiteSpace(c.Localizacao)) q = q.Where(b => b.Localizacao == c.Localizacao);
        if (!string.IsNullOrWhiteSpace(c.Busca))
        {
            var termo = c.Busca.Trim().ToLower();
            q = q.Where(b => b.Codigo.ToLower().Contains(termo) || b.Nome.ToLower().Contains(termo)
                             || (b.Localizacao != null && b.Localizacao.ToLower().Contains(termo)));
        }
        return await q.OrderBy(b => b.Codigo)
            .Select(b => new BemListaDto(b.Id, b.Codigo, b.Nome, b.Localizacao, b.Estado, b.Situacao, b.ValorAquisicao, b.DataAquisicao))
            .PaginarAsync(c, ct);
    }

    public async Task<ResumoPatrimonioDto> ResumoAsync(CancellationToken ct)
    {
        await Exigir(ct);
        var q = db.Bens.AsNoTracking().Where(b => b.ClubeId == ClubeId);
        return new ResumoPatrimonioDto(
            await q.CountAsync(b => b.Situacao != SituacaoBem.Baixado, ct),
            await q.CountAsync(b => b.Situacao == SituacaoBem.EmManutencao, ct),
            await q.CountAsync(b => b.Situacao == SituacaoBem.Baixado, ct),
            await q.Where(b => b.Situacao != SituacaoBem.Baixado).SumAsync(b => b.ValorAquisicao ?? 0, ct));
    }

    public async Task<BemDto> ObterAsync(Guid id, CancellationToken ct)
    {
        await Exigir(ct);
        return await DetalheAsync(id, ct);
    }

    public async Task<BemDto> CriarAsync(SalvarBemRequest r, CancellationToken ct)
    {
        await Exigir(ct);
        await validator.ValidarAsync(r, ct);
        var codigo = $"PAT-{await sequencias.ProximoAsync(ClubeId, "patrimonio", ct):000000}";
        var b = new Bem(ClubeId, codigo, r.Nome, r.Descricao, r.Localizacao, r.ValorAquisicao, r.DataAquisicao, r.Estado);
        db.Bens.Add(b);
        Historico(b.Id, "Cadastro", $"Cadastrado em {b.Localizacao ?? "local não informado"}, estado {b.Estado}.");
        await db.SaveChangesAsync(ct);
        return await DetalheAsync(b.Id, ct);
    }

    public async Task<BemDto> AlterarAsync(Guid id, SalvarBemRequest r, CancellationToken ct)
    {
        await Exigir(ct);
        await validator.ValidarAsync(r, ct);
        var b = await CarregarAsync(id, ct);
        b.Alterar(r.Nome, r.Descricao, r.ValorAquisicao, r.DataAquisicao);
        var mudanca = b.Movimentar(r.Localizacao, r.Estado);
        Historico(b.Id, "Edicao", mudanca is null ? "Dados atualizados." : $"Dados atualizados. {mudanca}");
        await db.SaveChangesAsync(ct);
        return await DetalheAsync(id, ct);
    }

    public async Task<BemDto> MovimentarAsync(Guid id, MovimentarBemRequest r, CancellationToken ct)
    {
        await Exigir(ct);
        var b = await CarregarAsync(id, ct);
        var mudanca = b.Movimentar(r.Localizacao, r.Estado);
        if (mudanca is not null)
        {
            Historico(b.Id, "Movimentacao", string.IsNullOrWhiteSpace(r.Observacao) ? mudanca : $"{mudanca}. {r.Observacao.Trim()}");
            await db.SaveChangesAsync(ct);
        }
        return await DetalheAsync(id, ct);
    }

    public async Task<BemDto> AbrirManutencaoAsync(Guid id, AbrirManutencaoRequest r, CancellationToken ct)
    {
        await Exigir(ct);
        var b = await CarregarAsync(id, ct);
        var m = b.AbrirManutencao(r.Descricao, r.Inicio, r.Fornecedor);
        Historico(b.Id, "Manutencao", $"Enviado para manutenção: {m.Descricao}{(m.Fornecedor is null ? "" : $" ({m.Fornecedor})")}.");
        await db.SaveChangesAsync(ct);
        return await DetalheAsync(id, ct);
    }

    public async Task<BemDto> ConcluirManutencaoAsync(Guid id, Guid manutencaoId, ConcluirManutencaoRequest r, CancellationToken ct)
    {
        await Exigir(ct);
        var b = await CarregarAsync(id, ct);
        var m = b.ConcluirManutencao(manutencaoId, r.Fim, r.Custo, r.Estado);
        Historico(b.Id, "ManutencaoConcluida", $"Manutenção concluída em {r.Fim:dd/MM/yyyy}{(m.Custo is { } c ? $", custo R$ {c:N2}" : "")}; estado {b.Estado}.");
        await db.SaveChangesAsync(ct);
        return await DetalheAsync(id, ct);
    }

    public async Task<BemDto> BaixarAsync(Guid id, string motivo, CancellationToken ct)
    {
        await Exigir(ct);
        var b = await CarregarAsync(id, ct);
        b.Baixar(motivo);
        Historico(b.Id, "Baixa", $"Baixado do patrimônio: {b.MotivoBaixa}");
        await db.SaveChangesAsync(ct);
        return await DetalheAsync(id, ct);
    }

    private Task Exigir(CancellationToken ct) => permissoes.ExigirAsync(Permissoes.Operacional.Patrimonio, ct);

    private void Historico(Guid bemId, string evento, string descricao) =>
        db.HistoricoBens.Add(new HistoricoBem(ClubeId, bemId, evento, descricao, atual.UsuarioId, Agora));

    private async Task<Bem> CarregarAsync(Guid id, CancellationToken ct) =>
        await db.Bens.Include(b => b.Manutencoes).FirstOrDefaultAsync(b => b.Id == id && b.ClubeId == ClubeId, ct)
        ?? throw new NaoEncontradoException("Bem");

    private async Task<BemDto> DetalheAsync(Guid id, CancellationToken ct)
    {
        var b = await db.Bens.AsNoTracking().Include(x => x.Manutencoes).FirstOrDefaultAsync(x => x.Id == id && x.ClubeId == ClubeId, ct)
                ?? throw new NaoEncontradoException("Bem");
        var historico = await HistoricoOperacional.ListarAsync(db, db.HistoricoBens.Where(h => h.BemId == id), ct);
        return new BemDto(b.Id, b.Codigo, b.Nome, b.Descricao, b.Localizacao, b.Estado, b.Situacao, b.ValorAquisicao, b.DataAquisicao,
            b.MotivoBaixa, b.Manutencoes.Sum(m => m.Custo ?? 0),
            b.Manutencoes.OrderByDescending(m => m.Inicio).Select(m => new ManutencaoDto(m.Id, m.Descricao, m.Fornecedor, m.Inicio, m.Fim, m.Custo)).ToList(),
            historico);
    }
}

// ============================================================================================== estoque

public sealed record ProdutoDto(Guid Id, string Nome, string Unidade, string? Categoria, decimal EstoqueMinimo, decimal Quantidade,
    bool AbaixoDoMinimo, bool Ativo, decimal? CompraEmAberto);

public sealed record MovimentacaoEstoqueDto(Guid Id, Guid ProdutoId, string Produto, string Unidade, TipoMovimentacaoEstoque Tipo,
    decimal Quantidade, decimal SaldoApos, string? Motivo, string? Usuario, DateTimeOffset OcorridaEm);

public sealed record SolicitacaoCompraDto(Guid Id, Guid ProdutoId, string Produto, string Unidade, decimal Quantidade, string? Observacao,
    SituacaoSolicitacaoCompra Situacao, decimal? QuantidadeRecebida, decimal? ValorTotal, DateTimeOffset CriadaEm);

public sealed record SalvarProdutoRequest(string Nome, string Unidade, decimal EstoqueMinimo, string? Categoria, decimal? QuantidadeInicial, bool Ativo = true);

public sealed record MovimentarEstoqueRequest(TipoMovimentacaoEstoque Tipo, decimal Quantidade, string? Motivo);

public sealed record CriarSolicitacaoCompraRequest(Guid ProdutoId, decimal Quantidade, string? Observacao);

public sealed record AtenderSolicitacaoCompraRequest(decimal QuantidadeRecebida, decimal? ValorTotal);

public sealed class SalvarProdutoRequestValidator : AbstractValidator<SalvarProdutoRequest>
{
    public SalvarProdutoRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().WithMessage("Informe o nome do produto.").MaximumLength(120);
        RuleFor(x => x.Unidade).NotEmpty().WithMessage("Informe a unidade.").MaximumLength(10);
        RuleFor(x => x.Categoria).MaximumLength(60);
        RuleFor(x => x.EstoqueMinimo).GreaterThanOrEqualTo(0);
        RuleFor(x => x.QuantidadeInicial).GreaterThanOrEqualTo(0).When(x => x.QuantidadeInicial is not null);
    }
}

public sealed class MovimentarEstoqueRequestValidator : AbstractValidator<MovimentarEstoqueRequest>
{
    public MovimentarEstoqueRequestValidator()
    {
        RuleFor(x => x.Tipo).IsInEnum();
        RuleFor(x => x.Quantidade).LessThan(1_000_000).WithMessage("Quantidade muito alta.");
        RuleFor(x => x.Motivo).MaximumLength(200);
    }
}

public interface IEstoqueService
{
    Task<IReadOnlyList<ProdutoDto>> ListarAsync(string? busca, bool somenteAbaixoDoMinimo, bool incluirInativos, CancellationToken ct);
    Task<ProdutoDto> CriarAsync(SalvarProdutoRequest r, CancellationToken ct);
    Task<ProdutoDto> AlterarAsync(Guid id, SalvarProdutoRequest r, CancellationToken ct);
    Task<ProdutoDto> MovimentarAsync(Guid id, MovimentarEstoqueRequest r, CancellationToken ct);
    Task<IReadOnlyList<MovimentacaoEstoqueDto>> MovimentacoesAsync(Guid? produtoId, DateOnly? de, DateOnly? ate, CancellationToken ct);
    Task<IReadOnlyList<SolicitacaoCompraDto>> ComprasAsync(bool somenteAbertas, CancellationToken ct);
    Task<SolicitacaoCompraDto> SolicitarCompraAsync(CriarSolicitacaoCompraRequest r, CancellationToken ct);
    Task<IReadOnlyList<SolicitacaoCompraDto>> SolicitarComprasAbaixoDoMinimoAsync(CancellationToken ct);
    Task<SolicitacaoCompraDto> AtenderCompraAsync(Guid id, AtenderSolicitacaoCompraRequest r, CancellationToken ct);
    Task<SolicitacaoCompraDto> CancelarCompraAsync(Guid id, CancellationToken ct);
}

/// <summary>
/// Estoque: o saldo só muda por movimentação, em transação com bloqueio por produto (duas saídas simultâneas não
/// deixam o saldo negativo; o banco ainda recusa saldo negativo por CHECK).
/// </summary>
public sealed class EstoqueService(
    IAppDbContext db,
    IUsuarioAtual atual,
    IPermissaoResolver permissoes,
    ITransacoes transacoes,
    TimeProvider relogio,
    IValidator<SalvarProdutoRequest> produtoValidator,
    IValidator<MovimentarEstoqueRequest> movimentoValidator) : IEstoqueService
{
    private Guid ClubeId => atual.ClubeIdObrigatorio;
    private DateTimeOffset Agora => relogio.GetUtcNow();

    public async Task<IReadOnlyList<ProdutoDto>> ListarAsync(string? busca, bool somenteAbaixoDoMinimo, bool incluirInativos, CancellationToken ct)
    {
        await Exigir(ct);
        var q = db.Produtos.AsNoTracking().Where(p => p.ClubeId == ClubeId);
        if (!incluirInativos) q = q.Where(p => p.Ativo);
        if (somenteAbaixoDoMinimo) q = q.Where(p => p.Quantidade < p.EstoqueMinimo);
        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim().ToLower();
            q = q.Where(p => p.Nome.ToLower().Contains(termo) || (p.Categoria != null && p.Categoria.ToLower().Contains(termo)));
        }
        return await Projetar(q.OrderBy(p => p.Nome)).Take(500).ToListAsync(ct);
    }

    public async Task<ProdutoDto> CriarAsync(SalvarProdutoRequest r, CancellationToken ct)
    {
        await Exigir(ct);
        await produtoValidator.ValidarAsync(r, ct);
        await ExigirNomeUnicoAsync(r.Nome, null, ct);
        var p = new Produto(ClubeId, r.Nome, r.Unidade, r.EstoqueMinimo, r.Categoria);
        db.Produtos.Add(p);
        if (r.QuantidadeInicial is > 0 and var q)
            db.MovimentacoesEstoque.Add(p.Movimentar(TipoMovimentacaoEstoque.Entrada, q, "Saldo inicial", atual.UsuarioId, Agora));
        await db.SaveChangesAsync(ct);
        return await ProdutoAsync(p.Id, ct);
    }

    public async Task<ProdutoDto> AlterarAsync(Guid id, SalvarProdutoRequest r, CancellationToken ct)
    {
        await Exigir(ct);
        await produtoValidator.ValidarAsync(r, ct);
        await ExigirNomeUnicoAsync(r.Nome, id, ct);
        var p = await db.Produtos.FirstOrDefaultAsync(x => x.Id == id && x.ClubeId == ClubeId, ct) ?? throw new NaoEncontradoException("Produto");
        p.Alterar(r.Nome, r.Unidade, r.EstoqueMinimo, r.Categoria);
        p.Ativo = r.Ativo;
        await db.SaveChangesAsync(ct);
        return await ProdutoAsync(id, ct);
    }

    public async Task<ProdutoDto> MovimentarAsync(Guid id, MovimentarEstoqueRequest r, CancellationToken ct)
    {
        await Exigir(ct);
        await movimentoValidator.ValidarAsync(r, ct);
        await MovimentarComBloqueioAsync(id, r.Tipo, r.Quantidade, r.Motivo, ct);
        return await ProdutoAsync(id, ct);
    }

    public async Task<IReadOnlyList<MovimentacaoEstoqueDto>> MovimentacoesAsync(Guid? produtoId, DateOnly? de, DateOnly? ate, CancellationToken ct)
    {
        await Exigir(ct);
        var q = db.MovimentacoesEstoque.AsNoTracking().Where(m => m.ClubeId == ClubeId);
        if (produtoId is { } pid) q = q.Where(m => m.ProdutoId == pid);
        if (de is { } d) { var inicio = new DateTimeOffset(d.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero); q = q.Where(m => m.OcorridaEm >= inicio); }
        if (ate is { } a) { var fim = new DateTimeOffset(a.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero); q = q.Where(m => m.OcorridaEm < fim); }
        var itens = await (from m in q
                           join p in db.Produtos on m.ProdutoId equals p.Id
                           orderby m.OcorridaEm descending
                           select new { m, p.Nome, p.Unidade }).Take(300).ToListAsync(ct);
        var ids = itens.Select(x => x.m.UsuarioId).OfType<Guid>().Distinct().ToList();
        var nomes = await db.Usuarios.IgnoreQueryFilters().Where(u => ids.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Nome, ct);
        return itens.Select(x => new MovimentacaoEstoqueDto(x.m.Id, x.m.ProdutoId, x.Nome, x.Unidade, x.m.Tipo, x.m.Quantidade, x.m.SaldoApos,
            x.m.Motivo, x.m.UsuarioId is { } u ? nomes.GetValueOrDefault(u) : null, x.m.OcorridaEm)).ToList();
    }

    public async Task<IReadOnlyList<SolicitacaoCompraDto>> ComprasAsync(bool somenteAbertas, CancellationToken ct)
    {
        await Exigir(ct);
        var q = db.SolicitacoesCompra.AsNoTracking().Where(s => s.ClubeId == ClubeId);
        if (somenteAbertas) q = q.Where(s => s.Situacao == SituacaoSolicitacaoCompra.Aberta);
        return await ProjetarCompras(q.OrderBy(s => s.Situacao).ThenByDescending(s => s.CriadoEm)).Take(200).ToListAsync(ct);
    }

    public async Task<SolicitacaoCompraDto> SolicitarCompraAsync(CriarSolicitacaoCompraRequest r, CancellationToken ct)
    {
        await Exigir(ct);
        if (!await db.Produtos.AnyAsync(p => p.Id == r.ProdutoId && p.ClubeId == ClubeId && p.Ativo, ct)) throw new NaoEncontradoException("Produto");
        var s = new SolicitacaoCompra(ClubeId, r.ProdutoId, r.Quantidade, r.Observacao);
        db.SolicitacoesCompra.Add(s);
        await db.SaveChangesAsync(ct);
        return await CompraAsync(s.Id, ct);
    }

    /// <summary>
    /// Gera pedidos para os produtos abaixo do mínimo que ainda não têm pedido aberto. Quantidade sugerida: o que falta
    /// para chegar ao dobro do mínimo.
    /// </summary>
    public async Task<IReadOnlyList<SolicitacaoCompraDto>> SolicitarComprasAbaixoDoMinimoAsync(CancellationToken ct)
    {
        await Exigir(ct);
        var produtos = await db.Produtos.Where(p => p.ClubeId == ClubeId && p.Ativo && p.Quantidade < p.EstoqueMinimo
                && !db.SolicitacoesCompra.Any(s => s.ProdutoId == p.Id && s.Situacao == SituacaoSolicitacaoCompra.Aberta))
            .ToListAsync(ct);
        var novas = produtos.Select(p => new SolicitacaoCompra(ClubeId, p.Id, p.EstoqueMinimo * 2 - p.Quantidade, "Gerada: abaixo do estoque mínimo"))
            .ToList();
        db.SolicitacoesCompra.AddRange(novas);
        await db.SaveChangesAsync(ct);
        var ids = novas.Select(n => n.Id).ToList();
        return await ProjetarCompras(db.SolicitacoesCompra.AsNoTracking().Where(s => ids.Contains(s.Id))).ToListAsync(ct);
    }

    public async Task<SolicitacaoCompraDto> AtenderCompraAsync(Guid id, AtenderSolicitacaoCompraRequest r, CancellationToken ct)
    {
        await Exigir(ct);
        var s = await db.SolicitacoesCompra.FirstOrDefaultAsync(x => x.Id == id && x.ClubeId == ClubeId, ct) ?? throw new NaoEncontradoException("Solicitação de compra");
        // Atender e dar entrada são uma coisa só: mesma transação, com o bloqueio do produto.
        await transacoes.ComBloqueioAsync($"produto:{s.ProdutoId}", async () =>
        {
            await db.Entry(s).ReloadAsync(ct); // outro atendimento simultâneo já pode ter encerrado o pedido
            s.Atender(r.QuantidadeRecebida, r.ValorTotal);
            var p = await db.Produtos.FirstAsync(x => x.Id == s.ProdutoId, ct);
            await db.Entry(p).ReloadAsync(ct);
            db.MovimentacoesEstoque.Add(p.Movimentar(TipoMovimentacaoEstoque.Entrada, r.QuantidadeRecebida, "Compra recebida", atual.UsuarioId, Agora));
            await db.SaveChangesAsync(ct);
            return true;
        }, ct);
        return await CompraAsync(id, ct);
    }

    public async Task<SolicitacaoCompraDto> CancelarCompraAsync(Guid id, CancellationToken ct)
    {
        await Exigir(ct);
        var s = await db.SolicitacoesCompra.FirstOrDefaultAsync(x => x.Id == id && x.ClubeId == ClubeId, ct) ?? throw new NaoEncontradoException("Solicitação de compra");
        s.Cancelar();
        await db.SaveChangesAsync(ct);
        return await CompraAsync(id, ct);
    }

    // ---------------------------------------------------------------- auxiliares

    private Task Exigir(CancellationToken ct) => permissoes.ExigirAsync(Permissoes.Operacional.Estoque, ct);

    private Task MovimentarComBloqueioAsync(Guid produtoId, TipoMovimentacaoEstoque tipo, decimal quantidade, string? motivo, CancellationToken ct) =>
        transacoes.ComBloqueioAsync($"produto:{produtoId}", async () =>
        {
            var p = await db.Produtos.FirstOrDefaultAsync(x => x.Id == produtoId && x.ClubeId == ClubeId, ct) ?? throw new NaoEncontradoException("Produto");
            await db.Entry(p).ReloadAsync(ct); // saldo atual dentro do bloqueio
            db.MovimentacoesEstoque.Add(p.Movimentar(tipo, quantidade, motivo, atual.UsuarioId, Agora));
            await db.SaveChangesAsync(ct);
            return true;
        }, ct);

    private async Task ExigirNomeUnicoAsync(string nome, Guid? id, CancellationToken ct)
    {
        var n = nome.Trim().ToLower();
        if (await db.Produtos.AnyAsync(p => p.ClubeId == ClubeId && p.Nome.ToLower() == n && p.Id != id, ct))
            throw new ConflitoException("produto.duplicado", "Já existe um produto com este nome.");
    }

    private IQueryable<ProdutoDto> Projetar(IQueryable<Produto> q) => q.Select(p => new ProdutoDto(p.Id, p.Nome, p.Unidade, p.Categoria,
        p.EstoqueMinimo, p.Quantidade, p.Quantidade < p.EstoqueMinimo, p.Ativo,
        db.SolicitacoesCompra.Any(s => s.ProdutoId == p.Id && s.Situacao == SituacaoSolicitacaoCompra.Aberta)
            ? db.SolicitacoesCompra.Where(s => s.ProdutoId == p.Id && s.Situacao == SituacaoSolicitacaoCompra.Aberta).Sum(s => s.Quantidade)
            : null));

    private IQueryable<SolicitacaoCompraDto> ProjetarCompras(IQueryable<SolicitacaoCompra> q) =>
        from s in q
        join p in db.Produtos on s.ProdutoId equals p.Id
        select new SolicitacaoCompraDto(s.Id, s.ProdutoId, p.Nome, p.Unidade, s.Quantidade, s.Observacao, s.Situacao, s.QuantidadeRecebida,
            s.ValorTotal, s.CriadoEm);

    private async Task<ProdutoDto> ProdutoAsync(Guid id, CancellationToken ct) =>
        await Projetar(db.Produtos.AsNoTracking().Where(p => p.Id == id && p.ClubeId == ClubeId)).FirstOrDefaultAsync(ct)
        ?? throw new NaoEncontradoException("Produto");

    private async Task<SolicitacaoCompraDto> CompraAsync(Guid id, CancellationToken ct) =>
        await ProjetarCompras(db.SolicitacoesCompra.AsNoTracking().Where(s => s.Id == id)).FirstOrDefaultAsync(ct)
        ?? throw new NaoEncontradoException("Solicitação de compra");
}
