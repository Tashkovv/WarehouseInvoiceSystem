namespace WarehouseInvoiceSystem.Application.Services
{
    using WarehouseInvoiceSystem.Application.DTOs.InventoryTransaction;
    using WarehouseInvoiceSystem.Application.DTOs.StockLevel;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Interfaces;

    public class InventoryService(
        IStockLevelRepository stockLevelRepository,
        IInventoryTransactionRepository transactionRepository,
        IProductRepository productRepository,
        IWarehouseRepository warehouseRepository) : IInventoryService
    {
        // Stock Levels
        public async Task<IEnumerable<StockLevelDto>> GetAllStockLevelAsync()
        {
            IEnumerable<StockLevel> stockLevels = await stockLevelRepository.GetAllStockLevelAsync();
            return stockLevels.Select(MapStockLevelToDto);
        }

        public async Task<StockLevelDto?> GetStockLevelAsync(Guid productId, Guid warehouseId)
        {
            StockLevel? stockLevel = await stockLevelRepository.GetByProductAndWarehouseAsync(productId, warehouseId);
            return stockLevel == null ? null : MapStockLevelToDto(stockLevel);
        }

        public async Task<IEnumerable<StockLevelDto>> GetStockByProductAsync(Guid productId)
        {
            IEnumerable<StockLevel> stockLevels = await stockLevelRepository.GetByProductIdAsync(productId);
            return stockLevels.Select(MapStockLevelToDto);
        }

        public async Task<IEnumerable<StockLevelDto>> GetStockByWarehouseAsync(Guid warehouseId)
        {
            IEnumerable<StockLevel> stockLevels = await stockLevelRepository.GetByWarehouseIdAsync(warehouseId);
            return stockLevels.Select(MapStockLevelToDto);
        }

        public async Task<IEnumerable<StockLevelDto>> GetLowStockItemsAsync(Guid? warehouseId = null)
        {
            IEnumerable<StockLevel> stockLevels = await stockLevelRepository.GetLowStockItemsAsync(warehouseId);
            return stockLevels.Select(MapStockLevelToDto);
        }

        public async Task<StockLevelDto> UpdateStockLevelAsync(Guid productId, Guid warehouseId, UpdateStockLevelDto updateDto)
        {
            StockLevel? stockLevel = await stockLevelRepository.GetByProductAndWarehouseAsync(productId, warehouseId);

            if (stockLevel == null)
            {
                // Create new stock level
                stockLevel = new StockLevel
                {
                    ProductId = productId,
                    WarehouseId = warehouseId,
                    Quantity = updateDto.Quantity,
                    MinimumQuantity = updateDto.MinimumQuantity,
                    ReorderPoint = updateDto.ReorderPoint,
                    LastRestockedAt = DateTime.UtcNow
                };
                stockLevel = await stockLevelRepository.CreateAsync(stockLevel);
            }
            else
            {
                stockLevel.Quantity = updateDto.Quantity;
                stockLevel.MinimumQuantity = updateDto.MinimumQuantity;
                stockLevel.ReorderPoint = updateDto.ReorderPoint;
                stockLevel = await stockLevelRepository.UpdateAsync(stockLevel);
            }

            return MapStockLevelToDto(stockLevel);
        }

        // Transactions
        public async Task<IEnumerable<InventoryTransactionDto>> GetAllTransactionsAsync()
        {
            IEnumerable<InventoryTransaction> transactions = await transactionRepository.GetAllAsync();
            return transactions.Select(MapTransactionToDto);
        }

        public async Task<IEnumerable<InventoryTransactionDto>> GetTransactionsByProductAsync(Guid productId)
        {
            IEnumerable<InventoryTransaction> transactions = await transactionRepository.GetByProductIdAsync(productId);
            return transactions.Select(MapTransactionToDto);
        }

        public async Task<IEnumerable<InventoryTransactionDto>> GetTransactionsByWarehouseAsync(Guid warehouseId)
        {
            IEnumerable<InventoryTransaction> transactions = await transactionRepository.GetByWarehouseIdAsync(warehouseId);
            return transactions.Select(MapTransactionToDto);
        }

        public async Task<InventoryTransactionDto> CreateTransactionAsync(CreateInventoryTransactionDto createDto)
        {
            // Validate product and warehouse exist
            if (!await productRepository.ExistsAsync(createDto.ProductId))
                throw new KeyNotFoundException($"Product with ID {createDto.ProductId} not found");

            if (!await warehouseRepository.ExistsAsync(createDto.WarehouseId))
                throw new KeyNotFoundException($"Warehouse with ID {createDto.WarehouseId} not found");

            InventoryTransaction transaction = new()
            {
                ProductId = createDto.ProductId,
                WarehouseId = createDto.WarehouseId,
                Type = createDto.Type,
                Quantity = createDto.Quantity,
                SourceDocumentId = createDto.SourceDocumentId,
                SourceDocumentType = createDto.SourceDocumentType,
                Note = createDto.Note,
            };

            InventoryTransaction created = await transactionRepository.CreateAsync(transaction);

            // Update stock level
            await UpdateStockFromTransactionAsync(created);

            return MapTransactionToDto(created);
        }

        public async Task AdjustStockAsync(Guid productId, Guid warehouseId, decimal quantityChange, string reason)
        {
            CreateInventoryTransactionDto transactionDto = new()
            {
                ProductId = productId,
                WarehouseId = warehouseId,
                Type = InventoryTransactionType.Adjustment,
                Quantity = quantityChange,
                Note = reason
            };

            await CreateTransactionAsync(transactionDto);
        }

        // Private helpers
        private async Task UpdateStockFromTransactionAsync(InventoryTransaction transaction)
        {
            StockLevel? stockLevel = await stockLevelRepository.GetByProductAndWarehouseAsync(
                transaction.ProductId,
                transaction.WarehouseId);

            if (stockLevel == null)
            {
                stockLevel = new StockLevel
                {
                    ProductId = transaction.ProductId,
                    WarehouseId = transaction.WarehouseId,
                    Quantity = 0,
                    MinimumQuantity = 0,
                    ReorderPoint = 0,
                    LastRestockedAt = DateTime.UtcNow
                };
                stockLevel = await stockLevelRepository.CreateAsync(stockLevel);
            }

            // Apply transaction to stock level
            decimal change = transaction.Type switch
            {
                InventoryTransactionType.Inbound => transaction.Quantity,
                InventoryTransactionType.TransferIn => transaction.Quantity,
                InventoryTransactionType.Outbound => -transaction.Quantity,
                InventoryTransactionType.TransferOut => -transaction.Quantity,
                InventoryTransactionType.Adjustment => transaction.Quantity,
                _ => 0
            };

            stockLevel.Quantity += change;

            if (transaction.Type == InventoryTransactionType.Inbound ||
                transaction.Type == InventoryTransactionType.TransferIn)
            {
                stockLevel.LastRestockedAt = DateTime.UtcNow;
            }

            await stockLevelRepository.UpdateAsync(stockLevel);
        }

        public async Task ReverseTransactionsForDocumentAsync(Guid sourceDocumentId, string sourceDocumentType, string reason)
        {
            IEnumerable<InventoryTransaction> existing = await transactionRepository
                .GetBySourceDocumentAsync(sourceDocumentId, sourceDocumentType);

            foreach (InventoryTransaction original in existing)
            {
                // Create the mirror-image transaction
                InventoryTransactionType reversalType = original.Type switch
                {
                    InventoryTransactionType.Outbound => InventoryTransactionType.Inbound,
                    InventoryTransactionType.Inbound => InventoryTransactionType.Outbound,
                    InventoryTransactionType.TransferIn => InventoryTransactionType.TransferOut,
                    InventoryTransactionType.TransferOut => InventoryTransactionType.TransferIn,
                    _ => InventoryTransactionType.Adjustment
                };

                InventoryTransaction reversal = new()
                {
                    ProductId = original.ProductId,
                    WarehouseId = original.WarehouseId,
                    Type = reversalType,
                    Quantity = original.Quantity,
                    SourceDocumentId = sourceDocumentId,
                    SourceDocumentType = $"{sourceDocumentType}_Reversal",
                    Note = reason
                };

                InventoryTransaction created = await transactionRepository.CreateAsync(reversal);
                await UpdateStockFromTransactionAsync(created);
            }
        }

        public async Task SoftDeleteReversalForDocumentAsync(Guid sourceDocumentId, string sourceDocumentType)
        {
            // Soft-delete the reversal transactions and undo their stock effect
            IEnumerable<InventoryTransaction> reversals = await transactionRepository
                .SoftDeleteReversalAsync(sourceDocumentId, sourceDocumentType);

            foreach (InventoryTransaction reversal in reversals)
            {
                // The reversal cancelled the original stock movement — undoing the reversal
                // means re-applying the original direction, which is the opposite of the reversal type
                InventoryTransactionType restoreType = reversal.Type switch
                {
                    InventoryTransactionType.Outbound => InventoryTransactionType.Inbound,
                    InventoryTransactionType.Inbound => InventoryTransactionType.Outbound,
                    InventoryTransactionType.TransferIn => InventoryTransactionType.TransferOut,
                    InventoryTransactionType.TransferOut => InventoryTransactionType.TransferIn,
                    _ => InventoryTransactionType.Adjustment
                };

                await UpdateStockFromTransactionAsync(new InventoryTransaction
                {
                    ProductId = reversal.ProductId,
                    WarehouseId = reversal.WarehouseId,
                    Type = restoreType,
                    Quantity = reversal.Quantity
                });
            }
        }

        private static StockLevelDto MapStockLevelToDto(StockLevel stockLevel)
        {
            return new StockLevelDto
            {
                Id = stockLevel.Id,
                ProductId = stockLevel.ProductId,
                ProductCode = stockLevel.Product?.Code ?? string.Empty,
                ProductName = stockLevel.Product?.Name ?? string.Empty,
                WarehouseId = stockLevel.WarehouseId,
                WarehouseName = stockLevel.Warehouse?.Name ?? string.Empty,
                Quantity = stockLevel.Quantity,
                ReservedQuantity = stockLevel.ReservedQuantity,
                AvailableQuantity = stockLevel.AvailableQuantity,
                MinimumQuantity = stockLevel.MinimumQuantity,
                ReorderPoint = stockLevel.ReorderPoint,
                LastRestockedAt = stockLevel.LastRestockedAt
            };
        }

        private static InventoryTransactionDto MapTransactionToDto(InventoryTransaction transaction)
        {
            return new InventoryTransactionDto
            {
                Id = transaction.Id,
                ProductId = transaction.ProductId,
                ProductCode = transaction.Product?.Code ?? string.Empty,
                ProductName = transaction.Product?.Name ?? string.Empty,
                WarehouseId = transaction.WarehouseId,
                WarehouseName = transaction.Warehouse?.Name ?? string.Empty,
                Type = transaction.Type,
                Quantity = transaction.Quantity,
                SourceDocumentId = transaction.SourceDocumentId,
                SourceDocumentType = transaction.SourceDocumentType,
                Note = transaction.Note,
                CreatedAt = transaction.CreatedAt
            };
        }
    }
}