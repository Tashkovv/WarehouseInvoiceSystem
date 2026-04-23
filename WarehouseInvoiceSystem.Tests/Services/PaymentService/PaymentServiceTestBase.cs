namespace WarehouseInvoiceSystem.Tests.Services.PaymentService;

using NSubstitute;
using WarehouseInvoiceSystem.Application.DTOs.Payment;
using WarehouseInvoiceSystem.Application.Interfaces;
using WarehouseInvoiceSystem.Application.Services;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;
using WarehouseInvoiceSystem.Domain.Interfaces;

public abstract class PaymentServiceTestBase
{
    protected readonly IPaymentRepository PaymentRepo = Substitute.For<IPaymentRepository>();
    protected readonly IInvoiceRepository InvoiceRepo = Substitute.For<IInvoiceRepository>();
    protected readonly IInvoiceService InvoiceSvc = Substitute.For<IInvoiceService>();
    protected readonly IInventoryTransactionRepository TransactionRepo = Substitute.For<IInventoryTransactionRepository>();
    protected readonly ILocalizationService LocalizationService = Substitute.For<ILocalizationService>();

    protected Application.Services.PaymentService CreateService() =>
        new(PaymentRepo, InvoiceRepo, InvoiceSvc, TransactionRepo, LocalizationService);

    protected static Invoice BuildInvoice(
        InvoiceStatus status = InvoiceStatus.Confirmed,
        decimal totalAmount = 1000m,
        decimal amountPaid = 0m)
    {
        var invoice = new Invoice
        {
            InvoiceNumber = "INV-000001",
            Status = status,
            TotalAmount = totalAmount,
            AmountPaid = amountPaid,
            Company = new Company { Name = "Test Co", Email = "test@test.com" },
        };
        SetEntityId(invoice, Guid.NewGuid());
        return invoice;
    }

    protected static Payment BuildPayment(Guid invoiceId, decimal amount = 100m)
    {
        var payment = new Payment
        {
            InvoiceId = invoiceId,
            Amount = amount,
            PaymentDate = DateTime.Today,
            PaymentMethod = PaymentMethod.Cash
        };
        SetEntityId(payment, Guid.NewGuid());
        return payment;
    }

    protected static CreatePaymentDto BuildCreateDto(Guid invoiceId, decimal amount = 100m) => new()
    {
        InvoiceId = invoiceId,
        Amount = amount,
        PaymentDate = DateTime.Today,
        PaymentMethod = PaymentMethod.Cash
    };

    protected static UpdatePaymentDto BuildUpdateDto(decimal amount = 100m) => new()
    {
        Amount = amount,
        PaymentDate = DateTime.Today,
        PaymentMethod = PaymentMethod.BankTransfer
    };

    protected static void SetEntityId(Domain.Common.Entity entity, Guid id)
    {
        typeof(Domain.Common.Entity)
            .GetProperty(nameof(Domain.Common.Entity.Id))!
            .SetValue(entity, id);
    }
}
