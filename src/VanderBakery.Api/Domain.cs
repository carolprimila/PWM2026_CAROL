using Microsoft.EntityFrameworkCore;

namespace VanderBakery.Api;

public sealed class BakeryDb(DbContextOptions<BakeryDb> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<RouteStop> RouteStops => Set<RouteStop>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<PaymentCondition> PaymentConditions => Set<PaymentCondition>();
    public DbSet<PaymentRule> PaymentRules => Set<PaymentRule>();
    public DbSet<Receivable> Receivables => Set<Receivable>();
    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Customer>().HasIndex(x => x.TaxId).IsUnique();
        model.Entity<Product>().Property(x => x.Price).HasPrecision(12, 2);
        model.Entity<Product>().Property(x => x.Cost).HasPrecision(12, 2);
        model.Entity<SaleItem>().Property(x => x.UnitPrice).HasPrecision(12, 2);
        model.Entity<SaleItem>().Property(x => x.UnitCost).HasPrecision(12, 2);
    }
}

public sealed class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? TradeName { get; set; }
    public string TaxId { get; set; } = "";
    public string Email { get; set; } = "";
    public string City { get; set; } = "Foz do Iguaçu";
    public string Neighborhood { get; set; } = "";
    public bool Active { get; set; } = true;
    public List<RouteStop> RouteStops { get; set; } = [];
}

public sealed class AppUser
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordSalt { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Seller";
    public bool Active { get; set; } = true;
}

public sealed class RouteStop
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public string Day { get; set; } = "Segunda-feira";
    public int Position { get; set; }
}

public sealed class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Group { get; set; } = "";
    public string Unit { get; set; } = "un";
    public decimal Price { get; set; }
    public decimal Cost { get; set; }
    public decimal Stock { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public bool Active { get; set; } = true;
}

public sealed class Sale
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int PaymentConditionId { get; set; }
    public PaymentCondition? PaymentCondition { get; set; }
    public List<SaleItem> Items { get; set; } = [];
    public List<Receivable> Receivables { get; set; } = [];
}

public sealed class SaleItem
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public Sale? Sale { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal UnitCost { get; set; }
}

public sealed class PaymentCondition
{
    public int Id { get; set; }
    public int Code { get; set; }
    public string Name { get; set; } = "";
    public decimal FinePercent { get; set; }
    public decimal DailyInterestPercent { get; set; }
    public decimal DailyDiscountPercent { get; set; }
    public bool Active { get; set; } = true;
    public List<PaymentRule> Rules { get; set; } = [];
}

public sealed class PaymentRule
{
    public int Id { get; set; }
    public int PaymentConditionId { get; set; }
    public PaymentCondition? PaymentCondition { get; set; }
    public int Days { get; set; }
    public decimal Percent { get; set; }
    public string PaymentMethod { get; set; } = "Boleto";
}

public sealed class Receivable
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public Sale? Sale { get; set; }
    public int InstallmentNumber { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Boleto";
    public decimal FinePercent { get; set; }
    public decimal DailyInterestPercent { get; set; }
    public decimal DailyDiscountPercent { get; set; }
    public bool Paid { get; set; }
}

public sealed record CustomerInput(string Name, string? TradeName, string TaxId, string Email, string City, string Neighborhood);
public sealed record LoginInput(string Email, string Password);
public sealed record RegisterInput(string Name, string Email, string Password);
public sealed record RouteInput(string Day, int Position);
public sealed record ProductInput(string Name, string Group, string Unit, decimal Price, decimal Cost, decimal Stock, DateOnly? ExpiryDate);
public sealed record SaleInput(int CustomerId, List<SaleLineInput> Items, int PaymentConditionId = 1, string PaymentMethod = "Boleto bancário");
public sealed record SaleLineInput(int ProductId, decimal Quantity);
