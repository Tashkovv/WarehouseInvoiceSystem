namespace WarehouseInvoiceSystem.Tests.Services.PurchaseNoteService;

using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using WarehouseInvoiceSystem.Application.DTOs.InventoryTransaction;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;

public class StatusTransitionTests : PurchaseNoteServiceTestBase
{
    // ── ReceiveAsync (Draft → Pending) ────────────────────────────────────────

    [Fact]
    public async Task Receive_FromDraft_SetsPending()
    {
        var note = CreateEntity(PurchaseNoteStatus.Draft);
        SetupEntityLookup(note.Id, note);
        LocalizationService.GetString(Arg.Any<string>()).Returns("Purchase from");
        var service = CreateService();

        var result = await service.ReceiveAsync(note.Id);

        result.Status.Should().Be(PurchaseNoteStatus.Pending);
        await PurchaseNoteRepo.Received(1).UpdateAsync(note);
    }

    [Fact]
    public async Task Receive_CreatesInventoryTransactions()
    {
        var note = CreateEntity(PurchaseNoteStatus.Draft);
        SetupEntityLookup(note.Id, note);
        LocalizationService.GetString(Arg.Any<string>()).Returns("Purchase from");
        var service = CreateService();

        await service.ReceiveAsync(note.Id);

        await InventoryService.Received(1).CreateBatchAsync(
            note.WarehouseId,
            Arg.Is<IEnumerable<CreateInventoryTransactionDto>>(items =>
                items.All(i => i.Type == InventoryTransactionType.Inbound)));
    }

    [Fact]
    public async Task Receive_SoftDeletesPreviousTransactions()
    {
        var note = CreateEntity(PurchaseNoteStatus.Draft);
        SetupEntityLookup(note.Id, note);
        LocalizationService.GetString(Arg.Any<string>()).Returns("Purchase from");
        var service = CreateService();

        await service.ReceiveAsync(note.Id);

        await InventoryService.Received(1).SoftDeleteTransactionsForDocumentAsync(note.Id, "PurchaseNote");
        await InventoryService.Received(1).SoftDeleteReversalForDocumentAsync(note.Id, "PurchaseNote");
    }

    [Fact]
    public async Task Receive_InventoryFailure_RollsBackStatus()
    {
        var note = CreateEntity(PurchaseNoteStatus.Draft);
        SetupEntityLookup(note.Id, note);
        LocalizationService.GetString(Arg.Any<string>()).Returns("Purchase from");
        InventoryService.SoftDeleteTransactionsForDocumentAsync(note.Id, "PurchaseNote")
            .Throws(new InvalidOperationException("Inventory error"));
        var service = CreateService();

        await service.Invoking(s => s.ReceiveAsync(note.Id))
            .Should().ThrowAsync<InvalidOperationException>();

        // Status should be rolled back to Draft
        note.Status.Should().Be(PurchaseNoteStatus.Draft);
        await PurchaseNoteRepo.Received(2).UpdateAsync(note);
    }

