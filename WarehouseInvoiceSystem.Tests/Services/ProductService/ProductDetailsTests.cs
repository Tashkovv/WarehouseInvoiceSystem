namespace WarehouseInvoiceSystem.Tests.Services.ProductService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Application.DTOs.StockLevel;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Queries.Results;

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
    public async Task EmptyStock_ReturnsZeroTotals()
    {
        var product = CreateEntity();
        SetupProductLookup(product.Id, product);
        InventoryService.GetStockByProductAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<StockLevelDto>());
        var service = CreateService();

        var result = await service.GetProductDetailsAsync(product.Id);

        result.TotalStockAcrossWarehouses.Should().Be(0);
        result.StockByWarehouse.Should().BeEmpty();
        result.HasLowStock.Should().BeFalse();
    }

    [Fact]
    public async Task StockAggregation_SumsAcrossWarehouses()
    {
        var product = CreateEntity();
        SetupProductLookup(product.Id, product);
        InventoryService.GetStockByProductAsync(product.Id, Arg.Any<CancellationToken>()).Returns(new[]
        {
            new StockLevelDto { WarehouseId = Guid.NewGuid(), WarehouseName = "WH1", Quantity = 50, MinimumQuantity = 10 },
            new StockLevelDto { WarehouseId = Guid.NewGuid(), WarehouseName = "WH2", Quantity = 30, MinimumQuantity = 10 }
        });
        var service = CreateService();

        var result = await service.GetProductDetailsAsync(product.Id);

        result.TotalStockAcrossWarehouses.Should().Be(80);
        result.StockByWarehouse.Should().HaveCount(2);
    }

    [Fact]
    public async Task HasLowStock_TrueWhenQuantityBelowMinimum()
    {
        var product = CreateEntity();
        SetupProductLookup(product.Id, product);
        InventoryService.GetStockByProductAsync(product.Id, Arg.Any<CancellationToken>()).Returns(new[]
        {
            new StockLevelDto { WarehouseId = Guid.NewGuid(), Quantity = 5, MinimumQuantity = 10 }
        });
        var service = CreateService();

        var result = await service.GetProductDetailsAsync(product.Id);

        result.HasLowStock.Should().BeTrue();
    }

    [Fact]
    public async Task TransactionSummary_PurchasedRows_MergesPurchaseNotesAndPayableInvoices()
    {
        var product = CreateEntity();
        var warehouseId = Guid.NewGuid();
        SetupEmptyAggregates(product.Id);

        PurchaseNoteRepo.GetProductPurchaseNoteAggregatesAsync(product.Id, Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductWarehouseSummary> { new(warehouseId, 1, 10m, 500m, 50m) });
        InvoiceRepo.GetProductPayableAggregatesAsync(product.Id, Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductWarehouseSummary> { new(warehouseId, 1, 5m, 300m, 60m) });

        var service = CreateService();

        var result = await service.GetProductTransactionSummaryAsync(product.Id);

        result.TotalPurchasedCount.Should().Be(2);
        result.TotalPurchasedQuantity.Should().Be(15m);
        result.TotalPurchasedAmount.Should().Be(800m);
        // Same warehouse → merged into one entry
        result.PurchasedByWarehouse.Should().HaveCount(1);
        result.PurchasedByWarehouse[0].WarehouseId.Should().Be(warehouseId);
    }

    [Fact]
    public async Task TransactionSummary_SoldRows_OnlyReceivableInvoices()
    {
        var product = CreateEntity();
        var warehouseId = Guid.NewGuid();
        SetupEmptyAggregates(product.Id);

        InvoiceRepo.GetProductSoldAggregatesAsync(product.Id, Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductWarehouseSummary> { new(warehouseId, 1, 8m, 960m, 120m) });
        InvoiceRepo.GetProductPayableAggregatesAsync(product.Id, Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductWarehouseSummary> { new(warehouseId, 1, 3m, 180m, 60m) });

        var service = CreateService();

        var result = await service.GetProductTransactionSummaryAsync(product.Id);

        result.TotalSoldCount.Should().Be(1);
        result.TotalSoldQuantity.Should().Be(8m);
        result.TotalPurchasedCount.Should().Be(1);
        result.TotalPurchasedQuantity.Should().Be(3m);
    }

    [Fact]
    public async Task TransactionSummary_AveragesAndTotals_ComputeCorrectly()
    {
        var product = CreateEntity();
        var warehouseId = Guid.NewGuid();
        SetupEmptyAggregates(product.Id);

        PurchaseNoteRepo.GetProductPurchaseNoteAggregatesAsync(product.Id, Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductWarehouseSummary> { new(warehouseId, 10, 10m, 500m, 50m) });
        InvoiceRepo.GetProductSoldAggregatesAsync(product.Id, Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductWarehouseSummary> { new(warehouseId, 10, 10m, 1000m, 100m) });

        var service = CreateService();

        var result = await service.GetProductTransactionSummaryAsync(product.Id);

        result.AveragePurchasePrice.Should().Be(50m);
        result.AverageSellingPrice.Should().Be(100m);
        result.TotalSoldAmount.Should().Be(1000m);
        result.TotalPurchasedAmount.Should().Be(500m);
    }

    [Fact]
    public async Task TransactionSummary_PurchasedByWarehouse_AggregatesFromDifferentWarehouses()
    {
        var product = CreateEntity();
        var wh1 = Guid.NewGuid();
        var wh2 = Guid.NewGuid();
        SetupEmptyAggregates(product.Id);

        PurchaseNoteRepo.GetProductPurchaseNoteAggregatesAsync(product.Id, Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductWarehouseSummary> { new(wh1, 2, 20m, 1000m, 50m) });
        InvoiceRepo.GetProductPayableAggregatesAsync(product.Id, Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductWarehouseSummary> { new(wh2, 3, 30m, 1500m, 50m) });

        var service = CreateService();

        var result = await service.GetProductTransactionSummaryAsync(product.Id);

        result.PurchasedByWarehouse.Should().HaveCount(2);
        result.TotalPurchasedCount.Should().Be(5);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetupProductLookup(Guid productId, Product product)
    {
        ProductRepo.GetByIdAsync(productId, Arg.Any<CancellationToken>()).Returns(product);
    }

    private void SetupEmptyAggregates(Guid productId)
    {
        PurchaseNoteRepo.GetProductPurchaseNoteAggregatesAsync(productId, Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductWarehouseSummary>());
        InvoiceRepo.GetProductSoldAggregatesAsync(productId, Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductWarehouseSummary>());
        InvoiceRepo.GetProductPayableAggregatesAsync(productId, Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductWarehouseSummary>());
    }
}
