using System.Security.Cryptography;
using System.Net.Http.Json;
using CleanVander.Admin.Models;
using Microsoft.AspNetCore.Mvc;

namespace CleanVander.Admin.Controllers;
public sealed class HomeController(IHttpClientFactory factory, IConfiguration config) : Controller
{
    private const string SessionKey = "CleanVanderAdminKey";
    private HttpClient Api(string? key = null) { var client=factory.CreateClient("CleanVanderApi"); if (!string.IsNullOrEmpty(key)) client.DefaultRequestHeaders.Add("X-Admin-Key", key); return client; }
    private string? Key => HttpContext.Session.GetString(SessionKey);

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (Key is null) return View("Login", new LoginForm());
        var api=Api(Key); var vm=new DashboardViewModel { Autenticado=true };
        try { vm.Produtos=await api.GetFromJsonAsync<List<Product>>("/api/admin/products") ?? []; vm.Clientes=await api.GetFromJsonAsync<List<Client>>("/api/admin/clients") ?? []; vm.Pedidos=await api.GetFromJsonAsync<List<Order>>("/api/admin/orders") ?? []; }
        catch (HttpRequestException) { ViewData["Erro"]="Não foi possível conectar à API. Confira se o serviço está iniciado."; }
        return View("Dashboard", vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Login(LoginForm form)
    {
        var configured=config["Admin:ApiKey"] ?? "";
        if (configured.Length == 0 || !CryptographicOperations.FixedTimeEquals(System.Text.Encoding.UTF8.GetBytes(configured), System.Text.Encoding.UTF8.GetBytes(form.Chave ?? ""))) { ModelState.AddModelError("", "Chave administrativa incorreta."); return View("Login", form); }
        HttpContext.Session.SetString(SessionKey, form.Chave ?? string.Empty); return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken] public IActionResult Logout() { HttpContext.Session.Clear(); return RedirectToAction(nameof(Index)); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SalvarProduto(ProductForm form)
    {
        if (Key is null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(form.Codigo) || string.IsNullOrWhiteSpace(form.Descricao) || form.Preco < 0) { TempData["Erro"]="Código, descrição e preço válido são obrigatórios."; return RedirectToAction(nameof(Index)); }
        var data=new { form.Codigo, form.Descricao, form.Categoria, form.UnidadeMedida, form.TamanhoVolume, form.Preco, form.Ativo };
        var api=Api(Key); var response=form.Id == 0 ? await api.PostAsJsonAsync("/api/admin/products", data) : await api.PutAsJsonAsync($"/api/admin/products/{form.Id}", data);
        TempData[response.IsSuccessStatusCode ? "Sucesso" : "Erro"] = response.IsSuccessStatusCode ? "Produto salvo." : await response.Content.ReadAsStringAsync();
        return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AtualizarStatus(StatusForm form)
    {
        if (Key is null) return Unauthorized();
        var response=await Api(Key).PutAsJsonAsync($"/api/admin/orders/{form.PedidoId}/status", new { form.Status });
        TempData[response.IsSuccessStatusCode ? "Sucesso" : "Erro"] = response.IsSuccessStatusCode ? "Status atualizado." : await response.Content.ReadAsStringAsync();
        return RedirectToAction(nameof(Index));
    }
}

