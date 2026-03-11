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

        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
        {
            IEnumerable<Product> products = await productRepository.GetAllAsync();
            return products.Select(MapToDto);
        }

        public async Task<PagedResult<ProductDto>> GetPagedAsync(GetProductsQuery query)
        {
            PagedResult<Product> result = await productRepository.GetPagedAsync(query);
            return new PagedResult<ProductDto>
            {
                Items = [.. result.Items.Select(MapToDto)],
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        public async Task<IEnumerable<ProductDto>> GetProductsByIdsAsync(List<Guid> productIds)
        {
            IEnumerable<Product> products = await productRepository.GetByIdsAsync(productIds);
            return products.Select(MapToDto);
        }

        public async Task<IEnumerable<ProductDto>> GetActiveProductsAsync()
        {
            IEnumerable<Product> products = await productRepository.GetActiveProductsAsync();
            return products.Select(MapToDto);
        }

        public async Task<ProductDto?> GetProductByIdAsync(Guid id)
        {
            Product? product = await productRepository.GetByIdAsync(id);
            return product == null ? null : MapToDto(product);
        }

        public async Task<ProductDto?> GetProductByCodeAsync(string code)
        {
            Product? product = await productRepository.GetByCodeAsync(code);
            return product == null ? null : MapToDto(product);
        }

        public async Task<ProductDetailsDto> GetProductDetailsAsync(Guid productId)
        {
            Product? product = await productRepository.GetByIdAsync(productId)
                ?? throw new KeyNotFoundException($"Product with ID {productId} not found");

            // EF Core's scoped DbContext cannot run concurrent queries on the same instance.
            // These must be awaited sequentially even though they're logically independent.
            IEnumerable<StockLevelDto> stockLevels = await inventoryService.GetStockByProductAsync(productId);
            IEnumerable<InventoryTransactionDto> allTransactions = await inventoryService.GetTransactionsByProductAsync(productId);
            IEnumerable<PurchaseNoteLine> purchaseLines = await purchaseNoteRepository.GetLineItemsByProductIdAsync(productId);
            IEnumerable<InvoiceLine> invoiceLines = await invoiceRepository.GetLineItemsByProductIdAsync(productId);

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

            // ── Build result ─────────────────────────────────────────────────
            var details = new ProductDetailsDto
            {
                CurrentSellingPrice = product.DefaultPrice,

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

                // Transaction history — purchased from purchase notes
                Purchased = purchaseLines
                    .Where(li => liveDocumentIds.Contains(li.PurchaseNoteId))
                    .Select(li => new ProductTransactionRowDto
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
                    }).ToList(),

                // Transaction history — sold via receivable invoices
                Sold = invoiceLines
                    .Where(li => li.Invoice.Type == InvoiceType.Receivable
                              && liveDocumentIds.Contains(li.InvoiceId))
                    .Select(li => new ProductTransactionRowDto
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
                    }).ToList()
            };

            // Purchased also includes payable invoices (goods purchased via invoice)
            details.Purchased.AddRange(invoiceLines
                .Where(li => li.Invoice.Type == InvoiceType.Payable
                          && liveDocumentIds.Contains(li.InvoiceId))
                .Select(li => new ProductTransactionRowDto
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
                }));

            // ── Gross margin — based on average purchase price vs selling price ─
            if (details.CurrentSellingPrice > 0 && details.AveragePurchasePrice > 0)
                details.GrossMarginPercentage = (details.CurrentSellingPrice - details.AveragePurchasePrice)
                    / details.CurrentSellingPrice * 100;

            // ── Raw movements for the stock movements tab ─────────────────────
            details.Movements = allTransactions.ToList();

            return details;
        }

        public async Task<ProductDto> CreateProductAsync(CreateProductDto createDto)
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

            Product created = await productRepository.CreateAsync(product);
            return MapToDto(created);
        }

        public async Task<ProductDto> UpdateProductAsync(Guid id, UpdateProductDto updateDto)
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

            Product updated = await productRepository.UpdateAsync(product);
            return MapToDto(updated);
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