namespace WarehouseInvoiceSystem.Tests.Services.ProductService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Application.DTOs.InventoryTransaction;
using WarehouseInvoiceSystem.Application.DTOs.StockLevel;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;

public class ProductDetailsTests : ProductServiceTestBase
{
    [Fact]
    public async Task NotFound_ThrowsKeyNotFound()
    {
        var id = Guid.NewGuid();
        ProductRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Product?)null);
        var service = CreateService();

        await service.Invoking(s => s.GetProductDetailsAsync(id))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task EmptyData_ReturnsZeroTotals()
    {
        var product = CreateEntity();
        SetupDetailsLookup(product.Id, product);
        SetupEmptyDetailsData(product.Id);
        var service = CreateService();

        var result = await service.GetProductDetailsAsync(product.Id);

        result.TotalStockAcrossWarehouses.Should().Be(0);
        result.TotalPurchasedCount.Should().Be(0);
        result.TotalSoldCount.Should().Be(0);
        result.TotalProfit.Should().Be(0);
        result.GrossMarginPercentage.Should().Be(0);
    }

    [Fact]
    public async Task StockAggregation_SumsAcrossWarehouses()
    {
        var product = CreateEntity();
        SetupDetailsLookup(product.Id, product);
        InventoryService.GetStockByProductAsync(product.Id).Returns(new[]
        {
            new StockLevelDto { WarehouseId = Guid.NewGuid(), WarehouseName = "WH1", Quantity = 50, MinimumQuantity = 10 },
            new StockLevelDto { WarehouseId = Guid.NewGuid(), WarehouseName = "WH2", Quantity = 30, MinimumQuantity = 10 }
        });
        InventoryService.GetTransactionsByProductAsync(product.Id).Returns(Array.Empty<InventoryTransactionDto>());
        PurchaseNoteRepo.GetLineItemsByProductIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(Array.Empty<PurchaseNoteLine>());
        InvoiceRepo.GetLineItemsByProductIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(Array.Empty<InvoiceLine>());
        var service = CreateService();

        var result = await service.GetProductDetailsAsync(product.Id);

        result.TotalStockAcrossWarehouses.Should().Be(80);
        result.StockByWarehouse.Should().HaveCount(2);
    }

    [Fact]
    public async Task HasLowStock_TrueWhenQuantityBelowMinimum()
    {
        var product = CreateEntity();
        SetupDetailsLookup(product.Id, product);
        InventoryService.GetStockByProductAsync(product.Id).Returns(new[]
        {
            new StockLevelDto { WarehouseId = Guid.NewGuid(), Quantity = 5, MinimumQuantity = 10 } // low stock: 5 <= 10 && 5 > 0
        });
        InventoryService.GetTransactionsByProductAsync(product.Id).Returns(Array.Empty<InventoryTransactionDto>());
        PurchaseNoteRepo.GetLineItemsByProductIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(Array.Empty<PurchaseNoteLine>());
        InvoiceRepo.GetLineItemsByProductIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(Array.Empty<InvoiceLine>());
        var service = CreateService();

        var result = await service.GetProductDetailsAsync(product.Id);

        result.HasLowStock.Should().BeTrue();
    }

    [Fact]
    public async Task ReversalFiltering_ExcludesReversedDocuments()
    {
        var product = CreateEntity();
        var liveDocId = Guid.NewGuid();
        var reversedDocId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        SetupDetailsLookup(product.Id, product);
        InventoryService.GetStockByProductAsync(product.Id).Returns(Array.Empty<StockLevelDto>());
        InventoryService.GetTransactionsByProductAsync(product.Id).Returns(new[]
        {
            // Live document — no reversal counterpart
            new InventoryTransactionDto { SourceDocumentId = liveDocId, SourceDocumentType = "PurchaseNote" },
            // Reversed document — has a reversal counterpart
            new InventoryTransactionDto { SourceDocumentId = reversedDocId, SourceDocumentType = "Invoice" },
            new InventoryTransactionDto { SourceDocumentId = reversedDocId, SourceDocumentType = "Invoice_Reversal" }
        });

        // Purchase note line on the live doc
        var livePnLine = CreatePurchaseNoteLine(product, liveDocId, warehouseId, quantity: 10, unitPrice: 50m);
        // Purchase note line on the reversed doc — should be excluded
        var reversedPnLine = CreatePurchaseNoteLine(product, reversedDocId, warehouseId, quantity: 5, unitPrice: 40m);

        PurchaseNoteRepo.GetLineItemsByProductIdAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { livePnLine, reversedPnLine });
        InvoiceRepo.GetLineItemsByProductIdAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<InvoiceLine>());
        var service = CreateService();

        var result = await service.GetProductDetailsAsync(product.Id);

        // Only the live doc should count
        result.TotalPurchasedCount.Should().Be(1);
        result.TotalPurchasedQuantity.Should().Be(10);
        result.TotalPurchasedAmount.Should().Be(500m); // 10 * 50
    }

    [Fact]
    public async Task PurchasedRows_IncludesPurchaseNotesAndPayableInvoices()
    {
        var product = CreateEntity();
        var pnDocId = Guid.NewGuid();
        var invDocId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        SetupDetailsLookup(product.Id, product);
        InventoryService.GetStockByProductAsync(product.Id).Returns(Array.Empty<StockLevelDto>());
        InventoryService.GetTransactionsByProductAsync(product.Id).Returns(new[]
        {
            new InventoryTransactionDto { SourceDocumentId = pnDocId, SourceDocumentType = "PurchaseNote" },
            new InventoryTransactionDto { SourceDocumentId = invDocId, SourceDocumentType = "Invoice" }
        });

        var pnLine = CreatePurchaseNoteLine(product, pnDocId, warehouseId, quantity: 10, unitPrice: 50m);
        PurchaseNoteRepo.GetLineItemsByProductIdAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { pnLine });

        var invLine = CreateInvoiceLine(product, invDocId, warehouseId, InvoiceType.Payable, quantity: 5, unitPrice: 60m);
        InvoiceRepo.GetLineItemsByProductIdAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { invLine });
        var service = CreateService();

        var result = await service.GetProductDetailsAsync(product.Id);

        // PurchaseNote: 10*50=500, Payable Invoice: TotalAmount of line
        result.TotalPurchasedCount.Should().Be(2);
        result.TotalPurchasedQuantity.Should().Be(15); // 10 + 5
    }

    [Fact]
    public async Task SoldRows_OnlyReceivableInvoices()
    {
        var product = CreateEntity();
        var recDocId = Guid.NewGuid();
        var payDocId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        SetupDetailsLookup(product.Id, product);
        InventoryService.GetStockByProductAsync(product.Id).Returns(Array.Empty<StockLevelDto>());
        InventoryService.GetTransactionsByProductAsync(product.Id).Returns(new[]
        {
            new InventoryTransactionDto { SourceDocumentId = recDocId, SourceDocumentType = "Invoice" },
            new InventoryTransactionDto { SourceDocumentId = payDocId, SourceDocumentType = "Invoice" }
        });

        var receivableLine = CreateInvoiceLine(product, recDocId, warehouseId, InvoiceType.Receivable, quantity: 8, unitPrice: 120m);
        var payableLine = CreateInvoiceLine(product, payDocId, warehouseId, InvoiceType.Payable, quantity: 3, unitPrice: 60m);
        PurchaseNoteRepo.GetLineItemsByProductIdAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<PurchaseNoteLine>());
        InvoiceRepo.GetLineItemsByProductIdAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { receivableLine, payableLine });
        var service = CreateService();

        var result = await service.GetProductDetailsAsync(product.Id);

        result.TotalSoldCount.Should().Be(1); // only receivable
        result.TotalSoldQuantity.Should().Be(8);
        result.TotalPurchasedCount.Should().Be(1); // payable counts as purchased
    }

    [Fact]
    public async Task ProfitabilityCalculation_ComputesMarginCorrectly()
    {
        var product = CreateEntity();
        product.SellingPrice = 100m;
        var pnDocId = Guid.NewGuid();
        var invDocId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        SetupDetailsLookup(product.Id, product);
        InventoryService.GetStockByProductAsync(product.Id).Returns(Array.Empty<StockLevelDto>());
        InventoryService.GetTransactionsByProductAsync(product.Id).Returns(new[]
        {
            new InventoryTransactionDto { SourceDocumentId = pnDocId, SourceDocumentType = "PurchaseNote" },
            new InventoryTransactionDto { SourceDocumentId = invDocId, SourceDocumentType = "Invoice" }
        });

        // Purchased at 50/unit
        var pnLine = CreatePurchaseNoteLine(product, pnDocId, warehouseId, quantity: 10, unitPrice: 50m);
        PurchaseNoteRepo.GetLineItemsByProductIdAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { pnLine });

        // Sold at 100/unit (no discount, no tax for simpler math)
        var invLine = CreateInvoiceLine(product, invDocId, warehouseId, InvoiceType.Receivable,
            quantity: 10, unitPrice: 100m, taxRate: 0, discountPercentage: 0);
        InvoiceRepo.GetLineItemsByProductIdAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { invLine });
        var service = CreateService();

        var result = await service.GetProductDetailsAsync(product.Id);

        result.AveragePurchasePrice.Should().Be(50m);
        result.AverageSellingPrice.Should().Be(100m);
        // GrossMargin = (100 - 50) / 100 * 100 = 50%
        result.GrossMarginPercentage.Should().Be(50m);
        // TotalProfit = soldAmount - purchasedAmount = 1000 - 500 = 500
        result.TotalProfit.Should().Be(500m);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetupDetailsLookup(Guid productId, Product product)
    {
        ProductRepo.GetByIdAsync(productId, Arg.Any<CancellationToken>()).Returns(product);
    }

    private void SetupEmptyDetailsData(Guid productId)
    {
        InventoryService.GetStockByProductAsync(productId).Returns(Array.Empty<StockLevelDto>());
        InventoryService.GetTransactionsByProductAsync(productId).Returns(Array.Empty<InventoryTransactionDto>());
        PurchaseNoteRepo.GetLineItemsByProductIdAsync(productId, Arg.Any<CancellationToken>()).Returns(Array.Empty<PurchaseNoteLine>());
        InvoiceRepo.GetLineItemsByProductIdAsync(productId, Arg.Any<CancellationToken>()).Returns(Array.Empty<InvoiceLine>());
    }

    private static PurchaseNoteLine CreatePurchaseNoteLine(Product product, Guid purchaseNoteId, Guid warehouseId,
        decimal quantity, decimal unitPrice)
    {
        var line = new PurchaseNoteLine
        {
            PurchaseNoteId = purchaseNoteId,
            ProductId = product.Id,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Product = product,
            PurchaseNote = new PurchaseNote
            {
                WarehouseId = warehouseId,
                IndividualId = Guid.NewGuid(),
                Individual = new Individual { FullName = "Test Individual" },
                Warehouse = new Warehouse { Name = "WH1" }
            }
        };
        SetEntityId(line, Guid.NewGuid());
        return line;
    }

    private static InvoiceLine CreateInvoiceLine(Product product, Guid invoiceId, Guid warehouseId,
        InvoiceType invoiceType, int quantity, decimal unitPrice, decimal taxRate = 0, decimal discountPercentage = 0)
    {
        var invoice = new Invoice
        {
            InvoiceNumber = "INV-000001",
            CompanyId = Guid.NewGuid(),
            WarehouseId = warehouseId,
            Type = invoiceType,
            Status = InvoiceStatus.Confirmed,
            Company = new Company { Name = "Test Company", Email = "test@test.com" },
            Warehouse = new Warehouse { Name = "WH1" },
            LineItems = []
        };
        SetEntityId(invoice, invoiceId);

        var line = new InvoiceLine
        {
            InvoiceId = invoiceId,
            ProductId = product.Id,
            Quantity = quantity,
            UnitPrice = unitPrice,
            TaxRate = taxRate,
            DiscountPercentage = discountPercentage,
            Product = product,
            Invoice = invoice
        };
        SetEntityId(line, Guid.NewGuid());
        return line;
    }
}
