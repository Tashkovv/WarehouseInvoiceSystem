namespace WarehouseInvoiceSystem.Tests.Services.PurchaseNoteService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;

public class DeleteTests : PurchaseNoteServiceTestBase
{
    [Fact]
    public async Task Delete_Cancelled_ReturnsTrue()
    {
        var note = CreateEntity(PurchaseNoteStatus.Cancelled);
        SetupEntityLookup(note.Id, note);
        TransactionRepo.SoftDeleteByDocumentAsync(note.Id, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        PurchaseNoteRepo.DeleteAsync(note.Id).Returns(true);
        var service = CreateService();

        var result = await service.DeletePurchaseNoteAsync(note.Id);

        result.Should().BeTrue();
        await PurchaseNoteRepo.Received(1).DeleteAsync(note.Id);
        await TransactionRepo.Received(2).SoftDeleteByDocumentAsync(note.Id, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(PurchaseNoteStatus.Draft)]
    [InlineData(PurchaseNoteStatus.Pending)]
    [InlineData(PurchaseNoteStatus.Paid)]
    public async Task Delete_NonCancelled_ThrowsInvalidOperation(PurchaseNoteStatus status)
    {
        var note = CreateEntity(status);
        SetupEntityLookup(note.Id, note);
        var service = CreateService();

        await service.Invoking(s => s.DeletePurchaseNoteAsync(note.Id))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Delete_NotFound_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        PurchaseNoteRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((PurchaseNote?)null);
        var service = CreateService();

        var result = await service.DeletePurchaseNoteAsync(id);

        result.Should().BeFalse();
    }
}
