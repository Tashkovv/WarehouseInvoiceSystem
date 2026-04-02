namespace WarehouseInvoiceSystem.Tests.Services;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Application.DTOs.PurchaseNote;
using WarehouseInvoiceSystem.Application.Interfaces;
using WarehouseInvoiceSystem.Application.Services;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;
using WarehouseInvoiceSystem.Domain.Interfaces;

public class PurchaseNoteServiceTests
{
    private readonly IPurchaseNoteRepository _purchaseNoteRepo = Substitute.For<IPurchaseNoteRepository>();
    private readonly IIndividualRepository _individualRepo = Substitute.For<IIndividualRepository>();
    private readonly IWarehouseRepository _warehouseRepo = Substitute.For<IWarehouseRepository>();
    private readonly IProductRepository _productRepo = Substitute.For<IProductRepository>();
    private readonly IInventoryService _inventoryService = Substitute.For<IInventoryService>();
    private readonly ILocalizationService _localizationService = Substitute.For<ILocalizationService>();

    private PurchaseNoteService CreateService() =>
        new(_purchaseNoteRepo, _individualRepo, _warehouseRepo, _productRepo, _inventoryService, _localizationService);

    private static CreatePurchaseNoteDto BuildCreateDto(bool markAsPaid = false) => new()
    {
        IndividualId = Guid.NewGuid(),
        WarehouseId = Guid.NewGuid(),
        PurchaseDate = DateTime.Today,
        MarkAsPaid = markAsPaid,
        LineItems =
        [
            new CreatePurchaseNoteLineDto
            {
                ProductId = Guid.NewGuid(),
                Description = "Test Product",
                GrossQuantity = 100m,
                KaloPercentage = 2m,
                Quantity = 98m,
                UnitPrice = 10m
            }
        ]
    };

    private void SetupValidCreate(CreatePurchaseNoteDto dto)
    {
        _individualRepo.GetByIdAsync(dto.IndividualId, Arg.Any<CancellationToken>())
            .Returns(new Individual { FirstName = "Test", LastName = "User" });
        _warehouseRepo.ExistsAsync(dto.WarehouseId, Arg.Any<CancellationToken>()).Returns(true);
        _productRepo.AllExistAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>()).Returns(true);
        _purchaseNoteRepo.GenerateNoteNumberAsync(Arg.Any<CancellationToken>()).Returns("OB-000001");
        _localizationService.GetString(Arg.Any<string>()).Returns("Purchase from");
    }

    [Fact]
    public async Task CreatePurchaseNote_Draft_DoesNotCreateInventoryTransactions()
    {
        var dto = BuildCreateDto(markAsPaid: false);
        SetupValidCreate(dto);
        var service = CreateService();

        await service.CreatePurchaseNoteAsync(dto);

        await _purchaseNoteRepo.Received(1).CreateAsync(Arg.Is<PurchaseNote>(pn =>
            pn.Status == PurchaseNoteStatus.Draft && pn.PaidDate == null));
        await _inventoryService.DidNotReceive().CreateBatchAsync(
            Arg.Any<Guid>(), Arg.Any<IEnumerable<Application.DTOs.InventoryTransaction.CreateInventoryTransactionDto>>());
    }

    [Fact]
    public async Task CreatePurchaseNote_MarkAsPaid_CreatesInventoryTransactions()
    {
        var dto = BuildCreateDto(markAsPaid: true);
        SetupValidCreate(dto);
        var service = CreateService();

        await service.CreatePurchaseNoteAsync(dto);

        await _purchaseNoteRepo.Received(1).CreateAsync(Arg.Is<PurchaseNote>(pn =>
            pn.Status == PurchaseNoteStatus.Paid && pn.PaidDate != null));
        await _inventoryService.Received(1).CreateBatchAsync(
            dto.WarehouseId, Arg.Any<IEnumerable<Application.DTOs.InventoryTransaction.CreateInventoryTransactionDto>>());
    }

    [Fact]
    public async Task CreatePurchaseNote_CalculatesTotalsCorrectly()
    {
        var dto = BuildCreateDto();
        SetupValidCreate(dto);
        var service = CreateService();

        await service.CreatePurchaseNoteAsync(dto);

        await _purchaseNoteRepo.Received(1).CreateAsync(Arg.Is<PurchaseNote>(pn =>
            pn.SubTotal == 980m && pn.TotalAmount == 980m));
    }

    [Fact]
    public async Task CreatePurchaseNote_InvalidIndividual_Throws()
    {
        var dto = BuildCreateDto();
        _individualRepo.GetByIdAsync(dto.IndividualId, Arg.Any<CancellationToken>())
            .Returns((Individual?)null);

        var service = CreateService();

        await service.Invoking(s => s.CreatePurchaseNoteAsync(dto))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task CreatePurchaseNote_InvalidWarehouse_Throws()
    {
        var dto = BuildCreateDto();
        _individualRepo.GetByIdAsync(dto.IndividualId, Arg.Any<CancellationToken>())
            .Returns(new Individual { FirstName = "Test", LastName = "User" });
        _warehouseRepo.ExistsAsync(dto.WarehouseId, Arg.Any<CancellationToken>()).Returns(false);

        var service = CreateService();

        await service.Invoking(s => s.CreatePurchaseNoteAsync(dto))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task MarkAsPaid_FromPending_Succeeds()
    {
        var noteId = Guid.NewGuid();
        var note = CreatePurchaseNoteEntity(PurchaseNoteStatus.Pending);
        _purchaseNoteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(note);

        var service = CreateService();
        var result = await service.MarkAsPaidAsync(noteId);

        result.Status.Should().Be(PurchaseNoteStatus.Paid);
        note.PaidDate.Should().NotBeNull();
        await _purchaseNoteRepo.Received(1).UpdateAsync(note);
    }

    [Fact]
    public async Task MarkAsPaid_FromDraft_Throws()
    {
        var noteId = Guid.NewGuid();
        var note = CreatePurchaseNoteEntity(PurchaseNoteStatus.Draft);
        _purchaseNoteRepo.GetByIdAsync(noteId, Arg.Any<CancellationToken>()).Returns(note);

        var service = CreateService();

        await service.Invoking(s => s.MarkAsPaidAsync(noteId))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    private static PurchaseNote CreatePurchaseNoteEntity(PurchaseNoteStatus status) => new()
    {
        NoteNumber = "OB-000001",
        Status = status,
        IndividualId = Guid.NewGuid(),
        WarehouseId = Guid.NewGuid(),
        Individual = new Individual { FirstName = "Test", LastName = "User" },
        Warehouse = new Warehouse { Name = "Main" },
        LineItems = []
    };
}
