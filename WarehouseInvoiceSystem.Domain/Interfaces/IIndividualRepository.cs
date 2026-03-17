namespace WarehouseInvoiceSystem.Domain.Interfaces
{
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public interface IIndividualRepository
    {
        Task<IEnumerable<Individual>> GetAllAsync(CancellationToken ct = default);
        Task<PagedResult<Individual>> GetPagedAsync(GetIndividualsQuery query, CancellationToken ct = default);

        Task<IEnumerable<Individual>> GetActiveIndividualsAsync(CancellationToken ct = default);
        Task<Individual?> GetByIdAsync(Guid id, CancellationToken ct = default);

        Task<Individual?> GetByIdentificationNumberAsync(string identificationNumber, CancellationToken ct = default);

        Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

        Task<bool> IdentificationNumberExistsAsync(string identificationNumber, Guid? excludeId = null);
        Task CreateAsync(Individual individual);
        Task UpdateAsync(Individual individual);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> SetActiveStatusAsync(Guid id, bool isActive);
        Task<(int total, int active)> GetCountsAsync(CancellationToken ct = default);
    }
}