namespace WarehouseInvoiceSystem.Domain.Interfaces
{
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public interface IStockLevelRepository
    {
        Task<IEnumerable<StockLevel>> GetAllStockLevelAsync();
        Task<PagedResult<StockLevel>> GetPagedAsync(GetStockQuery query);
        Task<StockLevel?> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId);
        Task<IEnumerable<StockLevel>> GetByProductIdAsync(Guid productId);
        Task<IEnumerable<StockLevel>> GetByWarehouseIdAsync(Guid warehouseId);
        Task<IEnumerable<StockLevel>> GetLowStockItemsAsync(Guid? warehouseId = null);
        Task<StockLevel> CreateAsync(StockLevel stockLevel);
        Task<StockLevel> UpdateAsync(StockLevel stockLevel);
        Task<bool> DeleteAsync(Guid id);
    }
}
