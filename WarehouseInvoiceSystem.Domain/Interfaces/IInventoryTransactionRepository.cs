namespace WarehouseInvoiceSystem.Domain.Interfaces
{
    using WarehouseInvoiceSystem.Domain.Entities;

    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public interface IInventoryTransactionRepository
    {
        Task<IEnumerable<InventoryTransaction>> GetAllAsync(CancellationToken ct = default);
        Task<IEnumerable<InventoryTransaction>> GetByProductIdAsync(Guid productId, CancellationToken ct = default);

        Task<PagedResult<InventoryTransaction>> GetPagedByProductAsync(GetInventoryTransactionsQuery query, CancellationToken ct = default);

        Task<IEnumerable<InventoryTransaction>> GetByWarehouseIdAsync(Guid warehouseId, CancellationToken ct = default);

        Task<IEnumerable<InventoryTransaction>> GetBySourceDocumentAsync(Guid sourceDocumentId, string sourceDocumentType, CancellationToken ct = default);

        Task<InventoryTransaction?> GetByIdAsync(Guid id, CancellationToken ct = default);

        Task<bool> HasTransactionsForDocumentAsync(Guid sourceDocumentId, string sourceDocumentType, CancellationToken ct = default);

        Task<IEnumerable<InventoryTransaction>> SoftDeleteReversalAsync(Guid sourceDocumentId, string sourceDocumentType, CancellationToken ct = default);

        Task<IEnumerable<InventoryTransaction>> SoftDeleteByDocumentAsync(Guid sourceDocumentId, string sourceDocumentType, CancellationToken ct = default);

        Task<InventoryTransaction> CreateAsync(InventoryTransaction transaction);
        Task CreateBatchAsync(IEnumerable<InventoryTransaction> transactions);

        // ── Dashboard aggregates ──────────────────────────────────────────────────
        Task<IEnumerable<InventoryTransaction>> GetTopRecentByWarehouseAsync(Guid warehouseId, int top, CancellationToken ct = default);
    }
}