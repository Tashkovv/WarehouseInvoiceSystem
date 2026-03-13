namespace WarehouseInvoiceSystem.Domain.Interfaces
{
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public interface IIndividualRepository
    {
        Task<IEnumerable<Individual>> GetAllAsync();
        Task<PagedResult<Individual>> GetPagedAsync(GetIndividualsQuery query);
        Task<IEnumerable<Individual>> GetActiveIndividualsAsync();
        Task<Individual?> GetByIdAsync(Guid id);
        Task<Individual?> GetByIdentificationNumberAsync(string identificationNumber);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> IdentificationNumberExistsAsync(string identificationNumber, Guid? excludeId = null);
        Task CreateAsync(Individual individual);
        Task UpdateAsync(Individual individual);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> SetActiveStatusAsync(Guid id, bool isActive);
    }
}