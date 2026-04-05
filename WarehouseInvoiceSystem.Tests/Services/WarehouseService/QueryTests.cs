namespace WarehouseInvoiceSystem.Tests.Services.WarehouseService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Queries;
using WarehouseInvoiceSystem.Domain.Queries.Common;

public class QueryTests : WarehouseServiceTestBase
{
    [Fact]
    public async Task GetAll_MapsEntitiesToDtos()
    {
        var entities = new[] { CreateEntity("WH-A"), CreateEntity("WH-B") };
        WarehouseRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(entities);
        var service = CreateService();

        var result = await service.GetAllWarehousesAsync();

        result.Should().HaveCount(2);
        var first = result.First();
        first.Name.Should().Be("WH-A");
        first.Address.Should().Be("123 Storage St");
    }

    [Fact]
    public async Task GetPaged_BatchQueriesHasProducts()
    {
        var wh1 = CreateEntity("WH-1");
        var wh2 = CreateEntity("WH-2");
        var query = new GetWarehousesQuery { Page = 1, PageSize = 10 };
        WarehouseRepo.GetPagedAsync(query, Arg.Any<CancellationToken>()).Returns(new PagedResult<Warehouse>
        {
            Items = [wh1, wh2],
            TotalCount = 2,
            Page = 1,
            PageSize = 10
        });
        WarehouseRepo.GetWarehouseIdsWithProductsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<Guid> { wh1.Id });
        var service = CreateService();

        var result = await service.GetPagedAsync(query);

        result.Items.Should().HaveCount(2);
        result.Items[0].HasProducts.Should().BeTrue();
        result.Items[1].HasProducts.Should().BeFalse();
        result.TotalCount.Should().Be(2);
        // Single batch call instead of N individual calls
        await WarehouseRepo.Received(1).GetWarehouseIdsWithProductsAsync(
            Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_Found_ReturnsDto()
    {
        var entity = CreateEntity("Found WH", isDefault: true, isActive: true);
        WarehouseRepo.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>()).Returns(entity);
        var service = CreateService();

        var result = await service.GetWarehouseByIdAsync(entity.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
        result.Name.Should().Be("Found WH");
        result.IsDefault.Should().BeTrue();
        result.IsActive.Should().BeTrue();
        result.Address.Should().Be("123 Storage St");
    }

    [Fact]
    public async Task GetById_NotFound_ReturnsNull()
    {
        var id = Guid.NewGuid();
        WarehouseRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Warehouse?)null);
        var service = CreateService();

        var result = await service.GetWarehouseByIdAsync(id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDefault_Found_ReturnsDto()
    {
        var entity = CreateEntity("Default WH", isDefault: true);
        WarehouseRepo.GetDefaultWarehouseAsync(Arg.Any<CancellationToken>()).Returns(entity);
        var service = CreateService();

        var result = await service.GetDefaultWarehouseAsync();

        result.Should().NotBeNull();
        result!.Name.Should().Be("Default WH");
        result.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task GetDefault_NotFound_ReturnsNull()
    {
        WarehouseRepo.GetDefaultWarehouseAsync(Arg.Any<CancellationToken>()).Returns((Warehouse?)null);
        var service = CreateService();

        var result = await service.GetDefaultWarehouseAsync();

        result.Should().BeNull();
    }
}
