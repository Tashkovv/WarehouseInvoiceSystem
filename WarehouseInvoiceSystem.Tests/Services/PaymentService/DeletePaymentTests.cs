namespace WarehouseInvoiceSystem.Tests.Services.PaymentService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;

public class DeletePaymentTests : PaymentServiceTestBase
{
    [Fact]
    public async Task PaymentNotFound_ReturnsFalse()
    {
        PaymentRepo.GetByIdAsync(Arg.Any<Guid>()).Returns((Payment?)null);
        var service = CreateService();

        var result = await service.DeletePaymentAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task PartialPaymentRemoved_StillHasBalance_KeepsPartiallyPaid()
    {
        var invoice = BuildInvoice(InvoiceStatus.PartiallyPaid, totalAmount: 100m, amountPaid: 80m);
        var payment = BuildPayment(invoice.Id, 30m);
        payment.Invoice = invoice;
        PaymentRepo.GetByIdAsync(payment.Id).Returns(payment);
        PaymentRepo.DeleteAsync(payment.Id).Returns(true);
        var service = CreateService();

        await service.DeletePaymentAsync(payment.Id);

        invoice.Status.Should().Be(InvoiceStatus.PartiallyPaid);
        invoice.AmountPaid.Should().Be(50m);
    }

    [Fact]
    public async Task LastPayment_NoTransactions_SetsDraft()
    {
        var invoice = BuildInvoice(InvoiceStatus.PartiallyPaid, totalAmount: 1000m, amountPaid: 300m);
        var payment = BuildPayment(invoice.Id, 300m);
        payment.Invoice = invoice;
        PaymentRepo.GetByIdAsync(payment.Id).Returns(payment);
        PaymentRepo.DeleteAsync(payment.Id).Returns(true);
        TransactionRepo.HasTransactionsForDocumentAsync(invoice.Id, "Invoice", Arg.Any<CancellationToken>()).Returns(false);
        var service = CreateService();

        await service.DeletePaymentAsync(payment.Id);

        invoice.Status.Should().Be(InvoiceStatus.Draft);
        invoice.AmountPaid.Should().Be(0m);
    }

    [Fact]
    public async Task LastPayment_HasTransactions_InvoiceHadConfirmed_SetsConfirmed()
    {
        var invoice = BuildInvoice(InvoiceStatus.PartiallyPaid, totalAmount: 1000m, amountPaid: 300m);
        var payment = BuildPayment(invoice.Id, 300m);
        payment.Invoice = invoice;
        PaymentRepo.GetByIdAsync(payment.Id).Returns(payment);
        PaymentRepo.DeleteAsync(payment.Id).Returns(true);
        TransactionRepo.HasTransactionsForDocumentAsync(invoice.Id, "Invoice", Arg.Any<CancellationToken>()).Returns(true);
        // Returning a remaining payment signals invoice was formerly Confirmed (had other payments)
        PaymentRepo.GetByInvoiceIdAsync(invoice.Id, Arg.Any<CancellationToken>())
                   .Returns(new[] { BuildPayment(invoice.Id, 100m) });
        var service = CreateService();

        await service.DeletePaymentAsync(payment.Id);

        invoice.Status.Should().Be(InvoiceStatus.Confirmed);
    }

    [Fact]
    public async Task LastPayment_HasTransactions_NeverConfirmed_ReversesThenSetsDraft()
    {
        var invoice = BuildInvoice(InvoiceStatus.Paid, totalAmount: 1000m, amountPaid: 1000m);
        var payment = BuildPayment(invoice.Id, 1000m);
        payment.Invoice = invoice;
        PaymentRepo.GetByIdAsync(payment.Id).Returns(payment);
        PaymentRepo.DeleteAsync(payment.Id).Returns(true);
        TransactionRepo.HasTransactionsForDocumentAsync(invoice.Id, "Invoice", Arg.Any<CancellationToken>()).Returns(true);
        LocalizationService.GetString(Arg.Any<string>()).Returns("Payment removed");
        // No remaining payments → invoice was never formally Confirmed (draft-paid path)
        PaymentRepo.GetByInvoiceIdAsync(invoice.Id, Arg.Any<CancellationToken>())
                   .Returns(Array.Empty<Payment>());
        var service = CreateService();

        await service.DeletePaymentAsync(payment.Id);

        await InvoiceSvc.Received(1).CreateReverseTransactionsIfNeeded(invoice, Arg.Any<string>());
        invoice.Status.Should().Be(InvoiceStatus.Draft);
    }

    [Fact]
    public async Task SuccessfulDelete_ReturnsTrue()
    {
        var invoice = BuildInvoice(InvoiceStatus.PartiallyPaid, totalAmount: 1000m, amountPaid: 300m);
        var payment = BuildPayment(invoice.Id, 300m);
        payment.Invoice = invoice;
        PaymentRepo.GetByIdAsync(payment.Id).Returns(payment);
        PaymentRepo.DeleteAsync(payment.Id).Returns(true);
        TransactionRepo.HasTransactionsForDocumentAsync(invoice.Id, "Invoice", Arg.Any<CancellationToken>()).Returns(false);
        var service = CreateService();

        var result = await service.DeletePaymentAsync(payment.Id);

        result.Should().BeTrue();
        await PaymentRepo.Received(1).DeleteAsync(payment.Id);
    }
}
