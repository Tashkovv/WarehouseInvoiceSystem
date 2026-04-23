namespace WarehouseInvoiceSystem.Tests.Services.PurchaseNoteService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Application.DTOs.PurchaseNote;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;

public class CreateTests : PurchaseNoteServiceTestBase
{
    [Fact]
    public async Task Draft_DoesNotCreateInventoryTransactions()
    {
        var dto = BuildCreateDto();
        SetupValidCreate(dto);
        var service = CreateService();

        await service.CreatePurchaseNoteAsync(dto);

        await PurchaseNoteRepo.Received(1).CreateAsync(Arg.Is<PurchaseNote>(pn =>
            pn.Status == PurchaseNoteStatus.Draft && pn.PaidDate == null));
        await InventoryService.DidNotReceive().CreateBatchAsync(
            Arg.Any<Guid>(), Arg.Any<IEnumerable<Application.DTOs.InventoryTransaction.CreateInventoryTransactionDto>>());
    }

    [Fact]
    public async Task CalculatesTotalsCorrectly()
    {
        var dto = BuildCreateDto();
        SetupValidCreate(dto);
        var service = CreateService();

        await service.CreatePurchaseNoteAsync(dto);

        // 98 quantity * 10 unit price = 980
        await PurchaseNoteRepo.Received(1).CreateAsync(Arg.Is<PurchaseNote>(pn =>
            pn.SubTotal == 980m && pn.TotalAmount == 980m));
    }

    [Fact]
    public async Task MultipleLineItems_CalculatesTotalsCorrectly()
    {
        var dto = new CreatePurchaseNoteDto
        {
            IndividualId = Guid.NewGuid(),
            WarehouseId = Guid.NewGuid(),
            PurchaseDate = DateTime.Today,
            LineItems =
            [
                new CreatePurchaseNoteLineDto
                {
                    ProductId = Guid.NewGuid(), Description = "A",
                    GrossQuantity = 100m, KaloPercentage = 0m, Quantity = 100m, UnitPrice = 5m
                },
                new CreatePurchaseNoteLineDto
                {
                    ProductId = Guid.NewGuid(), Description = "B",
                    GrossQuantity = 200m, KaloPercentage = 0m, Quantity = 200m, UnitPrice = 3m
                }
            ]
        };
        SetupValidCreate(dto);
        var service = CreateService();

        await service.CreatePurchaseNoteAsync(dto);

        // 100*5 + 200*3 = 500 + 600 = 1100
        await PurchaseNoteRepo.Received(1).CreateAsync(Arg.Is<PurchaseNote>(pn =>
            pn.SubTotal == 1100m && pn.TotalAmount == 1100m));
    }

    [Fact]
    public async Task InvalidIndividual_ThrowsKeyNotFound()
    {
        var dto = BuildCreateDto();
        IndividualRepo.ExistsAsync(dto.IndividualId, Arg.Any<CancellationToken>()).Returns(false);

        var service = CreateService();

        await service.Invoking(s => s.CreatePurchaseNoteAsync(dto))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task InvalidWarehouse_ThrowsKeyNotFound()
    {
        var dto = BuildCreateDto();
        IndividualRepo.ExistsAsync(dto.IndividualId, Arg.Any<CancellationToken>()).Returns(true);
        WarehouseRepo.ExistsAsync(dto.WarehouseId, Arg.Any<CancellationToken>()).Returns(false);

        var service = CreateService();

        await service.Invoking(s => s.CreatePurchaseNoteAsync(dto))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task InvalidProduct_ThrowsKeyNotFound()
    {
        var dto = BuildCreateDto();
        IndividualRepo.ExistsAsync(dto.IndividualId, Arg.Any<CancellationToken>()).Returns(true);
        WarehouseRepo.ExistsAsync(dto.WarehouseId, Arg.Any<CancellationToken>()).Returns(true);
        ProductRepo.AllExistAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>()).Returns(false);

        var service = CreateService();

        await service.Invoking(s => s.CreatePurchaseNoteAsync(dto))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task AlwaysCreatesDraft()
    {
        var dto = BuildCreateDto();
        SetupValidCreate(dto);
        var service = CreateService();

        await service.CreatePurchaseNoteAsync(dto);

        await PurchaseNoteRepo.Received(1).CreateAsync(Arg.Is<PurchaseNote>(pn =>
            pn.Status == PurchaseNoteStatus.Draft && pn.PaidDate == null));
    }

    [Fact]
    public async Task EmptyLineItems_CreatesSuccessfully()
    {
        var dto = new CreatePurchaseNoteDto
        {
            IndividualId = Guid.NewGuid(),
            WarehouseId = Guid.NewGuid(),
            PurchaseDate = DateTime.Today,
            LineItems = []
        };
        IndividualRepo.ExistsAsync(dto.IndividualId, Arg.Any<CancellationToken>()).Returns(true);
        WarehouseRepo.ExistsAsync(dto.WarehouseId, Arg.Any<CancellationToken>()).Returns(true);
        PurchaseNoteRepo.GenerateNoteNumberAsync(Arg.Any<CancellationToken>()).Returns("OB-000001");
        LocalizationService.GetString(Arg.Any<string>()).Returns("Purchase from");

        var service = CreateService();

        await service.Invoking(s => s.CreatePurchaseNoteAsync(dto))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task GeneratesNoteNumber()
    {
        var dto = BuildCreateDto();
        SetupValidCreate(dto);
        PurchaseNoteRepo.GenerateNoteNumberAsync(Arg.Any<CancellationToken>()).Returns("OB-000042");
        var service = CreateService();

        await service.CreatePurchaseNoteAsync(dto);

        await PurchaseNoteRepo.Received(1).CreateAsync(Arg.Is<PurchaseNote>(pn =>
            pn.NoteNumber == "OB-000042"));
    }

    [Fact]
    public async Task SetsCreatedAtOnLineItems()
    {
        var dto = BuildCreateDto();
        SetupValidCreate(dto);
        var before = DateTime.UtcNow;
        var service = CreateService();

        await service.CreatePurchaseNoteAsync(dto);

        await PurchaseNoteRepo.Received(1).CreateAsync(Arg.Is<PurchaseNote>(pn =>
            pn.LineItems.All(li => li.CreatedAt >= before)));
    }

    [Fact]
    public async Task RecalculatesQuantityFromGrossAndKalo()
    {
        var dto = new CreatePurchaseNoteDto
        {
            IndividualId = Guid.NewGuid(),
            WarehouseId = Guid.NewGuid(),
            PurchaseDate = DateTime.Today,
            LineItems =
            [
                new CreatePurchaseNoteLineDto
                {
                    ProductId = Guid.NewGuid(), Description = "A",
                    GrossQuantity = 200m, KaloPercentage = 10m,
                    Quantity = 999m, // wrong value from caller — service should override
                    UnitPrice = 5m
                }
            ]
        };
        SetupValidCreate(dto);
        var service = CreateService();

        await service.CreatePurchaseNoteAsync(dto);

        // Expected: 200 * (1 - 10/100) = 180, total = 180 * 5 = 900
        await PurchaseNoteRepo.Received(1).CreateAsync(Arg.Is<PurchaseNote>(pn =>
            pn.LineItems.First().Quantity == 180m &&
            pn.SubTotal == 900m));
    }
}
