using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Globalization;
using Microsoft.Maui.Controls.Shapes;

namespace VanderBakery.Mobile;

public partial class MainPage : ContentPage
{
    private readonly HttpClient _api = new() { BaseAddress = new Uri(DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5293/" : "http://localhost:5293/") };
    private readonly ObservableCollection<CustomerModel> _customers = [];
    private readonly List<(ProductModel Product, Entry Quantity)> _productRows = [];
    private string _pendingOrderKey = Guid.NewGuid().ToString("N");
    private string? _pendingOrderFingerprint;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public MainPage() { InitializeComponent(); CustomerPicker.ItemsSource = _customers; }

    private async void LoginClicked(object? sender, EventArgs e)
    {
        try
        {
            var response = await _api.PostAsJsonAsync("api/auth/login", new { email = EmailEntry.Text, password = PasswordEntry.Text });
            if (!response.IsSuccessStatusCode) { StatusLabel.Text = "E-mail ou senha inválidos."; return; }
            var session = await response.Content.ReadFromJsonAsync<LoginModel>(Json);
            _api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.Token);
            LoginPanel.IsVisible = false; SalesPanel.IsVisible = true;
            OrderFooter.IsVisible = true;
            SellerBadge.IsVisible = true;
            WelcomeLabel.Text = $"Olá, {session.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0]}";
            SellerInitials.Text = string.Concat(session.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(x => x[0])).ToUpperInvariant();
            await RefreshDataAsync();
        }
        catch { StatusLabel.Text = "Não foi possível conectar ao sistema da distribuidora."; }
    }

    private async void RefreshClicked(object? sender, EventArgs e) => await RefreshDataAsync();

    private async void LoadHistoryClicked(object? sender, EventArgs e)
    {
        if (CustomerPicker.SelectedItem is not CustomerModel customer) return;
        try
        {
            HistoryList.Children.Clear();
            var orders = await _api.GetFromJsonAsync<List<OrderHistoryModel>>($"api/orders?customerId={customer.Id}", Json) ?? [];
            if (orders.Count == 0) { HistoryList.Children.Add(new Label { Text = "Este cliente ainda não tem pedidos.", TextColor = Color.FromArgb("#718077") }); return; }
            foreach (var order in orders)
            {
                var content = new VerticalStackLayout { Spacing = 7 };
                content.Children.Add(new Label { Text = $"Pedido #{order.Id:0000} · {order.CreatedAt.ToLocalTime():dd/MM/yyyy}", FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#344C3D") });
                content.Children.Add(new Label { Text = $"{order.Total:C}   ·   {order.Status}", FontSize = 12, TextColor = Color.FromArgb("#718077") });
                content.Children.Add(new Label { Text = string.Join(" · ", order.Items.Select(x => $"{x.Product}: {x.RequestedQuantity:0.##} un.")), FontSize = 12, TextColor = Color.FromArgb("#718077") });
                var repeat = new Microsoft.Maui.Controls.Button { Text = "Repetir como novo pedido", BackgroundColor = Color.FromArgb("#EAF2E8"), TextColor = Color.FromArgb("#276444"), HeightRequest = 44, CornerRadius = 10, FontAttributes = FontAttributes.Bold, FontSize = 12 };
                repeat.Clicked += async (_, _) => await RepeatOrderAsync(order.Id);
                content.Children.Add(repeat);
                HistoryList.Children.Add(new Border { Content = content, Padding = 12, BackgroundColor = Colors.White, Stroke = Color.FromArgb("#E4E8DF"), StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 13 } });
            }
        }
        catch { StatusLabel.Text = "Não foi possível conectar ao sistema da distribuidora."; }
    }

    private async Task RepeatOrderAsync(int orderId)
    {
        try
        {
            var draft = await (await _api.PostAsync($"api/orders/{orderId}/repeat", null)).Content.ReadFromJsonAsync<DraftModel>(Json);
            if (draft is null) return;
            var condition = ConditionPicker.Items.FirstOrDefault(x => x.StartsWith($"{draft.PaymentConditionId} ·", StringComparison.Ordinal));
            if (condition is not null) ConditionPicker.SelectedItem = condition;
            foreach (var row in _productRows) row.Quantity.Text = draft.Items.FirstOrDefault(x => x.ProductId == row.Product.Id)?.Quantity.ToString() ?? "";
            _pendingOrderKey = Guid.NewGuid().ToString("N");
            _pendingOrderFingerprint = null;
            StatusLabel.Text = "Pedido anterior carregado como novo rascunho. Revise antes de enviar.";
        }
        catch { StatusLabel.Text = "Não foi possível conectar ao sistema da distribuidora."; }
    }

    private void ToggleCustomerForm(object? sender, EventArgs e) => CustomerForm.IsVisible = !CustomerForm.IsVisible;

    private async void CustomerSelected(object? sender, EventArgs e)
    {
        if (CustomerPicker.SelectedItem is not CustomerModel customer) return;
        try
        {
            var summary = await _api.GetFromJsonAsync<CustomerSummaryModel>($"api/customers/{customer.Id}/summary", Json);
            if (summary is null) return;
            var last = summary.LastPurchase is null ? "Sem compras anteriores" : $"Última compra: {summary.LastPurchase.CreatedAt.ToLocalTime():dd/MM/yyyy} · {summary.LastPurchase.Total:C}";
            var items = summary.LastPurchase?.Items.Count > 0 ? string.Join(", ", summary.LastPurchase.Items.Select(x => $"{x.ProductName}: {x.Quantity:0.##} × {x.UnitPrice:C}")) : "";
            CustomerSummary.Text = $"{customer.Name} · {customer.City}/{customer.State}\n{last}{(items.Length > 0 ? $"\n{items}" : "")}\nEm aberto: {summary.OpenBalance:C}";
            CustomerSummary.IsVisible = true;
        }
        catch { StatusLabel.Text = "Não foi possível conectar ao sistema da distribuidora."; }
    }

    private async void SaveCustomerClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NewName.Text) || string.IsNullOrWhiteSpace(NewTaxId.Text) || string.IsNullOrWhiteSpace(NewCity.Text) || string.IsNullOrWhiteSpace(NewState.Text)) { StatusLabel.Text = "Preencha todos os campos obrigatórios."; return; }
        if (!int.TryParse(NewVisitOrder.Text, out var order)) { StatusLabel.Text = "Informe a ordem de visita."; return; }
        try
        {
            var type = new string(NewTaxId.Text.Where(char.IsDigit).ToArray()).Length == 11 ? "F" : "J";
            var payload = new { personType = type, name = NewName.Text, legalName = NewLegalName.Text, taxId = NewTaxId.Text, phone = NewPhone.Text, email = NewEmail.Text, address = NewAddress.Text, city = NewCity.Text, state = NewState.Text?.ToUpperInvariant(), visitOrder = order };
            var response = await _api.PostAsJsonAsync("api/customers", payload);
            if (!response.IsSuccessStatusCode) { var error = await response.Content.ReadFromJsonAsync<ErrorModel>(Json); StatusLabel.Text = error?.Message ?? "Não foi possível cadastrar o cliente."; return; }
            StatusLabel.Text = "Cliente cadastrado com sucesso."; CustomerForm.IsVisible = false;
            NewName.Text = NewLegalName.Text = NewTaxId.Text = NewPhone.Text = NewEmail.Text = NewAddress.Text = NewCity.Text = NewState.Text = NewVisitOrder.Text = "";
            await RefreshDataAsync();
        }
        catch { StatusLabel.Text = "Não foi possível conectar ao sistema da distribuidora."; }
    }

    private async Task RefreshDataAsync()
    {
        try
        {
            var customers = await _api.GetFromJsonAsync<List<CustomerModel>>("api/customers", Json) ?? [];
            _customers.Clear(); foreach (var c in customers.OrderBy(x => x.VisitOrder)) _customers.Add(c);
            var products = await _api.GetFromJsonAsync<List<ProductModel>>("api/products", Json) ?? [];
            ProductsList.Children.Clear(); _productRows.Clear();
            foreach (var product in products)
            {
                var row = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(76) }, ColumnSpacing = 12 };
                var details = new VerticalStackLayout { Spacing = 4, VerticalOptions = LayoutOptions.Center };
                details.Children.Add(new Label { Text = product.Name, TextColor = Color.FromArgb("#294A37"), FontAttributes = FontAttributes.Bold, FontSize = 14, LineBreakMode = LineBreakMode.WordWrap });
                details.Children.Add(new Label { Text = $"{product.Group}  ·  {product.Package}", TextColor = Color.FromArgb("#879188"), FontSize = 11 });
                details.Children.Add(new Label { Text = $"{product.Price:C}  ·  {product.Available:0.##} disponíveis", TextColor = Color.FromArgb("#276444"), FontAttributes = FontAttributes.Bold, FontSize = 12 });
                if (product.Expiry is DateOnly expiry) details.Children.Add(new Label { Text = $"Validade mais próxima: {expiry:dd/MM/yyyy}", TextColor = Color.FromArgb("#A46C37"), FontSize = 11 });
                var qty = new Entry { Placeholder = "Qtd.", Keyboard = Keyboard.Numeric, BackgroundColor = Color.FromArgb("#F4F6F1"), TextColor = Color.FromArgb("#294A37"), PlaceholderColor = Color.FromArgb("#98A29A"), HorizontalTextAlignment = TextAlignment.Center, VerticalOptions = LayoutOptions.Center, HeightRequest = 48 };
                qty.TextChanged += (_, _) => UpdateDraftTotal();
                row.Add(details, 0, 0); row.Add(qty, 1, 0);
                ProductsList.Children.Add(new Border { Content = row, Padding = 13, BackgroundColor = Colors.White, Stroke = Color.FromArgb("#E4E8DF"), StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 13 } });
                _productRows.Add((product, qty));
            }
            var conditions = await _api.GetFromJsonAsync<List<ConditionModel>>("api/lookups/payment-conditions", Json) ?? [];
            ConditionPicker.ItemsSource = conditions.Select(x => $"{x.Id} · {x.Name}").ToList();
            if (ConditionPicker.Items.Count > 0) ConditionPicker.SelectedIndex = 0;
            UpdateDraftTotal();
            StatusLabel.Text = $"Roteiro carregado: {_customers.Count} clientes · {products.Count} produtos.";
        }
        catch { StatusLabel.Text = "Não foi possível conectar ao sistema da distribuidora."; }
    }

    private async void SendOrderClicked(object? sender, EventArgs e)
    {
        if (CustomerPicker.SelectedItem is not CustomerModel customer || ConditionPicker.SelectedItem is not string conditionText) { StatusLabel.Text = "Selecione um cliente e uma condição de pagamento."; return; }
        var items = _productRows.Where(x => decimal.TryParse(x.Quantity.Text, out var q) && q > 0).Select(x => new { productId = x.Product.Id, quantity = decimal.Parse(x.Quantity.Text!) }).ToList();
        if (items.Count == 0) { StatusLabel.Text = "Adicione ao menos um produto e uma quantidade."; return; }
        try
        {
            var conditionId = int.Parse(conditionText.Split('·')[0].Trim());
            var fingerprint = $"{customer.Id}|{conditionId}|{string.Join(';', items.OrderBy(x => x.productId).Select(x => $"{x.productId}:{x.quantity}"))}";
            if (_pendingOrderFingerprint != fingerprint) { _pendingOrderKey = Guid.NewGuid().ToString("N"); _pendingOrderFingerprint = fingerprint; }
            var response = await _api.PostAsJsonAsync("api/orders", new { customerId = customer.Id, paymentConditionId = conditionId, idempotencyKey = _pendingOrderKey, items });
            if (!response.IsSuccessStatusCode) { var body = await response.Content.ReadFromJsonAsync<ErrorModel>(Json); StatusLabel.Text = body?.Message ?? "Não foi possível enviar o pedido. Verifique sua conexão e tente novamente."; return; }
            foreach (var row in _productRows) row.Quantity.Text = "";
            UpdateDraftTotal();
            _pendingOrderKey = Guid.NewGuid().ToString("N");
            _pendingOrderFingerprint = null;
            StatusLabel.Text = "Pedido enviado com sucesso.";
        }
        catch { StatusLabel.Text = "Não foi possível enviar o pedido. Verifique sua conexão e tente novamente."; }
    }

    private void UpdateDraftTotal()
    {
        var total = _productRows.Sum(x => decimal.TryParse(x.Quantity.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var quantity) && quantity > 0 ? quantity * x.Product.Price : 0m);
        OrderTotalLabel.Text = total.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
    }

    private sealed class LoginModel { public string Token { get; set; } = ""; public string Name { get; set; } = "Vendedor"; }
    private sealed class CustomerModel { public int Id { get; set; } public string Name { get; set; } = ""; public string City { get; set; } = ""; public string State { get; set; } = ""; public int VisitOrder { get; set; } public string Display => $"{VisitOrder} · {Name} · {City}/{State}"; }
    private sealed class ProductModel { public int Id { get; set; } public string Name { get; set; } = ""; public string Group { get; set; } = ""; public string Package { get; set; } = ""; public decimal Price { get; set; } public decimal Available { get; set; } public DateOnly? Expiry { get; set; } }
    private sealed class ConditionModel { public int Id { get; set; } public string Name { get; set; } = ""; }
    private sealed class ErrorModel { public string? Message { get; set; } }
    private sealed class CustomerSummaryModel { public decimal OpenBalance { get; set; } public PurchaseModel? LastPurchase { get; set; } }
    private sealed class PurchaseModel { public DateTime CreatedAt { get; set; } public decimal Total { get; set; } public List<PurchaseItemModel> Items { get; set; } = []; }
    private sealed class PurchaseItemModel { public string ProductName { get; set; } = ""; public decimal Quantity { get; set; } public decimal UnitPrice { get; set; } }
    private sealed class OrderHistoryModel { public int Id { get; set; } public DateTime CreatedAt { get; set; } public decimal Total { get; set; } public string Status { get; set; } = ""; public List<OrderHistoryItemModel> Items { get; set; } = []; }
    private sealed class OrderHistoryItemModel { public string? Product { get; set; } public decimal RequestedQuantity { get; set; } }
    private sealed class DraftModel { public int CustomerId { get; set; } public int PaymentConditionId { get; set; } public List<DraftItemModel> Items { get; set; } = []; }
    private sealed class DraftItemModel { public int ProductId { get; set; } public decimal Quantity { get; set; } }
}
