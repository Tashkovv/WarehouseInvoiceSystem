namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Application.DTOs.Individual;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public interface IIndividualService
    {
        Task<IEnumerable<IndividualDto>> GetAllIndividualsAsync();
        Task<PagedResult<IndividualDto>> GetPagedAsync(GetIndividualsQuery query);
        Task<IEnumerable<IndividualDto>> GetActiveIndividualsAsync();
        Task<IndividualDto?> GetIndividualByIdAsync(Guid id);
        Task<IndividualDto?> GetIndividualByIdentificationNumberAsync(string identificationNumber);
        Task<IndividualAnalyticsDto> GetIndividualAnalyticsAsync(Guid individualId);
        Task CreateIndividualAsync(CreateIndividualDto createDto);
        Task UpdateIndividualAsync(Guid id, UpdateIndividualDto updateDto);
        Task<bool> DeleteIndividualAsync(Guid id);
        Task<bool> SetActiveStatusAsync(Guid id, bool isActive);
    }
}