using CleanVander.Api.Models;
using SQLite;

namespace CleanVander.Api;

public sealed class CleanVanderDatabase
{
    public SQLiteConnection Connection { get; }

    public CleanVanderDatabase(IConfiguration config)
    {
        var path = config["Database:Path"]
                   ?? Path.Combine(AppContext.BaseDirectory, "data", "cleanvander.db3");
        var full = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);

        Connection = new SQLiteConnection(full);
        Connection.CreateTable<Cliente>();
        Connection.CreateTable<Produto>();
        Connection.CreateTable<Pedido>();
        Connection.CreateTable<ItemPedido>();
    }

    public void Seed()
    {
        if (Connection.Table<Produto>().Any()) return;

        Connection.InsertAll(new[]
        {
            new Produto
            {
                Codigo = "DET-001", Descricao = "Detergente neutro", Categoria = "Cozinha",
                UnidadeMedida = "UN", TamanhoVolume = "500 ml", Preco = 2.99m
            },
            new Produto
            {
                Codigo = "DES-001", Descricao = "Desinfetante lavanda", Categoria = "Desinfetante",
                UnidadeMedida = "UN", TamanhoVolume = "2 L", Preco = 8.50m
            },
            new Produto
            {
                Codigo = "AGU-001", Descricao = "Água sanitária", Categoria = "Alvejante",
                UnidadeMedida = "UN", TamanhoVolume = "1 L", Preco = 4.75m
            },
            new Produto
            {
                Codigo = "SAB-001", Descricao = "Sabão líquido", Categoria = "Lavanderia",
                UnidadeMedida = "UN", TamanhoVolume = "3 L", Preco = 18.90m
            },
            new Produto
            {
                Codigo = "PAP-001", Descricao = "Papel toalha interfolhado", Categoria = "Descartáveis",
                UnidadeMedida = "PCT", TamanhoVolume = "1000 folhas", Preco = 21.00m
            }
        });
    }
}