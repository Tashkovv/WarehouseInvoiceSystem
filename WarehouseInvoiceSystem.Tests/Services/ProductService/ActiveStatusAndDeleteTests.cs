namespace WarehouseInvoiceSystem.Tests.Services.ProductService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Entities;

public class ActiveStatusAndDeleteTests : ProductServiceTestBase
{
    // ── SetActiveStatusAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task SetActiveStatus_DelegatesToRepository()
    {
        var id = Guid.NewGuid();
        ProductRepo.SetActiveStatusAsync(id, false).Returns(true);
        var service = CreateService();

        var result = await service.SetActiveStatusAsync(id, false);

        result.Should().BeTrue();
        await ProductRepo.Received(1).SetActiveStatusAsync(id, false);
    }

    [Fact]
    public async Task SetActiveStatus_NotFound_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        ProductRepo.SetActiveStatusAsync(id, true).Returns(false);
        var service = CreateService();

        var result = await service.SetActiveStatusAsync(id, true);

        result.Should().BeFalse();
    }

    // ── DeleteProductAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task Delete_InactiveProduct_ReturnsTrue()
    {
        var product = CreateEntity(isActive: false);
        ProductRepo.GetByIdAsync(product.Id).Returns(product);
        ProductRepo.DeleteAsync(product.Id).Returns(true);
        var service = CreateService();

        var result = await service.DeleteProductAsync(product.Id);

        result.Should().BeTrue();
        await ProductRepo.Received(1).DeleteAsync(product.Id);
    }

    [Fact]
    public async Task Delete_ActiveProduct_ThrowsInvalidOperation()
    {
        var product = CreateEntity(isActive: true);
        ProductRepo.GetByIdAsync(product.Id).Returns(product);
        var service = CreateService();

        await service.Invoking(s => s.DeleteProductAsync(product.Id))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*deactivated*");
    }

    [Fact]
    public async Task Delete_NotFound_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        ProductRepo.GetByIdAsync(id).Returns((Product?)null);
        var service = CreateService();

        var result = await service.DeleteProductAsync(id);

        result.Should().BeFalse();
        await ProductRepo.DidNotReceive().DeleteAsync(Arg.Any<Guid>());
    }
}
