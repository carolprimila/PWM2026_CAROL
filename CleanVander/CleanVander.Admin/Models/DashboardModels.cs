namespace CleanVander.Admin.Models;

public sealed record Product(
    int Id,
    string Codigo,
    string Descricao,
    string Categoria,
    string UnidadeMedida,
    string TamanhoVolume,
    decimal Preco,
    bool Ativo);

public sealed record Client(
    int Id,
    string TipoPessoa,
    string Nome,
    string Documento,
    string Email,
    string Telefone,
    string Endereco);

public sealed record OrderItem(
    int ProdutoId,
    string CodigoProduto,
    string DescricaoProduto,
    string UnidadeMedida,
    string TamanhoVolume,
    int Quantidade,
    decimal PrecoUnitario,
    decimal Subtotal);

public sealed record Order(
    int Id,
    string Numero,
    DateTime CriadoEm,
    string Status,
    string FormaPagamento,
    decimal Total,
    Client? Cliente,
    List<OrderItem> Itens);

public sealed class ProductForm
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Descricao { get; set; } = "";
    public string Categoria { get; set; } = "";
    public string UnidadeMedida { get; set; } = "UN";
    public string TamanhoVolume { get; set; } = "";
    public decimal Preco { get; set; }
    public bool Ativo { get; set; } = true;
}

public sealed class StatusForm
{
    public int PedidoId { get; set; }
    public string Status { get; set; } = "";
}

public sealed class LoginForm
{
    public string Chave { get; set; } = "";
}

public sealed class DashboardViewModel
{
    public List<Product> Produtos { get; set; } = [];
    public List<Client> Clientes { get; set; } = [];
    public List<Order> Pedidos { get; set; } = [];
    public ProductForm Produto { get; set; } = new();
    public bool Autenticado { get; set; }
}