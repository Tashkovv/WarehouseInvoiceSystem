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
        /// <summary>
        /// Atomically applies a quantity delta to the StockLevel row for the given
        /// product/warehouse pair — read and write happen inside the same DbContext so
        /// the xmin concurrency token is always valid when EF issues the UPDATE.
        /// Creates the row if it does not yet exist.
        /// </summary>
        Task ApplyDeltaAsync(Guid productId, Guid warehouseId, decimal delta, bool updateRestockDate);
        Task<IEnumerable<StockLevel>> GetByProductIdAsync(Guid productId);
        Task<IEnumerable<StockLevel>> GetByWarehouseIdAsync(Guid warehouseId);
        Task<IEnumerable<StockLevel>> GetLowStockItemsAsync(Guid? warehouseId = null);
        Task<StockLevel> CreateAsync(StockLevel stockLevel);
        Task<StockLevel> UpdateAsync(StockLevel stockLevel);
        Task<bool> DeleteAsync(Guid id);
    }
}