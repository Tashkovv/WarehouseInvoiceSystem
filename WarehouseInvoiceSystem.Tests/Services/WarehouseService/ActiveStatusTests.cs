namespace WarehouseInvoiceSystem.Tests.Services.WarehouseService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Entities;

public class ActiveStatusTests : WarehouseServiceTestBase
{
    [Fact]
    public async Task Deactivate_DefaultWarehouse_ThrowsInvalidOperation()
    {
        // Bug 2 fix: service must prevent deactivating the default warehouse
        var defaultWh = CreateEntity("Default WH", isDefault: true, isActive: true);
        WarehouseRepo.GetByIdAsync(defaultWh.Id, Arg.Any<CancellationToken>()).Returns(defaultWh);
        var service = CreateService();

        await service.Invoking(s => s.SetActiveStatusAsync(defaultWh.Id, false))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*default*");
    }

    [Fact]
    public async Task Deactivate_NonDefaultWarehouse_DelegatesToRepository()
    {
        var warehouse = CreateEntity("Regular WH", isDefault: false, isActive: true);
        WarehouseRepo.GetByIdAsync(warehouse.Id, Arg.Any<CancellationToken>()).Returns(warehouse);
        WarehouseRepo.SetActiveStatusAsync(warehouse.Id, false).Returns(true);
        var service = CreateService();

        var result = await service.SetActiveStatusAsync(warehouse.Id, false);

        result.Should().BeTrue();
        await WarehouseRepo.Received(1).SetActiveStatusAsync(warehouse.Id, false);
    }

    [Fact]
    public async Task Activate_Warehouse_SkipsDefaultCheck()
    {
        // Activating should never throw, even for a default warehouse
        var id = Guid.NewGuid();
        WarehouseRepo.SetActiveStatusAsync(id, true).Returns(true);
        var service = CreateService();

        var result = await service.SetActiveStatusAsync(id, true);

        result.Should().BeTrue();
        // Should NOT call GetByIdAsync — no guard needed for activation
        await WarehouseRepo.DidNotReceive().GetByIdAsync(id, Arg.Any<CancellationToken>());
    }
}
