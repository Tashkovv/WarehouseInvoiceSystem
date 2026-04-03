namespace WarehouseInvoiceSystem.Tests.Services.PurchaseNoteService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Application.DTOs.PurchaseNote;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;

public class UpdateTests : PurchaseNoteServiceTestBase
{
    [Fact]
    public async Task Draft_UpdatesHeaderFields()
    {
        var note = CreateEntity(PurchaseNoteStatus.Draft);
        var dto = BuildUpdateDto();
        SetupEntityLookup(note.Id, note);
        SetupValidUpdate(dto);
        var service = CreateService();

        await service.UpdatePurchaseNoteAsync(note.Id, dto);

        note.IndividualId.Should().Be(dto.IndividualId);
        note.WarehouseId.Should().Be(dto.WarehouseId);
        note.PurchaseDate.Should().Be(dto.PurchaseDate);
        note.Notes.Should().Be(dto.Notes);
        await PurchaseNoteRepo.Received(1).UpdateAsync(note);
    }

    [Theory]
    [InlineData(PurchaseNoteStatus.Pending)]
    [InlineData(PurchaseNoteStatus.Paid)]
    [InlineData(PurchaseNoteStatus.Cancelled)]
    public async Task NonDraft_ThrowsInvalidOperation(PurchaseNoteStatus status)
    {
        var note = CreateEntity(status);
        var dto = BuildUpdateDto();
        SetupEntityLookup(note.Id, note);
        var service = CreateService();

        await service.Invoking(s => s.UpdatePurchaseNoteAsync(note.Id, dto))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task NotFound_ThrowsKeyNotFound()
    {
        var id = Guid.NewGuid();
        PurchaseNoteRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((PurchaseNote?)null);
        var service = CreateService();

        await service.Invoking(s => s.UpdatePurchaseNoteAsync(id, BuildUpdateDto()))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task InvalidIndividual_ThrowsKeyNotFound()
    {
        var note = CreateEntity(PurchaseNoteStatus.Draft);
        var dto = BuildUpdateDto();
        SetupEntityLookup(note.Id, note);
        IndividualRepo.ExistsAsync(dto.IndividualId, Arg.Any<CancellationToken>()).Returns(false);
        var service = CreateService();

        await service.Invoking(s => s.UpdatePurchaseNoteAsync(note.Id, dto))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task InvalidWarehouse_ThrowsKeyNotFound()
    {
        var note = CreateEntity(PurchaseNoteStatus.Draft);
        var dto = BuildUpdateDto();
        SetupEntityLookup(note.Id, note);
        IndividualRepo.ExistsAsync(dto.IndividualId, Arg.Any<CancellationToken>()).Returns(true);
        WarehouseRepo.ExistsAsync(dto.WarehouseId, Arg.Any<CancellationToken>()).Returns(false);
        var service = CreateService();

        await service.Invoking(s => s.UpdatePurchaseNoteAsync(note.Id, dto))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task InvalidProduct_ThrowsKeyNotFound()
    {
        var note = CreateEntity(PurchaseNoteStatus.Draft);
        var dto = BuildUpdateDto();
        SetupEntityLookup(note.Id, note);
        IndividualRepo.ExistsAsync(dto.IndividualId, Arg.Any<CancellationToken>()).Returns(true);
        WarehouseRepo.ExistsAsync(dto.WarehouseId, Arg.Any<CancellationToken>()).Returns(true);
        ProductRepo.AllExistAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>()).Returns(false);
        var service = CreateService();

        await service.Invoking(s => s.UpdatePurchaseNoteAsync(note.Id, dto))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task MergeLines_SoftDeletesRemovedLines()
    {
        var note = CreateEntity(PurchaseNoteStatus.Draft, withLines: true);
        var existingLineId = note.LineItems.First().Id;

        // DTO replaces the existing line with a new one — the old line should be soft-deleted
        var dto = BuildUpdateDto();
        dto.LineItems =
        [
            new UpdatePurchaseNoteLineDto
            {
                Id = Guid.Empty,
                ProductId = Guid.NewGuid(),
                Description = "Replacement",
                GrossQuantity = 50m,
                KaloPercentage = 0m,
                Quantity = 50m,
                UnitPrice = 10m
            }
        ];

        SetupEntityLookup(note.Id, note);
        SetupValidUpdate(dto);
        var service = CreateService();

        await service.UpdatePurchaseNoteAsync(note.Id, dto);

        note.LineItems.First(li => li.Id == existingLineId).DeletedOn.Should().NotBeNull();
    }

    [Fact]
    public async Task MergeLines_UpdatesExistingLines()
    {
        var note = CreateEntity(PurchaseNoteStatus.Draft, withLines: true);
        var existingLine = note.LineItems.First();

        var dto = BuildUpdateDto();
        dto.LineItems =
        [
            new UpdatePurchaseNoteLineDto
            {
                Id = existingLine.Id,
                ProductId = existingLine.ProductId,
                Description = "Updated Description",
                GrossQuantity = 200m,
                KaloPercentage = 5m,
                Quantity = 190m,
                UnitPrice = 15m
            }
        ];

        SetupEntityLookup(note.Id, note);
        SetupValidUpdate(dto);
        var service = CreateService();

        await service.UpdatePurchaseNoteAsync(note.Id, dto);

        existingLine.Description.Should().Be("Updated Description");
        existingLine.GrossQuantity.Should().Be(200m);
        existingLine.Quantity.Should().Be(190m);
        existingLine.UnitPrice.Should().Be(15m);
    }

    [Fact]
    public async Task MergeLines_AddsNewLines()
    {
        var note = CreateEntity(PurchaseNoteStatus.Draft, withLines: false);

        var dto = BuildUpdateDto();
        dto.LineItems =
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
        ];

        SetupEntityLookup(note.Id, note);
        SetupValidUpdate(dto);
        var service = CreateService();

        await service.UpdatePurchaseNoteAsync(note.Id, dto);

        note.LineItems.Should().HaveCount(1);
        note.LineItems.First().Description.Should().Be("New Line");
    }

    [Fact]
    public async Task MergeLines_RecalculatesTotals()
    {
        var note = CreateEntity(PurchaseNoteStatus.Draft, withLines: false);

        var dto = BuildUpdateDto();
        dto.LineItems =
        [
            new UpdatePurchaseNoteLineDto
            {
                Id = Guid.Empty, ProductId = Guid.NewGuid(), Description = "A",
                GrossQuantity = 10m, KaloPercentage = 0m, Quantity = 10m, UnitPrice = 5m
            },
            new UpdatePurchaseNoteLineDto
            {
                Id = Guid.Empty, ProductId = Guid.NewGuid(), Description = "B",
                GrossQuantity = 20m, KaloPercentage = 0m, Quantity = 20m, UnitPrice = 3m
            }
        ];

        SetupEntityLookup(note.Id, note);
        SetupValidUpdate(dto);
        var service = CreateService();

        await service.UpdatePurchaseNoteAsync(note.Id, dto);

        // 10*5 + 20*3 = 50 + 60 = 110
        note.SubTotal.Should().Be(110m);
        note.TotalAmount.Should().Be(110m);
    }

    [Fact]
    public async Task EmptyLineItems_ThrowsInvalidOperation()
    {
        var dto = BuildUpdateDto();
        dto.LineItems.Clear();

        var service = CreateService();

        await service.Invoking(s => s.UpdatePurchaseNoteAsync(Guid.NewGuid(), dto))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task MergeLines_RecalculatesQuantityFromGrossAndKalo()
    {
        var note = CreateEntity(PurchaseNoteStatus.Draft, withLines: true);
        var existingLine = note.LineItems.First();

        var dto = BuildUpdateDto();
        dto.LineItems =
        [
            new UpdatePurchaseNoteLineDto
            {
                Id = existingLine.Id,
                ProductId = existingLine.ProductId,
                Description = existingLine.Description,
                GrossQuantity = 100m,
                KaloPercentage = 10m,
                Quantity = 999m, // wrong value — service should override
                UnitPrice = 10m
            }
        ];

        SetupEntityLookup(note.Id, note);
        SetupValidUpdate(dto);
        var service = CreateService();

        await service.UpdatePurchaseNoteAsync(note.Id, dto);

        // Expected: 100 * (1 - 10/100) = 90
        existingLine.Quantity.Should().Be(90m);
        note.SubTotal.Should().Be(900m); // 90 * 10
    }

    // ── UpdateNotesAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateNotes_UpdatesNotesField()
    {
        var note = CreateEntity(PurchaseNoteStatus.Pending);
        SetupEntityLookup(note.Id, note);
        var service = CreateService();

        await service.UpdateNotesAsync(note.Id, "New notes");

        note.Notes.Should().Be("New notes");
        await PurchaseNoteRepo.Received(1).UpdateAsync(note);
    }

    [Fact]
    public async Task UpdateNotes_NotFound_ThrowsKeyNotFound()
    {
        var id = Guid.NewGuid();
        PurchaseNoteRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((PurchaseNote?)null);
        var service = CreateService();

        await service.Invoking(s => s.UpdateNotesAsync(id, "notes"))
            .Should().ThrowAsync<KeyNotFoundException>();
    }
}
