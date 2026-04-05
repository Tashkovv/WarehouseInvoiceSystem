namespace WarehouseInvoiceSystem.Tests.Services.WarehouseService;

using NSubstitute;
using WarehouseInvoiceSystem.Application.DTOs.Warehouse;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Interfaces;

public abstract class WarehouseServiceTestBase
{
    protected IWarehouseRepository WarehouseRepo { get; } = Substitute.For<IWarehouseRepository>();

    protected Application.Services.WarehouseService CreateService() => new(WarehouseRepo);

    protected static Warehouse CreateEntity(
        string name = "Main Warehouse",
        bool isDefault = false,
        bool isActive = true,
        string? address = "123 Storage St")
    {
        var warehouse = new Warehouse
        {
            Name = name,
            Address = address,
            IsDefault = isDefault,
            IsActive = isActive
        };
        SetEntityId(warehouse, Guid.NewGuid());
        return warehouse;
    }

    protected static CreateWarehouseDto BuildCreateDto(
        string name = "New Warehouse",
        string? address = "456 New St") => new()
    {
        Name = name,
        Address = address
    };

    protected static UpdateWarehouseDto BuildUpdateDto(
        string name = "Updated Warehouse",
        string? address = "789 Updated Ave") => new()
    {
        Name = name,
        Address = address
    };

    protected static void SetEntityId(object entity, Guid id)
    {
        var prop = entity.GetType().GetProperty("Id")!;
        prop.SetValue(entity, id);
    }
}
