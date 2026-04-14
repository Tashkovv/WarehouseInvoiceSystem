namespace WarehouseInvoiceSystem.Tests.Services.PurchaseNoteService;

using NSubstitute;
using WarehouseInvoiceSystem.Application.DTOs.PurchaseNote;
using WarehouseInvoiceSystem.Application.Interfaces;
using WarehouseInvoiceSystem.Application.Services;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;
using WarehouseInvoiceSystem.Domain.Interfaces;

public abstract class PurchaseNoteServiceTestBase
{
    protected readonly IPurchaseNoteRepository PurchaseNoteRepo = Substitute.For<IPurchaseNoteRepository>();
    protected readonly IIndividualRepository IndividualRepo = Substitute.For<IIndividualRepository>();
    protected readonly IWarehouseRepository WarehouseRepo = Substitute.For<IWarehouseRepository>();
    protected readonly IProductRepository ProductRepo = Substitute.For<IProductRepository>();
    protected readonly IInventoryService InventoryService = Substitute.For<IInventoryService>();
    protected readonly ILocalizationService LocalizationService = Substitute.For<ILocalizationService>();
    protected readonly IInventoryTransactionRepository TransactionRepo = Substitute.For<IInventoryTransactionRepository>();

    protected PurchaseNoteService CreateService() =>
        new(PurchaseNoteRepo, IndividualRepo, WarehouseRepo, ProductRepo, InventoryService, LocalizationService, TransactionRepo);

    protected static CreatePurchaseNoteDto BuildCreateDto(bool markAsPaid = false) => new()
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

    protected static UpdatePurchaseNoteDto BuildUpdateDto() => new()
    {
        IndividualId = Guid.NewGuid(),
        WarehouseId = Guid.NewGuid(),
        PurchaseDate = DateTime.Today,
        Notes = "Updated notes",
        LineItems =
        [
            new UpdatePurchaseNoteLineDto
            {
                Id = Guid.Empty,
                ProductId = Guid.NewGuid(),
                Description = "New Line",
                GrossQuantity = 50m,
                KaloPercentage = 0m,
                Quantity = 50m,
                UnitPrice = 20m
            }
        ]
    };

    protected static PurchaseNote CreateEntity(PurchaseNoteStatus status, bool withLines = true)
    {
        var note = new PurchaseNote
        {
            NoteNumber = "OB-000001",
            Status = status,
            IndividualId = Guid.NewGuid(),
            WarehouseId = Guid.NewGuid(),
            PurchaseDate = DateTime.Today,
            SubTotal = 980m,
            TotalAmount = 980m,
            Individual = new Individual { FirstName = "Test", LastName = "User" },
            Warehouse = new Warehouse { Name = "Main" },
            LineItems = []
        };
        SetEntityId(note, Guid.NewGuid());

        if (withLines)
        {
            var line = new PurchaseNoteLine
            {
                PurchaseNoteId = note.Id,
                ProductId = Guid.NewGuid(),
                Description = "Test Product",
                GrossQuantity = 100m,
                KaloPercentage = 2m,
                Quantity = 98m,
                UnitPrice = 10m,
                Product = new Product { Code = "P001", Name = "Test Product", Unit = "kg" }
            };
            SetEntityId(line, Guid.NewGuid());
            note.LineItems.Add(line);
        }

        return note;
    }

    protected static void SetEntityId(Domain.Common.Entity entity, Guid id)
    {
        typeof(Domain.Common.Entity)
            .GetProperty(nameof(Domain.Common.Entity.Id))!
            .SetValue(entity, id);
    }

    protected void SetupValidCreate(CreatePurchaseNoteDto dto)
    {
        IndividualRepo.GetByIdAsync(dto.IndividualId, Arg.Any<CancellationToken>())
            .Returns(new Individual { FirstName = "Test", LastName = "User", IsActive = true });
        WarehouseRepo.ExistsAsync(dto.WarehouseId, Arg.Any<CancellationToken>()).Returns(true);
        ProductRepo.AllExistAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>()).Returns(true);
        PurchaseNoteRepo.GenerateNoteNumberAsync(Arg.Any<CancellationToken>()).Returns("OB-000001");
        LocalizationService.GetString(Arg.Any<string>()).Returns("Purchase from");
    }

    protected void SetupEntityLookup(Guid id, PurchaseNote entity)
    {
        PurchaseNoteRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(entity);
    }

    protected void SetupValidUpdate(UpdatePurchaseNoteDto dto)
    {
        IndividualRepo.ExistsAsync(dto.IndividualId, Arg.Any<CancellationToken>()).Returns(true);
        WarehouseRepo.ExistsAsync(dto.WarehouseId, Arg.Any<CancellationToken>()).Returns(true);
        ProductRepo.AllExistAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>()).Returns(true);
    }
}
