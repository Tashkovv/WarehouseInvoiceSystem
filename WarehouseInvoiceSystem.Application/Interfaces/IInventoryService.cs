namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Application.DTOs.InventoryTransaction;
    using WarehouseInvoiceSystem.Application.DTOs.StockLevel;

    public interface IInventoryService
    {
        // Stock Levels
        Task<IEnumerable<StockLevelDto>> GetAllStockLevelAsync();
        Task<StockLevelDto?> GetStockLevelAsync(Guid productId, Guid warehouseId);
        Task<IEnumerable<StockLevelDto>> GetStockByProductAsync(Guid productId);
        Task<IEnumerable<StockLevelDto>> GetStockByWarehouseAsync(Guid warehouseId);
        Task<IEnumerable<StockLevelDto>> GetLowStockItemsAsync(Guid? warehouseId = null);
        Task<StockLevelDto> UpdateStockLevelAsync(Guid productId, Guid warehouseId, UpdateStockLevelDto updateDto);

        // Transactions
        Task<IEnumerable<InventoryTransactionDto>> GetAllTransactionsAsync();
        Task<IEnumerable<InventoryTransactionDto>> GetTransactionsByProductAsync(Guid productId);
        Task<IEnumerable<InventoryTransactionDto>> GetTransactionsByWarehouseAsync(Guid warehouseId);
        Task<InventoryTransactionDto> CreateTransactionAsync(CreateInventoryTransactionDto createDto);

        // Stock Adjustments
        Task AdjustStockAsync(Guid productId, Guid warehouseId, decimal quantityChange, string reason);
    }
}