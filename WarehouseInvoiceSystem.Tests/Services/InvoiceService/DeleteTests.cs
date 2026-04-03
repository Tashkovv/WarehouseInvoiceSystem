namespace WarehouseInvoiceSystem.Tests.Services.InvoiceService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;

public class DeleteTests : InvoiceServiceTestBase
{
    [Fact]
    public async Task Delete_Cancelled_ReturnsTrue()
    {
        var invoice = CreateEntity(InvoiceStatus.Cancelled);
        SetupEntityLookup(invoice.Id, invoice);
        InvoiceRepo.DeleteAsync(invoice.Id).Returns(true);
        var service = CreateService();

        var result = await service.DeleteInvoiceAsync(invoice.Id);

        result.Should().BeTrue();
        await InvoiceRepo.Received(1).DeleteAsync(invoice.Id);
    }

    [Theory]
    [InlineData(InvoiceStatus.Draft)]
    [InlineData(InvoiceStatus.Confirmed)]
    [InlineData(InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.PartiallyPaid)]
    [InlineData(InvoiceStatus.Overdue)]
    public async Task Delete_NonCancelled_ThrowsInvalidOperation(InvoiceStatus status)
    {
        var invoice = CreateEntity(status);
        SetupEntityLookup(invoice.Id, invoice);
        var service = CreateService();

        await service.Invoking(s => s.DeleteInvoiceAsync(invoice.Id))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Delete_NotFound_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        InvoiceRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Invoice?)null);
        var service = CreateService();

        var result = await service.DeleteInvoiceAsync(id);

        result.Should().BeFalse();
    }
}
