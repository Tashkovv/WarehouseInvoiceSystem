namespace WarehouseInvoiceSystem.Tests.Services.PaymentService;

using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;

public class CreatePaymentTests : PaymentServiceTestBase
{
    [Fact]
    public async Task InvoiceNotFound_ThrowsKeyNotFound()
    {
        InvoiceRepo.GetByIdAsync(Arg.Any<Guid>()).Returns((Invoice?)null);
        var service = CreateService();

        await service.Invoking(s => s.CreatePaymentAsync(BuildCreateDto(Guid.NewGuid())))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task AmountExceedsBalance_ThrowsInvalidOperation()
    {
        var invoice = BuildInvoice(totalAmount: 100m, amountPaid: 50m);
        InvoiceRepo.GetByIdAsync(invoice.Id).Returns(invoice);
        var service = CreateService();

        await service.Invoking(s => s.CreatePaymentAsync(BuildCreateDto(invoice.Id, amount: 60m)))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ExactBalance_DoesNotThrow()
    {
        var invoice = BuildInvoice(totalAmount: 100m, amountPaid: 50m);
        InvoiceRepo.GetByIdAsync(invoice.Id).Returns(invoice);
        var service = CreateService();

        await service.Invoking(s => s.CreatePaymentAsync(BuildCreateDto(invoice.Id, amount: 50m)))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task PartialPayment_SetsPartiallyPaid()
    {
        var invoice = BuildInvoice(InvoiceStatus.Confirmed, totalAmount: 1000m, amountPaid: 0m);
        InvoiceRepo.GetByIdAsync(invoice.Id).Returns(invoice);
        var service = CreateService();

        await service.CreatePaymentAsync(BuildCreateDto(invoice.Id, amount: 300m));

        invoice.Status.Should().Be(InvoiceStatus.PartiallyPaid);
    }

    [Fact]
    public async Task FullPayment_SetsPaid()
    {
        var invoice = BuildInvoice(InvoiceStatus.Confirmed, totalAmount: 1000m, amountPaid: 0m);
        InvoiceRepo.GetByIdAsync(invoice.Id).Returns(invoice);
        var service = CreateService();

        await service.CreatePaymentAsync(BuildCreateDto(invoice.Id, amount: 1000m));

        invoice.Status.Should().Be(InvoiceStatus.Paid);
    }

    [Fact]
    public async Task DraftInvoice_WithPayment_TriggersInventoryCreation()
    {
        var invoice = BuildInvoice(InvoiceStatus.Draft, totalAmount: 1000m);
        InvoiceRepo.GetByIdAsync(invoice.Id).Returns(invoice);
        var service = CreateService();

        await service.CreatePaymentAsync(BuildCreateDto(invoice.Id, amount: 1000m));

        await InvoiceSvc.Received(1).CreateInventoryTransactionsIfNeededAsync(invoice);
    }

    [Fact]
    public async Task NonDraftInvoice_WithPayment_NoInventoryCreation()
    {
        var invoice = BuildInvoice(InvoiceStatus.Confirmed, totalAmount: 1000m);
        InvoiceRepo.GetByIdAsync(invoice.Id).Returns(invoice);
        var service = CreateService();

        await service.CreatePaymentAsync(BuildCreateDto(invoice.Id, amount: 1000m));

        await InvoiceSvc.DidNotReceive().CreateInventoryTransactionsIfNeededAsync(Arg.Any<Invoice>());
    }

    [Fact]
    public async Task InvoiceUpdateFails_DeletesPaymentAndRethrows()
    {
        var invoice = BuildInvoice(InvoiceStatus.Confirmed, totalAmount: 1000m);
        InvoiceRepo.GetByIdAsync(invoice.Id).Returns(invoice);
        InvoiceRepo.UpdateAsync(Arg.Any<Invoice>()).ThrowsAsync(new Exception("DB error"));
        var service = CreateService();

        await service.Invoking(s => s.CreatePaymentAsync(BuildCreateDto(invoice.Id, amount: 500m)))
            .Should().ThrowAsync<Exception>();

        await PaymentRepo.Received(1).DeleteAsync(Arg.Any<Guid>());
    }
}
