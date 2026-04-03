namespace WarehouseInvoiceSystem.Tests.Services.ProductService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Queries;
using WarehouseInvoiceSystem.Domain.Queries.Common;

public class QueryTests : ProductServiceTestBase
{
    [Fact]
    public async Task GetAll_MapsEntitiesToDtos()
    {
        var entities = new[] { CreateEntity(), CreateEntity() };
        ProductRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(entities);
        var service = CreateService();

        var result = await service.GetAllProductsAsync();

        result.Should().HaveCount(2);
        result.First().Code.Should().Be("P001");
    }

    [Fact]
    public async Task GetPaged_DelegatesToRepository()
    {
        var query = new GetProductsQuery { Page = 1, PageSize = 10 };
        var pagedResult = new PagedResult<Product>
        {
            Items = [CreateEntity()],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };
        ProductRepo.GetPagedAsync(query, Arg.Any<CancellationToken>()).Returns(pagedResult);
        var service = CreateService();

        var result = await service.GetPagedAsync(query);

        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetByIds_DelegatesToRepository()
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var entities = new[] { CreateEntity(), CreateEntity() };
        ProductRepo.GetByIdsAsync(ids, Arg.Any<CancellationToken>()).Returns(entities);
        var service = CreateService();

        var result = await service.GetProductsByIdsAsync(ids);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetActive_DelegatesToRepository()
    {
        var entities = new[] { CreateEntity() };
        ProductRepo.GetActiveProductsAsync(Arg.Any<CancellationToken>()).Returns(entities);
        var service = CreateService();

        var result = await service.GetActiveProductsAsync();

        result.Should().HaveCount(1);
        result.First().IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetById_Found_ReturnsDto()
    {
        var product = CreateEntity();
        ProductRepo.GetByIdAsync(product.Id).Returns(product);
        var service = CreateService();

        var result = await service.GetProductByIdAsync(product.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(product.Id);
        result.Code.Should().Be(product.Code);
        result.Name.Should().Be(product.Name);
        result.Description.Should().Be(product.Description);
        result.Unit.Should().Be(product.Unit);
        result.CostPrice.Should().Be(product.CostPrice);
        result.SellingPrice.Should().Be(product.SellingPrice);
        result.IsActive.Should().Be(product.IsActive);
    }

    [Fact]
    public async Task GetById_NotFound_ReturnsNull()
    {
        var id = Guid.NewGuid();
        ProductRepo.GetByIdAsync(id).Returns((Product?)null);
        var service = CreateService();

        var result = await service.GetProductByIdAsync(id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByCode_Found_ReturnsDto()
    {
        var product = CreateEntity();
        ProductRepo.GetByCodeAsync("P001", Arg.Any<CancellationToken>()).Returns(product);
        var service = CreateService();

        var result = await service.GetProductByCodeAsync("P001");

        result.Should().NotBeNull();
        result!.Code.Should().Be("P001");
    }
}
