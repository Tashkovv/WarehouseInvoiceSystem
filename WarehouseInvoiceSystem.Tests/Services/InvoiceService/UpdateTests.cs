namespace WarehouseInvoiceSystem.Tests.Services.InvoiceService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Application.DTOs.Invoice;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;

public class UpdateTests : InvoiceServiceTestBase
{
    [Fact]
    public async Task Draft_UpdatesHeaderFields()
    {
        var invoice = CreateEntity(InvoiceStatus.Draft);
        var dto = BuildUpdateDto();
        SetupEntityLookup(invoice.Id, invoice);
        SetupValidUpdate(dto);
        var service = CreateService();

        await service.UpdateInvoiceAsync(invoice.Id, dto);

        invoice.CompanyId.Should().Be(dto.CompanyId);
        invoice.WarehouseId.Should().Be(dto.WarehouseId);
        invoice.Type.Should().Be(dto.Type);
        invoice.IssueDate.Should().Be(dto.IssueDate);
        invoice.DueDate.Should().Be(dto.DueDate);
        invoice.Notes.Should().Be(dto.Notes);
        await InvoiceRepo.Received(1).UpdateAsync(invoice);
    }

    [Theory]
    [InlineData(InvoiceStatus.Confirmed)]
    [InlineData(InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Cancelled)]
    [InlineData(InvoiceStatus.Overdue)]
    [InlineData(InvoiceStatus.PartiallyPaid)]
    public async Task NonDraft_ThrowsInvalidOperation(InvoiceStatus status)
    {
        var invoice = CreateEntity(status);
        var dto = BuildUpdateDto();
        SetupEntityLookup(invoice.Id, invoice);
        var service = CreateService();

        await service.Invoking(s => s.UpdateInvoiceAsync(invoice.Id, dto))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task NotFound_ThrowsKeyNotFound()
    {
        var id = Guid.NewGuid();
        InvoiceRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Invoice?)null);
        var service = CreateService();

