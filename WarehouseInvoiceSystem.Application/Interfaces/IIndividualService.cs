namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Application.DTOs.Individual;

    public interface IIndividualService
    {
        Task<IEnumerable<IndividualDto>> GetAllIndividualsAsync();
        Task<IEnumerable<IndividualDto>> GetActiveIndividualsAsync();
        Task<IndividualDto?> GetIndividualByIdAsync(Guid id);
        Task<IndividualDto?> GetIndividualByIdentificationNumberAsync(string identificationNumber);
        Task<IndividualDto> CreateIndividualAsync(CreateIndividualDto createDto);
        Task<IndividualDto> UpdateIndividualAsync(Guid id, UpdateIndividualDto updateDto);
        Task<bool> DeleteIndividualAsync(Guid id);
    }
}