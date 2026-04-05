namespace WarehouseInvoiceSystem.Tests.Services.WarehouseService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Entities;

public class DefaultWarehouseTests : WarehouseServiceTestBase
{
    [Fact]
    public async Task SetDefault_SameWarehouse_ReturnsTrue()
    {
        var current = CreateEntity("Current Default", isDefault: true);
        WarehouseRepo.GetDefaultWarehouseAsync(Arg.Any<CancellationToken>()).Returns(current);
        var service = CreateService();

        var result = await service.SetDefaultWarehouseAsync(current.Id);

        result.Should().BeTrue();
        // No update calls — already default
        await WarehouseRepo.DidNotReceive().UpdateAsync(Arg.Any<Warehouse>());
    }

    [Fact]
    public async Task SetDefault_DifferentWarehouse_SwapsDefault()
    {
        var oldDefault = CreateEntity("Old Default", isDefault: true);
        var newDefault = CreateEntity("New Default", isDefault: false);
        WarehouseRepo.GetDefaultWarehouseAsync(Arg.Any<CancellationToken>()).Returns(oldDefault);
        WarehouseRepo.GetByIdAsync(newDefault.Id, Arg.Any<CancellationToken>()).Returns(newDefault);
        var service = CreateService();

        var result = await service.SetDefaultWarehouseAsync(newDefault.Id);

        result.Should().BeTrue();
        oldDefault.IsDefault.Should().BeFalse();
        newDefault.IsDefault.Should().BeTrue();
        await WarehouseRepo.Received(2).UpdateAsync(Arg.Any<Warehouse>());
    }

    [Fact]
    public async Task SetDefault_NoActiveDefault_SetsNewDefault()
    {
        // Bug 1 fix: when no active default exists (e.g. default was deactivated),
        // the method should still set the new warehouse as default
        var newDefault = CreateEntity("New Default", isDefault: false);
        WarehouseRepo.GetDefaultWarehouseAsync(Arg.Any<CancellationToken>()).Returns((Warehouse?)null);
        WarehouseRepo.GetByIdAsync(newDefault.Id, Arg.Any<CancellationToken>()).Returns(newDefault);
        var service = CreateService();

        var result = await service.SetDefaultWarehouseAsync(newDefault.Id);

        result.Should().BeTrue();
        newDefault.IsDefault.Should().BeTrue();
        // Only one update — no old default to unset
        await WarehouseRepo.Received(1).UpdateAsync(Arg.Is<Warehouse>(w => w.IsDefault == true));
    }

    [Fact]
    public async Task SetDefault_TargetNotFound_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        WarehouseRepo.GetDefaultWarehouseAsync(Arg.Any<CancellationToken>()).Returns((Warehouse?)null);
        WarehouseRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Warehouse?)null);
        var service = CreateService();

        var result = await service.SetDefaultWarehouseAsync(id);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasProducts_DelegatesToRepository()
    {
        var id = Guid.NewGuid();
        WarehouseRepo.HasProductsAsync(id, Arg.Any<CancellationToken>()).Returns(true);
        var service = CreateService();

        var result = await service.HasProductsAsync(id);

        result.Should().BeTrue();
    }
}
