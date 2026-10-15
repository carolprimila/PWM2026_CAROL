using SQLite;
using CleanVander.Client.Models;
namespace CleanVander.Client.Services;
public sealed class LocalStore
{
    private SQLiteAsyncConnection? _db;
    private async Task<SQLiteAsyncConnection> DbAsync() { if(_db is not null) return _db; _db=new SQLiteAsyncConnection(Path.Combine(FileSystem.AppDataDirectory,"cleanvander-local.db3")); await _db.CreateTableAsync<CarrinhoItem>(); return _db; }
    public async Task<List<CarrinhoItem>> CarrinhoAsync() => await (await DbAsync()).Table<CarrinhoItem>().ToListAsync();
    public async Task AdicionarAsync(Produto p) { var db=await DbAsync(); var item=await db.FindAsync<CarrinhoItem>(p.Id); if(item is null) await db.InsertAsync(new CarrinhoItem { ProdutoId=p.Id,Codigo=p.Codigo,Descricao=p.Descricao,Categoria=p.Categoria,UnidadeMedida=p.UnidadeMedida,TamanhoVolume=p.TamanhoVolume,Preco=p.Preco,Quantidade=1 }); else { item.Quantidade++; item.Preco=p.Preco; await db.UpdateAsync(item); } }
    public async Task QuantidadeAsync(CarrinhoItem i,int quantidade) { var db=await DbAsync(); if(quantidade<=0) await db.DeleteAsync<CarrinhoItem>(i.ProdutoId); else { i.Quantidade=quantidade; await db.UpdateAsync(i); } }
    public async Task LimparAsync() => await (await DbAsync()).DeleteAllAsync<CarrinhoItem>();
}

