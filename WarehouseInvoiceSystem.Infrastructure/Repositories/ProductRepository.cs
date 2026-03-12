namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;
    using WarehouseInvoiceSystem.Infrastructure.Data;

    public class ProductRepository(IDbContextFactory<ApplicationDbContext> factory)
        : BaseRepository(factory), IProductRepository
    {
        public Task<IEnumerable<Product>> GetAllAsync() =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<Product>)await All<Product>(context)
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            });

        public Task<PagedResult<Product>> GetPagedAsync(GetProductsQuery query) =>
            WithContextAsync(async context =>
            {
                IQueryable<Product> q = ApplyFilters(All<Product>(context), query);
                q = ApplySort(q, query.SortBy, query.SortAscending);

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
            });

        public Task<IEnumerable<Product>> GetByIdsAsync(List<Guid> ids) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<Product>)await All<Product>(context)
                    .Where(p => ids.Contains(p.Id))
                    .ToListAsync();
            });

        public Task<IEnumerable<Product>> GetActiveProductsAsync() =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<Product>)await All<Product>(context)
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            });

        public Task<Product?> GetByIdAsync(Guid id) =>
            WithContextAsync(context =>
                All<Product>(context)
                    .Include(p => p.StockLevels)
                    .FirstOrDefaultAsync(p => p.Id == id));

        public Task<Product?> GetByCodeAsync(string code) =>
            WithContextAsync(context =>
                All<Product>(context).FirstOrDefaultAsync(p => p.Code == code));

        public Task<bool> ExistsAsync(Guid id) =>
            WithContextAsync(context =>
                All<Product>(context).AnyAsync(p => p.Id == id));

        public Task<bool> AllExistAsync(IEnumerable<Guid> ids) =>
            WithContextAsync(async context =>
            {
                List<Guid> idList = ids.Distinct().ToList();
                int found = await All<Product>(context)
                    .Where(p => idList.Contains(p.Id))
                    .CountAsync();
                return found == idList.Count;
            });

        public Task<bool> CodeExistsAsync(string code, Guid? excludeId = null) =>
            WithContextAsync(context =>
            {
                IQueryable<Product> q = All<Product>(context).Where(p => p.Code == code);

                if (excludeId.HasValue)
                    q = q.Where(p => p.Id != excludeId.Value);

                return q.AnyAsync();
            });

        public Task<Product> CreateAsync(Product product) =>
            WithContextAsync(async context =>
            {
                product.CreatedAt = DateTime.UtcNow;
                context.Products.Add(product);
                await SaveAsync(context);
                return product;
            });

        public Task<Product> UpdateAsync(Product product) =>
            WithContextAsync(async context =>
            {
                context.Products.Update(product);
                await SaveAsync(context);
                return product;
            });

        public Task<bool> SetActiveStatusAsync(Guid id, bool isActive) =>
            WithContextAsync(async context =>
            {
                Product? product = await AllTracked<Product>(context)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product is null)
                    return false;

                product.IsActive = isActive;
                await SaveAsync(context);
                return true;
            });

        public Task<bool> DeleteAsync(Guid id) =>
            WithContextAsync(async context =>
            {
                Product? product = await context.Products.FindAsync(id);
                if (product == null)
                    return false;

                product.DeletedOn = DateTime.UtcNow;
                await SaveAsync(context);
                return true;
            });

        private static IQueryable<Product> ApplyFilters(IQueryable<Product> q, GetProductsQuery query)
        {
            if (query.IsActive.HasValue)
                q = q.Where(p => p.IsActive == query.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                string search = $"%{query.Search}%";
                q = q.Where(p =>
                    EF.Functions.ILike(p.Name, search) ||
                    EF.Functions.ILike(p.Code, search));
            }

            return q;
        }

        private static IQueryable<Product> ApplySort(IQueryable<Product> q, string? sortBy, bool ascending)
            => sortBy switch
            {
                "Code" => ascending ? q.OrderBy(p => p.Code) : q.OrderByDescending(p => p.Code),
                "Name" => ascending ? q.OrderBy(p => p.Name) : q.OrderByDescending(p => p.Name),
                "DefaultPrice" => ascending ? q.OrderBy(p => p.DefaultPrice) : q.OrderByDescending(p => p.DefaultPrice),
                _ => q.OrderBy(p => p.Name)
            };
    }
}