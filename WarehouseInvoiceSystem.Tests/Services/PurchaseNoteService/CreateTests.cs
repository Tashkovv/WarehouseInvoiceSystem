namespace WarehouseInvoiceSystem.Tests.Services.PurchaseNoteService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Application.DTOs.InventoryTransaction;
using WarehouseInvoiceSystem.Application.DTOs.PurchaseNote;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;

public class CreateTests : PurchaseNoteServiceTestBase
{
    [Fact]
    public async Task Draft_DoesNotCreateInventoryTransactions()
    {
        var dto = BuildCreateDto(markAsPaid: false);
        SetupValidCreate(dto);
        var service = CreateService();

        await service.CreatePurchaseNoteAsync(dto);

        await PurchaseNoteRepo.Received(1).CreateAsync(Arg.Is<PurchaseNote>(pn =>
            pn.Status == PurchaseNoteStatus.Draft && pn.PaidDate == null));
        await InventoryService.DidNotReceive().CreateBatchAsync(
            Arg.Any<Guid>(), Arg.Any<IEnumerable<CreateInventoryTransactionDto>>());
    }

    [Fact]
    public async Task MarkAsPaid_CreatesInventoryTransactions()
    {
        var dto = BuildCreateDto(markAsPaid: true);
        SetupValidCreate(dto);
        var service = CreateService();

        await service.CreatePurchaseNoteAsync(dto);

        await PurchaseNoteRepo.Received(1).CreateAsync(Arg.Is<PurchaseNote>(pn =>
            pn.Status == PurchaseNoteStatus.Paid && pn.PaidDate != null));
        await InventoryService.Received(1).CreateBatchAsync(
            dto.WarehouseId, Arg.Any<IEnumerable<CreateInventoryTransactionDto>>());
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
        IndividualRepo.GetByIdAsync(dto.IndividualId, Arg.Any<CancellationToken>())
            .Returns((Individual?)null);

        var service = CreateService();

        await service.Invoking(s => s.CreatePurchaseNoteAsync(dto))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task InactiveIndividual_ThrowsInvalidOperation()
    {
        var dto = BuildCreateDto();
        IndividualRepo.GetByIdAsync(dto.IndividualId, Arg.Any<CancellationToken>())
            .Returns(new Individual { FullName = "Test User", IsActive = false });

        var service = CreateService();

        await service.Invoking(s => s.CreatePurchaseNoteAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("IndividualInactiveCannotCreate");
    }

    [Fact]
    public async Task InvalidWarehouse_ThrowsKeyNotFound()
    {
        var dto = BuildCreateDto();
        IndividualRepo.GetByIdAsync(dto.IndividualId, Arg.Any<CancellationToken>())
            .Returns(new Individual { FullName = "Test User", IsActive = true });
        WarehouseRepo.ExistsAsync(dto.WarehouseId, Arg.Any<CancellationToken>()).Returns(false);

        var service = CreateService();

        await service.Invoking(s => s.CreatePurchaseNoteAsync(dto))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task InvalidProduct_ThrowsKeyNotFound()
    {
        var dto = BuildCreateDto();
        IndividualRepo.GetByIdAsync(dto.IndividualId, Arg.Any<CancellationToken>())
            .Returns(new Individual { FullName = "Test User", IsActive = true });
        WarehouseRepo.ExistsAsync(dto.WarehouseId, Arg.Any<CancellationToken>()).Returns(true);
        ProductRepo.AllExistAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>()).Returns(false);

        var service = CreateService();

        await service.Invoking(s => s.CreatePurchaseNoteAsync(dto))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Draft_SetsStatusDraftAndNoPaidDate()
    {
        var dto = BuildCreateDto(markAsPaid: false);
        SetupValidCreate(dto);
        var service = CreateService();

        await service.CreatePurchaseNoteAsync(dto);

        await PurchaseNoteRepo.Received(1).CreateAsync(Arg.Is<PurchaseNote>(pn =>
            pn.Status == PurchaseNoteStatus.Draft && pn.PaidDate == null));
    }

    [Fact]
    public async Task MarkAsPaid_SetsPaidDate()
    {
        var dto = BuildCreateDto(markAsPaid: true);
        SetupValidCreate(dto);
        var service = CreateService();

        await service.CreatePurchaseNoteAsync(dto);

        await PurchaseNoteRepo.Received(1).CreateAsync(Arg.Is<PurchaseNote>(pn =>
            pn.PaidDate != null));
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
    public async Task MarkAsPaid_InventoryTransactionContainsCorrectData()
    {
        var dto = BuildCreateDto(markAsPaid: true);
        SetupValidCreate(dto);
        var service = CreateService();

        await service.CreatePurchaseNoteAsync(dto);

        await InventoryService.Received(1).CreateBatchAsync(
            dto.WarehouseId,
            Arg.Is<IEnumerable<CreateInventoryTransactionDto>>(items =>
                items.All(i =>
                    i.Type == InventoryTransactionType.Inbound &&
                    i.SourceDocumentType == "PurchaseNote" &&
                    i.Quantity == 98m)));
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

    [Fact]
    public async Task EmptyLineItems_ThrowsInvalidOperation()
    {
        var dto = new CreatePurchaseNoteDto
        {
            IndividualId = Guid.NewGuid(),
            WarehouseId = Guid.NewGuid(),
            PurchaseDate = DateTime.Today,
            LineItems = []
        };
        var service = CreateService();

        await service.Invoking(s => s.CreatePurchaseNoteAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>();
    }
}
