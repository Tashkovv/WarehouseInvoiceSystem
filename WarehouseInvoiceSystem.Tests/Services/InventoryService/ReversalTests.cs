namespace WarehouseInvoiceSystem.Tests.Services.InventoryService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;

public class ReversalTests : InventoryServiceTestBase
{
    [Fact]
    public async Task Reverse_CreatesReversedTransactionsWithNegatedDelta()
    {
        var docId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var original = CreateTransaction(InventoryTransactionType.Inbound, productId, warehouseId, 10m,
            docId, "PurchaseNote");

        TransactionRepo.HasTransactionsForDocumentAsync(docId, "PurchaseNote_Reversal", Arg.Any<CancellationToken>())
            .Returns(false);
        TransactionRepo.GetBySourceDocumentAsync(docId, "PurchaseNote", Arg.Any<CancellationToken>())
            .Returns(new[] { original });
        TransactionRepo.CreateAsync(Arg.Any<InventoryTransaction>()).Returns(ci =>
        {
            var t = ci.Arg<InventoryTransaction>();
            SetEntityId(t, Guid.NewGuid());
            return t;
        });
        var service = CreateService();

        await service.ReverseTransactionsForDocumentAsync(docId, "PurchaseNote", "Cancelled");

        // Inbound qty=10 → ComputeStockChange returns +10 → reversal qty = -10
        await TransactionRepo.Received(1).CreateAsync(Arg.Is<InventoryTransaction>(t =>
            t.Type == InventoryTransactionType.Reversed &&
            t.Quantity == -10m &&
            t.SourceDocumentType == "PurchaseNote_Reversal" &&
            t.SourceDocumentId == docId));
    }

    [Fact]
    public async Task Reverse_AlreadyReversed_ReturnsEarly()
    {
        var docId = Guid.NewGuid();
        TransactionRepo.HasTransactionsForDocumentAsync(docId, "Invoice_Reversal", Arg.Any<CancellationToken>())
            .Returns(true);
        var service = CreateService();

        await service.ReverseTransactionsForDocumentAsync(docId, "Invoice", "Cancelled");

        await TransactionRepo.DidNotReceive().GetBySourceDocumentAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await TransactionRepo.DidNotReceive().CreateAsync(Arg.Any<InventoryTransaction>());
    }

    [Fact]
    public async Task Reverse_NoExistingTransactions_DoesNothing()
    {
        var docId = Guid.NewGuid();
        TransactionRepo.HasTransactionsForDocumentAsync(docId, "Invoice_Reversal", Arg.Any<CancellationToken>())
            .Returns(false);
        TransactionRepo.GetBySourceDocumentAsync(docId, "Invoice", Arg.Any<CancellationToken>())
            .Returns(Array.Empty<InventoryTransaction>());
        var service = CreateService();

        await service.ReverseTransactionsForDocumentAsync(docId, "Invoice", "Cancelled");

        await TransactionRepo.DidNotReceive().CreateAsync(Arg.Any<InventoryTransaction>());
    }

    [Fact]
    public async Task Reverse_OutboundTransaction_NegatesDelta()
    {
        var docId = Guid.NewGuid();
        var original = CreateTransaction(InventoryTransactionType.Outbound, sourceDocumentId: docId,
            sourceDocumentType: "Invoice", quantity: 10m);

        TransactionRepo.HasTransactionsForDocumentAsync(docId, "Invoice_Reversal", Arg.Any<CancellationToken>())
            .Returns(false);
        TransactionRepo.GetBySourceDocumentAsync(docId, "Invoice", Arg.Any<CancellationToken>())
            .Returns(new[] { original });
        TransactionRepo.CreateAsync(Arg.Any<InventoryTransaction>()).Returns(ci =>
        {
            var t = ci.Arg<InventoryTransaction>();
            SetEntityId(t, Guid.NewGuid());
            return t;
        });
        var service = CreateService();

        await service.ReverseTransactionsForDocumentAsync(docId, "Invoice", "Reverted");

        // Outbound qty=10 → ComputeStockChange returns -10 → reversal qty = +10
        await TransactionRepo.Received(1).CreateAsync(Arg.Is<InventoryTransaction>(t =>
            t.Quantity == 10m && t.Type == InventoryTransactionType.Reversed));
    }

    [Fact]
    public async Task SoftDeleteReversal_UndoesReversalStockEffect()
    {
        var docId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        // A Reversed transaction with quantity -10 (signed: originally undid a +10 inbound)
        var reversal = CreateTransaction(InventoryTransactionType.Reversed, productId, warehouseId, -10m);

        TransactionRepo.SoftDeleteReversalAsync(docId, "PurchaseNote", Arg.Any<CancellationToken>())
            .Returns(new[] { reversal });
        var service = CreateService();

        await service.SoftDeleteReversalForDocumentAsync(docId, "PurchaseNote");

        // Reversed type: restoreQuantity = -(-10) = +10
        // UpdateStockFromTransaction with Reversed type and qty +10 → delta = +10 (restores stock)
        await StockLevelRepo.Received(1).ApplyDeltaAsync(productId, warehouseId, 10m, false);
    }

    [Fact]
    public async Task SoftDeleteTransactions_UndoesOriginalStockEffect()
    {
        var docId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var inboundTx = CreateTransaction(InventoryTransactionType.Inbound, productId, warehouseId, 15m,
            docId, "PurchaseNote");

        TransactionRepo.SoftDeleteByDocumentAsync(docId, "PurchaseNote", Arg.Any<CancellationToken>())
            .Returns(new[] { inboundTx });
        var service = CreateService();

        await service.SoftDeleteTransactionsForDocumentAsync(docId, "PurchaseNote");

        // Inbound qty=15 → ComputeStockChange = +15 → undoQuantity = -15
        // Reversed type with qty=-15 → ComputeStockChange returns -15 (delta is -15)
        await StockLevelRepo.Received(1).ApplyDeltaAsync(productId, warehouseId, -15m, false);
    }
}
