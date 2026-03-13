namespace WarehouseInvoiceSystem.Domain.Interfaces
{
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public interface IWarehouseRepository
    {
        Task<IEnumerable<Warehouse>> GetAllAsync(CancellationToken ct = default);
        Task<PagedResult<Warehouse>> GetPagedAsync(GetWarehousesQuery query, CancellationToken ct = default);

        Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken ct = default);

        Task<Warehouse?> GetDefaultWarehouseAsync(CancellationToken ct = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

        Task<bool> HasProductsAsync(Guid id, CancellationToken ct = default);
        Task CreateAsync(Warehouse warehouse);
        Task UpdateAsync(Warehouse warehouse);
        Task<bool> SetActiveStatusAsync(Guid id, bool isActive);
    }
}