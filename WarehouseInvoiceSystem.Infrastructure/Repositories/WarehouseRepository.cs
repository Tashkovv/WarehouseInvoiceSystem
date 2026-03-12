namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;
    using WarehouseInvoiceSystem.Infrastructure.Data;

    public class WarehouseRepository(IDbContextFactory<ApplicationDbContext> factory)
        : BaseRepository(factory), IWarehouseRepository
    {
        public Task<IEnumerable<Warehouse>> GetAllAsync() =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<Warehouse>)await All<Warehouse>(context)
                    .Where(w => w.IsActive)
                    .OrderBy(w => w.Name)
                    .ToListAsync();
            });

        public Task<PagedResult<Warehouse>> GetPagedAsync(GetWarehousesQuery query) =>
            WithContextAsync(async context =>
            {
                IQueryable<Warehouse> q = ApplyFilters(All<Warehouse>(context), query);
                q = ApplySort(q, query.SortBy, query.SortAscending);

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
            });

        public Task<bool> HasProductsAsync(Guid id) =>
            WithContextAsync(context =>
                context.StockLevels.AnyAsync(s => s.WarehouseId == id && s.Quantity > 0));

        public Task<Warehouse?> GetByIdAsync(Guid id) =>
            WithContextAsync(context =>
                All<Warehouse>(context).FirstOrDefaultAsync(w => w.Id == id));

        public Task<Warehouse?> GetDefaultWarehouseAsync() =>
            WithContextAsync(context =>
                All<Warehouse>(context)
                    .Where(w => w.IsActive && w.IsDefault)
                    .FirstOrDefaultAsync());

        public Task<bool> ExistsAsync(Guid id) =>
            WithContextAsync(context =>
                All<Warehouse>(context).AnyAsync(w => w.Id == id));

        public Task<Warehouse> CreateAsync(Warehouse warehouse) =>
            WithContextAsync(async context =>
            {
                warehouse.CreatedAt = DateTime.UtcNow;
                context.Warehouses.Add(warehouse);
                await SaveAsync(context);
                return warehouse;
            });

        public Task<Warehouse> UpdateAsync(Warehouse warehouse) =>
            WithContextAsync(async context =>
            {
                context.Warehouses.Update(warehouse);
                await SaveAsync(context);
                return warehouse;
            });

        public Task<bool> SetActiveStatusAsync(Guid id, bool isActive) =>
            WithContextAsync(async context =>
            {
                Warehouse? warehouse = await AllTracked<Warehouse>(context)
                    .FirstOrDefaultAsync(w => w.Id == id);

                if (warehouse == null)
                    return false;

                warehouse.IsActive = isActive;
                await SaveAsync(context);
                return true;
            });

        private static IQueryable<Warehouse> ApplyFilters(IQueryable<Warehouse> q, GetWarehousesQuery query)
        {
            if (query.IsActive.HasValue)
                q = q.Where(w => w.IsActive == query.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                string search = $"%{query.Search}%";
                q = q.Where(w =>
                    EF.Functions.ILike(w.Name, search) ||
                    (w.Address != null && EF.Functions.ILike(w.Address, search)));
            }

            return q;
        }

        private static IQueryable<Warehouse> ApplySort(IQueryable<Warehouse> q, string? sortBy, bool ascending)
            => sortBy switch
            {
                "Name" => ascending ? q.OrderBy(w => w.Name) : q.OrderByDescending(w => w.Name),
                "Address" => ascending ? q.OrderBy(w => w.Address) : q.OrderByDescending(w => w.Address),
                _ => q.OrderBy(w => w.Name)
            };
    }
}