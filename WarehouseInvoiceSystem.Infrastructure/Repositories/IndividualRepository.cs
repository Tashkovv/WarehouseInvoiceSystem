namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Interfaces;
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
    }
}