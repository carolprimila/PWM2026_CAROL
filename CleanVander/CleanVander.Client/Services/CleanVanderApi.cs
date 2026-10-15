using System.Net.Http.Json;
using CleanVander.Client.Models;
namespace CleanVander.Client.Services;
public sealed class CleanVanderApi(HttpClient http)
{
    private readonly HttpClient _http=http;
    private async Task<HttpRequestMessage> Request(HttpMethod method,string url) { var req=new HttpRequestMessage(method,url); var key=await SecureStorage.Default.GetAsync("client-key"); if(!string.IsNullOrWhiteSpace(key)) req.Headers.Add("X-Client-Key",key); return req; }
    public async Task<List<Produto>> ProdutosAsync() { using var req=await Request(HttpMethod.Get,"/api/products"); using var res=await _http.SendAsync(req); res.EnsureSuccessStatusCode(); return await res.Content.ReadFromJsonAsync<List<Produto>>() ?? []; }
    public async Task<CadastroClienteResultado> CadastrarAsync(CadastroClienteDto dto) { using var res=await _http.PostAsJsonAsync("/api/clients",dto); res.EnsureSuccessStatusCode(); var item=await res.Content.ReadFromJsonAsync<CadastroClienteResultado>() ?? throw new InvalidOperationException("Resposta de cadastro vazia."); await SecureStorage.Default.SetAsync("client-key",item.ChaveAcesso); Preferences.Default.Set("client-name",item.Nome); return item; }
    public async Task<Pedido> CriarPedidoAsync(List<CarrinhoItem> itens) { using var req=await Request(HttpMethod.Post,"/api/orders"); req.Content=JsonContent.Create(new { Itens=itens.Select(i=>new ItemPedidoDto { ProdutoId=i.ProdutoId,Quantidade=i.Quantidade }).ToList() }); using var res=await _http.SendAsync(req); res.EnsureSuccessStatusCode(); return await res.Content.ReadFromJsonAsync<Pedido>() ?? throw new InvalidOperationException("Resposta vazia."); }
    public async Task<List<Pedido>> PedidosAsync() { using var req=await Request(HttpMethod.Get,"/api/orders"); using var res=await _http.SendAsync(req); res.EnsureSuccessStatusCode(); return await res.Content.ReadFromJsonAsync<List<Pedido>>() ?? []; }
}

