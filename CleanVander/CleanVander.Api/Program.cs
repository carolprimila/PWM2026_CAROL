using System.Security.Cryptography;
using System.Text;
using CleanVander.Api;
using CleanVander.Api.Models;
using SQLite;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<CleanVanderDatabase>();
builder.Services.AddSingleton(sp => sp.GetRequiredService<CleanVanderDatabase>().Connection);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<CleanVanderDatabase>().Seed();

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    await next();
});

// ---------------------------------------------------------------- Público

app.MapGet("/health", () => Results.Ok(new { status = "ok", sistema = "CleanVander" }));

app.MapGet("/api/products", (SQLiteConnection db) =>
    Results.Ok(db.Table<Produto>()
        .Where(p => p.Ativo)
        .OrderBy(p => p.Categoria)
        .ThenBy(p => p.Descricao)
        .ToList()
        .Select(ToDto)));

app.MapPost("/api/clients", (CadastroClienteDto input, SQLiteConnection db) =>
{
    var tipo = input.TipoPessoa.Equals("Juridica", StringComparison.OrdinalIgnoreCase)
            || input.TipoPessoa.Equals("PJ", StringComparison.OrdinalIgnoreCase)
        ? TipoPessoa.Juridica
        : TipoPessoa.Fisica;
    var documento = Digits(input.Documento);

    if (string.IsNullOrWhiteSpace(input.Nome)
        || documento.Length is < 11 or > 14
        || string.IsNullOrWhiteSpace(input.Telefone)
        || string.IsNullOrWhiteSpace(input.Endereco))
        return Results.BadRequest(new { erro = "Informe nome, CPF/CNPJ, telefone e endereço válidos." });

    if (db.Table<Cliente>().Any(c => c.Documento == documento))
        return Results.Conflict(new { erro = "Já existe cadastro com este documento." });

    var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    var cliente = new Cliente
    {
        TipoPessoa = tipo,
        Nome = input.Nome.Trim(),
        Documento = documento,
        Email = input.Email.Trim(),
        Telefone = input.Telefone.Trim(),
        Endereco = input.Endereco.Trim(),
        ChaveHash = Hash(key)
    };
    db.Insert(cliente);

    return Results.Created($"/api/clients/{cliente.Id}", new CadastroClienteResultado(cliente.Id, key, cliente.Nome));
});

// ---------------------------------------------------------------- Cliente (X-Client-Key)

app.MapGet("/api/orders", (HttpContext http, SQLiteConnection db) =>
{
    var c = ClientFromKey(http, db);
    if (c is null) return Results.Unauthorized();

    var orders = db.Table<Pedido>()
        .Where(p => p.ClienteId == c.Id)
        .OrderByDescending(p => p.CriadoEm)
        .ToList();

    return Results.Ok(orders.Select(p => ToOrder(p, db, null)));
});

app.MapPost("/api/orders", (CriarPedidoDto input, HttpContext http, SQLiteConnection db) =>
{
    var client = ClientFromKey(http, db);
    if (client is null) return Results.Unauthorized();

    if (input.Itens is null
        || input.Itens.Count == 0
        || input.Itens.Any(i => i.Quantidade <= 0 || i.Quantidade > 999))
        return Results.BadRequest(new { erro = "O pedido precisa ter itens com quantidades entre 1 e 999." });

    var quantities = input.Itens
        .GroupBy(i => i.ProdutoId)
        .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantidade));

    var products = db.Table<Produto>()
        .Where(p => p.Ativo)
        .ToList()
        .Where(p => quantities.ContainsKey(p.Id))
        .ToList();

    if (products.Count != quantities.Count)
        return Results.BadRequest(new { erro = "Um ou mais produtos não existem ou estão inativos." });

    var order = new Pedido
    {
        ClienteId = client.Id,
        Numero = $"CV-{DateTime.UtcNow:yyyyMMddHHmm}-{Random.Shared.Next(100, 999)}",
        CriadoEm = DateTime.UtcNow,
        Status = "Recebido",
        FormaPagamento = "À vista na entrega"
    };
    db.Insert(order);

    var items = products.Select(p => new ItemPedido
    {
        PedidoId = order.Id,
        ProdutoId = p.Id,
        CodigoProduto = p.Codigo,
        DescricaoProduto = p.Descricao,
        UnidadeMedida = p.UnidadeMedida,
        TamanhoVolume = p.TamanhoVolume,
        Quantidade = quantities[p.Id],
        PrecoUnitario = p.Preco
    }).ToList();
    db.InsertAll(items);

    order.Total = items.Sum(i => i.Subtotal);
    db.Update(order);

    return Results.Created($"/api/orders/{order.Id}", ToOrder(order, db, null));
});

// ---------------------------------------------------------------- Empresa (X-Admin-Key)

app.MapGet("/api/admin/clients", (HttpContext h, SQLiteConnection db, IConfiguration c) =>
    IsAdmin(h, c)
        ? Results.Ok(db.Table<Cliente>().OrderBy(x => x.Nome).ToList().Select(ToClient))
        : Results.Unauthorized());

app.MapGet("/api/admin/products", (HttpContext h, SQLiteConnection db, IConfiguration c) =>
    IsAdmin(h, c)
        ? Results.Ok(db.Table<Produto>().OrderBy(p => p.Codigo).ToList().Select(ToDto))
        : Results.Unauthorized());

