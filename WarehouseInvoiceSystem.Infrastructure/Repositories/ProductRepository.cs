namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Infrastructure.Data;

    public class ProductRepository(ApplicationDbContext context) : IProductRepository
    {
        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await context.Products
                .Where(p => p.DeletedOn == null)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetActiveProductsAsync()
        {
            return await context.Products
                .Where(p => p.DeletedOn == null && p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(Guid id)
        {
            return await context.Products
                .Where(p => p.DeletedOn == null)
                .Include(p => p.StockLevels)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Product?> GetByCodeAsync(string code)
        {
            return await context.Products
                .Where(p => p.DeletedOn == null)
                .FirstOrDefaultAsync(p => p.Code == code);
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await context.Products
                .AnyAsync(p => p.Id == id && p.DeletedOn == null);
        }

        public async Task<bool> CodeExistsAsync(string code, Guid? excludeId = null)
        {
            IQueryable<Product> query = context.Products
                .Where(p => p.DeletedOn == null && p.Code == code);

            if (excludeId.HasValue)
            {
                query = query.Where(p => p.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<Product> CreateAsync(Product product)
        {
            product.CreatedAt = DateTime.UtcNow;

            context.Products.Add(product);
            await context.SaveChangesAsync();

            return product;
        }

        public async Task<Product> UpdateAsync(Product product)
        {
            context.Products.Update(product);
            await context.SaveChangesAsync();

            return product;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            Product? product = await context.Products.FindAsync(id);
            if (product == null)
                return false;

            product.DeletedOn = DateTime.UtcNow;
            await context.SaveChangesAsync();

            return true;
        }
    }
}