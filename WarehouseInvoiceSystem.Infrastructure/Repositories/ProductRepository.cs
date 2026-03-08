namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;
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

        public async Task<PagedResult<Product>> GetPagedAsync(GetProductsQuery query)
        {
            IQueryable<Product> q = context.Products
                .Where(p => p.DeletedOn == null);

            if (query.IsActive.HasValue)
                q = q.Where(p => p.IsActive == query.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
                q = q.Where(p => p.Name.Contains(query.Search) ||
                                 p.Code.Contains(query.Search));

            q = query.SortBy switch
            {
                "Code" => query.SortAscending ? q.OrderBy(p => p.Code) : q.OrderByDescending(p => p.Code),
                "Name" => query.SortAscending ? q.OrderBy(p => p.Name) : q.OrderByDescending(p => p.Name),
                "DefaultPrice" => query.SortAscending ? q.OrderBy(p => p.DefaultPrice) : q.OrderByDescending(p => p.DefaultPrice),
                _ => q.OrderBy(p => p.Name)
            };

            int totalCount = await q.CountAsync();

            List<Product> items = await q
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<Product>
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        public async Task<IEnumerable<Product>> GetByIdsAsync(List<Guid> ids)
        {
            return await context.Products
                .Where(p => p.DeletedOn == null &&
                            ids.Contains(p.Id))
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

        public async Task<bool> AllExistAsync(IEnumerable<Guid> ids)
        {
            var idList = ids.Distinct().ToList();
            int found = await context.Products
                .Where(p => p.DeletedOn == null && idList.Contains(p.Id))
                .CountAsync();
            return found == idList.Count;
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