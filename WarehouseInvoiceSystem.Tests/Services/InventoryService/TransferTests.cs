namespace WarehouseInvoiceSystem.Tests.Services.InventoryService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;

public class TransferTests : InventoryServiceTestBase
{
    [Fact]
    public async Task Transfer_ValidInput_CreatesTwoTransactions()
    {
        var productId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var destId = Guid.NewGuid();

        ProductRepo.ExistsAsync(productId, Arg.Any<CancellationToken>()).Returns(true);
        WarehouseRepo.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        TransactionRepo.CreateAsync(Arg.Any<InventoryTransaction>()).Returns(ci =>
        {
            var t = ci.Arg<InventoryTransaction>();
            SetEntityId(t, Guid.NewGuid());
            return t;
        });
        TransactionRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => CreateTransaction(
                InventoryTransactionType.TransferOut, productId, sourceId, 20m));
        var service = CreateService();

        await service.TransferStockAsync(productId, sourceId, destId, 20m, "Transfer note");

        await TransactionRepo.Received(2).CreateAsync(Arg.Any<InventoryTransaction>());

        // Verify TransferOut to source warehouse
        await TransactionRepo.Received(1).CreateAsync(Arg.Is<InventoryTransaction>(t =>
            t.Type == InventoryTransactionType.TransferOut && t.WarehouseId == sourceId));

        // Verify TransferIn to destination warehouse
        await TransactionRepo.Received(1).CreateAsync(Arg.Is<InventoryTransaction>(t =>
            t.Type == InventoryTransactionType.TransferIn && t.WarehouseId == destId));
    }

    [Fact]
    public async Task Transfer_SameWarehouse_ThrowsInvalidOperation()
    {
        var warehouseId = Guid.NewGuid();
        var service = CreateService();

        await service.Invoking(s => s.TransferStockAsync(Guid.NewGuid(), warehouseId, warehouseId, 10m, null))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*different*");
    }

    [Fact]
    public async Task Transfer_ZeroQuantity_ThrowsArgumentOutOfRange()
    {
        var service = CreateService();

        await service.Invoking(s => s.TransferStockAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0m, null))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task Transfer_NegativeQuantity_ThrowsArgumentOutOfRange()
    {
        var service = CreateService();

        await service.Invoking(s => s.TransferStockAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), -5m, null))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
    }
}
