namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Application.DTOs.Individual;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public interface IIndividualService
    {
        Task<IEnumerable<IndividualDto>> GetAllIndividualsAsync(CancellationToken ct = default);
        Task<PagedResult<IndividualDto>> GetPagedAsync(GetIndividualsQuery query, CancellationToken ct = default);
        Task<IEnumerable<IndividualDto>> GetActiveIndividualsAsync(CancellationToken ct = default);
        Task<IndividualDto?> GetIndividualByIdAsync(Guid id, CancellationToken ct = default);
        Task<IndividualDto?> GetIndividualByIdentificationNumberAsync(string identificationNumber, CancellationToken ct = default);
        Task<IndividualAnalyticsDto> GetIndividualAnalyticsAsync(Guid individualId, CancellationToken ct = default);
        Task CreateIndividualAsync(CreateIndividualDto createDto);
        Task UpdateIndividualAsync(Guid id, UpdateIndividualDto updateDto);
        Task<bool> DeleteIndividualAsync(Guid id);
        Task<bool> SetActiveStatusAsync(Guid id, bool isActive);
    }
}