namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;
    using WarehouseInvoiceSystem.Infrastructure.Data;

    public class StockLevelRepository(ApplicationDbContext context) : IStockLevelRepository
    {
        public async Task<IEnumerable<StockLevel>> GetAllStockLevelAsync()
        {
            return await context.StockLevels
                .Where(s => s.DeletedOn == null)
                .Include(s => s.Warehouse)
                .ToListAsync();
        }

        public async Task<PagedResult<StockLevel>> GetPagedAsync(GetStockQuery query)
        {
            IQueryable<StockLevel> q = context.StockLevels
                .Include(s => s.Product)
                .Include(s => s.Warehouse);

            if (query.WarehouseId.HasValue)
                q = q.Where(s => s.WarehouseId == query.WarehouseId.Value);

            if (query.ProductId.HasValue)
                q = q.Where(s => s.ProductId == query.ProductId.Value);

            if (query.IsLowStock == true)
                q = q.Where(s => s.MinimumQuantity.HasValue && s.Quantity <= s.MinimumQuantity.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
                q = q.Where(s => s.Product.Name.Contains(query.Search) ||
                                 s.Product.Code.Contains(query.Search) ||
                                 s.Warehouse.Name.Contains(query.Search));

            q = ApplySort(q, query.SortBy, query.SortAscending);

            int totalCount = await q.CountAsync();

            List<StockLevel> items = await q
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<StockLevel>
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        public async Task<StockLevel?> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId)
        {
            return await context.StockLevels
                .Where(s => s.DeletedOn == null)
                .Include(s => s.Product)
                .Include(s => s.Warehouse)
                .FirstOrDefaultAsync(s => s.ProductId == productId && s.WarehouseId == warehouseId);
        }

        public async Task<IEnumerable<StockLevel>> GetByProductIdAsync(Guid productId)
        {
            return await context.StockLevels
                .Where(s => s.DeletedOn == null && s.ProductId == productId)
                .Include(s => s.Warehouse)
                .ToListAsync();
        }

        public async Task<IEnumerable<StockLevel>> GetByWarehouseIdAsync(Guid warehouseId)
        {
            return await context.StockLevels
                .Where(s => s.DeletedOn == null && s.WarehouseId == warehouseId)
                .Include(s => s.Product)
                .OrderBy(s => s.Product.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<StockLevel>> GetLowStockItemsAsync(Guid? warehouseId = null)
        {
            IQueryable<StockLevel> query = context.StockLevels
                .Where(s => s.DeletedOn == null && s.MinimumQuantity.HasValue && s.Quantity <= s.MinimumQuantity.Value)
                .Include(s => s.Product)
                .Include(s => s.Warehouse);

            if (warehouseId.HasValue)
            {
                query = query.Where(s => s.WarehouseId == warehouseId.Value);
            }

            return await query.OrderBy(s => s.Product.Name).ToListAsync();
        }

        public async Task<StockLevel> CreateAsync(StockLevel stockLevel)
        {
            stockLevel.CreatedAt = DateTime.UtcNow;

            context.StockLevels.Add(stockLevel);
            await context.SaveChangesAsync();

            return stockLevel;
        }

        public async Task<StockLevel> UpdateAsync(StockLevel stockLevel)
        {
            context.StockLevels.Update(stockLevel);
            await context.SaveChangesAsync();

            return stockLevel;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            StockLevel? stockLevel = await context.StockLevels.FindAsync(id);
            if (stockLevel == null)
                return false;

            stockLevel.DeletedOn = DateTime.UtcNow;
            await context.SaveChangesAsync();

            return true;
        }

        private static IQueryable<StockLevel> ApplySort(IQueryable<StockLevel> q, string? sortBy, bool ascending)
            => sortBy switch
            {
                "ProductCode" => ascending ? q.OrderBy(s => s.Product.Code) : q.OrderByDescending(s => s.Product.Code),
                "ProductName" => ascending  ? q.OrderBy(s => s.Product.Name) : q.OrderByDescending(s => s.Product.Name),
                "WarehouseName" => ascending ? q.OrderBy(s => s.Warehouse.Name) : q.OrderByDescending(s => s.Warehouse.Name),
                "Quantity" => ascending ? q.OrderBy(s => s.Quantity) : q.OrderByDescending(s => s.Quantity),
                "LastRestockedAt" => ascending ? q.OrderBy(s => s.LastRestockedAt) : q.OrderByDescending(s => s.LastRestockedAt),
                _ => q.OrderBy(s => s.Product.Name)
            };
    }
}