app.MapPost("/api/admin/products", (ProdutoEntrada input, HttpContext h, SQLiteConnection db, IConfiguration c) =>
{
    if (!IsAdmin(h, c)) return Results.Unauthorized();

    if (string.IsNullOrWhiteSpace(input.Codigo)
        || string.IsNullOrWhiteSpace(input.Descricao)
        || string.IsNullOrWhiteSpace(input.Categoria)
        || input.Preco < 0)
        return Results.BadRequest(new { erro = "Preencha os dados do produto e um preço válido." });

    if (db.Table<Produto>().Any(p => p.Codigo == input.Codigo.Trim()))
        return Results.Conflict(new { erro = "Já existe produto com este código." });

    var p = NewProduct(input);
    db.Insert(p);

    return Results.Created($"/api/admin/products/{p.Id}", ToDto(p));
});

app.MapPut("/api/admin/products/{id:int}", (int id, ProdutoEntrada input, HttpContext h, SQLiteConnection db, IConfiguration c) =>
{
    if (!IsAdmin(h, c)) return Results.Unauthorized();

    var p = db.Find<Produto>(id);
    if (p is null) return Results.NotFound();

    if (string.IsNullOrWhiteSpace(input.Descricao) || input.Preco < 0)
        return Results.BadRequest(new { erro = "Descrição e preço válido são obrigatórios." });

    if (db.Table<Produto>().Any(x => x.Id != id && x.Codigo == input.Codigo.Trim()))
        return Results.Conflict(new { erro = "Código já utilizado." });

    p.Codigo = input.Codigo.Trim();
    p.Descricao = input.Descricao.Trim();
    p.Categoria = input.Categoria.Trim();
    p.UnidadeMedida = input.UnidadeMedida.Trim();
    p.TamanhoVolume = input.TamanhoVolume.Trim();
    p.Preco = input.Preco;
    p.Ativo = input.Ativo;
    db.Update(p);

    return Results.Ok(ToDto(p));
});

app.MapGet("/api/admin/orders", (HttpContext h, SQLiteConnection db, IConfiguration c) =>
{
    if (!IsAdmin(h, c)) return Results.Unauthorized();

    return Results.Ok(db.Table<Pedido>()
        .OrderByDescending(p => p.CriadoEm)
        .ToList()
        .Select(p => ToOrder(p, db, db.Find<Cliente>(p.ClienteId))));
});

app.MapPut("/api/admin/orders/{id:int}/status", (int id, AtualizarStatusDto input, HttpContext h, SQLiteConnection db, IConfiguration c) =>
{
    if (!IsAdmin(h, c)) return Results.Unauthorized();

    var status = input.Status.Replace(" ", "");
    if (status is not ("Recebido" or "EmSeparacao" or "EnviadoParaEntrega" or "Entregue" or "Cancelado"))
        return Results.BadRequest(new { erro = "Status inválido." });

    var p = db.Find<Pedido>(id);
    if (p is null) return Results.NotFound();

    p.Status = status;
    db.Update(p);

    return Results.Ok(ToOrder(p, db, db.Find<Cliente>(p.ClienteId)));
});

app.Run();

// ---------------------------------------------------------------- Funções auxiliares

static string Digits(string s) => new(s.Where(char.IsDigit).ToArray());

static string Hash(string s) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)));

static Cliente? ClientFromKey(HttpContext h, SQLiteConnection db)
{
    var k = h.Request.Headers["X-Client-Key"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(k)) return null;

    var hash = Hash(k);
    return db.Table<Cliente>().FirstOrDefault(c => c.ChaveHash == hash);
}

static bool IsAdmin(HttpContext h, IConfiguration c)
{
    var a = c["Admin:ApiKey"] ?? "";
    var b = h.Request.Headers["X-Admin-Key"].FirstOrDefault() ?? "";
    return a.Length > 0
        && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}

static Produto NewProduct(ProdutoEntrada x) => new()
{
    Codigo = x.Codigo.Trim(),
    Descricao = x.Descricao.Trim(),
    Categoria = x.Categoria.Trim(),
    UnidadeMedida = x.UnidadeMedida.Trim(),
    TamanhoVolume = x.TamanhoVolume.Trim(),
    Preco = x.Preco,
    Ativo = x.Ativo
};

static ProdutoDto ToDto(Produto p) =>
    new(p.Id, p.Codigo, p.Descricao, p.Categoria, p.UnidadeMedida, p.TamanhoVolume, p.Preco, p.Ativo);

static ClienteResumoDto ToClient(Cliente c) =>
    new(c.Id, c.TipoPessoa.ToString(), c.Nome, c.Documento, c.Email, c.Telefone, c.Endereco);

static PedidoResposta ToOrder(Pedido p, SQLiteConnection db, Cliente? client) =>
    new(p.Id, p.Numero, p.CriadoEm, p.Status, p.FormaPagamento, p.Total,
        client is null ? null : ToClient(client),
        db.Table<ItemPedido>()
            .Where(i => i.PedidoId == p.Id)
            .ToList()
            .Select(i => new ItemPedidoResposta(
                i.ProdutoId, i.CodigoProduto, i.DescricaoProduto, i.UnidadeMedida,
                i.TamanhoVolume, i.Quantidade, i.PrecoUnitario, i.Subtotal))
            .ToList());