namespace WarehouseInvoiceSystem.Tests.Services.InvoiceService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;

public class DraftFieldUpdateTests : InvoiceServiceTestBase
{
    // ── UpdateIssueDateAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task UpdateIssueDate_Draft_UpdatesField()
    {
        var invoice = CreateEntity(InvoiceStatus.Draft);
        SetupEntityLookup(invoice.Id, invoice);
        var service = CreateService();
        var newDate = DateTime.Today.AddDays(-5);

        await service.UpdateIssueDateAsync(invoice.Id, newDate);

        invoice.IssueDate.Should().Be(newDate);
        await InvoiceRepo.Received(1).UpdateAsync(invoice);
    }

    [Theory]
    [InlineData(InvoiceStatus.Confirmed)]
    [InlineData(InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Cancelled)]
    [InlineData(InvoiceStatus.Overdue)]
    [InlineData(InvoiceStatus.PartiallyPaid)]
    public async Task UpdateIssueDate_NonDraft_ThrowsInvalidOperation(InvoiceStatus status)
    {
        var invoice = CreateEntity(status);
        SetupEntityLookup(invoice.Id, invoice);
        var service = CreateService();

        await service.Invoking(s => s.UpdateIssueDateAsync(invoice.Id, DateTime.Today))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateIssueDate_NotFound_ThrowsKeyNotFound()
    {
        var id = Guid.NewGuid();
        InvoiceRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Invoice?)null);
        var service = CreateService();

        await service.Invoking(s => s.UpdateIssueDateAsync(id, DateTime.Today))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── UpdateDueDateAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateDueDate_Draft_UpdatesField()
    {
        var invoice = CreateEntity(InvoiceStatus.Draft);
        SetupEntityLookup(invoice.Id, invoice);
        var service = CreateService();
        var newDate = DateTime.Today.AddDays(45);

        await service.UpdateDueDateAsync(invoice.Id, newDate);

        invoice.DueDate.Should().Be(newDate);
        await InvoiceRepo.Received(1).UpdateAsync(invoice);
    }

    [Theory]
    [InlineData(InvoiceStatus.Confirmed)]
    [InlineData(InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Cancelled)]
    public async Task UpdateDueDate_NonDraft_ThrowsInvalidOperation(InvoiceStatus status)
    {
        var invoice = CreateEntity(status);
        SetupEntityLookup(invoice.Id, invoice);
        var service = CreateService();

        await service.Invoking(s => s.UpdateDueDateAsync(invoice.Id, DateTime.Today.AddDays(30)))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateDueDate_NotFound_ThrowsKeyNotFound()
    {
        var id = Guid.NewGuid();
        InvoiceRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Invoice?)null);
        var service = CreateService();

        await service.Invoking(s => s.UpdateDueDateAsync(id, DateTime.Today.AddDays(30)))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── UpdateWarehouseAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task UpdateWarehouse_Draft_ValidWarehouse_UpdatesField()
    {
        var invoice = CreateEntity(InvoiceStatus.Draft);
        var newWarehouseId = Guid.NewGuid();
        SetupEntityLookup(invoice.Id, invoice);
        WarehouseRepo.ExistsAsync(newWarehouseId, Arg.Any<CancellationToken>()).Returns(true);
        var service = CreateService();

        await service.UpdateWarehouseAsync(invoice.Id, newWarehouseId);

        invoice.WarehouseId.Should().Be(newWarehouseId);
        await InvoiceRepo.Received(1).UpdateAsync(invoice);
    }

