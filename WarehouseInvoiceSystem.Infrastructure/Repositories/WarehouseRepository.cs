namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;
    using WarehouseInvoiceSystem.Infrastructure.Data;

    public class WarehouseRepository(ApplicationDbContext context) : IWarehouseRepository
    {
        public async Task<IEnumerable<Warehouse>> GetAllAsync()
        {
            return await context.Warehouses
                .Where(w => w.DeletedOn == null && w.IsActive)
                .OrderBy(w => w.Name)
                .ToListAsync();
        }

        public async Task<PagedResult<Warehouse>> GetPagedAsync(GetWarehousesQuery query)
        {
            IQueryable<Warehouse> q = context.Warehouses
                .Where(w => w.DeletedOn == null);

            if (query.IsActive.HasValue)
                q = q.Where(w => w.IsActive == query.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
                q = q.Where(w => w.Name.Contains(query.Search) ||
                                 (w.Address != null && w.Address.Contains(query.Search)));

            q = query.SortBy switch
            {
                "Name" => query.SortAscending ? q.OrderBy(w => w.Name) : q.OrderByDescending(w => w.Name),
                "Address" => query.SortAscending ? q.OrderBy(w => w.Address) : q.OrderByDescending(w => w.Address),
                _ => q.OrderBy(w => w.Name)
            };

            int totalCount = await q.CountAsync();

            List<Warehouse> items = await q
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<Warehouse>
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        public async Task<bool> HasProductsAsync(Guid id)
        {
            return await context.StockLevels
                .AnyAsync(s => s.WarehouseId == id && s.Quantity > 0);
        }

        public async Task<Warehouse?> GetByIdAsync(Guid id)
        {
            return await context.Warehouses
                .Where(w => w.DeletedOn == null)
                .FirstOrDefaultAsync(w => w.Id == id);
        }

        public async Task<Warehouse?> GetDefaultWarehouseAsync()
        {
            return await context.Warehouses
                .Where(w => w.DeletedOn == null && w.IsActive && w.IsDefault)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await context.Warehouses
                .AnyAsync(w => w.Id == id && w.DeletedOn == null);
        }

        public async Task<Warehouse> CreateAsync(Warehouse warehouse)
        {
            warehouse.CreatedAt = DateTime.UtcNow;

            context.Warehouses.Add(warehouse);
            await context.SaveChangesAsync();

            return warehouse;
        }

        public async Task<Warehouse> UpdateAsync(Warehouse warehouse)
        {
            context.Warehouses.Update(warehouse);
            await context.SaveChangesAsync();

            return warehouse;
        }

        public async Task<bool> SetActiveStatusAsync(Guid id, bool isActive)
        {
            Warehouse? warehouse = await context.Warehouses
                .Where(w => w.DeletedOn == null)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (warehouse == null)
                return false;

            warehouse.IsActive = isActive;
            await context.SaveChangesAsync();

            return true;
        }
    }
}