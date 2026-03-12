namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;
    using WarehouseInvoiceSystem.Infrastructure.Data;

    public class IndividualRepository(IDbContextFactory<ApplicationDbContext> factory)
        : BaseRepository(factory), IIndividualRepository
    {
        public Task<IEnumerable<Individual>> GetAllAsync() =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<Individual>)await All<Individual>(context)
                    .OrderBy(i => i.LastName)
                    .ThenBy(i => i.FirstName)
                    .ToListAsync();
            });

        public Task<PagedResult<Individual>> GetPagedAsync(GetIndividualsQuery query) =>
            WithContextAsync(async context =>
            {
                IQueryable<Individual> q = ApplyFilters(All<Individual>(context), query);
                q = ApplySort(q, query.SortBy, query.SortAscending);

                int totalCount = await q.CountAsync();

                List<Individual> items = await q
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync();

                return new PagedResult<Individual>
                {
                    Items = items,
                    TotalCount = totalCount,
                    Page = query.Page,
                    PageSize = query.PageSize
                };
            });

        public Task<IEnumerable<Individual>> GetActiveIndividualsAsync() =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<Individual>)await All<Individual>(context)
                    .Where(i => i.IsActive)
                    .OrderBy(i => i.LastName)
                    .ThenBy(i => i.FirstName)
                    .ToListAsync();
            });

        public Task<Individual?> GetByIdAsync(Guid id) =>
            WithContextAsync(context =>
                All<Individual>(context).FirstOrDefaultAsync(i => i.Id == id));

        public Task<Individual?> GetByIdentificationNumberAsync(string identificationNumber) =>
            WithContextAsync(context =>
                All<Individual>(context).FirstOrDefaultAsync(i => i.IdentificationNumber == identificationNumber));

        public Task<bool> ExistsAsync(Guid id) =>
            WithContextAsync(context =>
                All<Individual>(context).AnyAsync(i => i.Id == id));

        public Task<bool> IdentificationNumberExistsAsync(string identificationNumber, Guid? excludeId = null) =>
            WithContextAsync(context =>
            {
                IQueryable<Individual> q = All<Individual>(context)
                    .Where(i => i.IdentificationNumber == identificationNumber);

                if (excludeId.HasValue)
                    q = q.Where(i => i.Id != excludeId.Value);

                return q.AnyAsync();
            });

        public Task<Individual> CreateAsync(Individual individual) =>
            WithContextAsync(async context =>
            {
                individual.CreatedAt = DateTime.UtcNow;
                context.Individuals.Add(individual);
                await SaveAsync(context);
                return individual;
            });

        public Task<Individual> UpdateAsync(Individual individual) =>
            WithContextAsync(async context =>
            {
                context.Individuals.Update(individual);
                await SaveAsync(context);
                return individual;
            });

        public Task<bool> DeleteAsync(Guid id) =>
            WithContextAsync(async context =>
            {
                Individual? individual = await context.Individuals.FindAsync(id);
                if (individual == null)
                    return false;

                individual.DeletedOn = DateTime.UtcNow;
                await SaveAsync(context);
                return true;
            });

        public Task<bool> SetActiveStatusAsync(Guid id, bool isActive) =>
            WithContextAsync(async context =>
            {
                Individual? individual = await AllTracked<Individual>(context)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (individual is null)
                    return false;

                individual.IsActive = isActive;
                await SaveAsync(context);
                return true;
            });

        private static IQueryable<Individual> ApplyFilters(IQueryable<Individual> q, GetIndividualsQuery query)
        {
            if (query.IsActive.HasValue)
                q = q.Where(i => i.IsActive == query.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                string search = $"%{query.Search}%";
                q = q.Where(i =>
                    EF.Functions.ILike(i.FirstName, search) ||
                    EF.Functions.ILike(i.LastName, search) ||
                    EF.Functions.ILike(i.IdentificationNumber, search));
            }

            return q;
        }

        private static IQueryable<Individual> ApplySort(IQueryable<Individual> q, string? sortBy, bool ascending)
            => sortBy switch
            {
                "FirstName" => ascending ? q.OrderBy(i => i.FirstName) : q.OrderByDescending(i => i.FirstName),
                "LastName" => ascending ? q.OrderBy(i => i.LastName) : q.OrderByDescending(i => i.LastName),
                "IdentificationNumber" => ascending ? q.OrderBy(i => i.IdentificationNumber) : q.OrderByDescending(i => i.IdentificationNumber),
                _ => q.OrderBy(i => i.LastName)
            };
    }
}