namespace WarehouseInvoiceSystem.Tests.Services.ProductService;

using NSubstitute;
using WarehouseInvoiceSystem.Application.DTOs.Product;
using WarehouseInvoiceSystem.Application.Interfaces;
using WarehouseInvoiceSystem.Application.Services;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Interfaces;

public abstract class ProductServiceTestBase
{
    protected readonly IProductRepository ProductRepo = Substitute.For<IProductRepository>();
    protected readonly IInventoryService InventoryService = Substitute.For<IInventoryService>();
    protected readonly IInvoiceRepository InvoiceRepo = Substitute.For<IInvoiceRepository>();
    protected readonly IPurchaseNoteRepository PurchaseNoteRepo = Substitute.For<IPurchaseNoteRepository>();

    protected ProductService CreateService() =>
        new(ProductRepo, InventoryService, InvoiceRepo, PurchaseNoteRepo);

    protected static CreateProductDto BuildCreateDto() => new()
    {
        Code = "P001",
        Name = "Test Product",
        Description = "A test product",
        Unit = "kg",
        CostPrice = 50m,
        SellingPrice = 100m,
        IsActive = true
    };

    protected static UpdateProductDto BuildUpdateDto() => new()
    {
        Code = "P001-UPD",
        Name = "Updated Product",
        Description = "Updated description",
        Unit = "pcs",
        CostPrice = 60m,
        SellingPrice = 120m,
        IsActive = true
    };

    protected static Product CreateEntity(bool isActive = true)
    {
        var product = new Product
        {
            Code = "P001",
            Name = "Test Product",
            Description = "A test product",
            Unit = "kg",
            CostPrice = 50m,
            SellingPrice = 100m,
            IsActive = isActive
        };
        SetEntityId(product, Guid.NewGuid());
        return product;
    }

    protected static void SetEntityId(Domain.Common.Entity entity, Guid id)
    {
        typeof(Domain.Common.Entity)
            .GetProperty(nameof(Domain.Common.Entity.Id))!
            .SetValue(entity, id);
    }
}
