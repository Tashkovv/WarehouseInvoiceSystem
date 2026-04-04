namespace WarehouseInvoiceSystem.Tests.Services.InventoryService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Queries;
using WarehouseInvoiceSystem.Domain.Queries.Common;

public class StockLevelQueryTests : InventoryServiceTestBase
{
    [Fact]
    public async Task GetAllStockLevel_MapsEntitiesToDtos()
    {
        var entities = new[] { CreateStockLevel(quantity: 50), CreateStockLevel(quantity: 30) };
        StockLevelRepo.GetAllStockLevelAsync(Arg.Any<CancellationToken>()).Returns(entities);
        var service = CreateService();

        var result = await service.GetAllStockLevelAsync();

        result.Should().HaveCount(2);
        result.First().Quantity.Should().Be(50);
        result.First().ProductCode.Should().Be("P001");
        result.First().WarehouseName.Should().Be("WH1");
    }

    [Fact]
    public async Task GetPagedStock_DelegatesToRepository()
    {
        var query = new GetStockQuery { Page = 1, PageSize = 10 };
        var pagedResult = new PagedResult<StockLevel>
        {
            Items = [CreateStockLevel()],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };
        StockLevelRepo.GetPagedAsync(query, Arg.Any<CancellationToken>()).Returns(pagedResult);
        var service = CreateService();

        var result = await service.GetPagedStockAsync(query);

        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetStockLevel_Found_ReturnsDto()
    {
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var entity = CreateStockLevel(productId, warehouseId, 75m);
        StockLevelRepo.GetByProductAndWarehouseAsync(productId, warehouseId, Arg.Any<CancellationToken>())
            .Returns(entity);
        var service = CreateService();

        var result = await service.GetStockLevelAsync(productId, warehouseId);

        result.Should().NotBeNull();
        result!.Quantity.Should().Be(75m);
    }

    [Fact]
    public async Task GetStockLevel_NotFound_ReturnsNull()
    {
        StockLevelRepo.GetByProductAndWarehouseAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((StockLevel?)null);
        var service = CreateService();

        var result = await service.GetStockLevelAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetStockByProduct_DelegatesToRepository()
    {
        var productId = Guid.NewGuid();
        var entities = new[] { CreateStockLevel(productId) };
        StockLevelRepo.GetByProductIdAsync(productId, Arg.Any<CancellationToken>()).Returns(entities);
        var service = CreateService();

        var result = await service.GetStockByProductAsync(productId);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetStockByWarehouse_DelegatesToRepository()
    {
        var warehouseId = Guid.NewGuid();
        var entities = new[] { CreateStockLevel(warehouseId: warehouseId) };
        StockLevelRepo.GetByWarehouseIdAsync(warehouseId, Arg.Any<CancellationToken>()).Returns(entities);
        var service = CreateService();

        var result = await service.GetStockByWarehouseAsync(warehouseId);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetLowStockItems_DelegatesToRepository()
    {
        var entities = new[] { CreateStockLevel(quantity: 5) };
        StockLevelRepo.GetLowStockItemsAsync(null, Arg.Any<CancellationToken>()).Returns(entities);
        var service = CreateService();

        var result = await service.GetLowStockItemsAsync();

        result.Should().HaveCount(1);
        result.First().Quantity.Should().Be(5);
    }
}
