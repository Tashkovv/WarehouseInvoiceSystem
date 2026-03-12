namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Application.DTOs.InventoryTransaction;
    using WarehouseInvoiceSystem.Application.DTOs.StockLevel;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public interface IInventoryService
    {
        // Stock Levels
        Task<IEnumerable<StockLevelDto>> GetAllStockLevelAsync();
        Task<PagedResult<StockLevelDto>> GetPagedStockAsync(GetStockQuery query);
        Task<StockLevelDto?> GetStockLevelAsync(Guid productId, Guid warehouseId);
        Task<IEnumerable<StockLevelDto>> GetStockByProductAsync(Guid productId);
        Task<IEnumerable<StockLevelDto>> GetStockByWarehouseAsync(Guid warehouseId);
        Task<IEnumerable<StockLevelDto>> GetLowStockItemsAsync(Guid? warehouseId = null);
        Task<StockLevelDto> UpdateStockLevelAsync(Guid productId, Guid warehouseId, UpdateStockLevelDto updateDto);

        // Transactions
        Task<IEnumerable<InventoryTransactionDto>> GetAllTransactionsAsync();
        Task<IEnumerable<InventoryTransactionDto>> GetTransactionsByProductAsync(Guid productId);
        Task<PagedResult<InventoryTransactionDto>> GetPagedTransactionsByProductAsync(GetInventoryTransactionsQuery query);
        Task<IEnumerable<InventoryTransactionDto>> GetTransactionsByWarehouseAsync(Guid warehouseId);
        Task<InventoryTransactionDto> CreateTransactionAsync(CreateInventoryTransactionDto createDto);

        // Stock Adjustments
        Task AdjustStockAsync(Guid productId, Guid warehouseId, decimal quantityChange, string reason);
        Task ReverseTransactionsForDocumentAsync(Guid sourceDocumentId, string sourceDocumentType, string reason);
        Task SoftDeleteReversalForDocumentAsync(Guid sourceDocumentId, string sourceDocumentType);
    }
}