using SQLite;
namespace CleanVander.Client.Models;
[Table("Carrinho")]
public sealed class CarrinhoItem { [PrimaryKey] public int ProdutoId { get; set; } public string Codigo { get; set; } = ""; public string Descricao { get; set; } = ""; public string Categoria { get; set; } = ""; public string UnidadeMedida { get; set; } = "UN"; public string TamanhoVolume { get; set; } = ""; public decimal Preco { get; set; } public int Quantidade { get; set; } [Ignore] public decimal Subtotal => Preco * Quantidade; }

