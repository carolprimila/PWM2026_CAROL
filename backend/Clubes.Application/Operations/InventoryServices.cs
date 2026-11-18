using System.ComponentModel.DataAnnotations;
using Clubes.Application.Common;
using Clubes.Application.People;
using Clubes.Domain;
using Microsoft.EntityFrameworkCore;

namespace Clubes.Application.Operations;

// ---------- Funcionários ----------

public record EmployeeDto(Guid Id, string FullName, string Cpf, string? Phone, string? Email, string? Position, string? Department, DateOnly? HiredAt,
    EmployeeStatus Status, Guid? UserId, string? UserEmail, IReadOnlyList<string> Roles, DateTime CreatedAt);
public record SaveEmployeeRequest([Required, StringLength(150, MinimumLength = 3)] string FullName, [Required] string Cpf, [StringLength(30)] string? Phone,
    [EmailAddress, StringLength(150)] string? Email, [StringLength(80)] string? Position, [StringLength(80)] string? Department, DateOnly? HiredAt);

public class EmployeeService(IAppDb db, RequestContext ctx)
{
    public async Task<PagedResult<EmployeeDto>> ListAsync(PageQuery page, EmployeeStatus? status, CancellationToken ct = default)
    {
        var q = db.Set<Employee>().AsNoTracking();
        if (status is not null) q = q.Where(e => e.Status == status);
        if (!string.IsNullOrWhiteSpace(page.Search))
        {
            var like = page.Search.Like();
            q = q.Where(e => EF.Functions.ILike(e.FullName, like) || EF.Functions.ILike(e.Position!, like) || EF.Functions.ILike(e.Department!, like));
        }
        var result = await q.OrderBy(e => e.FullName).ToPagedAsync(page, ct);
        return new PagedResult<EmployeeDto>(await MapAsync(result.Items, ct), result.Total, result.Page, result.PageSize);
    }

    public async Task<EmployeeDto> GetAsync(Guid id, CancellationToken ct = default)
        => (await MapAsync([await db.Set<Employee>().AsNoTracking().Where(e => e.Id == id).FirstOr404Async("Funcionário", ct)], ct))[0];

    public async Task<EmployeeDto> SaveAsync(Guid? id, SaveEmployeeRequest req, CancellationToken ct = default)
    {
        var cpf = req.Cpf.OnlyDigits();
        if (!Cpf.IsValid(cpf)) throw new DomainException("CPF inválido.", "cpf_invalido", 400);
        Employee e;
        if (id is null) { e = new Employee(); db.Set<Employee>().Add(e); db.AddHistory(ctx, nameof(Employee), e.Id, null, EmployeeStatus.Active, "Cadastro"); }
        else e = await db.Set<Employee>().Where(x => x.Id == id).FirstOr404Async("Funcionário", ct);
        if (await db.Set<Employee>().AnyAsync(x => x.Cpf == cpf && x.Id != e.Id, ct)) throw DomainException.Conflict("Já existe um funcionário com este CPF.");
        (e.FullName, e.Cpf, e.Phone, e.Email, e.Position, e.Department, e.HiredAt) = (req.FullName.Trim(), cpf, req.Phone, req.Email?.Trim().ToLowerInvariant(), req.Position, req.Department, req.HiredAt);
        await db.SaveChangesAsync(ct);
        return await GetAsync(e.Id, ct);
    }

    /// <summary>Desativar o funcionário também encerra as sessões do usuário vinculado (o login passa a ser recusado).</summary>
    public async Task<EmployeeDto> SetStatusAsync(Guid id, EmployeeStatus status, StatusChangeRequest req, CancellationToken ct = default)
    {
        var e = await db.Set<Employee>().Where(x => x.Id == id).FirstOr404Async("Funcionário", ct);
        if (e.Status == status) return await GetAsync(id, ct);
        db.AddHistory(ctx, nameof(Employee), id, e.Status, status, req.Reason);
        e.Status = status;
        ctx.Justification = req.Reason;
        await db.SaveChangesAsync(ct);
        if (status == EmployeeStatus.Inactive)
        {
            var userIds = await db.Set<User>().Where(u => u.EmployeeId == id).Select(u => u.Id).ToListAsync(ct);
            await db.Set<RefreshToken>().Where(t => userIds.Contains(t.UserId) && t.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow).SetProperty(t => t.RevokedReason, "funcionario_inativo"), ct);
        }
        return await GetAsync(id, ct);
    }

    private async Task<List<EmployeeDto>> MapAsync(IReadOnlyList<Employee> items, CancellationToken ct)
    {
        var ids = items.Select(e => e.Id).ToList();
        var users = await db.Set<User>().Where(u => u.EmployeeId != null && ids.Contains(u.EmployeeId.Value))
            .Select(u => new { u.Id, u.EmployeeId, u.Email, Roles = u.Roles.Select(r => r.Role.Name).ToList() }).ToListAsync(ct);
        return items.Select(e =>
        {
            var user = users.FirstOrDefault(u => u.EmployeeId == e.Id);
            return new EmployeeDto(e.Id, e.FullName, e.Cpf, e.Phone, e.Email, e.Position, e.Department, e.HiredAt, e.Status, user?.Id, user?.Email, user?.Roles ?? [], e.CreatedAt);
        }).ToList();
    }
}

