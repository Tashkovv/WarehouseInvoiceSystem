namespace WarehouseInvoiceSystem.Application.Services
{
    using WarehouseInvoiceSystem.Application.DTOs.InventoryTransaction;
    using WarehouseInvoiceSystem.Application.DTOs.Product;
    using WarehouseInvoiceSystem.Application.DTOs.StockLevel;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Interfaces;

    public class ProductService(IProductRepository productRepository,
                                IInventoryService inventoryService,
                                IInvoiceRepository invoiceRepository,
                                IPurchaseNoteRepository purchaseNoteRepository) : IProductService
    {
        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
        {
            IEnumerable<Product> products = await productRepository.GetAllAsync();
            return products.Select(MapToDto);
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

            // Get sales analytics (from invoices)
            await PopulateSalesAnalytics(productId, analytics);

            // Get purchase analytics (from purchase notes)
            await PopulatePurchaseAnalytics(productId, analytics);

            // Calculate profitability
            CalculateProfitability(analytics);

            // Get recent transactions
            await PopulateRecentTransactions(productId, analytics);

            return analytics;
        }

        public async Task<ProductTransactionHistoryDto> GetProductTransactionHistoryAsync(Guid productId)
        {
            var result = new ProductTransactionHistoryDto();

            IEnumerable<PurchaseNoteLine> purchaseLines = await purchaseNoteRepository
                .GetLineItemsByProductIdAsync(productId);

            result.Purchased = purchaseLines.Select(li => new ProductTransactionRowDto
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
                .Where(li => li.Invoice.Type == InvoiceType.Payable)
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
                .Where(li => li.Invoice.Type == InvoiceType.Receivable)
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
                analytics.TotalStockValue = stockList.Sum(sl => sl.Quantity * analytics.CurrentSellingPrice);
                analytics.IsOutOfStock = analytics.TotalStockAcrossWarehouses == 0;
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

        private async Task PopulateSalesAnalytics(Guid productId, ProductAnalyticsDto analytics)
        {
            try
            {
                IEnumerable<InvoiceLine> lines = await invoiceRepository
                    .GetLineItemsByProductIdAsync(productId);

                var salesLines = lines
                    .Where(li => li.Invoice.Type == InvoiceType.Receivable)
                    .ToList();

                if (salesLines.Count == 0) return;

                analytics.TotalUnitsSold = salesLines.Sum(li => li.Quantity);
                analytics.TotalRevenue = salesLines.Sum(li => li.TotalAmount);
                analytics.AverageSaleQuantity = (decimal)salesLines.Average(li => li.Quantity);
                analytics.LastSaleDate = salesLines.Max(li => li.Invoice.IssueDate);

                var topCustomer = salesLines
                    .GroupBy(li => li.Invoice.Company?.Name ?? "")
                    .Select(g => new { Name = g.Key, Qty = g.Sum(li => li.Quantity) })
                    .OrderByDescending(x => x.Qty)
                    .FirstOrDefault();

                if (topCustomer != null)
                {
                    analytics.TopCustomer = topCustomer.Name;
                    analytics.TopCustomerQuantity = topCustomer.Qty;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading sales analytics: {ex.Message}");
            }
        }

        private async Task PopulatePurchaseAnalytics(Guid productId, ProductAnalyticsDto analytics)
        {
            try
            {
                IEnumerable<PurchaseNoteLine> lines = await purchaseNoteRepository
                    .GetLineItemsByProductIdAsync(productId);

                var purchaseLines = lines.ToList();

                if (purchaseLines.Count == 0) return;

                analytics.TotalUnitsPurchased = purchaseLines.Sum(li => li.Quantity);
                analytics.TotalPurchaseCost = purchaseLines.Sum(li => li.Amount);
                analytics.AveragePurchaseQuantity = (decimal)purchaseLines.Average(li => li.Quantity);
                analytics.AveragePurchasePrice = purchaseLines.Average(li => li.UnitPrice);
                analytics.LastPurchaseDate = purchaseLines.Max(li => li.PurchaseNote.PurchaseDate);

                var topSupplier = purchaseLines
                    .GroupBy(li => li.PurchaseNote.Individual?.FullName ?? "")
                    .Select(g => new { Name = g.Key, Qty = g.Sum(li => li.Quantity) })
                    .OrderByDescending(x => x.Qty)
                    .FirstOrDefault();

                if (topSupplier != null)
                {
                    analytics.TopSupplier = topSupplier.Name;
                    analytics.TopSupplierQuantity = topSupplier.Qty;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading purchase analytics: {ex.Message}");
            }
        }

        private static void CalculateProfitability(ProductAnalyticsDto analytics)
        {
            if (analytics.AveragePurchasePrice > 0 && analytics.CurrentSellingPrice > 0)
            {
                decimal grossProfit = analytics.CurrentSellingPrice - analytics.AveragePurchasePrice;
                analytics.GrossMarginPercentage = (grossProfit / analytics.CurrentSellingPrice) * 100;
                analytics.EstimatedProfitIfSoldAll = grossProfit * analytics.TotalStockAcrossWarehouses;
            }
        }

        private async Task PopulateRecentTransactions(Guid productId, ProductAnalyticsDto analytics)
        {
            try
            {
                IEnumerable<InventoryTransactionDto> transactions = await inventoryService.GetTransactionsByProductAsync(productId);
                analytics.TotalTransactions = transactions.Count();

                analytics.RecentTransactions = transactions
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