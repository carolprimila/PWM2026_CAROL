using ProjetoClubes.Domain.Common;

namespace ProjetoClubes.Domain.Operacional;

public enum EstadoConservacao
{
    Novo = 1,
    Bom = 2,
    Regular = 3,
    Ruim = 4,
    Inservivel = 5,
}

public enum SituacaoBem
{
    EmUso = 1,
    EmManutencao = 2,
    Baixado = 3,
}

/// <summary>Bem patrimonial com código sequencial por clube (ex.: PAT-000012), localização, valor e estado.</summary>
public class Bem : AuditableEntity, ITenantScoped
{
    private readonly List<ManutencaoBem> _manutencoes = [];

    private Bem() { }

    public Bem(Guid clubeId, string codigo, string nome, string? descricao, string? localizacao, decimal? valorAquisicao,
        DateOnly? dataAquisicao, EstadoConservacao estado)
    {
        ClubeId = clubeId;
        Codigo = codigo;
        Situacao = SituacaoBem.EmUso;
        Alterar(nome, descricao, valorAquisicao, dataAquisicao);
        Localizacao = Limpar(localizacao);
        Estado = estado;
    }

    public Guid ClubeId { get; private set; }
    public string Codigo { get; private set; } = null!;
    public string Nome { get; private set; } = null!;
    public string? Descricao { get; private set; }
    public string? Localizacao { get; private set; }
    public decimal? ValorAquisicao { get; private set; }
    public DateOnly? DataAquisicao { get; private set; }
    public EstadoConservacao Estado { get; private set; }
    public SituacaoBem Situacao { get; private set; }
    public string? MotivoBaixa { get; private set; }

    public IReadOnlyCollection<ManutencaoBem> Manutencoes => _manutencoes;

