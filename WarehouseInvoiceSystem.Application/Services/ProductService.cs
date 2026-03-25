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
    using WarehouseInvoiceSystem.Domain.Queries.Results;

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
            decimal avgSellingPrice = soldRows.Count > 0 ? soldRows.Average(r => r.UnitPrice) : product.SellingPrice;
            decimal grossMargin = avgSellingPrice > 0 && avgPurchasePrice > 0
                ? (avgSellingPrice - avgPurchasePrice) / avgSellingPrice * 100
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
                AverageSellingPrice = avgSellingPrice,
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
                        DocumentType = "Invoice",
                        PartyName = li.Invoice.Company.Name,
                        WarehouseId = li.Invoice.WarehouseId,
                        WarehouseName = li.Invoice.Warehouse.Name,
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
                // Purchased — query the vw_product_purchase_history view which UNIONs
                // purchase-note lines and payable invoice lines. All filtering, sorting,
                // and pagination happen in SQL — no in-memory merge needed.
                PagedResult<ProductPurchaseHistoryView> result =
                    await invoiceRepository.GetPagedPurchasedHistoryAsync(query, ct);

                return new PagedResult<ProductTransactionRowDto>
                {
                    Items = result.Items.Select(v => new ProductTransactionRowDto
                    {
                        Date           = v.Date,
                        DocumentNumber = v.DocumentNumber,
                        DocumentUrl    = v.DocumentUrl,
                        DocumentType   = v.DocumentUrl.StartsWith("/purchase-notes/") ? "PurchaseNote" : "Invoice",
                        PartyName      = v.PartyName,
                        WarehouseId    = v.WarehouseId,
                        WarehouseName  = v.WarehouseName,
                        Quantity       = v.Quantity,
                        UnitPrice      = v.UnitPrice,
                        TotalPrice     = v.TotalPrice
                    }).ToList(),
                    TotalCount = result.TotalCount,
                    Page       = result.Page,
                    PageSize   = result.PageSize
                };
            }
        }

        public async Task<(decimal TotalQuantity, decimal TotalAmount)> GetProductHistoryTotalsAsync(
            GetProductHistoryQuery query, CancellationToken ct = default)
        {
            return query.Purchased
                ? await invoiceRepository.GetPurchasedHistoryTotalsAsync(query, ct)
                : await invoiceRepository.GetSoldHistoryTotalsAsync(query, ct);
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
                CostPrice = createDto.CostPrice,
                SellingPrice = createDto.SellingPrice,
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
            product.CostPrice = updateDto.CostPrice;
            product.SellingPrice = updateDto.SellingPrice;
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

        public async Task<List<PartnerComparisonDto>> GetPartnerComparisonAsync(
            Guid productId,
            PartnerComparisonMode mode,
            Guid? warehouseId,
            DateTime? dateFrom,
            DateTime? dateTo,
            List<Guid>? partnerIds = null,
            CancellationToken ct = default)
        {
            List<PartnerComparisonDto> results = mode switch
            {
                PartnerComparisonMode.Individuals => await GetIndividualComparison(productId, warehouseId, dateFrom, dateTo, ct),
                PartnerComparisonMode.Vendors => await GetInvoiceComparison(productId, InvoiceType.Payable, warehouseId, dateFrom, dateTo, ct),
                PartnerComparisonMode.Clients => await GetInvoiceComparison(productId, InvoiceType.Receivable, warehouseId, dateFrom, dateTo, ct),
                _ => []
            };

            if (partnerIds is { Count: > 0 })
                results = results.Where(p => partnerIds.Contains(p.PartnerId)).ToList();

            return results;
        }

        private async Task<List<PartnerComparisonDto>> GetIndividualComparison(
            Guid productId, Guid? warehouseId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct)
        {
            IEnumerable<PurchaseNoteLine> lines = await purchaseNoteRepository
                .GetLineItemsByProductIdAsync(productId, warehouseId, dateFrom, dateTo, ct);

            return lines
                .GroupBy(li => new { li.PurchaseNote.IndividualId, Name = li.PurchaseNote.Individual?.FullName ?? "" })
                .Select(g => new PartnerComparisonDto(
                    g.Key.IndividualId,
                    g.Key.Name,
                    g.Sum(li => li.Quantity),
                    g.Sum(li => li.Amount),
                    g.Average(li => li.UnitPrice),
                    g.Select(li => li.PurchaseNoteId).Distinct().Count()
                ))
                .OrderByDescending(p => p.TotalQuantity)
                .ToList();
        }

        private async Task<List<PartnerComparisonDto>> GetInvoiceComparison(
            Guid productId, InvoiceType type, Guid? warehouseId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct)
        {
            IEnumerable<InvoiceLine> lines = await invoiceRepository
                .GetLineItemsByProductIdAsync(productId, type, warehouseId, dateFrom, dateTo, ct);

            return lines
                .GroupBy(li => new { li.Invoice.CompanyId, Name = li.Invoice.Company?.Name ?? "" })
                .Select(g => new PartnerComparisonDto(
                    g.Key.CompanyId,
                    g.Key.Name,
                    g.Sum(li => li.Quantity),
                    g.Sum(li => li.Amount),
                    g.Average(li => li.UnitPrice),
                    g.Select(li => li.InvoiceId).Distinct().Count()
                ))
                .OrderByDescending(p => p.TotalQuantity)
                .ToList();
        }

        public async Task<List<ProductComparisonDto>> GetProductComparisonAsync(
            List<Guid> productIds, Guid? warehouseId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default)
        {
            if (productIds.Count < 2) return [];

            Task<IEnumerable<PurchaseNoteLine>> purchaseTask = purchaseNoteRepository
                .GetLineItemsByProductIdsAsync(productIds, warehouseId, dateFrom, dateTo, ct);
            Task<IEnumerable<InvoiceLine>> payableTask = invoiceRepository
                .GetLineItemsByProductIdsAsync(productIds, warehouseId, dateFrom, dateTo, InvoiceType.Payable, ct);
            Task<IEnumerable<InvoiceLine>> receivableTask = invoiceRepository
                .GetLineItemsByProductIdsAsync(productIds, warehouseId, dateFrom, dateTo, InvoiceType.Receivable, ct);

            await Task.WhenAll(purchaseTask, payableTask, receivableTask);

            IEnumerable<PurchaseNoteLine> purchaseLines = purchaseTask.Result;
            IEnumerable<InvoiceLine> payableLines = payableTask.Result;
            IEnumerable<InvoiceLine> receivableLines = receivableTask.Result;

            // Purchase notes = incoming from individuals
            var purchaseByProduct = purchaseLines
                .GroupBy(li => li.ProductId)
                .ToDictionary(
                    g => g.Key,
                    g => (Qty: g.Sum(li => li.Quantity), Amt: g.Sum(li => li.Amount), Docs: g.Select(li => li.PurchaseNoteId).Distinct().Count()));

            // Payable invoices = incoming from vendors
            var payableByProduct = payableLines
                .GroupBy(li => li.ProductId)
                .ToDictionary(
                    g => g.Key,
                    g => (Qty: g.Sum(li => li.Quantity), Amt: g.Sum(li => li.Amount), Docs: g.Select(li => li.InvoiceId).Distinct().Count()));

            // Receivable invoices = outgoing to clients
            var receivableByProduct = receivableLines
                .GroupBy(li => li.ProductId)
                .ToDictionary(
                    g => g.Key,
                    g => (Qty: g.Sum(li => li.Quantity), Amt: g.Sum(li => li.Amount), Docs: g.Select(li => li.InvoiceId).Distinct().Count()));

            // Build product lookup for names/codes/units
            var products = purchaseLines.Select(li => li.Product)
                .Concat(payableLines.Select(li => li.Product))
                .Concat(receivableLines.Select(li => li.Product))
                .Where(p => p != null)
                .DistinctBy(p => p!.Id)
                .ToDictionary(p => p!.Id);

            return productIds
                .Where(id => products.ContainsKey(id))
                .Select(id =>
                {
                    Product p = products[id];
                    purchaseByProduct.TryGetValue(id, out var pn);
                    payableByProduct.TryGetValue(id, out var pay);
                    receivableByProduct.TryGetValue(id, out var rec);

                    decimal inQty = pn.Qty + pay.Qty;
                    decimal inAmt = pn.Amt + pay.Amt;
                    decimal outQty = rec.Qty;
                    decimal outAmt = rec.Amt;
                    int docs = pn.Docs + pay.Docs + rec.Docs;

                    return new ProductComparisonDto(
                        id, p.Name, p.Code, p.Unit,
                        inQty, outQty, inAmt, outAmt, docs);
                })
                .ToList();
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
                CostPrice = product.CostPrice,
                SellingPrice = product.SellingPrice,
                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt
            };
        }
    }
}