    [Theory]
    [InlineData(PurchaseNoteStatus.Pending)]
    [InlineData(PurchaseNoteStatus.Paid)]
    [InlineData(PurchaseNoteStatus.Cancelled)]
    public async Task Receive_FromNonDraft_Throws(PurchaseNoteStatus status)
    {
        var note = CreateEntity(status);
        SetupEntityLookup(note.Id, note);
        var service = CreateService();

        await service.Invoking(s => s.ReceiveAsync(note.Id))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    // ── MarkAsPaidAsync (Pending → Paid) ──────────────────────────────────────

    [Fact]
    public async Task MarkAsPaid_FromPending_Succeeds()
    {
        var note = CreateEntity(PurchaseNoteStatus.Pending);
        SetupEntityLookup(note.Id, note);
        var service = CreateService();

        var result = await service.MarkAsPaidAsync(note.Id);

        result.Status.Should().Be(PurchaseNoteStatus.Paid);
        note.PaidDate.Should().NotBeNull();
        await PurchaseNoteRepo.Received(1).UpdateAsync(note);
    }

    [Theory]
    [InlineData(PurchaseNoteStatus.Draft)]
    [InlineData(PurchaseNoteStatus.Paid)]
    [InlineData(PurchaseNoteStatus.Cancelled)]
    public async Task MarkAsPaid_FromNonPending_Throws(PurchaseNoteStatus status)
    {
        var note = CreateEntity(status);
        SetupEntityLookup(note.Id, note);
        var service = CreateService();

        await service.Invoking(s => s.MarkAsPaidAsync(note.Id))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task MarkAsPaid_DoesNotTouchInventory()
    {
        var note = CreateEntity(PurchaseNoteStatus.Pending);
        SetupEntityLookup(note.Id, note);
        var service = CreateService();

        await service.MarkAsPaidAsync(note.Id);

        await InventoryService.DidNotReceive().CreateBatchAsync(
            Arg.Any<Guid>(), Arg.Any<IEnumerable<CreateInventoryTransactionDto>>());
        await InventoryService.DidNotReceive().ReverseTransactionsForDocumentAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // ── RevertToDraftAsync (Pending → Draft) ──────────────────────────────────

    [Fact]
    public async Task RevertToDraft_FromPending_SetsDraft()
    {
        var note = CreateEntity(PurchaseNoteStatus.Pending);
        SetupEntityLookup(note.Id, note);
        var service = CreateService();

        var result = await service.RevertToDraftAsync(note.Id);

        result.Status.Should().Be(PurchaseNoteStatus.Draft);
    }

    [Fact]
    public async Task RevertToDraft_SoftDeletesInventoryTransactions()
    {
        var note = CreateEntity(PurchaseNoteStatus.Pending);
        SetupEntityLookup(note.Id, note);
        var service = CreateService();

        await service.RevertToDraftAsync(note.Id);

        await InventoryService.Received(1).SoftDeleteTransactionsForDocumentAsync(note.Id, "PurchaseNote");
    }

    [Theory]
    [InlineData(PurchaseNoteStatus.Draft)]
    [InlineData(PurchaseNoteStatus.Paid)]
    [InlineData(PurchaseNoteStatus.Cancelled)]
    public async Task RevertToDraft_FromNonPending_Throws(PurchaseNoteStatus status)
    {
        var note = CreateEntity(status);
        SetupEntityLookup(note.Id, note);
        var service = CreateService();

        await service.Invoking(s => s.RevertToDraftAsync(note.Id))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task RevertToDraft_InventoryFailure_RollsBackStatus()
    {
        var note = CreateEntity(PurchaseNoteStatus.Pending);
        SetupEntityLookup(note.Id, note);
        InventoryService.SoftDeleteTransactionsForDocumentAsync(note.Id, "PurchaseNote")
            .Throws(new InvalidOperationException("Inventory error"));
        var service = CreateService();

        await service.Invoking(s => s.RevertToDraftAsync(note.Id))
            .Should().ThrowAsync<InvalidOperationException>();

        // Status should be rolled back to Pending
        note.Status.Should().Be(PurchaseNoteStatus.Pending);
        // UpdateAsync called twice: once for the transition, once for the rollback
        await PurchaseNoteRepo.Received(2).UpdateAsync(note);
    }

    // ── CancelAsync (Draft/Pending → Cancelled) ──────────────────────────────

    [Fact]
    public async Task Cancel_FromDraft_SetsCancelled_NoInventoryReversal()
    {
        var note = CreateEntity(PurchaseNoteStatus.Draft);
        SetupEntityLookup(note.Id, note);
        var service = CreateService();

        var result = await service.CancelAsync(note.Id);

        result.Status.Should().Be(PurchaseNoteStatus.Cancelled);
        await InventoryService.DidNotReceive().ReverseTransactionsForDocumentAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Cancel_FromPending_ReversesInventory()
    {
        var note = CreateEntity(PurchaseNoteStatus.Pending);
        SetupEntityLookup(note.Id, note);
        LocalizationService.GetString(Arg.Any<string>()).Returns("Purchase note cancelled");
        var service = CreateService();

        var result = await service.CancelAsync(note.Id);

        result.Status.Should().Be(PurchaseNoteStatus.Cancelled);
        await InventoryService.Received(1).ReverseTransactionsForDocumentAsync(
            note.Id, "PurchaseNote", Arg.Any<string>());
    }

    [Theory]
    [InlineData(PurchaseNoteStatus.Paid)]
    [InlineData(PurchaseNoteStatus.Cancelled)]
    public async Task Cancel_FromTerminalStatus_Throws(PurchaseNoteStatus status)
    {
        var note = CreateEntity(status);
        SetupEntityLookup(note.Id, note);
        var service = CreateService();

        await service.Invoking(s => s.CancelAsync(note.Id))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Cancel_InventoryFailure_RollsBackStatus()
    {
        var note = CreateEntity(PurchaseNoteStatus.Pending);
        SetupEntityLookup(note.Id, note);
        LocalizationService.GetString(Arg.Any<string>()).Returns("Cancelled");
        InventoryService.ReverseTransactionsForDocumentAsync(note.Id, "PurchaseNote", Arg.Any<string>())
            .Throws(new InvalidOperationException("Inventory error"));
        var service = CreateService();

        await service.Invoking(s => s.CancelAsync(note.Id))
            .Should().ThrowAsync<InvalidOperationException>();

        // Status should be rolled back to Pending
        note.Status.Should().Be(PurchaseNoteStatus.Pending);
        await PurchaseNoteRepo.Received(2).UpdateAsync(note);
    }
}
