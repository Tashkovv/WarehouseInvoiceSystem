namespace WarehouseInvoiceSystem.Tests.Services.InvoiceService;

using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using WarehouseInvoiceSystem.Application.DTOs.InventoryTransaction;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;

public class StatusTransitionTests : InvoiceServiceTestBase
{
    // ── ConfirmAsync (Draft → Confirmed) ──────────────────────────────────────

    [Fact]
    public async Task Confirm_FromDraft_SetsConfirmed()
    {
        var invoice = CreateEntity(InvoiceStatus.Draft);
        SetupEntityLookup(invoice.Id, invoice);
        TransactionRepo.HasTransactionsForDocumentAsync(invoice.Id, "Invoice", Arg.Any<CancellationToken>()).Returns(false);
        LocalizationService.GetString(Arg.Any<string>()).Returns("Sale to");
        var service = CreateService();

        var (result, _) = await service.ConfirmAsync(invoice.Id);

        result.Status.Should().Be(InvoiceStatus.Confirmed);
    }

    [Fact]
    public async Task Confirm_CreatesInventoryTransactions()
    {
        var invoice = CreateEntity(InvoiceStatus.Draft);
        SetupEntityLookup(invoice.Id, invoice);
        TransactionRepo.HasTransactionsForDocumentAsync(invoice.Id, "Invoice", Arg.Any<CancellationToken>()).Returns(false);
        LocalizationService.GetString(Arg.Any<string>()).Returns("Sale to");
        var service = CreateService();

        await service.ConfirmAsync(invoice.Id);

        await InventoryService.Received(1).CreateBatchAsync(
            invoice.WarehouseId, Arg.Any<IEnumerable<CreateInventoryTransactionDto>>());
    }

    [Fact]
    public async Task Confirm_SoftDeletesPreviousTransactions()
    {
        var invoice = CreateEntity(InvoiceStatus.Draft);
        SetupEntityLookup(invoice.Id, invoice);
        TransactionRepo.HasTransactionsForDocumentAsync(invoice.Id, "Invoice", Arg.Any<CancellationToken>()).Returns(false);
        LocalizationService.GetString(Arg.Any<string>()).Returns("Sale to");
        var service = CreateService();

        await service.ConfirmAsync(invoice.Id);

        await InventoryService.Received(1).SoftDeleteTransactionsForDocumentAsync(invoice.Id, "Invoice");
        await InventoryService.Received(1).SoftDeleteReversalForDocumentAsync(invoice.Id, "Invoice");
    }

    [Fact]
    public async Task Confirm_Receivable_CreatesOutboundTransactions()
    {
        var invoice = CreateEntity(InvoiceStatus.Draft, InvoiceType.Receivable);
        SetupEntityLookup(invoice.Id, invoice);
        TransactionRepo.HasTransactionsForDocumentAsync(invoice.Id, "Invoice", Arg.Any<CancellationToken>()).Returns(false);
        LocalizationService.GetString(Arg.Any<string>()).Returns("Sale to");
        var service = CreateService();

        await service.ConfirmAsync(invoice.Id);

        await InventoryService.Received(1).CreateBatchAsync(
            invoice.WarehouseId,
            Arg.Is<IEnumerable<CreateInventoryTransactionDto>>(items =>
                items.All(i => i.Type == InventoryTransactionType.Outbound)));
    }

    [Fact]
    public async Task Confirm_Payable_CreatesInboundTransactions()
    {
        var invoice = CreateEntity(InvoiceStatus.Draft, InvoiceType.Payable);
        SetupEntityLookup(invoice.Id, invoice);
        TransactionRepo.HasTransactionsForDocumentAsync(invoice.Id, "Invoice", Arg.Any<CancellationToken>()).Returns(false);
        LocalizationService.GetString(Arg.Any<string>()).Returns("Purchase from");
        var service = CreateService();

        await service.ConfirmAsync(invoice.Id);

        await InventoryService.Received(1).CreateBatchAsync(
            invoice.WarehouseId,
            Arg.Is<IEnumerable<CreateInventoryTransactionDto>>(items =>
                items.All(i => i.Type == InventoryTransactionType.Inbound)));
    }

    [Fact]
    public async Task Confirm_ReturnsDueDatePassedFlag()
    {
        var invoice = CreateEntity(InvoiceStatus.Draft);
        invoice.DueDate = DateTime.UtcNow.Date.AddDays(-1); // past due
        SetupEntityLookup(invoice.Id, invoice);
        TransactionRepo.HasTransactionsForDocumentAsync(invoice.Id, "Invoice", Arg.Any<CancellationToken>()).Returns(false);
        LocalizationService.GetString(Arg.Any<string>()).Returns("Sale to");
        var service = CreateService();

        var (_, isDueDatePassed) = await service.ConfirmAsync(invoice.Id);

        isDueDatePassed.Should().BeTrue();
    }

