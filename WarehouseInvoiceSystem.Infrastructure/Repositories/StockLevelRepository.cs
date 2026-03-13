namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;
    using WarehouseInvoiceSystem.Infrastructure.Data;

    public class StockLevelRepository(IDbContextFactory<ApplicationDbContext> factory)
        : BaseRepository(factory), IStockLevelRepository
    {
        public Task<IEnumerable<StockLevel>> GetAllStockLevelAsync(CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<StockLevel>)await All<StockLevel>(context)
                    .Where(s => s.Product.IsActive && s.Warehouse.IsActive)
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
        /// Reads and writes the StockLevel row inside the same DbContext so the xmin
        /// concurrency token captured on the SELECT is still valid when EF issues the
        /// UPDATE — this is the fix for the split-context DbUpdateConcurrencyException.
        /// Creates the row if it does not yet exist.
        /// </summary>
        public Task ApplyDeltaAsync(Guid productId, Guid warehouseId, decimal delta, bool updateRestockDate) =>
            WithContextAsync(async context =>
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
            });

        public Task<IEnumerable<StockLevel>> GetByProductIdAsync(Guid productId, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<StockLevel>)await All<StockLevel>(context)
                    .Where(s => s.ProductId == productId)
                    .Include(s => s.Warehouse)
                    .ToListAsync(ct);
            });

        public Task<IEnumerable<StockLevel>> GetByWarehouseIdAsync(Guid warehouseId, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<StockLevel>)await All<StockLevel>(context)
                    .Where(s => s.WarehouseId == warehouseId)
                    .Include(s => s.Product)
                    .OrderBy(s => s.Product.Name)
                    .ToListAsync(ct);
            });

        public Task<IEnumerable<StockLevel>> GetLowStockItemsAsync(Guid? warehouseId = null, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                IQueryable<StockLevel> q = All<StockLevel>(context)
                    .Where(s => s.MinimumQuantity.HasValue &&
                                s.Quantity <= s.MinimumQuantity.Value &&
                                s.Product.IsActive)
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
                return stockLevel;
            });

        public Task<StockLevel> UpdateAsync(StockLevel stockLevel) =>
            WithContextAsync(async context =>
            {
                context.StockLevels.Update(stockLevel);
                await SaveAsync(context);
                return stockLevel;
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

        private static IQueryable<StockLevel> ApplyFilters(IQueryable<StockLevel> q, GetStockQuery query)
        {
            if (query.WarehouseId.HasValue)
                q = q.Where(s => s.WarehouseId == query.WarehouseId.Value);

            if (query.ProductId.HasValue)
                q = q.Where(s => s.ProductId == query.ProductId.Value);

            if (query.IsLowStock == true)
                q = q.Where(s => s.MinimumQuantity.HasValue && s.Quantity <= s.MinimumQuantity.Value);

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