using CleanVander.Client.Models; using CleanVander.Client.Services; using Microsoft.Extensions.DependencyInjection;
namespace CleanVander.Client.Views;
public partial class FrmPedidos : ContentPage
{
 private readonly CleanVanderApi _api; private readonly LocalStore _store;
 public FrmPedidos() { InitializeComponent(); var services=App.Current!.Handler!.MauiContext!.Services; _api=services.GetRequiredService<CleanVanderApi>(); _store=services.GetRequiredService<LocalStore>(); }
 protected override async void OnAppearing() { base.OnAppearing(); try { var pedidos=await _api.PedidosAsync(); foreach(var p in pedidos) p.ResumoItens=string.Join(" · ",p.Itens.Select(i=>$"{i.Quantidade}x {i.DescricaoProduto}")); ListaPedidos.ItemsSource=pedidos; } catch(Exception ex) { await DisplayAlertAsync("Não foi possível carregar",ex.Message,"OK"); } }
 private async void RepetirPedido(object? sender,EventArgs e) { if((sender as Button)?.CommandParameter is not Pedido p) return; foreach(var i in p.Itens) await _store.AdicionarAsync(new Produto { Id=i.ProdutoId,Codigo=i.CodigoProduto,Descricao=i.DescricaoProduto,UnidadeMedida=i.UnidadeMedida,TamanhoVolume=i.TamanhoVolume,Preco=i.PrecoUnitario,Ativo=true }); await DisplayAlertAsync("Pedido copiado","Itens copiados para o carrinho. Revise quantidades e preços antes de enviar.","Revisar"); await Navigation.PushAsync(new FrmCarrinho()); }
}

