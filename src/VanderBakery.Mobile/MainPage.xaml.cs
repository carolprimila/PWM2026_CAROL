using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;

namespace VanderBakery.Mobile;

public partial class MainPage : ContentPage
{
    private readonly HttpClient _api = new() { BaseAddress = new Uri(DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5293/" : "http://localhost:5293/") };
    private readonly ObservableCollection<CustomerModel> _customers = [];
    private readonly List<ProductModel> _products = [];
    private readonly List<PaymentConditionModel> _paymentConditions = [];
    private readonly Dictionary<int, Entry> _quantities = [];
    private Dictionary<string, string[]> _locations = [];
    private static readonly string[] Days = ["Segunda-feira", "Terça-feira", "Quarta-feira", "Quinta-feira", "Sexta-feira", "Sábado", "Domingo"];
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public MainPage()
    {
        InitializeComponent();
        DayPicker.ItemsSource = Days;
        DayPicker.SelectedItem = Days[((int)DateTime.Today.DayOfWeek + 6) % 7];
        CityPicker.ItemsSource = new[] { "Foz do Iguaçu", "Santa Terezinha de Itaipu" };
        CityPicker.SelectedIndex = 0;
        CustomerPicker.ItemsSource = _customers;
        HistoryCustomerPicker.ItemsSource = _customers;
        CustomerPicker.ItemDisplayBinding = new Binding(nameof(CustomerModel.Name));
        HistoryCustomerPicker.ItemDisplayBinding = new Binding(nameof(CustomerModel.Name));
        PaymentConditionPicker.ItemDisplayBinding = new Binding(nameof(PaymentConditionModel.Name));
        AuthPanel.IsVisible = true;
    }

    private bool _registering;
    private void AuthToggleClicked(object? sender, EventArgs e)
    {
        _registering = !_registering;
        AuthNameEntry.IsVisible = _registering;
        AuthTitle.Text = _registering ? "Cadastro de vendedor" : "Entrar como vendedor";
        AuthSubmitButton.Text = _registering ? "Criar cadastro" : "Entrar";
        AuthToggleButton.Text = _registering ? "Já tenho cadastro · entrar" : "Criar cadastro de vendedor";
        AuthMessageLabel.Text = "";
    }

    private async void AuthSubmitClicked(object? sender, EventArgs e)
    {
        try
        {
            var endpoint = _registering ? "api/auth/register/seller" : "api/auth/login";
            var payload = _registering ? (object)new { name = AuthNameEntry.Text, email = AuthEmailEntry.Text, password = AuthPasswordEntry.Text } : new { email = AuthEmailEntry.Text, password = AuthPasswordEntry.Text };
            var response = await _api.PostAsJsonAsync(endpoint, payload, Json);
            if (!response.IsSuccessStatusCode) { AuthMessageLabel.Text = "Confira seus dados. A senha precisa ter pelo menos 8 caracteres."; return; }
            var session = await response.Content.ReadFromJsonAsync<AuthResult>(Json);
            if (session is null || session.Role != "Seller") { AuthMessageLabel.Text = "Este acesso é exclusivo do vendedor."; return; }
            _api.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.Token);
            var period = DateTime.Now.Hour < 12 ? "Bom dia" : DateTime.Now.Hour < 18 ? "Boa tarde" : "Boa noite";
            GreetingLabel.Text = $"{period}, {session.Name.Split(' ')[0]}";
            AuthPanel.IsVisible = false; AppPanel.IsVisible = true;
            await RefreshDataAsync();
        }
        catch { AuthMessageLabel.Text = "Não foi possível conectar ao sistema da distribuidora."; }
    }

    private void LogoutClicked(object? sender, EventArgs e)
    {
        _api.DefaultRequestHeaders.Authorization = null;
        AppPanel.IsVisible = false; AuthPanel.IsVisible = true;
    }

    private async Task RefreshDataAsync()
    {
        try
        {
            _locations = await _api.GetFromJsonAsync<Dictionary<string, string[]>>("api/locations", Json) ?? [];
            CityChanged(this, EventArgs.Empty);
            var customers = await _api.GetFromJsonAsync<List<CustomerModel>>("api/customers", Json) ?? [];
            _customers.Clear(); foreach (var customer in customers) _customers.Add(customer);
            _products.Clear(); _products.AddRange(await _api.GetFromJsonAsync<List<ProductModel>>("api/products", Json) ?? []);
            _paymentConditions.Clear(); _paymentConditions.AddRange(await _api.GetFromJsonAsync<List<PaymentConditionModel>>("api/payment-conditions", Json) ?? []);
            PaymentConditionPicker.ItemsSource = _paymentConditions;
            PaymentMethodPicker.ItemsSource = await _api.GetFromJsonAsync<List<string>>("api/payment-methods", Json) ?? [];
            if (PaymentMethodPicker.SelectedItem is null) PaymentMethodPicker.SelectedItem = "Boleto bancário";
            if (PaymentConditionPicker.SelectedItem is null) PaymentConditionPicker.SelectedItem = _paymentConditions.FirstOrDefault(x => x.Code == 10);
            UpdatePaymentTerms();
            RenderProducts(); await LoadRouteAsync();
            StatusLabel.Text = "Dados atualizados · VanderBakery, sede Foz do Iguaçu.";
        }
        catch { StatusLabel.Text = "Não foi possível conectar à API. Confira se o sistema da sede está ativo."; }
    }

    private async void RefreshClicked(object? sender, EventArgs e) => await RefreshDataAsync();
    private async void DayChanged(object? sender, EventArgs e) => await LoadRouteAsync();
    private void PaymentConditionChanged(object? sender, EventArgs e) => UpdatePaymentTerms();

    private void UpdatePaymentTerms()
    {
        if (PaymentConditionPicker.SelectedItem is PaymentConditionModel condition)
            PaymentTermsLabel.Text = $"Multa {condition.FinePercent:0.##}% · Juros {condition.DailyInterestPercent:0.##}% ao dia · Desconto {condition.DailyDiscountPercent:0.##}% ao dia";
    }

    private async Task LoadRouteAsync()
    {
        if (DayPicker.SelectedItem is not string day) return;
        try
        {
            var stops = await _api.GetFromJsonAsync<List<RouteModel>>($"api/routes?day={Uri.EscapeDataString(day)}", Json) ?? [];
            RouteList.Children.Clear();
            if (stops.Count == 0) { RouteList.Children.Add(new Label { Text = "Sem visitas programadas para este dia.", TextColor = Color.FromArgb("#A18A93"), FontSize = 12 }); return; }
            foreach (var stop in stops)
                RouteList.Children.Add(BuildRouteCard(stop));
        }
        catch { StatusLabel.Text = "Falha ao carregar sua rota."; }
    }

    private static View BuildRouteCard(RouteModel stop)
    {
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 11 };
        grid.Children.Add(new Label { Text = stop.Position.ToString("00"), TextColor = Color.FromArgb("#C16988"), BackgroundColor = Color.FromArgb("#FFF0F5"), FontAttributes = FontAttributes.Bold, Padding = new Thickness(10, 8), VerticalOptions = LayoutOptions.Center });
        var details = new VerticalStackLayout { Spacing = 2, Children = { new Label { Text = stop.Name, TextColor = Color.FromArgb("#503742"), FontAttributes = FontAttributes.Bold, FontSize = 13 }, new Label { Text = $"{stop.City} · {stop.Neighborhood}", TextColor = Color.FromArgb("#A18A93"), FontSize = 10 } } };
        Grid.SetColumn(details, 1); grid.Children.Add(details);
        return new Border { Stroke = Color.FromArgb("#F2E7EB"), StrokeThickness = 1, StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 11 }, Padding = 10, Content = grid };
    }

    private void RenderProducts()
    {
        ProductList.Children.Clear(); _quantities.Clear();
        foreach (var product in _products)
        {
            var quantity = new Entry { Placeholder = "Qtd.", Keyboard = Keyboard.Numeric, WidthRequest = 75, HorizontalTextAlignment = TextAlignment.Center, BackgroundColor = Color.FromArgb("#FFF8FA"), TextColor = Color.FromArgb("#503742") };
            _quantities[product.Id] = quantity;
            var row = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 8 };
            row.Children.Add(new VerticalStackLayout { Spacing = 2, Children = { new Label { Text = product.Name, TextColor = Color.FromArgb("#503742"), FontAttributes = FontAttributes.Bold, FontSize = 12 }, new Label { Text = $"{product.Price:C} · Estoque: {product.Stock:0.##} {product.Unit} · Val.: {product.ExpiryDate?.ToString("dd/MM/yy") ?? "—"}", TextColor = product.Stock > 0 ? Color.FromArgb("#9B7E88") : Color.FromArgb("#D36B75"), FontSize = 10 } } });
            Grid.SetColumn(quantity, 1); row.Children.Add(quantity);
            ProductList.Children.Add(new Border { Stroke = Color.FromArgb("#F2E7EB"), StrokeThickness = 1, StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 }, Padding = 9, Content = row });
        }
    }

    private async void SellClicked(object? sender, EventArgs e)
    {
        if (CustomerPicker.SelectedItem is not CustomerModel customer) { StatusLabel.Text = "Selecione o cliente da venda."; return; }
        var lines = new List<object>();
        foreach (var product in _products)
        {
            var text = _quantities[product.Id].Text;
            if (decimal.TryParse(text, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.CurrentCulture, out var qty) || decimal.TryParse(text, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out qty))
                if (qty > 0) lines.Add(new { productId = product.Id, quantity = qty });
        }
        if (lines.Count == 0) { StatusLabel.Text = "Informe a quantidade de pelo menos um produto."; return; }
        try
        {
            if (PaymentConditionPicker.SelectedItem is not PaymentConditionModel condition) { StatusLabel.Text = "Selecione o parcelamento."; return; }
            if (PaymentMethodPicker.SelectedItem is not string paymentMethod) { StatusLabel.Text = "Selecione a forma de pagamento."; return; }
            var response = await _api.PostAsJsonAsync("api/sales", new { customerId = customer.Id, items = lines, paymentConditionId = condition.Id, paymentMethod }, Json);
            if (!response.IsSuccessStatusCode) { var error = await response.Content.ReadFromJsonAsync<ApiError>(Json); StatusLabel.Text = error?.Message ?? "Venda não concluída."; return; }
            var sale = await response.Content.ReadFromJsonAsync<SaleResult>(Json); StatusLabel.Text = $"Venda #{sale?.Id:0000} registrada · total {sale?.Total:C} · lucro {sale?.Profit:C}.";
            foreach (var entry in _quantities.Values) entry.Text = "";
            await RefreshDataAsync();
        }
        catch { StatusLabel.Text = "Falha de conexão. Venda não registrada."; }
    }

    private void ToggleCustomerForm(object? sender, EventArgs e) => CustomerForm.IsVisible = !CustomerForm.IsVisible;
    private void CityChanged(object? sender, EventArgs e)
    {
        var city = CityPicker.SelectedItem as string ?? "Foz do Iguaçu";
        NeighborhoodPicker.ItemsSource = _locations.GetValueOrDefault(city, []);
        NeighborhoodPicker.SelectedItem = null;
    }

    private async void SaveCustomerClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameEntry.Text) || string.IsNullOrWhiteSpace(TaxIdEntry.Text) || string.IsNullOrWhiteSpace(EmailEntry.Text) || NeighborhoodPicker.SelectedItem is not string neighborhood)
        { StatusLabel.Text = "Preencha nome, CPF/CNPJ, e-mail, cidade e bairro."; return; }
        try
        {
            var response = await _api.PostAsJsonAsync("api/customers", new { name = NameEntry.Text, tradeName = TradeEntry.Text, taxId = TaxIdEntry.Text, email = EmailEntry.Text, city = CityPicker.SelectedItem, neighborhood }, Json);
            if (!response.IsSuccessStatusCode) { StatusLabel.Text = (await response.Content.ReadFromJsonAsync<ApiError>(Json))?.Message ?? "Não foi possível cadastrar."; return; }
            StatusLabel.Text = "Cliente cadastrado com sucesso!"; NameEntry.Text = TradeEntry.Text = TaxIdEntry.Text = EmailEntry.Text = ""; NeighborhoodPicker.SelectedItem = null; CustomerForm.IsVisible = false; await RefreshDataAsync();
        }
        catch { StatusLabel.Text = "Não foi possível conectar à API."; }
    }

    private async void HistoryCustomerChanged(object? sender, EventArgs e)
    {
        if (HistoryCustomerPicker.SelectedItem is not CustomerModel customer) return;
        try
        {
            var info = await _api.GetFromJsonAsync<CustomerHistory>($"api/customers/{customer.Id}", Json); HistoryList.Children.Clear();
            if (info is not null) HistoryList.Children.Add(new Label { Text = $"Saldo em aberto: {info.OpenBalance:C}", TextColor = Color.FromArgb("#C16988"), FontAttributes = FontAttributes.Bold, FontSize = 12 });
            if (info?.Purchases.Count == 0) { HistoryList.Children.Add(new Label { Text = "Este cliente ainda não tem compras.", TextColor = Color.FromArgb("#A18A93"), FontSize = 11 }); return; }
            foreach (var purchase in info!.Purchases)
            {
                HistoryList.Children.Add(new Border { BackgroundColor = Color.FromArgb("#FFF8FA"), StrokeThickness = 0, StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 }, Padding = 10, Content = new VerticalStackLayout { Spacing = 3, Children = { new Label { Text = $"Compra #{purchase.Id:0000} · {purchase.CreatedAt.ToLocalTime():dd/MM/yyyy HH:mm}", TextColor = Color.FromArgb("#503742"), FontAttributes = FontAttributes.Bold, FontSize = 12 }, new Label { Text = $"{purchase.PaymentStatus} · {purchase.PaymentMethod}", TextColor = purchase.PaymentStatus == "Quitado" ? Color.FromArgb("#54876B") : Color.FromArgb("#C16988"), FontAttributes = FontAttributes.Bold, FontSize = 11 }, new Label { Text = string.Join(" · ", purchase.Items.Select(i => $"{i.Quantity:0.##} {i.ProductName} ({i.UnitPrice:C})")), TextColor = Color.FromArgb("#927B84"), FontSize = 10 }, new Label { Text = $"Total {purchase.Total:C}", TextColor = Color.FromArgb("#C16988"), FontAttributes = FontAttributes.Bold, FontSize = 12 }, new Label { Text = string.Join(" · ", purchase.Receivables.Select(r => $"Parcela {r.InstallmentNumber}: {r.Amount:C}{(!r.Paid && r.CurrentAmount != r.Amount ? $" (hoje {r.CurrentAmount:C})" : "")} em {r.DueDate:dd/MM} ({r.PaymentMethod}) · {(r.Paid ? "quitada" : "em aberto")}")), TextColor = Color.FromArgb("#927B84"), FontSize = 10 }, new Label { Text = "Multa 5% · juros 2% a.d. · desconto 1% a.d.", TextColor = Color.FromArgb("#A18A93"), FontSize = 9 } } } });
            }
        }
        catch { StatusLabel.Text = "Não foi possível carregar o histórico."; }
    }

    private sealed record CustomerModel(int Id, string Name, string? TradeName, string TaxId, string Email, string City, string Neighborhood);
    private sealed record AuthResult(string Token, string Name, string Role);
    private sealed record RouteModel(int Id, string Day, int Position, int CustomerId, string Name, string? TradeName, string City, string Neighborhood);
    private sealed record ProductModel(int Id, string Name, string Group, string Unit, decimal Price, decimal Cost, decimal Stock, DateOnly? ExpiryDate);
    private sealed record PaymentConditionModel(int Id, int Code, string Name, decimal FinePercent, decimal DailyInterestPercent, decimal DailyDiscountPercent);
    private sealed record ApiError(string? Message);
    private sealed record SaleResult(int Id, DateTime CreatedAt, decimal Total, decimal Profit);
    private sealed record CustomerHistory(decimal OpenBalance, List<Purchase> Purchases);
    private sealed record Purchase(int Id, DateTime CreatedAt, decimal Total, string PaymentMethod, string PaymentStatus, List<PurchaseItem> Items, List<Installment> Receivables);
    private sealed record PurchaseItem(string ProductName, decimal Quantity, decimal UnitPrice);
    private sealed record Installment(int InstallmentNumber, DateOnly DueDate, decimal Amount, decimal CurrentAmount, string PaymentMethod, bool Paid);
}
