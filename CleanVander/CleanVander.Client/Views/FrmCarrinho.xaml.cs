using CleanVander.Client.Models; using CleanVander.Client.Services; using Microsoft.Extensions.DependencyInjection;
namespace CleanVander.Client.Views;
public partial class FrmCarrinho : ContentPage
{
 private readonly LocalStore _store; private readonly CleanVanderApi _api; private List<CarrinhoItem> _itens=[];
 public FrmCarrinho() { InitializeComponent(); var services=App.Current!.Handler!.MauiContext!.Services; _store=services.GetRequiredService<LocalStore>(); _api=services.GetRequiredService<CleanVanderApi>(); }
 protected override async void OnAppearing() { base.OnAppearing(); await Carregar(); }
 private async Task Carregar() { _itens=await _store.CarrinhoAsync(); ListaCarrinho.ItemsSource=null; ListaCarrinho.ItemsSource=_itens; Total.Text=_itens.Sum(i=>i.Subtotal).ToString("C2",System.Globalization.CultureInfo.GetCultureInfo("pt-BR")); }
 private async Task Change(object? value,int delta) { if(value is CarrinhoItem i) { await _store.QuantidadeAsync(i,i.Quantidade+delta); await Carregar(); } }
 private async void Aumentar(object? sender,EventArgs e) => await Change((sender as Button)?.CommandParameter,1); private async void Diminuir(object? sender,EventArgs e) => await Change((sender as Button)?.CommandParameter,-1);
 private async void Remover(object? sender,EventArgs e) { if((sender as Button)?.CommandParameter is CarrinhoItem i) { await _store.QuantidadeAsync(i,0); await Carregar(); } } private async void Voltar(object? sender,EventArgs e) => await Navigation.PopAsync();
 private async void EnviarPedido(object? sender,EventArgs e) { if(_itens.Count==0) { await DisplayAlertAsync("Carrinho vazio","Adicione produtos antes de enviar.","OK"); return; } if(!await DisplayAlertAsync("Confirmar pedido",$"Enviar {_itens.Sum(i=>i.Quantidade)} item(ns), total {Total.Text}? Pagamento à vista na entrega.","Enviar","Revisar")) return; try { var p=await _api.CriarPedidoAsync(_itens); await _store.LimparAsync(); await DisplayAlertAsync("Pedido recebido",$"Pedido {p.Numero} enviado com sucesso.","Ver pedidos"); await Navigation.PushAsync(new FrmPedidos()); } catch(Exception ex) { await DisplayAlertAsync("Falha ao enviar",ex.Message,"OK"); } }
}

