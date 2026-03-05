namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Application.DTOs.Warehouse;

    public interface IWarehouseService
    {
        Task<IEnumerable<WarehouseDto>> GetAllWarehousesAsync();
        Task<WarehouseDto?> GetWarehouseByIdAsync(Guid id);
        Task<WarehouseDto?> GetDefaultWarehouseAsync();
        Task<bool> SetDefaultWarehouseAsync(Guid id);
        Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseDto createDto);
        Task<WarehouseDto> UpdateWarehouseAsync(Guid id, UpdateWarehouseDto updateDto);
        Task<bool> DeleteWarehouseAsync(Guid id);
    }
}