namespace WarehouseInvoiceSystem.Tests.Services.InventoryService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;

public class CreateTransactionTests : InventoryServiceTestBase
{
    [Fact]
    public async Task Create_ValidInput_CreatesAndUpdatesStock()
    {
        var dto = BuildCreateDto(InventoryTransactionType.Inbound, 10m);
        SetupValidCreateTransaction(dto);
        var service = CreateService();

        var result = await service.CreateTransactionAsync(dto);

        result.Should().NotBeNull();
        await TransactionRepo.Received(1).CreateAsync(Arg.Any<InventoryTransaction>());
        await StockLevelRepo.Received(1).ApplyDeltaAsync(
            dto.ProductId, dto.WarehouseId, 10m, true);
    }

    [Fact]
    public async Task Create_ProductNotFound_ThrowsKeyNotFound()
    {
        var dto = BuildCreateDto();
        ProductRepo.ExistsAsync(dto.ProductId, Arg.Any<CancellationToken>()).Returns(false);
        var service = CreateService();

        await service.Invoking(s => s.CreateTransactionAsync(dto))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Create_WarehouseNotFound_ThrowsKeyNotFound()
    {
        var dto = BuildCreateDto();
        ProductRepo.ExistsAsync(dto.ProductId, Arg.Any<CancellationToken>()).Returns(true);
        WarehouseRepo.ExistsAsync(dto.WarehouseId, Arg.Any<CancellationToken>()).Returns(false);
        var service = CreateService();

        await service.Invoking(s => s.CreateTransactionAsync(dto))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Create_MapsAllDtoFieldsToEntity()
    {
        var dto = BuildCreateDto(InventoryTransactionType.Outbound, 25m);
        SetupValidCreateTransaction(dto);
        var service = CreateService();

        await service.CreateTransactionAsync(dto);

        await TransactionRepo.Received(1).CreateAsync(Arg.Is<InventoryTransaction>(t =>
            t.ProductId == dto.ProductId &&
            t.WarehouseId == dto.WarehouseId &&
            t.Type == InventoryTransactionType.Outbound &&
            t.Quantity == 25m &&
            t.SourceDocumentId == dto.SourceDocumentId &&
            t.SourceDocumentType == dto.SourceDocumentType &&
            t.Note == dto.Note));
    }

    [Fact]
    public async Task Create_InboundType_UpdatesStockPositive()
    {
        var dto = BuildCreateDto(InventoryTransactionType.Inbound, 15m);
        SetupValidCreateTransaction(dto);
        var service = CreateService();

        await service.CreateTransactionAsync(dto);

        await StockLevelRepo.Received(1).ApplyDeltaAsync(
            dto.ProductId, dto.WarehouseId, 15m, true);
    }

    [Fact]
    public async Task Create_OutboundType_UpdatesStockNegative()
    {
        var dto = BuildCreateDto(InventoryTransactionType.Outbound, 15m);
        SetupValidCreateTransaction(dto);
        var service = CreateService();

        await service.CreateTransactionAsync(dto);

        await StockLevelRepo.Received(1).ApplyDeltaAsync(
            dto.ProductId, dto.WarehouseId, -15m, false);
    }
}
