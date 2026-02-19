namespace WarehouseInvoiceSystem.Domain.Interfaces
{
    using WarehouseInvoiceSystem.Domain.Entities;

    public interface IWarehouseRepository
    {
        Task<IEnumerable<Warehouse>> GetAllAsync();
        Task<Warehouse?> GetByIdAsync(Guid id);
        Task<Warehouse?> GetDefaultWarehouseAsync();
        Task<bool> ExistsAsync(Guid id);
        Task<Warehouse> CreateAsync(Warehouse warehouse);
        Task<Warehouse> UpdateAsync(Warehouse warehouse);
        Task<bool> DeleteAsync(Guid id);
    }
}
