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

        public async Task<ProductAnalyticsDto> GetProductAnalyticsAsync(Guid productId)
        {
            Product? product = await productRepository.GetByIdAsync(productId)
                ?? throw new KeyNotFoundException($"Product with ID {productId} not found");

            var analytics = new ProductAnalyticsDto
            {
                CurrentSellingPrice = product.DefaultPrice
            };

            // Get stock information
            await PopulateStockAnalytics(productId, analytics);

            // Get recent transactions
            await PopulateRecentTransactions(productId, analytics);

            return analytics;
        }

        public async Task<ProductTransactionHistoryDto> GetProductTransactionHistoryAsync(Guid productId)
        {
            var result = new ProductTransactionHistoryDto();

            // Only show documents that have live (non-reversed) inventory transactions
            IEnumerable<InventoryTransactionDto> allTransactions = await inventoryService.GetTransactionsByProductAsync(productId);

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

            IEnumerable<PurchaseNoteLine> purchaseLines = await purchaseNoteRepository
                .GetLineItemsByProductIdAsync(productId);

            result.Purchased = purchaseLines
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
                }).ToList();

            IEnumerable<InvoiceLine> invoiceLines = await invoiceRepository
                .GetLineItemsByProductIdAsync(productId);

            result.Purchased.AddRange(invoiceLines
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

            result.Sold = invoiceLines
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
                }).ToList();

            return result;
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

        private async Task PopulateStockAnalytics(Guid productId, ProductAnalyticsDto analytics)
        {
            try
            {
                IEnumerable<StockLevelDto> stockLevels = await inventoryService.GetStockByProductAsync(productId);
                var stockList = stockLevels.ToList();

                analytics.TotalStockAcrossWarehouses = stockList.Sum(sl => sl.Quantity);
                analytics.HasLowStock = stockList.Any(sl => sl.Quantity <= sl.MinimumQuantity && sl.Quantity > 0);

                analytics.StockByWarehouse = stockList.Select(sl => new WarehouseStockDto
                {
                    WarehouseId = sl.WarehouseId,
                    WarehouseName = sl.WarehouseName ?? "",
                    Quantity = sl.Quantity,
                    ReservedQuantity = sl.ReservedQuantity,
                    AvailableQuantity = sl.AvailableQuantity,
                    MinimumQuantity = sl.MinimumQuantity,
                    ReorderPoint = sl.ReorderPoint,
                    LastRestockedAt = sl.LastRestockedAt
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading stock analytics: {ex.Message}");
            }
        }

        private async Task PopulateRecentTransactions(Guid productId, ProductAnalyticsDto analytics)
        {
            try
            {
                IEnumerable<InventoryTransactionDto> transactions = await inventoryService.GetTransactionsByProductAsync(productId);

                var reversalSourceIds = transactions
                    .Where(t => t.SourceDocumentType != null && t.SourceDocumentType.EndsWith(reversalString))
                    .Select(t => t.SourceDocumentId)
                    .ToHashSet();

                var meaningfulTransactions = transactions
                    .Where(t => t.SourceDocumentType == null
                             || (!t.SourceDocumentType.EndsWith(reversalString)
                                 && !reversalSourceIds.Contains(t.SourceDocumentId)))
                    .ToList();

                analytics.RecentTransactions = meaningfulTransactions
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(10)
                    .Select(t => new RecentTransactionDto
                    {
                        Date = t.CreatedAt,
                        Type = t.Type.ToString(),
                        WarehouseName = t.WarehouseName ?? "",
                        Quantity = t.Quantity,
                        SourceDocument = t.SourceDocumentType != null ? $"{t.SourceDocumentType}" : null,
                        Note = t.Note
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading recent transactions: {ex.Message}");
            }
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