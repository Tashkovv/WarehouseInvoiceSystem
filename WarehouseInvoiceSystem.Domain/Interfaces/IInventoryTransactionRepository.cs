namespace WarehouseInvoiceSystem.Domain.Interfaces
{
    using WarehouseInvoiceSystem.Domain.Entities;

    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public interface IInventoryTransactionRepository
    {
        Task<IEnumerable<InventoryTransaction>> GetAllAsync();
        Task<IEnumerable<InventoryTransaction>> GetByProductIdAsync(Guid productId);
        Task<PagedResult<InventoryTransaction>> GetPagedByProductAsync(GetInventoryTransactionsQuery query);
        Task<IEnumerable<InventoryTransaction>> GetByWarehouseIdAsync(Guid warehouseId);
        Task<IEnumerable<InventoryTransaction>> GetBySourceDocumentAsync(Guid sourceDocumentId, string sourceDocumentType);
        Task<InventoryTransaction?> GetByIdAsync(Guid id);
        Task<bool> HasTransactionsForDocumentAsync(Guid sourceDocumentId, string sourceDocumentType);
        Task<IEnumerable<InventoryTransaction>> SoftDeleteReversalAsync(Guid sourceDocumentId, string sourceDocumentType);
        Task<InventoryTransaction> CreateAsync(InventoryTransaction transaction);
    }
}