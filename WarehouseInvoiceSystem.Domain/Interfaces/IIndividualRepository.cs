namespace WarehouseInvoiceSystem.Domain.Interfaces
{
    using WarehouseInvoiceSystem.Domain.Entities;

    public interface IIndividualRepository
    {
        Task<IEnumerable<Individual>> GetAllAsync();
        Task<IEnumerable<Individual>> GetActiveIndividualsAsync();
        Task<Individual?> GetByIdAsync(Guid id);
        Task<Individual?> GetByIdentificationNumberAsync(string identificationNumber);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> IdentificationNumberExistsAsync(string identificationNumber, Guid? excludeId = null);
        Task<Individual> CreateAsync(Individual individual);
        Task<Individual> UpdateAsync(Individual individual);
        Task<bool> DeleteAsync(Guid id);
    }
}