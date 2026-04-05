namespace WarehouseInvoiceSystem.Tests.Services.WarehouseService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Entities;

public class MutationTests : WarehouseServiceTestBase
{
    [Fact]
    public async Task Create_MapsFieldsToEntity()
    {
        var dto = BuildCreateDto("Acme Warehouse", "100 Industrial Blvd");
        WarehouseRepo.GetDefaultWarehouseAsync(Arg.Any<CancellationToken>()).Returns(CreateEntity(isDefault: true));
        var service = CreateService();

        await service.CreateWarehouseAsync(dto);

        await WarehouseRepo.Received(1).CreateAsync(Arg.Is<Warehouse>(w =>
            w.Name == "Acme Warehouse" &&
            w.Address == "100 Industrial Blvd"));
    }

    [Fact]
    public async Task Create_FirstWarehouse_SetsAsDefault()
    {
        WarehouseRepo.GetDefaultWarehouseAsync(Arg.Any<CancellationToken>()).Returns((Warehouse?)null);
        var service = CreateService();

        await service.CreateWarehouseAsync(BuildCreateDto());

        await WarehouseRepo.Received(1).CreateAsync(Arg.Is<Warehouse>(w => w.IsDefault == true));
    }

    [Fact]
    public async Task Create_SubsequentWarehouse_NotDefault()
    {
        WarehouseRepo.GetDefaultWarehouseAsync(Arg.Any<CancellationToken>()).Returns(CreateEntity(isDefault: true));
        var service = CreateService();

        await service.CreateWarehouseAsync(BuildCreateDto());

        await WarehouseRepo.Received(1).CreateAsync(Arg.Is<Warehouse>(w => w.IsDefault == false));
    }

    [Fact]
    public async Task Update_Found_MapsFields()
    {
        var entity = CreateEntity();
        WarehouseRepo.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>()).Returns(entity);
        var dto = BuildUpdateDto("Renamed WH", "New Address");
        var service = CreateService();

        await service.UpdateWarehouseAsync(entity.Id, dto);

        await WarehouseRepo.Received(1).UpdateAsync(Arg.Is<Warehouse>(w =>
            w.Name == "Renamed WH" &&
            w.Address == "New Address"));
    }

    [Fact]
    public async Task Update_NotFound_ThrowsKeyNotFound()
    {
        var id = Guid.NewGuid();
        WarehouseRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Warehouse?)null);
        var service = CreateService();

        await service.Invoking(s => s.UpdateWarehouseAsync(id, BuildUpdateDto()))
            .Should().ThrowAsync<KeyNotFoundException>();
    }
}
