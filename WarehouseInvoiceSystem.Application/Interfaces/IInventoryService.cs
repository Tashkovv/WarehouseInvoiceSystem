namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Application.DTOs.InventoryTransaction;
    using WarehouseInvoiceSystem.Application.DTOs.StockLevel;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;
    using WarehouseInvoiceSystem.Domain.Queries.Results;

    public interface IInventoryService
    {
        // Stock Levels
        Task<IEnumerable<StockLevelDto>> GetAllStockLevelAsync(CancellationToken ct = default);
        Task<PagedResult<StockLevelDto>> GetPagedStockAsync(GetStockQuery query, CancellationToken ct = default);
        Task<StockLevelDto?> GetStockLevelAsync(Guid productId, Guid warehouseId, CancellationToken ct = default);
        Task<IEnumerable<StockLevelDto>> GetStockByProductAsync(Guid productId, CancellationToken ct = default);
        Task<IEnumerable<StockLevelDto>> GetStockByWarehouseAsync(Guid warehouseId, CancellationToken ct = default);
        Task<IEnumerable<StockLevelDto>> GetLowStockItemsAsync(Guid? warehouseId = null, CancellationToken ct = default);
        Task<StockLevelDto> UpdateStockLevelAsync(Guid productId, Guid warehouseId, UpdateStockLevelDto updateDto);

        // Transactions
        Task<IEnumerable<InventoryTransactionDto>> GetAllTransactionsAsync(CancellationToken ct = default);
        Task<IEnumerable<InventoryTransactionDto>> GetTransactionsByProductAsync(Guid productId, CancellationToken ct = default);
        Task<PagedResult<InventoryTransactionDto>> GetPagedTransactionsByProductAsync(GetInventoryTransactionsQuery query, CancellationToken ct = default);
        Task<IEnumerable<InventoryTransactionDto>> GetTransactionsByWarehouseAsync(Guid warehouseId, CancellationToken ct = default);
        Task<InventoryTransactionDto> CreateTransactionAsync(CreateInventoryTransactionDto createDto);

        /// <summary>
        /// Validates product and warehouse once, inserts all transactions in a single
        /// SaveAsync call, then applies each stock delta. Use instead of looping
        /// CreateTransactionAsync per line to eliminate N+1 DB round-trips.
        /// </summary>
        Task CreateBatchAsync(Guid warehouseId, IEnumerable<CreateInventoryTransactionDto> items);

        // Stock Adjustments
        Task AdjustStockAsync(Guid productId, Guid warehouseId, decimal quantityChange, string reason);
        Task TransferStockAsync(Guid productId, Guid sourceWarehouseId, Guid destinationWarehouseId, decimal quantity, string? note);
        Task ReverseTransactionsForDocumentAsync(Guid sourceDocumentId, string sourceDocumentType, string reason);
        Task SoftDeleteReversalForDocumentAsync(Guid sourceDocumentId, string sourceDocumentType);

        // ── Dashboard aggregates ──────────────────────────────────────────────────
        Task<WarehouseStockSummaryResult> GetWarehouseStockSummaryAsync(Guid? warehouseId, CancellationToken ct = default);
        Task<IEnumerable<StockLevelDto>> GetStockAlertsAsync(Guid? warehouseId, int top, CancellationToken ct = default);
        Task<IEnumerable<StockLevelDto>> GetTopProductsByStockAsync(Guid warehouseId, int top, CancellationToken ct = default);
        Task<IEnumerable<InventoryTransactionDto>> GetRecentTransactionsAsync(Guid warehouseId, int top, CancellationToken ct = default);
    }
}