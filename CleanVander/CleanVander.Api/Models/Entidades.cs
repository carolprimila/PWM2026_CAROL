using SQLite;
namespace CleanVander.Api.Models;
public enum TipoPessoa { Fisica, Juridica }
[Table("Clientes")]
public sealed class Cliente { [PrimaryKey,AutoIncrement] public int Id { get; set; } public TipoPessoa TipoPessoa { get; set; } public string Nome { get; set; }=""; [Unique] public string Documento { get; set; }=""; public string Email { get; set; }=""; public string Telefone { get; set; }=""; public string Endereco { get; set; }=""; public string ChaveHash { get; set; }=""; public DateTime CriadoEm { get; set; }=DateTime.UtcNow; }
[Table("Produtos")]
public sealed class Produto { [PrimaryKey,AutoIncrement] public int Id { get; set; } [Unique] public string Codigo { get; set; }=""; public string Descricao { get; set; }=""; public string Categoria { get; set; }=""; public string UnidadeMedida { get; set; }="UN"; public string TamanhoVolume { get; set; }=""; public decimal Preco { get; set; } public bool Ativo { get; set; }=true; }
[Table("Pedidos")]
public sealed class Pedido { [PrimaryKey,AutoIncrement] public int Id { get; set; } public string Numero { get; set; }=""; public int ClienteId { get; set; } public DateTime CriadoEm { get; set; }=DateTime.UtcNow; public string Status { get; set; }="Recebido"; public string FormaPagamento { get; set; }="À vista na entrega"; public decimal Total { get; set; } }
[Table("ItensPedido")]
public sealed class ItemPedido { [PrimaryKey,AutoIncrement] public int Id { get; set; } public int PedidoId { get; set; } public int ProdutoId { get; set; } public string CodigoProduto { get; set; }=""; public string DescricaoProduto { get; set; }=""; public string UnidadeMedida { get; set; }="UN"; public string TamanhoVolume { get; set; }=""; public int Quantidade { get; set; } public decimal PrecoUnitario { get; set; } public decimal Subtotal => Quantidade*PrecoUnitario; }