// ---------- Patrimônio ----------

public record AssetDto(Guid Id, string Code, string Name, string? Category, string? Description, string? Location, Guid? SpaceId, string? SpaceName,
    AssetCondition Condition, DateOnly? AcquiredOn, decimal? Value, Guid? ResponsibleEmployeeId, string? ResponsibleName, int Maintenances, DateTime CreatedAt);
public record SaveAssetRequest([Required, StringLength(40)] string Code, [Required, StringLength(150, MinimumLength = 2)] string Name, [StringLength(80)] string? Category,
    [StringLength(2000)] string? Description, [StringLength(150)] string? Location, Guid? SpaceId, AssetCondition Condition, DateOnly? AcquiredOn,
    [Range(0, 100_000_000)] decimal? Value, Guid? ResponsibleEmployeeId);

public class AssetQuery : PageQuery
{
    public string? Category { get; set; }
    public AssetCondition? Condition { get; set; }
}

public class AssetService(IAppDb db, RequestContext ctx)
{
    public async Task<PagedResult<AssetDto>> ListAsync(AssetQuery query, CancellationToken ct = default)
    {
        var q = db.Set<Asset>().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Category)) q = q.Where(a => a.Category == query.Category);
        if (query.Condition is not null) q = q.Where(a => a.Condition == query.Condition);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var like = query.Search.Like();
            q = q.Where(a => EF.Functions.ILike(a.Name, like) || EF.Functions.ILike(a.Code, like) || EF.Functions.ILike(a.Location!, like));
        }
        return await Project(q.OrderBy(a => a.Name)).ToPagedAsync(query, ct);
    }

    public async Task<AssetDto> GetAsync(Guid id, CancellationToken ct = default) => await Project(db.Set<Asset>().Where(a => a.Id == id)).FirstOr404Async("Bem patrimonial", ct);

    private IQueryable<AssetDto> Project(IQueryable<Asset> q)
        => from a in q
           join s in db.Set<Space>() on a.SpaceId equals s.Id into spaces
           from s in spaces.DefaultIfEmpty()
           join e in db.Set<Employee>() on a.ResponsibleEmployeeId equals e.Id into employees
           from e in employees.DefaultIfEmpty()
           select new AssetDto(a.Id, a.Code, a.Name, a.Category, a.Description, a.Location, a.SpaceId, s.Name, a.Condition, a.AcquiredOn, a.Value,
               a.ResponsibleEmployeeId, e.FullName, db.Set<MaintenanceOrder>().Count(m => m.AssetId == a.Id), a.CreatedAt);

    public async Task<AssetDto> SaveAsync(Guid? id, SaveAssetRequest req, CancellationToken ct = default)
    {
        Asset a;
        if (id is null) { a = new Asset(); db.Set<Asset>().Add(a); }
        else a = await db.Set<Asset>().Where(x => x.Id == id).FirstOr404Async("Bem patrimonial", ct);
        var code = req.Code.Trim();
        if (await db.Set<Asset>().AnyAsync(x => x.Code == code && x.Id != a.Id, ct)) throw DomainException.Conflict("Já existe um bem com esta identificação.");
        if (id is not null && a.Condition != req.Condition) db.AddHistory(ctx, nameof(Asset), a.Id, a.Condition, req.Condition, "Estado de conservação alterado");
        (a.Code, a.Name, a.Category, a.Description, a.Location, a.SpaceId) = (code, req.Name.Trim(), req.Category, req.Description, req.Location, req.SpaceId);
        (a.Condition, a.AcquiredOn, a.Value, a.ResponsibleEmployeeId) = (req.Condition, req.AcquiredOn, req.Value, req.ResponsibleEmployeeId);
        await db.SaveChangesAsync(ct);
        return await GetAsync(a.Id, ct);
    }

    public async Task DeleteAsync(Guid id, StatusChangeRequest req, CancellationToken ct = default)
    {
        var a = await db.Set<Asset>().Where(x => x.Id == id).FirstOr404Async("Bem patrimonial", ct);
        ctx.Justification = req.Reason;
        db.AddHistory(ctx, nameof(Asset), id, a.Condition, "Baixado", req.Reason);
        db.Set<Asset>().Remove(a);
        await db.SaveChangesAsync(ct);
    }
}