    public void Alterar(string nome, string? descricao, decimal? valorAquisicao, DateOnly? dataAquisicao)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new DomainException("bem.nome", "Informe o nome do bem.");
        if (valorAquisicao < 0) throw new DomainException("bem.valor", "O valor de aquisição não pode ser negativo.");
        Nome = nome.Trim();
        Descricao = Limpar(descricao);
        ValorAquisicao = valorAquisicao;
        DataAquisicao = dataAquisicao;
    }

    /// <summary>Muda localização e/ou estado; retorna a descrição da mudança para o histórico (ou nulo se nada mudou).</summary>
    public string? Movimentar(string? localizacao, EstadoConservacao estado)
    {
        ExigirAtivo();
        var mudancas = new List<string>();
        var nova = Limpar(localizacao);
        if (nova != Localizacao) mudancas.Add($"Localização: {Localizacao ?? "—"} → {nova ?? "—"}");
        if (estado != Estado) mudancas.Add($"Estado: {Estado} → {estado}");
        Localizacao = nova;
        Estado = estado;
        return mudancas.Count == 0 ? null : string.Join("; ", mudancas);
    }

    public ManutencaoBem AbrirManutencao(string descricao, DateOnly inicio, string? fornecedor)
    {
        ExigirAtivo();
        if (Situacao == SituacaoBem.EmManutencao) throw new DomainException("bem.em_manutencao", "O bem já está em manutenção.");
        if (string.IsNullOrWhiteSpace(descricao)) throw new DomainException("bem.manutencao", "Descreva a manutenção.");
        var m = new ManutencaoBem(ClubeId, Id, descricao.Trim(), inicio, Limpar(fornecedor));
        _manutencoes.Add(m);
        Situacao = SituacaoBem.EmManutencao;
        return m;
    }

    public ManutencaoBem ConcluirManutencao(Guid manutencaoId, DateOnly fim, decimal? custo, EstadoConservacao estado)
    {
        var m = _manutencoes.FirstOrDefault(x => x.Id == manutencaoId && x.Fim is null)
                ?? throw new DomainException("bem.manutencao", "Manutenção não encontrada ou já concluída.");
        m.Concluir(fim, custo);
        Situacao = SituacaoBem.EmUso;
        Estado = estado;
        return m;
    }

    public void Baixar(string motivo)
    {
        ExigirAtivo();
        if (string.IsNullOrWhiteSpace(motivo)) throw new DomainException("bem.motivo", "Informe o motivo da baixa.");
        if (Situacao == SituacaoBem.EmManutencao) throw new DomainException("bem.em_manutencao", "Conclua a manutenção antes da baixa.");
        Situacao = SituacaoBem.Baixado;
        MotivoBaixa = motivo.Trim();
    }

    private void ExigirAtivo()
    {
        if (Situacao == SituacaoBem.Baixado) throw new DomainException("bem.baixado", "Este bem já foi baixado do patrimônio.");
    }

    private static string? Limpar(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

public class ManutencaoBem : Entity, ITenantScoped
{
    private ManutencaoBem() { }

    internal ManutencaoBem(Guid clubeId, Guid bemId, string descricao, DateOnly inicio, string? fornecedor)
    {
        ClubeId = clubeId;
        BemId = bemId;
        Descricao = descricao;
        Inicio = inicio;
        Fornecedor = fornecedor;
    }

    public Guid ClubeId { get; private set; }
    public Guid BemId { get; private set; }
    public string Descricao { get; private set; } = null!;
    public string? Fornecedor { get; private set; }
    public DateOnly Inicio { get; private set; }
    public DateOnly? Fim { get; private set; }
    public decimal? Custo { get; private set; }

    internal void Concluir(DateOnly fim, decimal? custo)
    {
        if (fim < Inicio) throw new DomainException("bem.periodo", "A conclusão não pode ser antes do início.");
        if (custo < 0) throw new DomainException("bem.custo", "O custo não pode ser negativo.");
        Fim = fim;
        Custo = custo;
    }
}

public class HistoricoBem : RegistroHistorico
{
    private HistoricoBem() { }

    public HistoricoBem(Guid clubeId, Guid bemId, string evento, string descricao, Guid? usuarioId, DateTimeOffset agora)
    {
        ClubeId = clubeId;
        BemId = bemId;
        Evento = evento;
        Descricao = descricao;
        UsuarioId = usuarioId;
        OcorridoEm = agora;
    }

    public Guid BemId { get; private set; }
}

// ---------------------------------------------------------------------------------------------- estoque

public enum TipoMovimentacaoEstoque
{
    Entrada = 1,
    Saida = 2,
    /// <summary>Acerto de inventário: define o saldo contado.</summary>
    Ajuste = 3,
}

/// <summary>Produto controlado em estoque. O saldo só muda por movimentação (nunca fica negativo).</summary>
public class Produto : AuditableEntity, ITenantScoped
{
    private Produto() { }

    public Produto(Guid clubeId, string nome, string unidade, decimal estoqueMinimo, string? categoria)
    {
        ClubeId = clubeId;
        Ativo = true;
        Alterar(nome, unidade, estoqueMinimo, categoria);
    }

    public Guid ClubeId { get; private set; }
    public string Nome { get; private set; } = null!;
    /// <summary>Unidade de medida (un, kg, L, cx...).</summary>
    public string Unidade { get; private set; } = null!;
    public string? Categoria { get; private set; }
    public decimal EstoqueMinimo { get; private set; }
    public decimal Quantidade { get; private set; }
    public bool Ativo { get; set; }

    public bool AbaixoDoMinimo => Quantidade < EstoqueMinimo;

    public void Alterar(string nome, string unidade, decimal estoqueMinimo, string? categoria)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new DomainException("produto.nome", "Informe o nome do produto.");
        if (string.IsNullOrWhiteSpace(unidade)) throw new DomainException("produto.unidade", "Informe a unidade.");
        if (estoqueMinimo < 0) throw new DomainException("produto.minimo", "O estoque mínimo não pode ser negativo.");
        Nome = nome.Trim();
        Unidade = unidade.Trim();
        EstoqueMinimo = estoqueMinimo;
        Categoria = string.IsNullOrWhiteSpace(categoria) ? null : categoria.Trim();
    }

    public MovimentacaoEstoque Movimentar(TipoMovimentacaoEstoque tipo, decimal quantidade, string? motivo, Guid? usuarioId, DateTimeOffset agora)
    {
        if (!Ativo) throw new DomainException("produto.inativo", "Produto inativo.");
        if (tipo != TipoMovimentacaoEstoque.Ajuste && quantidade <= 0)
            throw new DomainException("estoque.quantidade", "A quantidade deve ser maior que zero.");
        if (tipo == TipoMovimentacaoEstoque.Ajuste && quantidade < 0)
            throw new DomainException("estoque.quantidade", "O saldo contado não pode ser negativo.");
        if (tipo == TipoMovimentacaoEstoque.Saida && quantidade > Quantidade)
            throw new DomainException("estoque.insuficiente", $"Estoque insuficiente: há {Quantidade:0.###} {Unidade}.");
        if (tipo == TipoMovimentacaoEstoque.Ajuste && string.IsNullOrWhiteSpace(motivo))
            throw new DomainException("estoque.motivo", "Informe o motivo do ajuste.");

        var delta = tipo switch
        {
            TipoMovimentacaoEstoque.Entrada => quantidade,
            TipoMovimentacaoEstoque.Saida => -quantidade,
            _ => quantidade - Quantidade,
        };
        Quantidade += delta;
        return new MovimentacaoEstoque(ClubeId, Id, tipo, delta, Quantidade, motivo, usuarioId, agora);
    }
}

