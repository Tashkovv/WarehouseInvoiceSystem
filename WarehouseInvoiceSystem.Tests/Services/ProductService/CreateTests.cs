namespace WarehouseInvoiceSystem.Tests.Services.ProductService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Entities;

public class CreateTests : ProductServiceTestBase
{
    [Fact]
    public async Task Create_MapsAllFieldsToEntity()
    {
        var dto = BuildCreateDto();
        ProductRepo.CodeExistsAsync(dto.Code).Returns(false);
        var service = CreateService();

        await service.CreateProductAsync(dto);

        await ProductRepo.Received(1).CreateAsync(Arg.Is<Product>(p =>
            p.Code == dto.Code &&
            p.Name == dto.Name &&
            p.Description == dto.Description &&
            p.Unit == dto.Unit &&
            p.CostPrice == dto.CostPrice &&
            p.SellingPrice == dto.SellingPrice &&
            p.IsActive == dto.IsActive));
    }

    [Fact]
    public async Task Create_DuplicateCode_ThrowsInvalidOperation()
    {
        var dto = BuildCreateDto();
        ProductRepo.CodeExistsAsync(dto.Code).Returns(true);
        var service = CreateService();

        await service.Invoking(s => s.CreateProductAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Create_UniqueCode_CallsRepository()
    {
        var dto = BuildCreateDto();
        ProductRepo.CodeExistsAsync(dto.Code).Returns(false);
        var service = CreateService();

        await service.CreateProductAsync(dto);

        await ProductRepo.Received(1).CreateAsync(Arg.Any<Product>());
    }

    [Fact]
    public async Task Create_DuplicateCode_DoesNotCallRepository()
    {
        var dto = BuildCreateDto();
        ProductRepo.CodeExistsAsync(dto.Code).Returns(true);
        var service = CreateService();

        await service.Invoking(s => s.CreateProductAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>();

        await ProductRepo.DidNotReceive().CreateAsync(Arg.Any<Product>());
    }
}
