namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;
    using WarehouseInvoiceSystem.Infrastructure.Data;

    public class IndividualRepository(ApplicationDbContext context) : IIndividualRepository
    {
        public async Task<IEnumerable<Individual>> GetAllAsync()
        {
            return await context.Individuals
                .Where(i => i.DeletedOn == null)
                .OrderBy(i => i.LastName)
                .ThenBy(i => i.FirstName)
                .ToListAsync();
        }

        public async Task<PagedResult<Individual>> GetPagedAsync(GetIndividualsQuery query)
        {
            IQueryable<Individual> q = context.Individuals
                .Where(i => i.DeletedOn == null);

            if (query.IsActive.HasValue)
                q = q.Where(i => i.IsActive == query.IsActive.Value);

            string search = query.Search?.ToLower() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(query.Search))
                q = q.Where(i => i.FirstName.ToLower().Contains(search) ||
                                 i.LastName.ToLower().Contains(search) ||
                                 i.IdentificationNumber.Contains(search));

            q = query.SortBy switch
            {
                "FirstName" => query.SortAscending ? q.OrderBy(i => i.FirstName) : q.OrderByDescending(i => i.FirstName),
                "LastName" => query.SortAscending ? q.OrderBy(i => i.LastName) : q.OrderByDescending(i => i.LastName),
                "IdentificationNumber" => query.SortAscending ? q.OrderBy(i => i.IdentificationNumber) : q.OrderByDescending(i => i.IdentificationNumber),
                _ => q.OrderBy(i => i.LastName)
            };

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
        }

        public async Task<IEnumerable<Individual>> GetActiveIndividualsAsync()
        {
            return await context.Individuals
                .Where(i => i.DeletedOn == null && i.IsActive)
                .OrderBy(i => i.LastName)
                .ThenBy(i => i.FirstName)
                .ToListAsync();
        }

        public async Task<Individual?> GetByIdAsync(Guid id)
        {
            return await context.Individuals
                .Where(i => i.DeletedOn == null)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<Individual?> GetByIdentificationNumberAsync(string identificationNumber)
        {
            return await context.Individuals
                .Where(i => i.DeletedOn == null)
                .FirstOrDefaultAsync(i => i.IdentificationNumber == identificationNumber);
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await context.Individuals
                .AnyAsync(i => i.Id == id && i.DeletedOn == null);
        }

        public async Task<bool> IdentificationNumberExistsAsync(string identificationNumber, Guid? excludeId = null)
        {
            IQueryable<Individual> query = context.Individuals
                .Where(i => i.DeletedOn == null && i.IdentificationNumber == identificationNumber);

            if (excludeId.HasValue)
            {
                query = query.Where(i => i.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<Individual> CreateAsync(Individual individual)
        {
            individual.CreatedAt = DateTime.UtcNow;

            context.Individuals.Add(individual);
            await context.SaveChangesAsync();

            return individual;
        }

        public async Task<Individual> UpdateAsync(Individual individual)
        {
            context.Individuals.Update(individual);
            await context.SaveChangesAsync();

            return individual;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            Individual? individual = await context.Individuals.FindAsync(id);
            if (individual == null)
                return false;

            individual.DeletedOn = DateTime.UtcNow;
            await context.SaveChangesAsync();

            return true;
        }
        public async Task<bool> SetActiveStatusAsync(Guid id, bool isActive)
        {
            Individual? individual = await context.Individuals
                .Where(i => i.DeletedOn == null)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (individual is null)
                return false;

            individual.IsActive = isActive;
            await context.SaveChangesAsync();

            return true;
        }
    }
}