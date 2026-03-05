namespace WarehouseInvoiceSystem.Application.Services
{
    using WarehouseInvoiceSystem.Application.DTOs.InventoryTransaction;
    using WarehouseInvoiceSystem.Application.DTOs.Invoice;
    using WarehouseInvoiceSystem.Application.DTOs.Product;
    using WarehouseInvoiceSystem.Application.DTOs.PurchaseNote;
    using WarehouseInvoiceSystem.Application.DTOs.StockLevel;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Interfaces;

    public class ProductService(IProductRepository productRepository,
                                IInventoryService inventoryService,
                                IInvoiceService invoiceService,
                                IPurchaseNoteService purchaseNoteService) : IProductService
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
                // Get all invoices (receivable = sales)
                IEnumerable<InvoiceDto> allInvoices = await invoiceService.GetAllInvoicesAsync();
                var salesInvoices = allInvoices
                    .Where(i => i.Type == Domain.Enums.InvoiceType.Receivable)
                    .ToList();

                // Get all line items for this product
                var productLines = salesInvoices
                    .SelectMany(i => i.LineItems.Where(li => li.ProductId == productId))
                    .ToList();

                if (productLines.Count > 0)
                {
                    analytics.TotalUnitsSold = productLines.Sum(li => li.Quantity);
                    analytics.TotalRevenue = productLines.Sum(li => li.TotalAmount);
                    analytics.AverageSaleQuantity = (decimal)productLines.Average(li => li.Quantity);

                    // Last sale date
                    var lastSale = salesInvoices
                        .Where(i => i.LineItems.Any(li => li.ProductId == productId))
                        .OrderByDescending(i => i.IssueDate)
                        .FirstOrDefault();
                    analytics.LastSaleDate = lastSale?.IssueDate;

                    // Top customer
                    var customerSales = salesInvoices
                        .Where(i => i.LineItems.Any(li => li.ProductId == productId))
                        .GroupBy(i => i.CompanyName)
                        .Select(g => new
                        {
                            CustomerName = g.Key,
                            TotalQuantity = g.SelectMany(i => i.LineItems.Where(li => li.ProductId == productId))
                                            .Sum(li => li.Quantity)
                        })
                        .OrderByDescending(x => x.TotalQuantity)
                        .FirstOrDefault();

                    if (customerSales != null)
                    {
                        analytics.TopCustomer = customerSales.CustomerName;
                        analytics.TopCustomerQuantity = customerSales.TotalQuantity;
                    }
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
                // Get all purchase notes
                IEnumerable<PurchaseNoteDto> allPurchaseNotes = await purchaseNoteService.GetAllPurchaseNotesAsync();
                var purchaseNotesList = allPurchaseNotes.ToList();

                // Get all line items for this product
                var productLines = purchaseNotesList
                    .SelectMany(pn => pn.LineItems.Where(li => li.ProductId == productId))
                    .ToList();

                if (productLines.Count > 0)
                {
                    analytics.TotalUnitsPurchased = productLines.Sum(li => li.Quantity);
                    analytics.TotalPurchaseCost = productLines.Sum(li => li.Amount);
                    analytics.AveragePurchaseQuantity = (decimal)productLines.Average(li => li.Quantity);
                    analytics.AveragePurchasePrice = productLines.Average(li => li.UnitPrice);

                    // Last purchase date
                    var lastPurchase = purchaseNotesList
                        .Where(pn => pn.LineItems.Any(li => li.ProductId == productId))
                        .OrderByDescending(pn => pn.PurchaseDate)
                        .FirstOrDefault();
                    analytics.LastPurchaseDate = lastPurchase?.PurchaseDate;

                    // Top supplier (individual)
                    var supplierPurchases = purchaseNotesList
                        .Where(pn => pn.LineItems.Any(li => li.ProductId == productId))
                        .GroupBy(pn => pn.IndividualFullName)
                        .Select(g => new
                        {
                            SupplierName = g.Key,
                            TotalQuantity = g.SelectMany(pn => pn.LineItems.Where(li => li.ProductId == productId))
                                            .Sum(li => li.Quantity)
                        })
                        .OrderByDescending(x => x.TotalQuantity)
                        .FirstOrDefault();

                    if (supplierPurchases != null)
                    {
                        analytics.TopSupplier = supplierPurchases.SupplierName;
                        analytics.TopSupplierQuantity = supplierPurchases.TotalQuantity;
                    }
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