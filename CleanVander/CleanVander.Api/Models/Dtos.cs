namespace CleanVander.Api.Models;

public sealed record CadastroClienteDto(
    string TipoPessoa,
    string Nome,
    string Documento,
    string Email,
    string Telefone,
    string Endereco);

public sealed record CadastroClienteResultado(
    int ClienteId,
    string ChaveAcesso,
    string Nome);

public sealed record ProdutoDto(
    int Id,
    string Codigo,
    string Descricao,
    string Categoria,
    string UnidadeMedida,
    string TamanhoVolume,
    decimal Preco,
    bool Ativo);

public sealed record ProdutoEntrada(
    string Codigo,
    string Descricao,
    string Categoria,
    string UnidadeMedida,
    string TamanhoVolume,
    decimal Preco,
    bool Ativo = true);

public sealed record ItemPedidoDto(
    int ProdutoId,
    int Quantidade);

public sealed record CriarPedidoDto(
    List<ItemPedidoDto> Itens);

public sealed record AtualizarStatusDto(
    string Status);

public sealed record ClienteResumoDto(
    int Id,
    string TipoPessoa,
    string Nome,
    string Documento,
    string Email,
    string Telefone,
    string Endereco);

public sealed record ItemPedidoResposta(
    int ProdutoId,
    string CodigoProduto,
    string DescricaoProduto,
    string UnidadeMedida,
    string TamanhoVolume,
    int Quantidade,
    decimal PrecoUnitario,
    decimal Subtotal);

public sealed record PedidoResposta(
    int Id,
    string Numero,
    DateTime CriadoEm,
    string Status,
    string FormaPagamento,
    decimal Total,
    ClienteResumoDto? Cliente,
    List<ItemPedidoResposta> Itens);