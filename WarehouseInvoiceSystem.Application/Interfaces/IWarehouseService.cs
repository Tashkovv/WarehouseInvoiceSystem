namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Application.DTOs.Warehouse;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public interface IWarehouseService
    {
        Task<IEnumerable<WarehouseDto>> GetAllWarehousesAsync(CancellationToken ct = default);
        Task<PagedResult<WarehouseDto>> GetPagedAsync(GetWarehousesQuery query, CancellationToken ct = default);
        Task<WarehouseDto?> GetWarehouseByIdAsync(Guid id, CancellationToken ct = default);
        Task<WarehouseDto?> GetDefaultWarehouseAsync(CancellationToken ct = default);
        Task<bool> SetDefaultWarehouseAsync(Guid id);
        Task<bool> HasProductsAsync(Guid id, CancellationToken ct = default);
        Task CreateWarehouseAsync(CreateWarehouseDto createDto);
        Task UpdateWarehouseAsync(Guid id, UpdateWarehouseDto updateDto);
        Task<bool> SetActiveStatusAsync(Guid id, bool isActive);
    }
}