namespace WarehouseInvoiceSystem.Tests.Services.InventoryService;

using NSubstitute;
using WarehouseInvoiceSystem.Application.DTOs.InventoryTransaction;
using WarehouseInvoiceSystem.Application.DTOs.StockLevel;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;
using WarehouseInvoiceSystem.Domain.Interfaces;

public abstract class InventoryServiceTestBase
{
    protected readonly IStockLevelRepository StockLevelRepo = Substitute.For<IStockLevelRepository>();
    protected readonly IInventoryTransactionRepository TransactionRepo = Substitute.For<IInventoryTransactionRepository>();
    protected readonly IProductRepository ProductRepo = Substitute.For<IProductRepository>();
    protected readonly IWarehouseRepository WarehouseRepo = Substitute.For<IWarehouseRepository>();

    protected Application.Services.InventoryService CreateService() =>
        new(StockLevelRepo, TransactionRepo, ProductRepo, WarehouseRepo);

    protected static StockLevel CreateStockLevel(
        Guid? productId = null, Guid? warehouseId = null, decimal quantity = 100m)
    {
        var pid = productId ?? Guid.NewGuid();
        var wid = warehouseId ?? Guid.NewGuid();
        var stockLevel = new StockLevel
        {
            ProductId = pid,
            WarehouseId = wid,
            Quantity = quantity,
            MinimumQuantity = 10m,
            ReorderPoint = 20m,
            LastRestockedAt = DateTime.UtcNow,
            Product = new Product
            {
                Code = "P001",
                Name = "Test Product",
                Unit = "kg",
                CostPrice = 50m,
                SellingPrice = 100m,
                IsActive = true
            },
            Warehouse = new Warehouse { Name = "WH1" }
        };
        SetEntityId(stockLevel, Guid.NewGuid());
        SetEntityId(stockLevel.Product, pid);
        SetEntityId(stockLevel.Warehouse, wid);
        return stockLevel;
    }

    protected static InventoryTransaction CreateTransaction(
        InventoryTransactionType type = InventoryTransactionType.Inbound,
        Guid? productId = null, Guid? warehouseId = null, decimal quantity = 10m,
        Guid? sourceDocumentId = null, string? sourceDocumentType = null)
    {
        var pid = productId ?? Guid.NewGuid();
        var wid = warehouseId ?? Guid.NewGuid();
        var transaction = new InventoryTransaction
        {
            ProductId = pid,
            WarehouseId = wid,
            Type = type,
            Quantity = quantity,
            SourceDocumentId = sourceDocumentId,
            SourceDocumentType = sourceDocumentType,
            Note = "Test transaction",
            Product = new Product
            {
                Code = "P001",
                Name = "Test Product",
                Unit = "kg",
                CostPrice = 50m,
                SellingPrice = 100m,
                IsActive = true
            },
            Warehouse = new Warehouse { Name = "WH1" }
        };
        SetEntityId(transaction, Guid.NewGuid());
        SetEntityId(transaction.Product, pid);
        SetEntityId(transaction.Warehouse, wid);
        return transaction;
    }

    protected static CreateInventoryTransactionDto BuildCreateDto(
        InventoryTransactionType type = InventoryTransactionType.Inbound,
        decimal quantity = 10m,
        Guid? productId = null, Guid? warehouseId = null)
    {
        return new CreateInventoryTransactionDto
        {
            ProductId = productId ?? Guid.NewGuid(),
            WarehouseId = warehouseId ?? Guid.NewGuid(),
            Type = type,
            Quantity = quantity,
            SourceDocumentId = Guid.NewGuid(),
            SourceDocumentType = "PurchaseNote",
            Note = "Test"
        };
    }

    protected static UpdateStockLevelDto BuildUpdateStockDto(
        decimal quantity = 50m, decimal? minimum = 10m, decimal? reorder = 20m)
    {
        return new UpdateStockLevelDto
        {
            Quantity = quantity,
            MinimumQuantity = minimum,
            ReorderPoint = reorder
        };
    }

    protected void SetupValidCreateTransaction(CreateInventoryTransactionDto dto, InventoryTransaction? returned = null)
    {
        ProductRepo.ExistsAsync(dto.ProductId, Arg.Any<CancellationToken>()).Returns(true);
        WarehouseRepo.ExistsAsync(dto.WarehouseId, Arg.Any<CancellationToken>()).Returns(true);
        TransactionRepo.CreateAsync(Arg.Any<InventoryTransaction>()).Returns(ci =>
        {
            var t = ci.Arg<InventoryTransaction>();
            SetEntityId(t, Guid.NewGuid());
            return t;
        });
        var nav = returned ?? CreateTransaction(dto.Type, dto.ProductId, dto.WarehouseId, dto.Quantity);
        TransactionRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(nav);
    }

    protected static void SetEntityId(Domain.Common.Entity entity, Guid id)
    {
        typeof(Domain.Common.Entity)
            .GetProperty(nameof(Domain.Common.Entity.Id))!
            .SetValue(entity, id);
    }
}
