namespace WarehouseInvoiceSystem.Application.Services
{
    using WarehouseInvoiceSystem.Application.DTOs.InventoryTransaction;
    using WarehouseInvoiceSystem.Application.DTOs.StockLevel;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;
    using WarehouseInvoiceSystem.Domain.Queries.Results;

    public class InventoryService(
        IStockLevelRepository stockLevelRepository,
        IInventoryTransactionRepository transactionRepository,
        IProductRepository productRepository,
        IWarehouseRepository warehouseRepository) : IInventoryService
    {
        // Stock Levels
        public async Task<IEnumerable<StockLevelDto>> GetAllStockLevelAsync(CancellationToken ct = default)
        {
            IEnumerable<StockLevel> stockLevels = await stockLevelRepository.GetAllStockLevelAsync(ct);
            return stockLevels.Select(MapStockLevelToDto);
        }

        public async Task<PagedResult<StockLevelDto>> GetPagedStockAsync(GetStockQuery query, CancellationToken ct = default)
        {
            PagedResult<StockLevel> result = await stockLevelRepository.GetPagedAsync(query, ct);
            return new PagedResult<StockLevelDto>
            {
                Items = [.. result.Items.Select(MapStockLevelToDto)],
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        public async Task<StockLevelDto?> GetStockLevelAsync(Guid productId, Guid warehouseId, CancellationToken ct = default)
        {
            StockLevel? stockLevel = await stockLevelRepository.GetByProductAndWarehouseAsync(productId, warehouseId, ct);
            return stockLevel == null ? null : MapStockLevelToDto(stockLevel);
        }

        public async Task<IEnumerable<StockLevelDto>> GetStockByProductAsync(Guid productId, CancellationToken ct = default)
        {
            IEnumerable<StockLevel> stockLevels = await stockLevelRepository.GetByProductIdAsync(productId, ct);
            return stockLevels.Select(MapStockLevelToDto);
        }

        public async Task<IEnumerable<StockLevelDto>> GetStockByWarehouseAsync(Guid warehouseId, CancellationToken ct = default)
        {
            IEnumerable<StockLevel> stockLevels = await stockLevelRepository.GetByWarehouseIdAsync(warehouseId, ct);
            return stockLevels.Select(MapStockLevelToDto);
        }

        public async Task<IEnumerable<StockLevelDto>> GetLowStockItemsAsync(Guid? warehouseId = null, CancellationToken ct = default)
        {
            IEnumerable<StockLevel> stockLevels = await stockLevelRepository.GetLowStockItemsAsync(warehouseId, ct);
            return stockLevels.Select(MapStockLevelToDto);
        }

        public Task<WarehouseStockSummaryResult> GetWarehouseStockSummaryAsync(
            Guid? warehouseId, CancellationToken ct = default)
            => stockLevelRepository.GetWarehouseStockSummaryAsync(warehouseId, ct);

        public async Task<IEnumerable<StockLevelDto>> GetStockAlertsAsync(
            Guid? warehouseId, int top, CancellationToken ct = default)
        {
            IEnumerable<StockLevel> stockLevels = await stockLevelRepository.GetStockAlertsAsync(warehouseId, top, ct);
            return stockLevels.Select(MapStockLevelToDto);
        }

        public async Task<IEnumerable<StockLevelDto>> GetTopProductsByStockAsync(
            Guid warehouseId, int top, CancellationToken ct = default)
        {
            IEnumerable<StockLevel> stockLevels = await stockLevelRepository.GetTopByStockAsync(warehouseId, top, ct);
            return stockLevels.Select(MapStockLevelToDto);
        }

        public async Task<IEnumerable<InventoryTransactionDto>> GetRecentTransactionsAsync(
            Guid warehouseId, int top, CancellationToken ct = default)
        {
            IEnumerable<InventoryTransaction> transactions = await transactionRepository.GetTopRecentByWarehouseAsync(warehouseId, top, ct);
            return transactions.Select(MapTransactionToDto);
        }

        public async Task<StockLevelDto> UpdateStockLevelAsync(Guid productId, Guid warehouseId, UpdateStockLevelDto updateDto)
        {
            StockLevel? stockLevel = await stockLevelRepository.GetByProductAndWarehouseAsync(productId, warehouseId);

            StockLevel saved;
            if (stockLevel == null)
            {
                saved = await stockLevelRepository.CreateAsync(new StockLevel
                {
                    ProductId = productId,
                    WarehouseId = warehouseId,
                    Quantity = updateDto.Quantity,
                    MinimumQuantity = updateDto.MinimumQuantity,
                    ReorderPoint = updateDto.ReorderPoint,
                    LastRestockedAt = DateTime.UtcNow
                });
            }
            else
            {
                stockLevel.Quantity = updateDto.Quantity;
                stockLevel.MinimumQuantity = updateDto.MinimumQuantity;
                stockLevel.ReorderPoint = updateDto.ReorderPoint;
                saved = await stockLevelRepository.UpdateAsync(stockLevel);
            }

            return MapStockLevelToDto(saved);
        }

        // Transactions
        public async Task<IEnumerable<InventoryTransactionDto>> GetAllTransactionsAsync(CancellationToken ct = default)
        {
            IEnumerable<InventoryTransaction> transactions = await transactionRepository.GetAllAsync(ct);
            return transactions.Select(MapTransactionToDto);
        }

        public async Task<IEnumerable<InventoryTransactionDto>> GetTransactionsByProductAsync(Guid productId, CancellationToken ct = default)
        {
            IEnumerable<InventoryTransaction> transactions = await transactionRepository.GetByProductIdAsync(productId, ct);
            return transactions.Select(MapTransactionToDto);
        }

        public async Task<PagedResult<InventoryTransactionDto>> GetPagedTransactionsByProductAsync(GetInventoryTransactionsQuery query, CancellationToken ct = default)
        {
            PagedResult<InventoryTransaction> result = await transactionRepository.GetPagedByProductAsync(query, ct);

            return new PagedResult<InventoryTransactionDto>
            {
                Items = [.. result.Items.Select(MapTransactionToDto)],
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        public async Task<IEnumerable<InventoryTransactionDto>> GetAllFilteredTransactionsAsync(GetInventoryTransactionsQuery query, CancellationToken ct = default)
        {
            var unpaged = new GetInventoryTransactionsQuery
            {
                ProductId = query.ProductId,
                WarehouseId = query.WarehouseId,
                Types = query.Types,
                Page = 1,
                PageSize = int.MaxValue
            };
            PagedResult<InventoryTransaction> result = await transactionRepository.GetPagedByProductAsync(unpaged, ct);
            return result.Items.Select(MapTransactionToDto);
        }

        public async Task<IEnumerable<InventoryTransactionDto>> GetTransactionsByWarehouseAsync(Guid warehouseId, CancellationToken ct = default)
        {
            IEnumerable<InventoryTransaction> transactions = await transactionRepository.GetByWarehouseIdAsync(warehouseId, ct);
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

            // Re-fetch with navigation properties loaded for DTO mapping
            InventoryTransaction withNav = await transactionRepository.GetByIdAsync(created.Id)
                ?? throw new InvalidOperationException($"Transaction {created.Id} not found after creation.");

            return MapTransactionToDto(withNav);
        }

        public async Task TransferStockAsync(Guid productId, Guid sourceWarehouseId, Guid destinationWarehouseId, decimal quantity, string? note)
        {
            if (sourceWarehouseId == destinationWarehouseId)
                throw new InvalidOperationException("Source and destination warehouse must be different.");

            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), "Transfer quantity must be greater than zero.");

            Guid transferId = Guid.NewGuid();
            const string transferType = "Transfer";

            CreateInventoryTransactionDto outbound = new()
            {
                ProductId = productId,
                WarehouseId = sourceWarehouseId,
                Type = InventoryTransactionType.TransferOut,
                Quantity = quantity,
                SourceDocumentId = transferId,
                SourceDocumentType = transferType,
                Note = note
            };

            CreateInventoryTransactionDto inbound = new()
            {
                ProductId = productId,
                WarehouseId = destinationWarehouseId,
                Type = InventoryTransactionType.TransferIn,
                Quantity = quantity,
                SourceDocumentId = transferId,
                SourceDocumentType = transferType,
                Note = note
            };

            await CreateTransactionAsync(outbound);
            await CreateTransactionAsync(inbound);
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

        public async Task CreateBatchAsync(Guid warehouseId, IEnumerable<CreateInventoryTransactionDto> items)
        {
            List<CreateInventoryTransactionDto> dtos = items.ToList();
            if (dtos.Count == 0) return;

            // Validate warehouse once for the whole batch
            if (!await warehouseRepository.ExistsAsync(warehouseId))
                throw new KeyNotFoundException($"Warehouse with ID {warehouseId} not found");

            // Validate all products in one query
            List<Guid> productIds = dtos.Select(d => d.ProductId).Distinct().ToList();
            if (!await productRepository.AllExistAsync(productIds))
                throw new KeyNotFoundException("One or more products in the batch were not found");

            // Build and insert all transaction rows in a single SaveAsync
            List<InventoryTransaction> transactions = dtos.Select(d => new InventoryTransaction
            {
                ProductId = d.ProductId,
                WarehouseId = warehouseId,
                Type = d.Type,
                Quantity = d.Quantity,
                SourceDocumentId = d.SourceDocumentId,
                SourceDocumentType = d.SourceDocumentType,
                Note = d.Note,
            }).ToList();

            await transactionRepository.CreateBatchAsync(transactions);

            // Apply stock deltas — each call is its own WithContextAsync so the
            // xmin read and write share the same DbContext (Bug 2 fix preserved).
            foreach (InventoryTransaction t in transactions)
                await UpdateStockFromTransactionAsync(t);
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
                InventoryTransactionType.Reversed => transaction.Quantity, // signed: negated at creation
                _ => throw new ArgumentOutOfRangeException(nameof(transaction), $"Unknown transaction type: {transaction.Type}")
            };

        private Task UpdateStockFromTransactionAsync(InventoryTransaction transaction)
        {
            decimal delta = ComputeStockChange(transaction);
            bool updateRestockDate = transaction.Type is InventoryTransactionType.Inbound
                                                      or InventoryTransactionType.TransferIn;
            return stockLevelRepository.ApplyDeltaAsync(
                transaction.ProductId,
                transaction.WarehouseId,
                delta,
                updateRestockDate);
        }

        public async Task ReverseTransactionsForDocumentAsync(Guid sourceDocumentId, string sourceDocumentType, string reason)
        {
            bool hasReversals = await transactionRepository
                .HasTransactionsForDocumentAsync(sourceDocumentId, $"{sourceDocumentType}_Reversal");
            if (hasReversals) return;

            IEnumerable<InventoryTransaction> existing = await transactionRepository
                .GetBySourceDocumentAsync(sourceDocumentId, sourceDocumentType);

            foreach (InventoryTransaction original in existing)
            {
                // Create a Reversed transaction that negates the original's stock effect
                InventoryTransaction reversal = new()
                {
                    ProductId = original.ProductId,
                    WarehouseId = original.WarehouseId,
                    Type = InventoryTransactionType.Reversed,
                    Quantity = -ComputeStockChange(original), // signed: undoes the original delta
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
                // Undo the reversal's stock effect by negating it
                // For new Reversed transactions (signed quantity): negate the quantity
                // For legacy reversals (flipped-type): use the opposite type
                InventoryTransactionType restoreType = reversal.Type switch
                {
                    InventoryTransactionType.Outbound    => InventoryTransactionType.Inbound,
                    InventoryTransactionType.Inbound     => InventoryTransactionType.Outbound,
                    InventoryTransactionType.TransferIn  => InventoryTransactionType.TransferOut,
                    InventoryTransactionType.TransferOut => InventoryTransactionType.TransferIn,
                    InventoryTransactionType.Reversed    => InventoryTransactionType.Reversed,
                    _                                    => InventoryTransactionType.Adjustment
                };

                // Reversed uses signed quantity — negate to undo; all others keep positive quantity
                decimal restoreQuantity = reversal.Type == InventoryTransactionType.Reversed
                    ? -reversal.Quantity
                    : reversal.Quantity;

                await UpdateStockFromTransactionAsync(new InventoryTransaction
                {
                    ProductId = reversal.ProductId,
                    WarehouseId = reversal.WarehouseId,
                    Type = restoreType,
                    Quantity = restoreQuantity
                });
            }
        }

        public async Task SoftDeleteTransactionsForDocumentAsync(Guid sourceDocumentId, string sourceDocumentType)
        {
            IEnumerable<InventoryTransaction> deleted = await transactionRepository
                .SoftDeleteByDocumentAsync(sourceDocumentId, sourceDocumentType);

            foreach (InventoryTransaction t in deleted)
            {
                decimal undoQuantity = -ComputeStockChange(t);
                await UpdateStockFromTransactionAsync(new InventoryTransaction
                {
                    ProductId = t.ProductId,
                    WarehouseId = t.WarehouseId,
                    Type = InventoryTransactionType.Reversed,
                    Quantity = undoQuantity
                });
            }
        }

        private static StockLevelDto MapStockLevelToDto(StockLevel stockLevel)
        {
            return new StockLevelDto
            {
                Id = stockLevel.Id,
                ProductId = stockLevel.ProductId,
                ProductCode = stockLevel.Product.Code,
                ProductName = stockLevel.Product.Name,
                ProductUnit = stockLevel.Product.Unit,
                WarehouseId = stockLevel.WarehouseId,
                WarehouseName = stockLevel.Warehouse.Name,
                Quantity = stockLevel.Quantity,
                ReservedQuantity = stockLevel.ReservedQuantity,
                AvailableQuantity = stockLevel.AvailableQuantity,
                MinimumQuantity = stockLevel.MinimumQuantity,
                ReorderPoint = stockLevel.ReorderPoint,
                LastRestockedAt = stockLevel.LastRestockedAt,
                UnitPrice = stockLevel.Product.SellingPrice
            };
        }

        private static InventoryTransactionDto MapTransactionToDto(InventoryTransaction transaction)
        {
            return new InventoryTransactionDto
            {
                Id = transaction.Id,
                ProductId = transaction.ProductId,
                ProductCode = transaction.Product.Code,
                ProductName = transaction.Product.Name,
                ProductUnit = transaction.Product.Unit,
                WarehouseId = transaction.WarehouseId,
                WarehouseName = transaction.Warehouse.Name,
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