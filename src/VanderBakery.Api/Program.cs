using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using VanderBakery.Api;

var builder = WebApplication.CreateBuilder(args);
var sqlite = new SqliteConnectionStringBuilder(builder.Configuration.GetConnectionString("Default") ?? "Data Source=vanderbakery-foz-v2.db");
if (!Path.IsPathRooted(sqlite.DataSource)) sqlite.DataSource = Path.Combine(builder.Environment.ContentRootPath, sqlite.DataSource);
builder.Services.AddDbContext<BakeryDb>(options => options.UseSqlite(sqlite.ConnectionString));
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));
builder.Services.AddOpenApi();
var app = builder.Build();
var days = new[] { "Segunda-feira", "Terça-feira", "Quarta-feira", "Quinta-feira", "Sexta-feira", "Sábado", "Domingo" };
var locations = new Dictionary<string, string[]>
{
    ["Foz do Iguaçu"] = ["Alvorada", "América", "Bourbon", "Campos do Iguaçu", "Carimã", "Cataratas", "Centro", "Centro Cívico", "Cidade Nova", "Cognópolis", "Itaipu A", "Itaipu B", "Itaipu C", "Itaipu Binacional", "Ipê", "KLP", "Lancaster", "Lote Grande", "Maracanã", "Mata Verde", "Monjolo", "Morumbi", "Náutica", "Panorama", "Parque Nacional", "Polo Centro", "Polo Universitário", "Portes", "Portal", "Porto Belo", "Porto Meira", "Remanso", "São Roque", "Três Bandeiras", "Três Fronteiras", "Três Lagoas", "Yolanda", "Área Rural", "Vila A", "Vila B", "Vila C", "Jardim América", "Jardim Central", "Jardim das Flores", "Jardim Europa", "Jardim Lancaster", "Jardim São Paulo", "Jardim Social", "Portal da Foz", "Vila Portes", "Vila Yolanda", "Parque Presidente", "Profilurb I", "Profilurb II", "Conjunto Libra", "Cohapar III", "Arroio Dourado", "Remanso Grande", "Alto da Boa Vista", "Loteamento Bourbon", "Parque Ouro Verde"],
    ["Santa Terezinha de Itaipu"] = ["Ascari", "Bairro Empresarial e Logístico I", "Bairro Empresarial e Logístico II", "Centro", "Ipê", "Parque dos Estados", "Parque São Lourenço", "Planalto", "Região dos Conjuntos", "Santa Mônica", "Tarumã", "Área Rural", "BNH", "Conjunto Panorama", "Santa Inês", "Residencial Brasília", "Jardim Ascari", "Jardim Canadá", "Jardim das Palmeiras", "Parque dos Imigrantes", "Vila Aparecidinha"]
};
app.UseCors();
if (app.Environment.IsDevelopment()) app.MapOpenApi();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BakeryDb>();
    db.Database.EnsureCreated();
    if (!db.Products.Any()) db.Products.AddRange(
        new Product { Name = "Fermento biológico seco", Group = "Fermentos", Unit = "pct 500 g", Price = 18.90m, Cost = 11.20m, Stock = 48, ExpiryDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(8)) },
        new Product { Name = "Mistura para bolo", Group = "Misturas", Unit = "pct 5 kg", Price = 42.50m, Cost = 28m, Stock = 22, ExpiryDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(6)) },
        new Product { Name = "Margarina para panificação", Group = "Gorduras", Unit = "balde 20 L", Price = 189m, Cost = 137m, Stock = 9, ExpiryDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(5)) },
        new Product { Name = "Pré-mistura para pão", Group = "Misturas", Unit = "saco 25 kg", Price = 129.90m, Cost = 94m, Stock = 16, ExpiryDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(7)) });
    if (!db.PaymentConditions.Any())
    {
        var weekly = new PaymentCondition { Code = 10, Name = "7/14 dias · Boleto", FinePercent = 5m, DailyInterestPercent = 2m, DailyDiscountPercent = 1m, Rules = [new PaymentRule { Days = 7, Percent = 50m, PaymentMethod = "Boleto bancário" }, new PaymentRule { Days = 14, Percent = 50m, PaymentMethod = "Boleto bancário" }] };
        var threeWeeks = new PaymentCondition { Code = 20, Name = "7/14/21 dias · PIX e boleto", FinePercent = 5m, DailyInterestPercent = 2m, DailyDiscountPercent = 1m, Rules = [new PaymentRule { Days = 7, Percent = 33.33m, PaymentMethod = "PIX" }, new PaymentRule { Days = 14, Percent = 33.33m, PaymentMethod = "Boleto bancário" }, new PaymentRule { Days = 21, Percent = 33.34m, PaymentMethod = "Boleto bancário" }] };
        db.PaymentConditions.AddRange(weekly, threeWeeks);
    }
    if (!db.Customers.Any())
    {
        var michel = new Customer { Name = "Michel Padaria", TradeName = "Pão do Michel", TaxId = "12345678909", Email = "michel@exemplo.com", City = "Foz do Iguaçu", Neighborhood = "Centro" };
        var marcos = new Customer { Name = "Marcos Alimentos", TradeName = "Padaria Marcos", TaxId = "11222333000181", Email = "marcos@exemplo.com", City = "Foz do Iguaçu", Neighborhood = "Vila A" };
        var maria = new Customer { Name = "Maria da Silva", TaxId = "11144477735", Email = "maria@exemplo.com", City = "Santa Terezinha de Itaipu", Neighborhood = "Centro" };
        var ana = new Customer { Name = "Ana Panificadora", TradeName = "Ponto do Pão", TaxId = "98765432000198", Email = "ana@exemplo.com", City = "Santa Terezinha de Itaipu", Neighborhood = "Parque dos Estados" };
        db.Customers.AddRange(michel, marcos, maria, ana);
        db.SaveChanges();
        db.RouteStops.AddRange(new RouteStop { CustomerId = michel.Id, Day = "Segunda-feira", Position = 1 }, new RouteStop { CustomerId = marcos.Id, Day = "Segunda-feira", Position = 2 }, new RouteStop { CustomerId = maria.Id, Day = "Quarta-feira", Position = 1 }, new RouteStop { CustomerId = ana.Id, Day = "Quarta-feira", Position = 3 });
    }
    db.SaveChanges();
}

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", sede = "Foz do Iguaçu" }));
app.MapGet("/api/locations", () => Results.Ok(locations));
app.MapGet("/api/customers", async (BakeryDb db) => Results.Ok(await db.Customers.Include(x => x.RouteStops).OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.TradeName, x.TaxId, x.Email, x.City, x.Neighborhood, x.Active, OpenBalance = db.Receivables.Where(r => !r.Paid && r.Sale!.CustomerId == x.Id).Sum(r => (decimal?)r.Amount) ?? 0m, Route = x.RouteStops.OrderBy(r => r.Day).ThenBy(r => r.Position).Select(r => new { r.Day, r.Position }) }).ToListAsync()));
app.MapGet("/api/customers/{id:int}", async (int id, BakeryDb db) =>
{
    var customer = await db.Customers.Include(x => x.RouteStops).FirstOrDefaultAsync(x => x.Id == id);
    if (customer is null) return Results.NotFound();
    var sales = await db.Sales.Where(x => x.CustomerId == id).Include(x => x.Items).Include(x => x.Receivables).OrderByDescending(x => x.CreatedAt).ToListAsync();
    return Results.Ok(new { customer.Id, customer.Name, customer.TradeName, customer.TaxId, customer.Email, customer.City, customer.Neighborhood, customer.Active, Route = customer.RouteStops.Select(r => new { r.Day, r.Position }), OpenBalance = sales.SelectMany(s => s.Receivables).Where(r => !r.Paid).Sum(r => r.Amount), Purchases = sales.Select(s => new { s.Id, s.CreatedAt, Total = s.Items.Sum(i => i.Quantity * i.UnitPrice), Receivables = s.Receivables.Select(r => new { r.InstallmentNumber, r.DueDate, r.Amount, r.PaymentMethod, r.Paid }), Items = s.Items.Select(i => new { i.ProductName, i.Quantity, i.UnitPrice }) }) });
});
app.MapPost("/api/customers", async (CustomerInput input, BakeryDb db) =>
{
    if (string.IsNullOrWhiteSpace(input.Name) || string.IsNullOrWhiteSpace(input.TaxId) || string.IsNullOrWhiteSpace(input.Email) || string.IsNullOrWhiteSpace(input.Neighborhood)) return Results.BadRequest(new { message = "Informe nome, CPF/CNPJ, e-mail, cidade e bairro." });
    if (!IsValidDocument(input.TaxId)) return Results.BadRequest(new { message = "CPF ou CNPJ inválido." });
    if (input.City is not ("Foz do Iguaçu" or "Santa Terezinha de Itaipu")) return Results.BadRequest(new { message = "Atendemos somente Foz do Iguaçu e Santa Terezinha de Itaipu." });
    if (!locations[input.City].Contains(input.Neighborhood)) return Results.BadRequest(new { message = "Selecione um bairro cadastrado para a cidade escolhida." });
    if (await db.Customers.AnyAsync(x => x.TaxId == input.TaxId)) return Results.Conflict(new { message = "Já existe cliente com este CPF/CNPJ." });
    var item = new Customer { Name = input.Name.Trim(), TradeName = input.TradeName?.Trim(), TaxId = input.TaxId.Trim(), Email = input.Email.Trim(), City = input.City, Neighborhood = input.Neighborhood.Trim() };
    db.Customers.Add(item); await db.SaveChangesAsync(); return Results.Created($"/api/customers/{item.Id}", item);
});
app.MapPost("/api/customers/{id:int}/route", async (int id, RouteInput input, BakeryDb db) =>
{
    if (input.Position < 1 || !days.Contains(input.Day)) return Results.BadRequest(new { message = "Dia ou posição da rota inválidos." });
    if (!await db.Customers.AnyAsync(x => x.Id == id)) return Results.NotFound();
    var stop = await db.RouteStops.FirstOrDefaultAsync(x => x.CustomerId == id && x.Day == input.Day);
    if (stop is null) db.RouteStops.Add(new RouteStop { CustomerId = id, Day = input.Day, Position = input.Position }); else stop.Position = input.Position;
    await db.SaveChangesAsync(); return Results.Ok(new { message = "Rota atualizada." });
});
app.MapGet("/api/routes", async (string? day, BakeryDb db) =>
{
    var selected = day ?? DateTime.Today.ToString("dddd", new System.Globalization.CultureInfo("pt-BR"));
    selected = char.ToUpper(selected[0]) + selected[1..] + (selected.EndsWith("-feira") || selected == "Sábado" || selected == "Domingo" ? "" : "-feira");
    return Results.Ok(await db.RouteStops.Include(x => x.Customer).Where(x => x.Day == selected && x.Customer!.Active).OrderBy(x => x.Position).Select(x => new { x.Id, x.Day, x.Position, CustomerId = x.CustomerId, Name = x.Customer!.Name, x.Customer.TradeName, x.Customer.City, x.Customer.Neighborhood }).ToListAsync());
});
app.MapGet("/api/products", async (BakeryDb db) => Results.Ok(await db.Products.Where(x => x.Active).OrderBy(x => x.Group).ThenBy(x => x.Name).Select(x => new { x.Id, x.Name, x.Group, x.Unit, x.Price, x.Cost, x.Stock, x.ExpiryDate }).ToListAsync()));
app.MapPost("/api/products", async (ProductInput input, BakeryDb db) =>
{
    if (string.IsNullOrWhiteSpace(input.Name) || input.Price < 0 || input.Cost < 0 || input.Stock < 0) return Results.BadRequest(new { message = "Confira os dados do produto e estoque." });
    var product = new Product { Name = input.Name.Trim(), Group = input.Group.Trim(), Unit = input.Unit.Trim(), Price = input.Price, Cost = input.Cost, Stock = input.Stock, ExpiryDate = input.ExpiryDate };
    db.Products.Add(product); await db.SaveChangesAsync(); return Results.Created($"/api/products/{product.Id}", product);
});
app.MapPut("/api/products/{id:int}/stock", async (int id, decimal quantity, BakeryDb db) =>
{
    if (quantity < 0) return Results.BadRequest(new { message = "Estoque não pode ser negativo." });
    var product = await db.Products.FindAsync(id); if (product is null) return Results.NotFound(); product.Stock = quantity; await db.SaveChangesAsync(); return Results.Ok(product);
});
app.MapPost("/api/sales", async (SaleInput input, BakeryDb db) =>
{
    if (input.Items.Count == 0 || input.Items.Any(i => i.Quantity <= 0)) return Results.BadRequest(new { message = "Adicione produtos e quantidades maiores que zero." });
    var customer = await db.Customers.FindAsync(input.CustomerId); if (customer is null || !customer.Active) return Results.BadRequest(new { message = "Cliente não disponível." });
    var condition = await db.PaymentConditions.Include(x => x.Rules).FirstOrDefaultAsync(x => x.Id == input.PaymentConditionId && x.Active);
    if (condition is null) return Results.BadRequest(new { message = "Selecione uma condição de pagamento ativa." });
    await using var transaction = await db.Database.BeginTransactionAsync();
    var sale = new Sale { CustomerId = customer.Id, PaymentConditionId = condition.Id };
    foreach (var line in input.Items)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == line.ProductId && p.Active);
        if (product is null) return Results.BadRequest(new { message = "Produto indisponível." });
        if (product.Stock < line.Quantity) return Results.Conflict(new { message = $"Estoque insuficiente de {product.Name}. Disponível: {product.Stock:0.##} {product.Unit}." });
        product.Stock -= line.Quantity;
        sale.Items.Add(new SaleItem { ProductId = product.Id, ProductName = product.Name, Quantity = line.Quantity, UnitPrice = product.Price, UnitCost = product.Cost });
    }
    var total = sale.Items.Sum(i => i.Quantity * i.UnitPrice);
    var allocated = 0m;
    var dueDate = DateOnly.FromDateTime(sale.CreatedAt.ToLocalTime());
    for (var index = 0; index < condition.Rules.Count; index++)
    {
        var rule = condition.Rules.OrderBy(r => r.Days).ElementAt(index);
        var amount = index == condition.Rules.Count - 1 ? total - allocated : decimal.Round(total * rule.Percent / 100m, 2, MidpointRounding.AwayFromZero);
        allocated += amount;
        sale.Receivables.Add(new Receivable { InstallmentNumber = index + 1, DueDate = dueDate.AddDays(rule.Days), Amount = amount, PaymentMethod = rule.PaymentMethod, FinePercent = condition.FinePercent, DailyInterestPercent = condition.DailyInterestPercent, DailyDiscountPercent = condition.DailyDiscountPercent });
    }
    db.Sales.Add(sale); await db.SaveChangesAsync(); await transaction.CommitAsync();
    return Results.Created($"/api/sales/{sale.Id}", new { sale.Id, sale.CreatedAt, Total = total, Profit = sale.Items.Sum(i => i.Quantity * (i.UnitPrice - i.UnitCost)), Receivables = sale.Receivables.Select(r => new { r.InstallmentNumber, r.DueDate, r.Amount, r.PaymentMethod }) });
});
app.MapGet("/api/payment-conditions", async (BakeryDb db) => Results.Ok(await db.PaymentConditions.Include(x => x.Rules).Where(x => x.Active).OrderBy(x => x.Code).Select(x => new { x.Id, x.Code, x.Name, x.FinePercent, x.DailyInterestPercent, x.DailyDiscountPercent, Rules = x.Rules.OrderBy(r => r.Days).Select(r => new { r.Days, r.Percent, r.PaymentMethod }) }).ToListAsync()));
app.MapGet("/api/receivables", async (DateOnly? date, bool? paid, BakeryDb db) =>
{
    var query = db.Receivables.Include(x => x.Sale).ThenInclude(s => s!.Customer).AsQueryable();
    if (date is not null) query = query.Where(x => x.DueDate == date);
    if (paid is not null) query = query.Where(x => x.Paid == paid);
    return Results.Ok(await query.OrderBy(x => x.DueDate).Select(x => new { x.Id, x.SaleId, Customer = x.Sale!.Customer!.Name, x.InstallmentNumber, x.DueDate, x.Amount, x.PaymentMethod, x.Paid, x.FinePercent, x.DailyInterestPercent, x.DailyDiscountPercent }).ToListAsync());
});
app.MapPut("/api/receivables/{id:int}/paid", async (int id, bool paid, BakeryDb db) =>
{
    var receivable = await db.Receivables.FindAsync(id); if (receivable is null) return Results.NotFound(); receivable.Paid = paid; await db.SaveChangesAsync(); return Results.Ok(new { receivable.Id, receivable.Paid });
});
app.MapGet("/api/sales", async (DateOnly? date, int? customerId, BakeryDb db) =>
{
    var from = (date ?? DateOnly.FromDateTime(DateTime.Today)).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    var to = from.AddDays(1);
    var query = db.Sales.Include(x => x.Customer).Include(x => x.Items).Where(x => x.CreatedAt >= from && x.CreatedAt < to);
    if (customerId is not null) query = query.Where(x => x.CustomerId == customerId);
    return Results.Ok(await query.OrderByDescending(x => x.CreatedAt).Select(x => new { x.Id, x.CreatedAt, Customer = x.Customer!.Name, x.Customer.City, Total = x.Items.Sum(i => i.Quantity * i.UnitPrice), Profit = x.Items.Sum(i => i.Quantity * (i.UnitPrice - i.UnitCost)), Items = x.Items.Select(i => new { i.ProductName, i.Quantity, i.UnitPrice, i.UnitCost, Profit = i.Quantity * (i.UnitPrice - i.UnitCost) }) }).ToListAsync());
});
app.MapGet("/api/reports/daily", async (DateOnly? date, BakeryDb db) =>
{
    var from = (date ?? DateOnly.FromDateTime(DateTime.Today)).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc); var to = from.AddDays(1);
    var sales = await db.Sales.Include(x => x.Items).Where(x => x.CreatedAt >= from && x.CreatedAt < to).ToListAsync();
    var items = sales.SelectMany(s => s.Items).GroupBy(i => i.ProductName).Select(g => new { Product = g.Key, Quantity = g.Sum(i => i.Quantity), Revenue = g.Sum(i => i.Quantity * i.UnitPrice), Cost = g.Sum(i => i.Quantity * i.UnitCost), Profit = g.Sum(i => i.Quantity * (i.UnitPrice - i.UnitCost)) });
    return Results.Ok(new { Date = DateOnly.FromDateTime(from), Revenue = sales.SelectMany(s => s.Items).Sum(i => i.Quantity * i.UnitPrice), Profit = sales.SelectMany(s => s.Items).Sum(i => i.Quantity * (i.UnitPrice - i.UnitCost)), SaleCount = sales.Count, Products = items });
});
app.Run();

static bool IsValidDocument(string value)
{
    var digits = new string(value.Where(char.IsDigit).ToArray());
    if (digits.Length == 11 && digits.Distinct().Count() > 1)
    {
        var numbers = digits.Select(c => c - '0').ToArray();
        var first = numbers.Take(9).Select((n, i) => n * (10 - i)).Sum() % 11;
        var d1 = first < 2 ? 0 : 11 - first;
        var second = numbers.Take(9).Append(d1).Select((n, i) => n * (11 - i)).Sum() % 11;
        var d2 = second < 2 ? 0 : 11 - second;
        if (numbers[9] == d1 && numbers[10] == d2) return true;
    }
    if (digits.Length == 14 && digits.Distinct().Count() > 1)
    {
        var numbers = digits.Select(c => c - '0').ToArray();
        int Digit(int length, int[] weights)
        {
            var sum = Enumerable.Range(0, length).Sum(i => numbers[i] * weights[i]);
            var remainder = sum % 11;
            return remainder < 2 ? 0 : 11 - remainder;
        }
        var d1 = Digit(12, [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
        var d2 = Digit(13, [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
        if (numbers[12] == d1 && numbers[13] == d2) return true;
    }
    return false;
}
