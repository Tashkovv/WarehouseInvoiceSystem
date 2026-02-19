namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Infrastructure.Data;

    public class StockLevelRepository(ApplicationDbContext context) : IStockLevelRepository
    {
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
    }
}