    [Theory]
    [InlineData(InvoiceStatus.Confirmed)]
    [InlineData(InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Cancelled)]
    public async Task UpdateWarehouse_NonDraft_ThrowsInvalidOperation(InvoiceStatus status)
    {
        var invoice = CreateEntity(status);
        SetupEntityLookup(invoice.Id, invoice);
        var service = CreateService();

        await service.Invoking(s => s.UpdateWarehouseAsync(invoice.Id, Guid.NewGuid()))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateWarehouse_InvalidWarehouse_ThrowsKeyNotFound()
    {
        var invoice = CreateEntity(InvoiceStatus.Draft);
        var badId = Guid.NewGuid();
        SetupEntityLookup(invoice.Id, invoice);
        WarehouseRepo.ExistsAsync(badId, Arg.Any<CancellationToken>()).Returns(false);
        var service = CreateService();

        await service.Invoking(s => s.UpdateWarehouseAsync(invoice.Id, badId))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateWarehouse_NotFound_ThrowsKeyNotFound()
    {
        var id = Guid.NewGuid();
        InvoiceRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Invoice?)null);
        var service = CreateService();

        await service.Invoking(s => s.UpdateWarehouseAsync(id, Guid.NewGuid()))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── DuplicateInvoiceAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task Duplicate_CreatesNewDraftWithSameCompanyAndType()
    {
        var source = CreateEntity(InvoiceStatus.Confirmed, withLines: true);
        InvoiceRepo.GetByIdAsync(source.Id, Arg.Any<CancellationToken>()).Returns(source);
        InvoiceRepo.GenerateInvoiceNumberAsync(source.Type, Arg.Any<CancellationToken>()).Returns("INV-000002");
        InvoiceRepo.CreateAsync(Arg.Any<Invoice>()).Returns(ci => ci.Arg<Invoice>().Id);
        var service = CreateService();

        await service.DuplicateInvoiceAsync(source.Id);

        await InvoiceRepo.Received(1).CreateAsync(Arg.Is<Invoice>(inv =>
            inv.CompanyId == source.CompanyId &&
            inv.Type == source.Type &&
            inv.WarehouseId == source.WarehouseId &&
            inv.Status == InvoiceStatus.Draft &&
            inv.AmountPaid == 0m));
    }

    [Fact]
    public async Task Duplicate_IssueDateIsToday()
    {
        var source = CreateEntity(InvoiceStatus.Paid, withLines: true);
        InvoiceRepo.GetByIdAsync(source.Id, Arg.Any<CancellationToken>()).Returns(source);
        InvoiceRepo.GenerateInvoiceNumberAsync(source.Type, Arg.Any<CancellationToken>()).Returns("INV-000002");
        InvoiceRepo.CreateAsync(Arg.Any<Invoice>()).Returns(ci => ci.Arg<Invoice>().Id);
        var service = CreateService();
        var before = DateTime.UtcNow.Date;

        await service.DuplicateInvoiceAsync(source.Id);

        await InvoiceRepo.Received(1).CreateAsync(Arg.Is<Invoice>(inv =>
            inv.IssueDate >= before));
    }

    [Fact]
    public async Task Duplicate_DueDatePreservesOriginalSpan()
    {
        var source = CreateEntity(InvoiceStatus.Paid, withLines: true);
        source.IssueDate = DateTime.Today.AddDays(-10);
        source.DueDate = DateTime.Today.AddDays(20); // span = 30 days
        InvoiceRepo.GetByIdAsync(source.Id, Arg.Any<CancellationToken>()).Returns(source);
        InvoiceRepo.GenerateInvoiceNumberAsync(source.Type, Arg.Any<CancellationToken>()).Returns("INV-000002");
        InvoiceRepo.CreateAsync(Arg.Any<Invoice>()).Returns(ci => ci.Arg<Invoice>().Id);
        var service = CreateService();

        await service.DuplicateInvoiceAsync(source.Id);

        await InvoiceRepo.Received(1).CreateAsync(Arg.Is<Invoice>(inv =>
            (inv.DueDate.Date - inv.IssueDate.Date).Days == 30));
    }

    [Fact]
    public async Task Duplicate_CopiesLineItems()
    {
        var source = CreateEntity(InvoiceStatus.Confirmed, withLines: true);
        InvoiceRepo.GetByIdAsync(source.Id, Arg.Any<CancellationToken>()).Returns(source);
        InvoiceRepo.GenerateInvoiceNumberAsync(source.Type, Arg.Any<CancellationToken>()).Returns("INV-000002");
        InvoiceRepo.CreateAsync(Arg.Any<Invoice>()).Returns(ci => ci.Arg<Invoice>().Id);
        var service = CreateService();

        await service.DuplicateInvoiceAsync(source.Id);

        await InvoiceRepo.Received(1).CreateAsync(Arg.Is<Invoice>(inv =>
            inv.LineItems.Count == source.LineItems.Count &&
            inv.LineItems.All(li => li.Id == Guid.Empty || li.Id != source.LineItems.First().Id)));
    }

    [Fact]
    public async Task Duplicate_NotFound_ThrowsKeyNotFound()
    {
        var id = Guid.NewGuid();
        InvoiceRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Invoice?)null);
        var service = CreateService();

        await service.Invoking(s => s.DuplicateInvoiceAsync(id))
            .Should().ThrowAsync<KeyNotFoundException>();
    }
}
