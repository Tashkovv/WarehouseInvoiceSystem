namespace WarehouseInvoiceSystem.Domain.Interfaces
{
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public interface IWarehouseRepository
    {
        Task<IEnumerable<Warehouse>> GetAllAsync();
        Task<PagedResult<Warehouse>> GetPagedAsync(GetWarehousesQuery query);
        Task<Warehouse?> GetByIdAsync(Guid id);
        Task<Warehouse?> GetDefaultWarehouseAsync();
        Task<bool> ExistsAsync(Guid id);
        Task<Warehouse> CreateAsync(Warehouse warehouse);
        Task<Warehouse> UpdateAsync(Warehouse warehouse);
        Task<bool> DeleteAsync(Guid id);
    }
}
