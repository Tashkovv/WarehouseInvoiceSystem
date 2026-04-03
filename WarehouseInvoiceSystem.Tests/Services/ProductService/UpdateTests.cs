namespace WarehouseInvoiceSystem.Tests.Services.ProductService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Entities;

public class UpdateTests : ProductServiceTestBase
{
    [Fact]
    public async Task Update_MapsAllFieldsToEntity()
    {
        var product = CreateEntity();
        var dto = BuildUpdateDto();
        ProductRepo.GetByIdAsync(product.Id).Returns(product);
        ProductRepo.CodeExistsAsync(dto.Code, product.Id).Returns(false);
        var service = CreateService();

        await service.UpdateProductAsync(product.Id, dto);

        product.Code.Should().Be(dto.Code);
        product.Name.Should().Be(dto.Name);
        product.Description.Should().Be(dto.Description);
        product.Unit.Should().Be(dto.Unit);
        product.CostPrice.Should().Be(dto.CostPrice);
        product.SellingPrice.Should().Be(dto.SellingPrice);
        product.IsActive.Should().Be(dto.IsActive);
        await ProductRepo.Received(1).UpdateAsync(product);
    }

    [Fact]
    public async Task Update_NotFound_ThrowsKeyNotFound()
    {
        var id = Guid.NewGuid();
        ProductRepo.GetByIdAsync(id).Returns((Product?)null);
        var service = CreateService();

        await service.Invoking(s => s.UpdateProductAsync(id, BuildUpdateDto()))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Update_DuplicateCode_ThrowsInvalidOperation()
    {
        var product = CreateEntity();
        var dto = BuildUpdateDto();
        ProductRepo.GetByIdAsync(product.Id).Returns(product);
        ProductRepo.CodeExistsAsync(dto.Code, product.Id).Returns(true);
        var service = CreateService();

        await service.Invoking(s => s.UpdateProductAsync(product.Id, dto))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Update_SameCode_Allowed()
    {
        var product = CreateEntity();
        var dto = BuildUpdateDto();
        dto.Code = product.Code; // same code
        ProductRepo.GetByIdAsync(product.Id).Returns(product);
        ProductRepo.CodeExistsAsync(dto.Code, product.Id).Returns(false);
        var service = CreateService();

        await service.UpdateProductAsync(product.Id, dto);

        await ProductRepo.Received(1).UpdateAsync(product);
    }

    [Fact]
    public async Task Update_DuplicateCode_DoesNotCallUpdate()
    {
        var product = CreateEntity();
        var dto = BuildUpdateDto();
        ProductRepo.GetByIdAsync(product.Id).Returns(product);
        ProductRepo.CodeExistsAsync(dto.Code, product.Id).Returns(true);
        var service = CreateService();

        await service.Invoking(s => s.UpdateProductAsync(product.Id, dto))
            .Should().ThrowAsync<InvalidOperationException>();

        await ProductRepo.DidNotReceive().UpdateAsync(Arg.Any<Product>());
    }
}
