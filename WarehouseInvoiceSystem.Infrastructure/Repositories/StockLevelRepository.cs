namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;
    using WarehouseInvoiceSystem.Domain.Queries.Results;
    using WarehouseInvoiceSystem.Infrastructure.Data;

    public class StockLevelRepository(IDbContextFactory<ApplicationDbContext> factory, IAuditContextService auditContext)
        : BaseRepository(factory, auditContext), IStockLevelRepository
    {
        public Task<IEnumerable<StockLevel>> GetAllStockLevelAsync(CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<StockLevel>)await All<StockLevel>(context)
                    .Where(s => s.Product.IsActive && s.Warehouse.IsActive)
                    .Include(s => s.Product)
                    .Include(s => s.Warehouse)
                    .ToListAsync(ct);
            });

        public Task<PagedResult<StockLevel>> GetPagedAsync(GetStockQuery query, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                IQueryable<StockLevel> q = ApplyFilters(
                    All<StockLevel>(context)
                        .Include(s => s.Product)
                        .Include(s => s.Warehouse)
                        .Where(s => s.Product.IsActive && s.Warehouse.IsActive),
                    query);

                q = ApplySort(q, query.SortBy, query.SortAscending);

                int totalCount = await q.CountAsync(ct);

                List<StockLevel> items = await q
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync(ct);

                return new PagedResult<StockLevel>
                {
                    Items = items,
                    TotalCount = totalCount,
                    Page = query.Page,
                    PageSize = query.PageSize
                };
            });

        public Task<StockLevel?> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId, CancellationToken ct = default) =>
            WithContextAsync(context =>
                All<StockLevel>(context)
                    .Include(s => s.Product)
                    .Include(s => s.Warehouse)
                    .FirstOrDefaultAsync(s => s.ProductId == productId && s.WarehouseId == warehouseId, ct));

        /// <summary>
        /// Atomically applies a quantity delta to the StockLevel row, retrying up to 5 times
        /// on DbUpdateConcurrencyException (concurrent xmin advance on the PostgreSQL row).
        /// Read and write share the same DbContext so the captured xmin is valid on UPDATE.
        /// ChangeTracker is cleared between retries to force a fresh SELECT and xmin read.
        /// Creates the row if it does not yet exist.
        /// </summary>
        public Task ApplyDeltaAsync(Guid productId, Guid warehouseId, decimal delta, bool updateRestockDate) =>
            WithContextAsync(async context =>
            {
                const int MaxRetries = 5;

                for (int attempt = 1; attempt <= MaxRetries; attempt++)
                {
                    try
                    {
                        StockLevel? stockLevel = await AllTracked<StockLevel>(context)
                            .FirstOrDefaultAsync(s => s.ProductId == productId && s.WarehouseId == warehouseId);

                        if (stockLevel == null)
                        {
                            stockLevel = new StockLevel
                            {
                                ProductId = productId,
                                WarehouseId = warehouseId,
                                Quantity = delta,
                                MinimumQuantity = 0,
                                ReorderPoint = 0,
                                LastRestockedAt = DateTime.UtcNow
                            };
                            context.StockLevels.Add(stockLevel);
                        }
                        else
                        {
                            stockLevel.Quantity += delta;
                            if (updateRestockDate)
                                stockLevel.LastRestockedAt = DateTime.UtcNow;
                            context.StockLevels.Update(stockLevel);
                        }

                        await SaveAsync(context);
                        return;
                    }
                    catch (DbUpdateConcurrencyException) when (attempt < MaxRetries)
                    {
                        // A concurrent write advanced xmin between SELECT and UPDATE.
                        // Clear stale tracked state so the next iteration re-reads a fresh xmin.
                        context.ChangeTracker.Clear();
                        await Task.Delay(TimeSpan.FromMilliseconds(20 * attempt));
                    }
                }

                throw new InvalidOperationException(
                    $"Failed to update stock for product {productId} in warehouse {warehouseId} " +
                    $"after {MaxRetries} attempts due to concurrent modifications. Please retry the operation.");
            });

        public Task<IEnumerable<StockLevel>> GetByProductIdAsync(Guid productId, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<StockLevel>)await All<StockLevel>(context)
                    .Where(s => s.ProductId == productId)
                    .Include(s => s.Product)
                    .Include(s => s.Warehouse)
                    .ToListAsync(ct);
            });

        public Task<IEnumerable<StockLevel>> GetByWarehouseIdAsync(Guid warehouseId, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<StockLevel>)await All<StockLevel>(context)
                    .Where(s => s.WarehouseId == warehouseId)
                    .Include(s => s.Product)
                    .Include(s => s.Warehouse)
                    .OrderBy(s => s.Product.Name)
                    .ToListAsync(ct);
            });

        public Task<IEnumerable<StockLevel>> GetLowStockItemsAsync(Guid? warehouseId = null, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                IQueryable<StockLevel> q = All<StockLevel>(context)
                    .Where(s => s.Product.IsActive &&
                                (s.Quantity == 0 ||
                                 (s.MinimumQuantity.HasValue && s.Quantity <= s.MinimumQuantity.Value)))
                    .Include(s => s.Product)
                    .Include(s => s.Warehouse);

                if (warehouseId.HasValue)
                    q = q.Where(s => s.WarehouseId == warehouseId.Value);

                return (IEnumerable<StockLevel>)await q.OrderBy(s => s.Product.Name).ToListAsync(ct);
            });

        public Task<StockLevel> CreateAsync(StockLevel stockLevel) =>
            WithContextAsync(async context =>
            {
                stockLevel.CreatedAt = DateTime.UtcNow;
                context.StockLevels.Add(stockLevel);
                await SaveAsync(context);
                return (await All<StockLevel>(context)
                    .Include(s => s.Product)
                    .Include(s => s.Warehouse)
                    .FirstOrDefaultAsync(s => s.Id == stockLevel.Id))!;
            });

        public Task<StockLevel> UpdateAsync(StockLevel stockLevel) =>
            WithContextAsync(async context =>
            {
                StockLevel tracked = await context.StockLevels.FindAsync(stockLevel.Id)
                    ?? throw new KeyNotFoundException($"StockLevel {stockLevel.Id} not found");
                context.Entry(tracked).CurrentValues.SetValues(stockLevel);
                await SaveAsync(context);
                return (await All<StockLevel>(context)
                    .Include(s => s.Product)
                    .Include(s => s.Warehouse)
                    .FirstOrDefaultAsync(s => s.Id == stockLevel.Id))!;
            });

        public Task<bool> DeleteAsync(Guid id) =>
            WithContextAsync(async context =>
            {
                StockLevel? stockLevel = await context.StockLevels.FindAsync(id);
                if (stockLevel == null)
                    return false;

                stockLevel.DeletedOn = DateTime.UtcNow;
                await SaveAsync(context);
                return true;
            });

        // ── Home dashboard aggregates ─────────────────────────────────────────────

        public Task<WarehouseStockSummaryResult> GetWarehouseStockSummaryAsync(
            Guid? warehouseId, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                IQueryable<StockLevel> q = All<StockLevel>(context)
                    .Where(s => s.Product.IsActive && s.Warehouse.IsActive);

                if (warehouseId.HasValue)
                    q = q.Where(s => s.WarehouseId == warehouseId.Value);

                // Total Products = catalog count of active products. Counting from StockLevel
                // would miss any product that has never had an inventory transaction
                // (StockLevel rows are only created on first ApplyDeltaAsync call).
                int totalProducts = await All<Product>(context)
                    .Where(p => p.IsActive)
                    .CountAsync(ct);

                int inStockCount = await q.Where(s => s.Quantity > 0).Select(s => s.ProductId).Distinct().CountAsync(ct);
                decimal totalStockValue = await q.SumAsync(s => s.Quantity * s.Product.SellingPrice, ct);
                int lowStockCount = await q.CountAsync(
                    s => s.MinimumQuantity.HasValue && s.Quantity > 0 && s.Quantity <= s.MinimumQuantity.Value, ct);
                int outOfStockCount = await q.Where(s => s.Quantity == 0).Select(s => s.ProductId).Distinct().CountAsync(ct);
                int warehouseCount = await q.Select(s => s.WarehouseId).Distinct().CountAsync(ct);

                return new WarehouseStockSummaryResult
                {
                    TotalProducts = totalProducts,
                    InStockCount = inStockCount,
                    TotalStockValue = totalStockValue,
                    LowStockCount = lowStockCount,
                    OutOfStockCount = outOfStockCount,
                    WarehouseCount = warehouseCount
                };
            });

        public Task<WarehouseDetailStatsResult> GetWarehouseDetailStatsAsync(
            Guid warehouseId, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                IQueryable<StockLevel> q = All<StockLevel>(context)
                    .Where(s => s.WarehouseId == warehouseId);

                int productCount = await q.CountAsync(ct);
                decimal totalValue = productCount > 0
                    ? await q.SumAsync(s => s.Quantity * s.Product.SellingPrice, ct)
                    : 0m;
                int lowStockCount = await q.CountAsync(
                    s => s.MinimumQuantity.HasValue && s.Quantity > 0 && s.Quantity <= s.MinimumQuantity.Value, ct);

                return new WarehouseDetailStatsResult(productCount, lowStockCount, totalValue);
            });

        public Task<IEnumerable<StockLevel>> GetStockAlertsAsync(
            Guid? warehouseId, int top, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                IQueryable<StockLevel> q = All<StockLevel>(context)
                    .Where(s => s.Product.IsActive && s.Warehouse.IsActive)
                    .Where(s => s.Quantity == 0 ||
                                (s.MinimumQuantity.HasValue && s.Quantity > 0 && s.Quantity <= s.MinimumQuantity.Value))
                    .Include(s => s.Product)
                    .Include(s => s.Warehouse);

                if (warehouseId.HasValue)
                    q = q.Where(s => s.WarehouseId == warehouseId.Value);

                return (IEnumerable<StockLevel>)await q
                    .OrderBy(s => s.Quantity)
                    .Take(top)
                    .ToListAsync(ct);
            });

        public Task<IEnumerable<StockLevel>> GetTopByStockAsync(
            Guid warehouseId, int top, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<StockLevel>)await All<StockLevel>(context)
                    .Where(s => s.WarehouseId == warehouseId &&
                                s.Product.IsActive &&
                                s.Warehouse.IsActive)
                    .Include(s => s.Product)
                    .Include(s => s.Warehouse)
                    .OrderByDescending(s => s.Quantity)
                    .Take(top)
                    .ToListAsync(ct);
            });

        private static IQueryable<StockLevel> ApplyFilters(IQueryable<StockLevel> q, GetStockQuery query)
        {
            if (query.WarehouseId.HasValue)
                q = q.Where(s => s.WarehouseId == query.WarehouseId.Value);

            if (query.ProductId.HasValue)
                q = q.Where(s => s.ProductId == query.ProductId.Value);

            if (query.IsLowStock == true)
                q = q.Where(s => s.Quantity == 0 ||
                    (s.MinimumQuantity.HasValue && s.Quantity <= s.MinimumQuantity.Value));

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                string search = $"%{query.Search}%";
                q = q.Where(s =>
                    EF.Functions.ILike(s.Product.Name, search) ||
                    EF.Functions.ILike(s.Product.Code, search) ||
                    EF.Functions.ILike(s.Warehouse.Name, search));
            }

            return q;
        }

        private static IQueryable<StockLevel> ApplySort(IQueryable<StockLevel> q, string? sortBy, bool ascending)
            => sortBy switch
            {
                "ProductCode" => ascending ? q.OrderBy(s => s.Product.Code) : q.OrderByDescending(s => s.Product.Code),
                "ProductName" => ascending ? q.OrderBy(s => s.Product.Name) : q.OrderByDescending(s => s.Product.Name),
                "WarehouseName" => ascending ? q.OrderBy(s => s.Warehouse.Name) : q.OrderByDescending(s => s.Warehouse.Name),
                "Quantity" => ascending ? q.OrderBy(s => s.Quantity) : q.OrderByDescending(s => s.Quantity),
                "LastRestockedAt" => ascending ? q.OrderBy(s => s.LastRestockedAt) : q.OrderByDescending(s => s.LastRestockedAt),
                _ => q.OrderBy(s => s.Product.Name)
            };
    }
}