        await service.Invoking(s => s.UpdateInvoiceAsync(id, BuildUpdateDto()))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task InvalidCompany_ThrowsKeyNotFound()
    {
        var invoice = CreateEntity(InvoiceStatus.Draft);
        var dto = BuildUpdateDto();
        SetupEntityLookup(invoice.Id, invoice);
        CompanyRepo.ExistsAsync(dto.CompanyId, Arg.Any<CancellationToken>()).Returns(false);
        var service = CreateService();

        await service.Invoking(s => s.UpdateInvoiceAsync(invoice.Id, dto))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task InvalidWarehouse_ThrowsKeyNotFound()
    {
        var invoice = CreateEntity(InvoiceStatus.Draft);
        var dto = BuildUpdateDto();
        SetupEntityLookup(invoice.Id, invoice);
        CompanyRepo.ExistsAsync(dto.CompanyId, Arg.Any<CancellationToken>()).Returns(true);
        WarehouseRepo.ExistsAsync(dto.WarehouseId, Arg.Any<CancellationToken>()).Returns(false);
        var service = CreateService();

        await service.Invoking(s => s.UpdateInvoiceAsync(invoice.Id, dto))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task InvalidProduct_ThrowsKeyNotFound()
    {
        var invoice = CreateEntity(InvoiceStatus.Draft);
        var dto = BuildUpdateDto();
        SetupEntityLookup(invoice.Id, invoice);
        CompanyRepo.ExistsAsync(dto.CompanyId, Arg.Any<CancellationToken>()).Returns(true);
        WarehouseRepo.ExistsAsync(dto.WarehouseId, Arg.Any<CancellationToken>()).Returns(true);
        ProductRepo.AllExistAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>()).Returns(false);
        var service = CreateService();

        await service.Invoking(s => s.UpdateInvoiceAsync(invoice.Id, dto))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task EmptyLineItems_ThrowsInvalidOperation()
    {
        var dto = BuildUpdateDto();
        dto.LineItems.Clear();
        var service = CreateService();

        await service.Invoking(s => s.UpdateInvoiceAsync(Guid.NewGuid(), dto))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task MergeLines_SoftDeletesRemovedLines()
    {
        var invoice = CreateEntity(InvoiceStatus.Draft, withLines: true);
        var existingLineId = invoice.LineItems.First().Id;

        // DTO replaces existing line with a new one
        var dto = BuildUpdateDto();
        SetupEntityLookup(invoice.Id, invoice);
        SetupValidUpdate(dto);
        var service = CreateService();

        await service.UpdateInvoiceAsync(invoice.Id, dto);

        invoice.LineItems.First(li => li.Id == existingLineId).DeletedOn.Should().NotBeNull();
    }

    [Fact]
    public async Task MergeLines_UpdatesExistingLines()
    {
        var invoice = CreateEntity(InvoiceStatus.Draft, withLines: true);
        var existingLine = invoice.LineItems.First();

        var dto = BuildUpdateDto();
        dto.LineItems =
        [
            new UpdateInvoiceLineDto
            {
                Id = existingLine.Id,
                ProductId = existingLine.ProductId,
                Description = "Updated",
                Quantity = 20,
                UnitPrice = 50m,
                TaxRate = 10m,
                DiscountPercentage = 0m
            }
        ];

        SetupEntityLookup(invoice.Id, invoice);
        SetupValidUpdate(dto);
        var service = CreateService();

        await service.UpdateInvoiceAsync(invoice.Id, dto);

        existingLine.Description.Should().Be("Updated");
        existingLine.Quantity.Should().Be(20);
        existingLine.UnitPrice.Should().Be(50m);
        existingLine.TaxRate.Should().Be(10m);
    }

    [Fact]
    public async Task MergeLines_AddsNewLines()
    {
        var invoice = CreateEntity(InvoiceStatus.Draft, withLines: false);
        var dto = BuildUpdateDto();
        SetupEntityLookup(invoice.Id, invoice);
        SetupValidUpdate(dto);
        var service = CreateService();

        await service.UpdateInvoiceAsync(invoice.Id, dto);

        invoice.LineItems.Should().HaveCount(1);
        invoice.LineItems.First().Description.Should().Be("New Line");
    }

    [Fact]
    public async Task MergeLines_RecalculatesTotals()
    {
        var invoice = CreateEntity(InvoiceStatus.Draft, withLines: false);

        var dto = BuildUpdateDto();
        dto.LineItems =
        [
            new UpdateInvoiceLineDto
            {
                Id = Guid.Empty, ProductId = Guid.NewGuid(), Description = "A",
                Quantity = 10, UnitPrice = 100m, TaxRate = 0m, DiscountPercentage = 10m
            }
        ];

        SetupEntityLookup(invoice.Id, invoice);
        SetupValidUpdate(dto);
        var service = CreateService();

        await service.UpdateInvoiceAsync(invoice.Id, dto);

        // Amount=1000, Discount=100, Tax=0, Total=900
        invoice.SubTotal.Should().Be(1000m);
        invoice.DiscountTotal.Should().Be(100m);
        invoice.TaxAmount.Should().Be(0m);
        invoice.TotalAmount.Should().Be(900m);
    }

    // ── UpdateNotesAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateNotes_UpdatesNotesField()
    {
        var invoice = CreateEntity(InvoiceStatus.Confirmed);
        SetupEntityLookup(invoice.Id, invoice);
        var service = CreateService();

        await service.UpdateNotesAsync(invoice.Id, "New notes");

        invoice.Notes.Should().Be("New notes");
        await InvoiceRepo.Received(1).UpdateAsync(invoice);
    }

    [Fact]
    public async Task UpdateNotes_NotFound_ThrowsKeyNotFound()
    {
        var id = Guid.NewGuid();
        InvoiceRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Invoice?)null);
        var service = CreateService();

        await service.Invoking(s => s.UpdateNotesAsync(id, "notes"))
            .Should().ThrowAsync<KeyNotFoundException>();
    }
}