// ---------- Estoque ----------

public record SupplierDto(Guid Id, string Name, string? Document, string? Phone, string? Email, string? Notes);
public record SaveSupplierRequest([Required, StringLength(150, MinimumLength = 2)] string Name, [StringLength(20)] string? Document, [StringLength(30)] string? Phone,
    [EmailAddress] string? Email, [StringLength(1000)] string? Notes);
public record StockItemDto(Guid Id, string Name, string? Category, string Unit, decimal Quantity, decimal MinQuantity, bool IsLow, Guid? SupplierId, string? SupplierName);
public record SaveStockItemRequest([Required, StringLength(150, MinimumLength = 2)] string Name, [StringLength(80)] string? Category, [Required, StringLength(10)] string Unit,
    [Range(0, 100_000_000)] decimal MinQuantity, Guid? SupplierId);
public record StockMovementDto(Guid Id, Guid StockItemId, string ItemName, StockMovementKind Kind, decimal Quantity, decimal BalanceAfter, decimal? UnitCost, string? Reason,
    string? SupplierName, string UserName, DateTime At);
public record MoveStockRequest(Guid StockItemId, StockMovementKind Kind, [Range(0, 100_000_000)] decimal Quantity, [Range(0, 100_000_000)] decimal? UnitCost,
    [StringLength(300)] string? Reason, Guid? SupplierId);

public class StockQuery : PageQuery
{
    public bool? LowOnly { get; set; }
    public string? Category { get; set; }
}

public class StockService(IAppDb db, RequestContext ctx, NotificationService notifications)
{
    public async Task<List<SupplierDto>> SuppliersAsync(CancellationToken ct = default)
        => await db.Set<Supplier>().OrderBy(s => s.Name).Select(s => new SupplierDto(s.Id, s.Name, s.Document, s.Phone, s.Email, s.Notes)).ToListAsync(ct);

    public async Task<SupplierDto> SaveSupplierAsync(Guid? id, SaveSupplierRequest req, CancellationToken ct = default)
    {
        Supplier s;
        if (id is null) { s = new Supplier(); db.Set<Supplier>().Add(s); }
        else s = await db.Set<Supplier>().Where(x => x.Id == id).FirstOr404Async("Fornecedor", ct);
        (s.Name, s.Document, s.Phone, s.Email, s.Notes) = (req.Name.Trim(), req.Document, req.Phone, req.Email, req.Notes);
        await db.SaveChangesAsync(ct);
        return new SupplierDto(s.Id, s.Name, s.Document, s.Phone, s.Email, s.Notes);
    }

