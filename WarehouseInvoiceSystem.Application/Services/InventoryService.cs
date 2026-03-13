namespace WarehouseInvoiceSystem.Application.Services
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Application.DTOs.InventoryTransaction;
    using WarehouseInvoiceSystem.Application.DTOs.StockLevel;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

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

        public async Task<PagedResult<StockLevelDto>> GetPagedStockAsync(GetStockQuery query)
        {
            PagedResult<StockLevel> result = await stockLevelRepository.GetPagedAsync(query);
            return new PagedResult<StockLevelDto>
            {
                Items = [.. result.Items.Select(MapStockLevelToDto)],
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
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

        public async Task<PagedResult<InventoryTransactionDto>> GetPagedTransactionsByProductAsync(GetInventoryTransactionsQuery query)
        {
            PagedResult<InventoryTransaction> result = await transactionRepository.GetPagedByProductAsync(query);

            return new PagedResult<InventoryTransactionDto>
            {
                Items = [.. result.Items.Select(MapTransactionToDto)],
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
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

        private static decimal ComputeStockChange(InventoryTransaction transaction) =>
            transaction.Type switch
            {
                InventoryTransactionType.Inbound => transaction.Quantity,
                InventoryTransactionType.TransferIn => transaction.Quantity,
                InventoryTransactionType.Outbound => -transaction.Quantity,
                InventoryTransactionType.TransferOut => -transaction.Quantity,
                InventoryTransactionType.Adjustment => transaction.Quantity,
                _ => 0
            };

        /// <summary>
        /// Applies a transaction's quantity delta to the corresponding StockLevel row.
        ///
        /// ApplyDeltaAsync performs the read and write inside a single DbContext so the
        /// xmin concurrency token is always valid when EF issues the UPDATE.  A genuine
        /// concurrent write will still cause DbUpdateConcurrencyException; the retry loop
        /// handles that by re-entering ApplyDeltaAsync with a fresh context and a fresh
        /// xmin read.
        ///
        /// Max 5 attempts before giving up; each retry back-offs slightly to reduce
        /// thundering-herd contention when many invoices confirm at the same instant.
        /// </summary>
        private async Task UpdateStockFromTransactionAsync(InventoryTransaction transaction)
        {
            const int MaxRetries = 5;

            decimal delta = ComputeStockChange(transaction);
            bool updateRestockDate = transaction.Type is InventoryTransactionType.Inbound
                                                      or InventoryTransactionType.TransferIn;

            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    await stockLevelRepository.ApplyDeltaAsync(
                        transaction.ProductId,
                        transaction.WarehouseId,
                        delta,
                        updateRestockDate);

                    return;
                }
                catch (DbUpdateConcurrencyException) when (attempt < MaxRetries)
                {
                    // A genuine concurrent write advanced xmin between our SELECT and UPDATE.
                    // Re-enter ApplyDeltaAsync — it will read the fresh xmin on the next attempt.
                    await Task.Delay(TimeSpan.FromMilliseconds(20 * attempt));
                }
            }

            throw new InvalidOperationException(
                $"Failed to update stock for product {transaction.ProductId} in warehouse " +
                $"{transaction.WarehouseId} after {MaxRetries} attempts due to concurrent modifications. " +
                "Please retry the operation.");
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