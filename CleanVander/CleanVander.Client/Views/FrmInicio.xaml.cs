using CleanVander.Client.Models; using CleanVander.Client.Services; using Microsoft.Extensions.DependencyInjection;
namespace CleanVander.Client.Views;
public partial class FrmInicio : ContentPage
{
 private readonly CleanVanderApi _api; private readonly LocalStore _store; private List<Produto> _produtos=[]; private bool _checked;
 public FrmInicio() { InitializeComponent(); var services=App.Current!.Handler!.MauiContext!.Services; _api=services.GetRequiredService<CleanVanderApi>(); _store=services.GetRequiredService<LocalStore>(); }
 protected override async void OnAppearing() { base.OnAppearing(); if(!_checked && string.IsNullOrWhiteSpace(await SecureStorage.Default.GetAsync("client-key"))) { _checked=true; await Navigation.PushAsync(new FrmSignUp()); return; } await Carregar(); }
 private async Task Carregar() { try { _produtos=await _api.ProdutosAsync(); Filtrar(Busca.Text??""); } catch { await DisplayAlertAsync("Sem conexão","Não foi possível consultar a API. Inicie o CleanVander.Api e tente novamente.","OK"); } }
 private void Filtrar(string termo) => ListaProdutos.ItemsSource=_produtos.Where(p=>$"{p.Descricao} {p.Categoria} {p.Codigo}".Contains(termo,StringComparison.OrdinalIgnoreCase)).ToList();
 private void BuscarProdutos(object? sender,TextChangedEventArgs e) => Filtrar(e.NewTextValue??"");
 private async void AdicionarProduto(object? sender,EventArgs e) { if((sender as Button)?.CommandParameter is Produto p) { await _store.AdicionarAsync(p); await DisplayAlertAsync("Adicionado",$"{p.Descricao} entrou no carrinho.","Continuar"); } }
 private async void AbrirCarrinho(object? sender,EventArgs e) => await Navigation.PushAsync(new FrmCarrinho()); private async void AbrirPedidos(object? sender,EventArgs e) => await Navigation.PushAsync(new FrmPedidos());
}

