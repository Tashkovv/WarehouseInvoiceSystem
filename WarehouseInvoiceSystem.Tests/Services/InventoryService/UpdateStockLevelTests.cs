namespace WarehouseInvoiceSystem.Tests.Services.InventoryService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Entities;

public class UpdateStockLevelTests : InventoryServiceTestBase
{
    [Fact]
    public async Task UpdateStockLevel_ExistingRecord_Updates()
    {
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var existing = CreateStockLevel(productId, warehouseId, 100m);
        StockLevelRepo.GetByProductAndWarehouseAsync(productId, warehouseId, Arg.Any<CancellationToken>())
            .Returns(existing);
        StockLevelRepo.UpdateAsync(Arg.Any<StockLevel>()).Returns(ci => ci.Arg<StockLevel>());
        var dto = BuildUpdateStockDto(quantity: 75m, minimum: 5m, reorder: 15m);
        var service = CreateService();

        var result = await service.UpdateStockLevelAsync(productId, warehouseId, dto);

        await StockLevelRepo.Received(1).UpdateAsync(Arg.Is<StockLevel>(s =>
            s.Quantity == 75m && s.MinimumQuantity == 5m && s.ReorderPoint == 15m));
        await StockLevelRepo.DidNotReceive().CreateAsync(Arg.Any<StockLevel>());
    }

    [Fact]
    public async Task UpdateStockLevel_NewRecord_Creates()
    {
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        StockLevelRepo.GetByProductAndWarehouseAsync(productId, warehouseId, Arg.Any<CancellationToken>())
            .Returns((StockLevel?)null);
        StockLevelRepo.CreateAsync(Arg.Any<StockLevel>()).Returns(ci =>
        {
            var sl = ci.Arg<StockLevel>();
            sl.Product = new Product { Code = "P001", Name = "Test", Unit = "kg", CostPrice = 50, SellingPrice = 100 };
            sl.Warehouse = new Warehouse { Name = "WH1" };
            SetEntityId(sl, Guid.NewGuid());
            SetEntityId(sl.Product, productId);
            SetEntityId(sl.Warehouse, warehouseId);
            return sl;
        });
        var dto = BuildUpdateStockDto(quantity: 50m);
        var service = CreateService();

        var result = await service.UpdateStockLevelAsync(productId, warehouseId, dto);

        await StockLevelRepo.Received(1).CreateAsync(Arg.Is<StockLevel>(s =>
            s.ProductId == productId && s.WarehouseId == warehouseId && s.Quantity == 50m));
        await StockLevelRepo.DidNotReceive().UpdateAsync(Arg.Any<StockLevel>());
    }

    [Fact]
    public async Task UpdateStockLevel_NewRecord_SetsLastRestockedAt()
    {
        var before = DateTime.UtcNow;
        StockLevelRepo.GetByProductAndWarehouseAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((StockLevel?)null);
        StockLevelRepo.CreateAsync(Arg.Any<StockLevel>()).Returns(ci =>
        {
            var sl = ci.Arg<StockLevel>();
            sl.Product = new Product { Code = "P001", Name = "Test", Unit = "kg", CostPrice = 50, SellingPrice = 100 };
            sl.Warehouse = new Warehouse { Name = "WH1" };
            SetEntityId(sl, Guid.NewGuid());
            SetEntityId(sl.Product, sl.ProductId);
            SetEntityId(sl.Warehouse, sl.WarehouseId);
            return sl;
        });
        var service = CreateService();

        await service.UpdateStockLevelAsync(Guid.NewGuid(), Guid.NewGuid(), BuildUpdateStockDto());

        await StockLevelRepo.Received(1).CreateAsync(Arg.Is<StockLevel>(s =>
            s.LastRestockedAt >= before && s.LastRestockedAt <= DateTime.UtcNow));
    }
}