    [Theory]
    [InlineData(InvoiceStatus.Confirmed)]
    [InlineData(InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Cancelled)]
    [InlineData(InvoiceStatus.PartiallyPaid)]
    [InlineData(InvoiceStatus.Overdue)]
    public async Task Confirm_FromNonDraft_Throws(InvoiceStatus status)
    {
        var invoice = CreateEntity(status);
        SetupEntityLookup(invoice.Id, invoice);
        var service = CreateService();

        await service.Invoking(s => s.ConfirmAsync(invoice.Id))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Confirm_InventoryFailure_RollsBackStatus()
    {
        var invoice = CreateEntity(InvoiceStatus.Draft);
        SetupEntityLookup(invoice.Id, invoice);
        InventoryService.SoftDeleteTransactionsForDocumentAsync(invoice.Id, "Invoice")
            .Throws(new InvalidOperationException("Inventory error"));
        var service = CreateService();

        await service.Invoking(s => s.ConfirmAsync(invoice.Id))
            .Should().ThrowAsync<InvalidOperationException>();

        invoice.Status.Should().Be(InvoiceStatus.Draft);
        await InvoiceRepo.Received(2).UpdateAsync(invoice);
    }

    // ── RevertToDraftAsync (Confirmed/Overdue → Draft) ────────────────────────

    [Fact]
    public async Task RevertToDraft_FromConfirmed_SetsDraft()
    {
        var invoice = CreateEntity(InvoiceStatus.Confirmed);
        SetupEntityLookup(invoice.Id, invoice);
        var service = CreateService();

        var result = await service.RevertToDraftAsync(invoice.Id);

        result.Status.Should().Be(InvoiceStatus.Draft);
    }

    [Fact]
    public async Task RevertToDraft_SoftDeletesInventoryTransactions()
    {
        var invoice = CreateEntity(InvoiceStatus.Confirmed);
        SetupEntityLookup(invoice.Id, invoice);
        var service = CreateService();

        await service.RevertToDraftAsync(invoice.Id);

        await InventoryService.Received(1).SoftDeleteTransactionsForDocumentAsync(invoice.Id, "Invoice");
    }

    [Theory]
    [InlineData(InvoiceStatus.Draft)]
    [InlineData(InvoiceStatus.PartiallyPaid)]
    [InlineData(InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Cancelled)]
    public async Task RevertToDraft_FromInvalidStatus_Throws(InvoiceStatus status)
    {
        var invoice = CreateEntity(status);
        SetupEntityLookup(invoice.Id, invoice);
        var service = CreateService();

        await service.Invoking(s => s.RevertToDraftAsync(invoice.Id))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task RevertToDraft_InventoryFailure_RollsBackStatus()
    {
        var invoice = CreateEntity(InvoiceStatus.Confirmed);
        SetupEntityLookup(invoice.Id, invoice);
        InventoryService.SoftDeleteTransactionsForDocumentAsync(invoice.Id, "Invoice")
            .Throws(new InvalidOperationException("Inventory error"));
        var service = CreateService();

        await service.Invoking(s => s.RevertToDraftAsync(invoice.Id))
            .Should().ThrowAsync<InvalidOperationException>();

        invoice.Status.Should().Be(InvoiceStatus.Confirmed);
        await InvoiceRepo.Received(2).UpdateAsync(invoice);
    }

    // ── MarkAsPaidAsync (Confirmed/PartiallyPaid/Overdue → Paid) ─────────────

    [Fact]
    public async Task MarkAsPaid_FromConfirmed_SetsPaid()
    {
        var invoice = CreateEntity(InvoiceStatus.Confirmed);
        SetupEntityLookup(invoice.Id, invoice);
        TransactionRepo.HasTransactionsForDocumentAsync(invoice.Id, "Invoice", Arg.Any<CancellationToken>()).Returns(true);
        var service = CreateService();

        var result = await service.MarkAsPaidAsync(invoice.Id);

        result.Status.Should().Be(InvoiceStatus.Paid);
    }

    [Theory]
    [InlineData(InvoiceStatus.Draft)]
    [InlineData(InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Cancelled)]
    public async Task MarkAsPaid_FromInvalidStatus_Throws(InvoiceStatus status)
    {
        var invoice = CreateEntity(status);
        SetupEntityLookup(invoice.Id, invoice);
        var service = CreateService();

        await service.Invoking(s => s.MarkAsPaidAsync(invoice.Id))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task MarkAsPaid_CallsIdempotentInventoryCreation()
    {
        var invoice = CreateEntity(InvoiceStatus.Confirmed);
        SetupEntityLookup(invoice.Id, invoice);
        // Transactions already exist — should skip creation
        TransactionRepo.HasTransactionsForDocumentAsync(invoice.Id, "Invoice", Arg.Any<CancellationToken>()).Returns(true);
        var service = CreateService();

        await service.MarkAsPaidAsync(invoice.Id);

        await InventoryService.DidNotReceive().CreateBatchAsync(
            Arg.Any<Guid>(), Arg.Any<IEnumerable<CreateInventoryTransactionDto>>());
    }

    [Fact]
    public async Task MarkAsPaid_InventoryFailure_RollsBackStatus()
    {
        var invoice = CreateEntity(InvoiceStatus.Confirmed);
        SetupEntityLookup(invoice.Id, invoice);
        TransactionRepo.HasTransactionsForDocumentAsync(invoice.Id, "Invoice", Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Inventory error"));
        var service = CreateService();

        await service.Invoking(s => s.MarkAsPaidAsync(invoice.Id))
            .Should().ThrowAsync<InvalidOperationException>();

        invoice.Status.Should().Be(InvoiceStatus.Confirmed);
        await InvoiceRepo.Received(2).UpdateAsync(invoice);
    }

    // ── CancelAsync (Draft/Confirmed/Overdue → Cancelled) ────────────────────

    [Fact]
    public async Task Cancel_FromDraft_SetsCancelled()
    {
        var invoice = CreateEntity(InvoiceStatus.Draft);
        SetupEntityLookup(invoice.Id, invoice);
        TransactionRepo.HasTransactionsForDocumentAsync(invoice.Id, "Invoice_Reversal", Arg.Any<CancellationToken>()).Returns(true);
        var service = CreateService();

        var result = await service.CancelAsync(invoice.Id);

        result.Status.Should().Be(InvoiceStatus.Cancelled);
    }

    [Fact]
    public async Task Cancel_FromConfirmed_ReversesInventory()
    {
        var invoice = CreateEntity(InvoiceStatus.Confirmed);
        SetupEntityLookup(invoice.Id, invoice);
        TransactionRepo.HasTransactionsForDocumentAsync(invoice.Id, "Invoice_Reversal", Arg.Any<CancellationToken>()).Returns(false);
        LocalizationService.GetString(Arg.Any<string>()).Returns("Invoice cancelled");
        var service = CreateService();

        await service.CancelAsync(invoice.Id);

        await InventoryService.Received(1).ReverseTransactionsForDocumentAsync(
            invoice.Id, "Invoice", Arg.Any<string>());
    }

    [Fact]
    public async Task Cancel_FromPartiallyPaid_Throws()
    {
        var invoice = CreateEntity(InvoiceStatus.PartiallyPaid);
        SetupEntityLookup(invoice.Id, invoice);
        var service = CreateService();

        await service.Invoking(s => s.CancelAsync(invoice.Id))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Cancel_FromPaid_Throws()
    {
        var invoice = CreateEntity(InvoiceStatus.Paid);
        SetupEntityLookup(invoice.Id, invoice);
        var service = CreateService();

        await service.Invoking(s => s.CancelAsync(invoice.Id))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Cancel_FromCancelled_Throws()
    {
        var invoice = CreateEntity(InvoiceStatus.Cancelled);
        SetupEntityLookup(invoice.Id, invoice);
        var service = CreateService();

        await service.Invoking(s => s.CancelAsync(invoice.Id))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Cancel_InventoryFailure_RollsBackStatus()
    {
        var invoice = CreateEntity(InvoiceStatus.Confirmed);
        SetupEntityLookup(invoice.Id, invoice);
        TransactionRepo.HasTransactionsForDocumentAsync(invoice.Id, "Invoice_Reversal", Arg.Any<CancellationToken>()).Returns(false);
        LocalizationService.GetString(Arg.Any<string>()).Returns("Cancelled");
        InventoryService.ReverseTransactionsForDocumentAsync(invoice.Id, "Invoice", Arg.Any<string>())
            .Throws(new InvalidOperationException("Inventory error"));
        var service = CreateService();

        await service.Invoking(s => s.CancelAsync(invoice.Id))
            .Should().ThrowAsync<InvalidOperationException>();

        invoice.Status.Should().Be(InvoiceStatus.Confirmed);
        await InvoiceRepo.Received(2).UpdateAsync(invoice);
    }

    // ── MarkAsOverdueAsync (Confirmed/PartiallyPaid → Overdue) ───────────────

    [Fact]
    public async Task MarkAsOverdue_FromConfirmed_SetsOverdue()
    {
        var invoice = CreateEntity(InvoiceStatus.Confirmed);
        SetupEntityLookup(invoice.Id, invoice);
        var service = CreateService();

        var result = await service.MarkAsOverdueAsync(invoice.Id);

        result.Status.Should().Be(InvoiceStatus.Overdue);
    }

    [Theory]
    [InlineData(InvoiceStatus.Draft)]
    [InlineData(InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Cancelled)]
    [InlineData(InvoiceStatus.Overdue)]
    public async Task MarkAsOverdue_FromInvalidStatus_Throws(InvoiceStatus status)
    {
        var invoice = CreateEntity(status);
        SetupEntityLookup(invoice.Id, invoice);
        var service = CreateService();

        await service.Invoking(s => s.MarkAsOverdueAsync(invoice.Id))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task MarkAsOverdue_DoesNotTouchInventory()
    {
        var invoice = CreateEntity(InvoiceStatus.Confirmed);
        SetupEntityLookup(invoice.Id, invoice);
        var service = CreateService();

        await service.MarkAsOverdueAsync(invoice.Id);

        await InventoryService.DidNotReceive().CreateBatchAsync(
            Arg.Any<Guid>(), Arg.Any<IEnumerable<CreateInventoryTransactionDto>>());
        await InventoryService.DidNotReceive().ReverseTransactionsForDocumentAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>());
    }
}