    public async Task DeleteSupplierAsync(Guid id, CancellationToken ct = default)
    {
        var s = await db.Set<Supplier>().Where(x => x.Id == id).FirstOr404Async("Fornecedor", ct);
        db.Set<Supplier>().Remove(s);
        await db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<StockItemDto>> ItemsAsync(StockQuery query, CancellationToken ct = default)
    {
        var q = db.Set<StockItem>().AsNoTracking();
        if (query.LowOnly is true) q = q.Where(i => i.Quantity <= i.MinQuantity);
        if (!string.IsNullOrWhiteSpace(query.Category)) q = q.Where(i => i.Category == query.Category);
        if (!string.IsNullOrWhiteSpace(query.Search)) { var like = query.Search.Like(); q = q.Where(i => EF.Functions.ILike(i.Name, like) || EF.Functions.ILike(i.Category!, like)); }
        return await Project(q.OrderBy(i => i.Name)).ToPagedAsync(query, ct);
    }

    private IQueryable<StockItemDto> Project(IQueryable<StockItem> q)
        => from i in q
           join s in db.Set<Supplier>() on i.SupplierId equals s.Id into suppliers
           from s in suppliers.DefaultIfEmpty()
           select new StockItemDto(i.Id, i.Name, i.Category, i.Unit, i.Quantity, i.MinQuantity, i.Quantity <= i.MinQuantity, i.SupplierId, s.Name);

    public async Task<StockItemDto> SaveItemAsync(Guid? id, SaveStockItemRequest req, CancellationToken ct = default)
    {
        StockItem item;
        if (id is null) { item = new StockItem(); db.Set<StockItem>().Add(item); }
        else item = await db.Set<StockItem>().Where(x => x.Id == id).FirstOr404Async("Produto", ct);
        if (await db.Set<StockItem>().AnyAsync(x => x.Name == req.Name.Trim() && x.Id != item.Id, ct)) throw DomainException.Conflict("Já existe um produto com este nome.");
        (item.Name, item.Category, item.Unit, item.MinQuantity, item.SupplierId) = (req.Name.Trim(), req.Category, req.Unit.Trim(), req.MinQuantity, req.SupplierId);
        await db.SaveChangesAsync(ct);
        return await Project(db.Set<StockItem>().Where(i => i.Id == item.Id)).FirstAsync(ct);
    }

    public async Task DeleteItemAsync(Guid id, CancellationToken ct = default)
    {
        var item = await db.Set<StockItem>().Where(x => x.Id == id).FirstOr404Async("Produto", ct);
        if (item.Quantity != 0) throw DomainException.Conflict("Zere o saldo do produto antes de excluí-lo.");
        db.Set<StockItem>().Remove(item);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Toda alteração de saldo passa por uma movimentação rastreável (quem, quando, por quê e saldo resultante).</summary>
    public async Task<StockMovementDto> MoveAsync(MoveStockRequest req, CancellationToken ct = default)
    {
        var item = await db.Set<StockItem>().Where(x => x.Id == req.StockItemId).FirstOr404Async("Produto", ct);
        if (req.Kind != StockMovementKind.Adjustment && req.Quantity <= 0) throw new DomainException("Informe uma quantidade maior que zero.", "quantidade_invalida", 400);
        if (req.Kind is StockMovementKind.Out or StockMovementKind.Adjustment && string.IsNullOrWhiteSpace(req.Reason))
            throw new DomainException("Informe o motivo da saída ou do ajuste.", "motivo_obrigatorio", 400);

        var wasLow = item.Quantity <= item.MinQuantity;
        var balance = req.Kind switch
        {
            StockMovementKind.In => item.Quantity + req.Quantity,
            StockMovementKind.Out => item.Quantity - req.Quantity,
            _ => req.Quantity, // ajuste: a quantidade informada passa a ser o saldo (inventário)
        };
        if (balance < 0) throw new DomainException($"Saldo insuficiente. Disponível: {item.Quantity:0.###} {item.Unit}.", "saldo_insuficiente");
        item.Quantity = balance;
        var movement = new StockMovement
        {
            StockItemId = item.Id, Kind = req.Kind, Quantity = req.Quantity, BalanceAfter = balance, UnitCost = req.UnitCost, Reason = req.Reason,
            SupplierId = req.SupplierId, UserId = ctx.RequireUser(),
        };
        db.Set<StockMovement>().Add(movement);
        if (!wasLow && balance <= item.MinQuantity)
            await notifications.ToStaffAsync(Permissions.StockManage, NotificationCategory.General, "stock.low", "Estoque mínimo atingido",
                $"{item.Name}: saldo de {balance:0.###} {item.Unit} (mínimo {item.MinQuantity:0.###}).", nameof(StockItem), item.Id, ct);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw DomainException.Conflict("O saldo foi alterado por outra movimentação. Atualize e tente novamente."); }
        return (await MovementsAsync(new PageQuery { PageSize = 1 }, item.Id, ct)).Items[0];
    }

    public async Task<PagedResult<StockMovementDto>> MovementsAsync(PageQuery page, Guid? itemId, CancellationToken ct = default)
        => await (from m in db.Set<StockMovement>()
                  join u in db.Set<User>() on m.UserId equals u.Id
                  join s in db.Set<Supplier>() on m.SupplierId equals s.Id into suppliers
                  from s in suppliers.DefaultIfEmpty()
                  where itemId == null || m.StockItemId == itemId
                  orderby m.CreatedAt descending
                  select new StockMovementDto(m.Id, m.StockItemId, m.StockItem.Name, m.Kind, m.Quantity, m.BalanceAfter, m.UnitCost, m.Reason, s.Name, u.Name, m.CreatedAt))
            .ToPagedAsync(page, ct);
}
