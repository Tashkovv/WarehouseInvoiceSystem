namespace WarehouseInvoiceSystem.Application.Services
{
    using WarehouseInvoiceSystem.Application.DTOs.InventoryTransaction;
    using WarehouseInvoiceSystem.Application.DTOs.Product;
    using WarehouseInvoiceSystem.Application.DTOs.StockLevel;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public class ProductService(IProductRepository productRepository,
                                IInventoryService inventoryService,
                                IInvoiceRepository invoiceRepository,
                                IPurchaseNoteRepository purchaseNoteRepository) : IProductService
    {
        private const string reversalString = "_Reversal";

        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync(CancellationToken ct = default)
        {
            IEnumerable<Product> products = await productRepository.GetAllAsync(ct);
            return products.Select(MapToDto);
        }

        public async Task<PagedResult<ProductDto>> GetPagedAsync(GetProductsQuery query, CancellationToken ct = default)
        {
            PagedResult<Product> result = await productRepository.GetPagedAsync(query, ct);
            return new PagedResult<ProductDto>
            {
                Items = [.. result.Items.Select(MapToDto)],
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        public async Task<IEnumerable<ProductDto>> GetProductsByIdsAsync(List<Guid> productIds, CancellationToken ct = default)
        {
            IEnumerable<Product> products = await productRepository.GetByIdsAsync(productIds, ct);
            return products.Select(MapToDto);
        }

        public async Task<IEnumerable<ProductDto>> GetActiveProductsAsync(CancellationToken ct = default)
        {
            IEnumerable<Product> products = await productRepository.GetActiveProductsAsync(ct);
            return products.Select(MapToDto);
        }

        public async Task<ProductDto?> GetProductByIdAsync(Guid id, CancellationToken ct = default)
        {
            Product? product = await productRepository.GetByIdAsync(id);
            return product == null ? null : MapToDto(product);
        }

        public async Task<ProductDto?> GetProductByCodeAsync(string code, CancellationToken ct = default)
        {
            Product? product = await productRepository.GetByCodeAsync(code, ct);
            return product == null ? null : MapToDto(product);
        }

        public async Task<ProductDetailsDto> GetProductDetailsAsync(Guid productId, CancellationToken ct = default)
        {
            Product? product = await productRepository.GetByIdAsync(productId, ct)
                ?? throw new KeyNotFoundException($"Product with ID {productId} not found");

            // EF Core's scoped DbContext cannot run concurrent queries on the same instance.
            // These must be awaited sequentially even though they're logically independent.
            IEnumerable<StockLevelDto> stockLevels = await inventoryService.GetStockByProductAsync(productId);
            IEnumerable<InventoryTransactionDto> allTransactions = await inventoryService.GetTransactionsByProductAsync(productId);
            IEnumerable<PurchaseNoteLine> purchaseLines = await purchaseNoteRepository.GetLineItemsByProductIdAsync(productId, ct);
            IEnumerable<InvoiceLine> invoiceLines = await invoiceRepository.GetLineItemsByProductIdAsync(productId, ct);

            var stockList = stockLevels.ToList();

            // ── Build live document id set (exclude reversed docs) ────────────
            var reversalSourceIds = allTransactions
                .Where(t => t.SourceDocumentType != null && t.SourceDocumentType.EndsWith(reversalString))
                .Select(t => t.SourceDocumentId)
                .ToHashSet();

            var liveDocumentIds = allTransactions
                .Where(t => t.SourceDocumentType != null
                         && !t.SourceDocumentType.EndsWith(reversalString)
                         && !reversalSourceIds.Contains(t.SourceDocumentId))
                .Select(t => t.SourceDocumentId)
                .ToHashSet();

            // ── Aggregate purchased rows (purchase notes + payable invoices) ──
            var purchasedRows = purchaseLines
                .Where(li => liveDocumentIds.Contains(li.PurchaseNoteId))
                .Select(li => new { li.PurchaseNote.WarehouseId, li.Quantity, li.UnitPrice, TotalPrice = li.Amount })
                .Concat(invoiceLines
                    .Where(li => li.Invoice.Type == InvoiceType.Payable && liveDocumentIds.Contains(li.InvoiceId))
                    .Select(li => new { li.Invoice.WarehouseId, li.Quantity, li.UnitPrice, TotalPrice = li.TotalAmount }))
                .ToList();

            var purchasedByWarehouse = purchasedRows
                .GroupBy(r => r.WarehouseId)
                .Select(g => new WarehouseTransactionSummaryDto
                {
                    WarehouseId = g.Key,
                    Count = g.Count(),
                    TotalQuantity = g.Sum(r => r.Quantity),
                    TotalAmount = g.Sum(r => r.TotalPrice),
                    AverageUnitPrice = g.Average(r => r.UnitPrice)
                }).ToList();

            // ── Aggregate sold rows (receivable invoices) ─────────────────────
            var soldRows = invoiceLines
                .Where(li => li.Invoice.Type == InvoiceType.Receivable && liveDocumentIds.Contains(li.InvoiceId))
                .Select(li => new { li.Invoice.WarehouseId, li.Quantity, li.UnitPrice, TotalPrice = li.TotalAmount })
                .ToList();

            var soldByWarehouse = soldRows
                .GroupBy(r => r.WarehouseId)
                .Select(g => new WarehouseTransactionSummaryDto
                {
                    WarehouseId = g.Key,
                    Count = g.Count(),
                    TotalQuantity = g.Sum(r => r.Quantity),
                    TotalAmount = g.Sum(r => r.TotalPrice),
                    AverageUnitPrice = g.Average(r => r.UnitPrice)
                }).ToList();

            // ── Compute profitability ─────────────────────────────────────────
            decimal avgPurchasePrice = purchasedRows.Count > 0 ? purchasedRows.Average(r => r.UnitPrice) : 0;
            decimal sellingPrice = product.DefaultPrice;
            decimal grossMargin = sellingPrice > 0 && avgPurchasePrice > 0
                ? (sellingPrice - avgPurchasePrice) / sellingPrice * 100
                : 0;

            decimal totalPurchasedAmount = purchasedRows.Sum(r => r.TotalPrice);
            decimal totalSoldAmount = soldRows.Sum(r => r.TotalPrice);

            return new ProductDetailsDto
            {
                // Stock
                TotalStockAcrossWarehouses = stockList.Sum(sl => sl.Quantity),
                HasLowStock = stockList.Any(sl => sl.Quantity <= sl.MinimumQuantity && sl.Quantity > 0),
                StockByWarehouse = stockList.Select(sl => new WarehouseStockDto
                {
                    WarehouseId = sl.WarehouseId,
                    WarehouseName = sl.WarehouseName ?? "",
                    Quantity = sl.Quantity,
                    ReservedQuantity = sl.ReservedQuantity,
                    AvailableQuantity = sl.AvailableQuantity,
                    MinimumQuantity = sl.MinimumQuantity,
                    ReorderPoint = sl.ReorderPoint,
                    LastRestockedAt = sl.LastRestockedAt
                }).ToList(),

                // Profitability
                CurrentSellingPrice = sellingPrice,
                AveragePurchasePrice = avgPurchasePrice,
                GrossMarginPercentage = grossMargin,

                // Per-warehouse summaries
                PurchasedByWarehouse = purchasedByWarehouse,
                SoldByWarehouse = soldByWarehouse,

                // Global totals
                TotalPurchasedCount = purchasedRows.Count,
                TotalPurchasedQuantity = purchasedRows.Sum(r => r.Quantity),
                TotalPurchasedAmount = totalPurchasedAmount,
                TotalSoldCount = soldRows.Count,
                TotalSoldQuantity = soldRows.Sum(r => r.Quantity),
                TotalSoldAmount = totalSoldAmount,
                TotalProfit = totalSoldAmount - totalPurchasedAmount
            };
        }

        public async Task<PagedResult<ProductTransactionRowDto>> GetPagedProductHistoryAsync(GetProductHistoryQuery query, CancellationToken ct = default)
        {
            if (!query.Purchased)
            {
                // Sold — receivable invoices only, single repo call
                PagedResult<InvoiceLine> result = await invoiceRepository.GetPagedLineItemsByProductIdAsync(query, ct);

                return new PagedResult<ProductTransactionRowDto>
                {
                    Items = result.Items.Select(li => new ProductTransactionRowDto
                    {
                        Date = li.Invoice.IssueDate,
                        DocumentNumber = li.Invoice.InvoiceNumber,
                        DocumentUrl = $"/invoices/{li.Invoice.Id}",
                        PartyName = li.Invoice.Company?.Name ?? "-",
                        WarehouseId = li.Invoice.WarehouseId,
                        WarehouseName = li.Invoice.Warehouse?.Name ?? "-",
                        Quantity = li.Quantity,
                        UnitPrice = li.UnitPrice,
                        TotalPrice = li.TotalAmount
                    }).ToList(),
                    TotalCount = result.TotalCount,
                    Page = result.Page,
                    PageSize = result.PageSize
                };
            }
            else
            {
                // Purchased — purchase note lines + payable invoice lines merged.
                // Both repos are queried with filters applied, then merged in memory and re-paged.
                // This is necessary because the two sources have different date fields and party types.
                var unpaged = new GetProductHistoryQuery
                {
                    ProductId = query.ProductId,
                    WarehouseId = query.WarehouseId,
                    Purchased = true,
                    PartyName = query.PartyName,
                    DateFrom = query.DateFrom,
                    DateTo = query.DateTo,
                    Page = 1,
                    PageSize = int.MaxValue
                };

                PagedResult<PurchaseNoteLine> noteResult =
                    await purchaseNoteRepository.GetPagedLineItemsByProductIdAsync(unpaged, ct);

                PagedResult<InvoiceLine> invoiceResult =
                    await invoiceRepository.GetPagedLineItemsByProductIdAsync(unpaged, ct);

                List<ProductTransactionRowDto> allRows =
                [
                    .. noteResult.Items.Select(li => new ProductTransactionRowDto
                    {
                        Date = li.PurchaseNote.PurchaseDate,
                        DocumentNumber = li.PurchaseNote.NoteNumber,
                        DocumentUrl = $"/purchase-notes/{li.PurchaseNote.Id}",
                        PartyName = li.PurchaseNote.Individual?.FullName ?? "-",
                        WarehouseId = li.PurchaseNote.WarehouseId,
                        WarehouseName = li.PurchaseNote.Warehouse?.Name ?? "-",
                        Quantity = li.Quantity,
                        UnitPrice = li.UnitPrice,
                        TotalPrice = li.Amount
                    }),
                    .. invoiceResult.Items.Select(li => new ProductTransactionRowDto
                    {
                        Date = li.Invoice.IssueDate,
                        DocumentNumber = li.Invoice.InvoiceNumber,
                        DocumentUrl = $"/invoices/{li.Invoice.Id}",
                        PartyName = li.Invoice.Company?.Name ?? "-",
                        WarehouseId = li.Invoice.WarehouseId,
                        WarehouseName = li.Invoice.Warehouse?.Name ?? "-",
                        Quantity = li.Quantity,
                        UnitPrice = li.UnitPrice,
                        TotalPrice = li.TotalAmount
                    })
                ];

                List<ProductTransactionRowDto> sorted = [.. allRows.OrderByDescending(r => r.Date)];
                int totalCount = sorted.Count;

                List<ProductTransactionRowDto> page = sorted
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToList();

                return new PagedResult<ProductTransactionRowDto>
                {
                    Items = page,
                    TotalCount = totalCount,
                    Page = query.Page,
                    PageSize = query.PageSize
                };
            }
        }

        public async Task CreateProductAsync(CreateProductDto createDto)
        {
            // Validate unique code
            if (await productRepository.CodeExistsAsync(createDto.Code))
                throw new InvalidOperationException($"Product with code '{createDto.Code}' already exists");

            Product product = new()
            {
                Code = createDto.Code,
                Name = createDto.Name,
                Description = createDto.Description,
                Unit = createDto.Unit,
                DefaultPrice = createDto.DefaultPrice,
                IsActive = createDto.IsActive
            };

            await productRepository.CreateAsync(product);
        }

        public async Task UpdateProductAsync(Guid id, UpdateProductDto updateDto)
        {
            Product? product = await productRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Product with ID {id} not found");

            // Validate unique code
            if (await productRepository.CodeExistsAsync(updateDto.Code, id))
                throw new InvalidOperationException($"Product with code '{updateDto.Code}' already exists");

            product.Code = updateDto.Code;
            product.Name = updateDto.Name;
            product.Description = updateDto.Description;
            product.Unit = updateDto.Unit;
            product.DefaultPrice = updateDto.DefaultPrice;
            product.IsActive = updateDto.IsActive;

            await productRepository.UpdateAsync(product);
        }

        public async Task<bool> SetActiveStatusAsync(Guid id, bool isActive)
        {
            return await productRepository.SetActiveStatusAsync(id, isActive);
        }

        public async Task<bool> DeleteProductAsync(Guid id)
        {
            return await productRepository.DeleteAsync(id);
        }

        private static ProductDto MapToDto(Product product)
        {
            return new ProductDto
            {
                Id = product.Id,
                Code = product.Code,
                Name = product.Name,
                Description = product.Description,
                Unit = product.Unit,
                DefaultPrice = product.DefaultPrice,
                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt
            };
        }
    }
}