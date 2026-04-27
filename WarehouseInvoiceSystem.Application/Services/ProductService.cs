namespace WarehouseInvoiceSystem.Application.Services
{
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
            _ = await productRepository.GetByIdAsync(productId, ct)
                ?? throw new KeyNotFoundException($"Product with ID {productId} not found");

            List<StockLevelDto> stockList = (await inventoryService.GetStockByProductAsync(productId, ct)).ToList();

            return new ProductDetailsDto
            {
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
                    LastRestockedAt = sl.LastRestockedAt,
                    IsDefault = sl.IsDefault
                }).ToList()
            };
        }

        public Task<ProductTransactionSummaryDto> GetProductTransactionSummaryAsync(Guid productId, DateTime? dateFrom = null, DateTime? dateTo = null, CancellationToken ct = default) =>
            BuildTransactionSummaryAsync(productId, dateFrom, dateTo, ct);

        private async Task<ProductTransactionSummaryDto> BuildTransactionSummaryAsync(Guid productId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct)
        {
            Task<List<ProductWarehouseSummary>> purchaseNoteTask = purchaseNoteRepository.GetProductPurchaseNoteAggregatesAsync(productId, dateFrom, dateTo, ct);
            Task<List<ProductWarehouseSummary>> soldTask = invoiceRepository.GetProductSoldAggregatesAsync(productId, dateFrom, dateTo, ct);
            Task<List<ProductWarehouseSummary>> payableTask = invoiceRepository.GetProductPayableAggregatesAsync(productId, dateFrom, dateTo, ct);

            await Task.WhenAll(purchaseNoteTask, soldTask, payableTask);

            List<ProductWarehouseSummary> purchaseNoteAgg = purchaseNoteTask.Result;
            List<ProductWarehouseSummary> soldAgg = soldTask.Result;
            List<ProductWarehouseSummary> payableAgg = payableTask.Result;

            var purchasedByWarehouse = purchaseNoteAgg
                .Concat(payableAgg)
                .GroupBy(x => x.WarehouseId)
                .Select(g =>
                {
                    int cnt = g.Sum(x => x.Count);
                    return new WarehouseTransactionSummaryDto
                    {
                        WarehouseId = g.Key,
                        Count = cnt,
                        TotalQuantity = g.Sum(x => x.TotalQuantity),
                        TotalAmount = g.Sum(x => x.TotalAmount),
                        AverageUnitPrice = cnt > 0 ? g.Sum(x => x.AvgUnitPrice * x.Count) / cnt : 0
                    };
                }).ToList();

            var soldByWarehouse = soldAgg
                .Select(x => new WarehouseTransactionSummaryDto
                {
                    WarehouseId = x.WarehouseId,
                    Count = x.Count,
                    TotalQuantity = x.TotalQuantity,
                    TotalAmount = x.TotalAmount,
                    AverageUnitPrice = x.AvgUnitPrice
                }).ToList();

            int totalPurchasedCount = purchaseNoteAgg.Sum(x => x.Count) + payableAgg.Sum(x => x.Count);
            decimal totalPurchasedAmount = purchaseNoteAgg.Sum(x => x.TotalAmount) + payableAgg.Sum(x => x.TotalAmount);
            int totalSoldCount = soldAgg.Sum(x => x.Count);
            decimal totalSoldAmount = soldAgg.Sum(x => x.TotalAmount);

            decimal avgPurchasePrice = totalPurchasedCount > 0
                ? (purchaseNoteAgg.Sum(x => x.AvgUnitPrice * x.Count) + payableAgg.Sum(x => x.AvgUnitPrice * x.Count)) / totalPurchasedCount
                : 0;
            decimal avgSellingPrice = totalSoldCount > 0
                ? soldAgg.Sum(x => x.AvgUnitPrice * x.Count) / totalSoldCount
                : 0;

            return new ProductTransactionSummaryDto
            {
                PurchasedByWarehouse = purchasedByWarehouse,
                SoldByWarehouse = soldByWarehouse,
                TotalPurchasedCount = totalPurchasedCount,
                TotalPurchasedQuantity = purchaseNoteAgg.Sum(x => x.TotalQuantity) + payableAgg.Sum(x => x.TotalQuantity),
                TotalPurchasedAmount = totalPurchasedAmount,
                TotalSoldCount = totalSoldCount,
                TotalSoldQuantity = soldAgg.Sum(x => x.TotalQuantity),
                TotalSoldAmount = totalSoldAmount,
                AveragePurchasePrice = avgPurchasePrice,
                AverageSellingPrice = avgSellingPrice
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

        public Task<(int Count, decimal TotalQuantity, decimal TotalAmount)> GetProductHistoryStatsAsync(
            GetProductHistoryQuery query, CancellationToken ct = default) =>
            query.Purchased
                ? invoiceRepository.GetPurchasedHistoryStatsAsync(query, ct)
                : invoiceRepository.GetSoldHistoryStatsAsync(query, ct);

        public async Task CreateProductAsync(CreateProductDto createDto)
        {
            // Validate unique code
            if (await productRepository.CodeExistsAsync(createDto.Code))
                throw new InvalidOperationException("ProductCodeAlreadyExists");

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
            Product? product = await productRepository.GetByIdAsync(id);
            if (product == null) return false;
            if (product.IsActive)
                throw new InvalidOperationException("ProductMustBeInactiveToDelete");

            bool inActiveInvoices = await invoiceRepository.IsProductInActiveInvoicesAsync(id);
            bool inActivePurchaseNotes = await purchaseNoteRepository.IsProductInActivePurchaseNotesAsync(id);
            if (inActiveInvoices || inActivePurchaseNotes)
                throw new InvalidOperationException("ProductInUseCannotDelete");

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
            return mode switch
            {
                PartnerComparisonMode.Individuals => await GetIndividualComparison(productId, warehouseId, partnerIds, dateFrom, dateTo, ct),
                PartnerComparisonMode.Vendors => await GetInvoiceComparison(productId, InvoiceType.Payable, warehouseId, partnerIds, dateFrom, dateTo, ct),
                PartnerComparisonMode.Clients => await GetInvoiceComparison(productId, InvoiceType.Receivable, warehouseId, partnerIds, dateFrom, dateTo, ct),
                _ => []
            };
        }

        private async Task<List<PartnerComparisonDto>> GetIndividualComparison(
            Guid productId, Guid? warehouseId, List<Guid>? individualIds, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct)
        {
            List<PartnerSummary> rows = await purchaseNoteRepository
                .GetIndividualAggregatesForProductAsync(productId, warehouseId, individualIds, dateFrom, dateTo, ct);

            return rows.Select(s => new PartnerComparisonDto(
                s.PartnerId, s.PartnerName, s.TotalQuantity, s.TotalAmount, s.AvgUnitPrice, s.DocumentCount))
                .ToList();
        }

        private async Task<List<PartnerComparisonDto>> GetInvoiceComparison(
            Guid productId, InvoiceType type, Guid? warehouseId, List<Guid>? companyIds, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct)
        {
            List<PartnerSummary> rows = await invoiceRepository
                .GetCompanyAggregatesForProductAsync(productId, type, warehouseId, companyIds, dateFrom, dateTo, ct);

            return rows.Select(s => new PartnerComparisonDto(
                s.PartnerId, s.PartnerName, s.TotalQuantity, s.TotalAmount, s.AvgUnitPrice, s.DocumentCount))
                .ToList();
        }

        public async Task<List<ProductComparisonDto>> GetProductComparisonAsync(
            List<Guid> productIds, Guid? warehouseId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default)
        {
            if (productIds.Count < 2) return [];

            Task<List<ProductSummary>> purchaseTask = purchaseNoteRepository
                .GetProductsPurchaseAggregatesAsync(productIds, warehouseId, dateFrom, dateTo, ct);
            Task<List<ProductSummary>> payableTask = invoiceRepository
                .GetProductsInvoiceAggregatesAsync(productIds, InvoiceType.Payable, warehouseId, dateFrom, dateTo, ct);
            Task<List<ProductSummary>> receivableTask = invoiceRepository
                .GetProductsInvoiceAggregatesAsync(productIds, InvoiceType.Receivable, warehouseId, dateFrom, dateTo, ct);
            Task<IEnumerable<Product>> productsTask = productRepository.GetByIdsAsync(productIds, ct);

            await Task.WhenAll(purchaseTask, payableTask, receivableTask, productsTask);

            Dictionary<Guid, ProductSummary> purchaseByProduct = purchaseTask.Result.ToDictionary(s => s.ProductId);
            Dictionary<Guid, ProductSummary> payableByProduct = payableTask.Result.ToDictionary(s => s.ProductId);
            Dictionary<Guid, ProductSummary> receivableByProduct = receivableTask.Result.ToDictionary(s => s.ProductId);
            Dictionary<Guid, Product> products = productsTask.Result.ToDictionary(p => p.Id);

            return productIds
                .Where(id => products.ContainsKey(id))
                .Select(id =>
                {
                    Product p = products[id];
                    purchaseByProduct.TryGetValue(id, out ProductSummary? pn);
                    payableByProduct.TryGetValue(id, out ProductSummary? pay);
                    receivableByProduct.TryGetValue(id, out ProductSummary? rec);

                    decimal inQty = (pn?.TotalQuantity ?? 0) + (pay?.TotalQuantity ?? 0);
                    decimal inAmt = (pn?.TotalAmount ?? 0) + (pay?.TotalAmount ?? 0);
                    decimal outQty = rec?.TotalQuantity ?? 0;
                    decimal outAmt = rec?.TotalAmount ?? 0;
                    int docs = (pn?.DocumentCount ?? 0) + (pay?.DocumentCount ?? 0) + (rec?.DocumentCount ?? 0);

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