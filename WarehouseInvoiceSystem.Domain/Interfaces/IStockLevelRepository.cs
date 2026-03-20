namespace WarehouseInvoiceSystem.Domain.Interfaces
{
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;
    using WarehouseInvoiceSystem.Domain.Queries.Results;

    public interface IStockLevelRepository
    {
        Task<IEnumerable<StockLevel>> GetAllStockLevelAsync(CancellationToken ct = default);
        Task<PagedResult<StockLevel>> GetPagedAsync(GetStockQuery query, CancellationToken ct = default);

        Task<StockLevel?> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId, CancellationToken ct = default);

        /// <summary>
        /// Atomically applies a quantity delta to the StockLevel row for the given
        /// product/warehouse pair — read and write happen inside the same DbContext so
        /// the xmin concurrency token is always valid when EF issues the UPDATE.
        /// Creates the row if it does not yet exist.
        /// </summary>
        Task ApplyDeltaAsync(Guid productId, Guid warehouseId, decimal delta, bool updateRestockDate);
        Task<IEnumerable<StockLevel>> GetByProductIdAsync(Guid productId, CancellationToken ct = default);

        Task<IEnumerable<StockLevel>> GetByWarehouseIdAsync(Guid warehouseId, CancellationToken ct = default);

        Task<IEnumerable<StockLevel>> GetLowStockItemsAsync(Guid? warehouseId = null, CancellationToken ct = default);

        Task<StockLevel> CreateAsync(StockLevel stockLevel);
        Task<StockLevel> UpdateAsync(StockLevel stockLevel);
        Task<bool> DeleteAsync(Guid id);

        // ── Dashboard aggregates ──────────────────────────────────────────────────
        Task<WarehouseStockSummaryResult> GetWarehouseStockSummaryAsync(Guid? warehouseId, CancellationToken ct = default);
        Task<IEnumerable<StockLevel>> GetStockAlertsAsync(Guid? warehouseId, int top, CancellationToken ct = default);
        Task<IEnumerable<StockLevel>> GetTopByStockAsync(Guid warehouseId, int top, CancellationToken ct = default);
    }
}