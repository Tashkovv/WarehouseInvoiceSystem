namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Infrastructure.Data;

    public class WarehouseRepository(ApplicationDbContext context) : IWarehouseRepository
    {
        public async Task<IEnumerable<Warehouse>> GetAllAsync()
        {
            return await context.Warehouses
                .Where(w => w.DeletedOn == null)
                .OrderBy(w => w.Name)
                .ToListAsync();
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
                .Where(w => w.DeletedOn == null && w.IsDefault)
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

        public async Task<bool> DeleteAsync(Guid id)
        {
            Warehouse? warehouse = await context.Warehouses.FindAsync(id);
            if (warehouse == null)
                return false;

            warehouse.DeletedOn = DateTime.UtcNow;
            await context.SaveChangesAsync();

            return true;
        }
    }
}