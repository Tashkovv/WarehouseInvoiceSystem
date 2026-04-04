namespace WarehouseInvoiceSystem.Tests.Services.InventoryService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;

public class AdjustStockTests : InventoryServiceTestBase
{
    [Fact]
    public async Task Adjust_PositiveQuantity_CreatesAdjustmentTransaction()
    {
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        ProductRepo.ExistsAsync(productId, Arg.Any<CancellationToken>()).Returns(true);
        WarehouseRepo.ExistsAsync(warehouseId, Arg.Any<CancellationToken>()).Returns(true);
        TransactionRepo.CreateAsync(Arg.Any<InventoryTransaction>()).Returns(ci =>
        {
            var t = ci.Arg<InventoryTransaction>();
            SetEntityId(t, Guid.NewGuid());
            return t;
        });
        TransactionRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(CreateTransaction(InventoryTransactionType.Adjustment, productId, warehouseId, 25m));
        var service = CreateService();

        await service.AdjustStockAsync(productId, warehouseId, 25m, "Recount");

        await TransactionRepo.Received(1).CreateAsync(Arg.Is<InventoryTransaction>(t =>
            t.Type == InventoryTransactionType.Adjustment && t.Quantity == 25m && t.Note == "Recount"));
        await StockLevelRepo.Received(1).ApplyDeltaAsync(productId, warehouseId, 25m, false);
    }

    [Fact]
    public async Task Adjust_NegativeQuantity_CreatesAdjustmentWithNegativeSignPreserved()
    {
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        ProductRepo.ExistsAsync(productId, Arg.Any<CancellationToken>()).Returns(true);
        WarehouseRepo.ExistsAsync(warehouseId, Arg.Any<CancellationToken>()).Returns(true);
        TransactionRepo.CreateAsync(Arg.Any<InventoryTransaction>()).Returns(ci =>
        {
            var t = ci.Arg<InventoryTransaction>();
            SetEntityId(t, Guid.NewGuid());
            return t;
        });
        TransactionRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(CreateTransaction(InventoryTransactionType.Adjustment, productId, warehouseId, -10m));
        var service = CreateService();

        await service.AdjustStockAsync(productId, warehouseId, -10m, "Damage");

        await TransactionRepo.Received(1).CreateAsync(Arg.Is<InventoryTransaction>(t =>
            t.Type == InventoryTransactionType.Adjustment && t.Quantity == -10m));
        await StockLevelRepo.Received(1).ApplyDeltaAsync(productId, warehouseId, -10m, false);
    }
}