/// <summary>Movimentação de estoque (histórico imutável). <see cref="Quantidade"/> é a variação com sinal.</summary>
public class MovimentacaoEstoque : Entity, ITenantScoped, INaoAuditavel
{
    private MovimentacaoEstoque() { }

    internal MovimentacaoEstoque(Guid clubeId, Guid produtoId, TipoMovimentacaoEstoque tipo, decimal quantidade, decimal saldo,
        string? motivo, Guid? usuarioId, DateTimeOffset agora)
    {
        ClubeId = clubeId;
        ProdutoId = produtoId;
        Tipo = tipo;
        Quantidade = quantidade;
        SaldoApos = saldo;
        Motivo = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();
        UsuarioId = usuarioId;
        OcorridaEm = agora;
    }

    public Guid ClubeId { get; private set; }
    public Guid ProdutoId { get; private set; }
    public TipoMovimentacaoEstoque Tipo { get; private set; }
    public decimal Quantidade { get; private set; }
    public decimal SaldoApos { get; private set; }
    public string? Motivo { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public DateTimeOffset OcorridaEm { get; private set; }
}

public enum SituacaoSolicitacaoCompra
{
    Aberta = 1,
    Atendida = 2,
    Cancelada = 3,
}

/// <summary>Pedido de compra de um produto. Ao ser atendido, gera a entrada no estoque.</summary>
public class SolicitacaoCompra : AuditableEntity, ITenantScoped
{
    private SolicitacaoCompra() { }

    public SolicitacaoCompra(Guid clubeId, Guid produtoId, decimal quantidade, string? observacao)
    {
        if (quantidade <= 0) throw new DomainException("compra.quantidade", "A quantidade deve ser maior que zero.");
        ClubeId = clubeId;
        ProdutoId = produtoId;
        Quantidade = quantidade;
        Observacao = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim();
        Situacao = SituacaoSolicitacaoCompra.Aberta;
    }

    public Guid ClubeId { get; private set; }
    public Guid ProdutoId { get; private set; }
    public decimal Quantidade { get; private set; }
    public string? Observacao { get; private set; }
    public SituacaoSolicitacaoCompra Situacao { get; private set; }
    public decimal? QuantidadeRecebida { get; private set; }
    public decimal? ValorTotal { get; private set; }

    public void Atender(decimal quantidadeRecebida, decimal? valorTotal)
    {
        if (Situacao != SituacaoSolicitacaoCompra.Aberta) throw new DomainException("compra.encerrada", "Esta solicitação já foi encerrada.");
        if (quantidadeRecebida <= 0) throw new DomainException("compra.quantidade", "Informe a quantidade recebida.");
        if (valorTotal < 0) throw new DomainException("compra.valor", "O valor não pode ser negativo.");
        Situacao = SituacaoSolicitacaoCompra.Atendida;
        QuantidadeRecebida = quantidadeRecebida;
        ValorTotal = valorTotal;
    }

    public void Cancelar()
    {
        if (Situacao != SituacaoSolicitacaoCompra.Aberta) throw new DomainException("compra.encerrada", "Esta solicitação já foi encerrada.");
        Situacao = SituacaoSolicitacaoCompra.Cancelada;
    }
}
