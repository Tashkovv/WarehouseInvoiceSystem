namespace WarehouseInvoiceSystem.Tests.Services.PaymentService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;

public class UpdatePaymentTests : PaymentServiceTestBase
{
    [Fact]
    public async Task PaymentNotFound_ThrowsKeyNotFound()
    {
        PaymentRepo.GetByIdAsync(Arg.Any<Guid>()).Returns((Payment?)null);
        var service = CreateService();

        await service.Invoking(s => s.UpdatePaymentAsync(Guid.NewGuid(), BuildUpdateDto(100m)))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task InvoiceNotFound_ThrowsKeyNotFound()
    {
        var payment = BuildPayment(Guid.NewGuid(), 100m);
        PaymentRepo.GetByIdAsync(payment.Id).Returns(payment);
        InvoiceRepo.GetByIdAsync(payment.InvoiceId).Returns((Invoice?)null);
        var service = CreateService();

        await service.Invoking(s => s.UpdatePaymentAsync(payment.Id, BuildUpdateDto(100m)))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateExceedsTotal_ThrowsInvalidOperation()
    {
        var invoice = BuildInvoice(InvoiceStatus.PartiallyPaid, totalAmount: 1000m, amountPaid: 300m);
        var payment = BuildPayment(invoice.Id, 300m);
        PaymentRepo.GetByIdAsync(payment.Id).Returns(payment);
        InvoiceRepo.GetByIdAsync(invoice.Id).Returns(invoice);
        var service = CreateService();

        // newTotal = (300 - 300) + 1100 = 1100 > 1000
        await service.Invoking(s => s.UpdatePaymentAsync(payment.Id, BuildUpdateDto(1100m)))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task NewAmountHigher_AmountPaidIncreased()
    {
        var invoice = BuildInvoice(InvoiceStatus.PartiallyPaid, totalAmount: 1000m, amountPaid: 300m);
        var payment = BuildPayment(invoice.Id, 300m);
        PaymentRepo.GetByIdAsync(payment.Id).Returns(payment);
        InvoiceRepo.GetByIdAsync(invoice.Id).Returns(invoice);
        var service = CreateService();

        await service.UpdatePaymentAsync(payment.Id, BuildUpdateDto(500m));

        invoice.AmountPaid.Should().Be(500m);
    }

    [Fact]
    public async Task NewAmountLower_AmountPaidDecreased()
    {
        var invoice = BuildInvoice(InvoiceStatus.PartiallyPaid, totalAmount: 1000m, amountPaid: 300m);
        var payment = BuildPayment(invoice.Id, 300m);
        PaymentRepo.GetByIdAsync(payment.Id).Returns(payment);
        InvoiceRepo.GetByIdAsync(invoice.Id).Returns(invoice);
        var service = CreateService();

        await service.UpdatePaymentAsync(payment.Id, BuildUpdateDto(100m));

        invoice.AmountPaid.Should().Be(100m);
    }

    [Fact]
    public async Task PaidInFull_StatusSetToPaid()
    {
        var invoice = BuildInvoice(InvoiceStatus.PartiallyPaid, totalAmount: 1000m, amountPaid: 500m);
        var payment = BuildPayment(invoice.Id, 500m);
        PaymentRepo.GetByIdAsync(payment.Id).Returns(payment);
        InvoiceRepo.GetByIdAsync(invoice.Id).Returns(invoice);
        var service = CreateService();

        // newTotal = (500 - 500) + 1000 = 1000 == totalAmount
        await service.UpdatePaymentAsync(payment.Id, BuildUpdateDto(1000m));

        invoice.Status.Should().Be(InvoiceStatus.Paid);
    }

    [Fact]
    public async Task PartialAfterUpdate_StatusSetToPartiallyPaid()
    {
        var invoice = BuildInvoice(InvoiceStatus.Paid, totalAmount: 1000m, amountPaid: 1000m);
        var payment = BuildPayment(invoice.Id, 1000m);
        PaymentRepo.GetByIdAsync(payment.Id).Returns(payment);
        InvoiceRepo.GetByIdAsync(invoice.Id).Returns(invoice);
        var service = CreateService();

        // newTotal = (1000 - 1000) + 600 = 600 < 1000
        await service.UpdatePaymentAsync(payment.Id, BuildUpdateDto(600m));

        invoice.Status.Should().Be(InvoiceStatus.PartiallyPaid);
    }

    [Fact]
    public async Task ZeroPaidAfterUpdate_StatusSetToConfirmed()
    {
        var invoice = BuildInvoice(InvoiceStatus.PartiallyPaid, totalAmount: 1000m, amountPaid: 300m);
        var payment = BuildPayment(invoice.Id, 300m);
        PaymentRepo.GetByIdAsync(payment.Id).Returns(payment);
        InvoiceRepo.GetByIdAsync(invoice.Id).Returns(invoice);
        var service = CreateService();

        // newTotal = (300 - 300) + 0 = 0
        await service.UpdatePaymentAsync(payment.Id, BuildUpdateDto(0m));

        invoice.Status.Should().Be(InvoiceStatus.Confirmed);
    }
}
