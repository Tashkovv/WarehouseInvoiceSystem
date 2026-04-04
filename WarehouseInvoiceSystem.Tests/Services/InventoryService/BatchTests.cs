namespace WarehouseInvoiceSystem.Tests.Services.InventoryService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Application.DTOs.InventoryTransaction;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;

public class BatchTests : InventoryServiceTestBase
{
    [Fact]
    public async Task Batch_ValidInput_CreatesAllTransactionsAndUpdatesStock()
    {
        var warehouseId = Guid.NewGuid();
        var product1 = Guid.NewGuid();
        var product2 = Guid.NewGuid();
        WarehouseRepo.ExistsAsync(warehouseId, Arg.Any<CancellationToken>()).Returns(true);
        ProductRepo.AllExistAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>()).Returns(true);
        var items = new List<CreateInventoryTransactionDto>
        {
            BuildCreateDto(InventoryTransactionType.Inbound, 10m, product1, warehouseId),
            BuildCreateDto(InventoryTransactionType.Inbound, 20m, product2, warehouseId)
        };
        var service = CreateService();

        await service.CreateBatchAsync(warehouseId, items);

        await TransactionRepo.Received(1).CreateBatchAsync(Arg.Is<List<InventoryTransaction>>(list =>
            list.Count == 2));
        // Stock updated per transaction
        await StockLevelRepo.Received(1).ApplyDeltaAsync(product1, warehouseId, 10m, true);
        await StockLevelRepo.Received(1).ApplyDeltaAsync(product2, warehouseId, 20m, true);
    }

    [Fact]
    public async Task Batch_EmptyList_ReturnsImmediately()
    {
        var service = CreateService();

        await service.CreateBatchAsync(Guid.NewGuid(), Array.Empty<CreateInventoryTransactionDto>());

        await WarehouseRepo.DidNotReceive().ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await TransactionRepo.DidNotReceive().CreateBatchAsync(Arg.Any<IEnumerable<InventoryTransaction>>());
    }

    [Fact]
    public async Task Batch_WarehouseNotFound_ThrowsKeyNotFound()
    {
        WarehouseRepo.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        var items = new[] { BuildCreateDto() };
        var service = CreateService();

        await service.Invoking(s => s.CreateBatchAsync(Guid.NewGuid(), items))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Batch_ProductNotFound_ThrowsKeyNotFound()
    {
        WarehouseRepo.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        ProductRepo.AllExistAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>()).Returns(false);
        var items = new[] { BuildCreateDto() };
        var service = CreateService();

        await service.Invoking(s => s.CreateBatchAsync(Guid.NewGuid(), items))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Batch_OverridesWarehouseIdFromParameter()
    {
        var validWarehouseId = Guid.NewGuid();
        var differentWarehouseId = Guid.NewGuid();
        WarehouseRepo.ExistsAsync(validWarehouseId, Arg.Any<CancellationToken>()).Returns(true);
        ProductRepo.AllExistAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>()).Returns(true);
        // DTO has a different warehouseId than the parameter
        var items = new[] { BuildCreateDto(warehouseId: differentWarehouseId) };
        var service = CreateService();

        await service.CreateBatchAsync(validWarehouseId, items);

        // Bug 2 fix: entities should use the validated warehouseId parameter, not the DTO's
        await TransactionRepo.Received(1).CreateBatchAsync(Arg.Is<List<InventoryTransaction>>(list =>
            list.All(t => t.WarehouseId == validWarehouseId)));
    }
}
