namespace WarehouseInvoiceSystem.Tests.Services.NotificationService;

using Microsoft.Extensions.Options;
using NSubstitute;
using WarehouseInvoiceSystem.Application.Interfaces;
using WarehouseInvoiceSystem.Application.Settings;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;
using WarehouseInvoiceSystem.Domain.Interfaces;

public abstract class NotificationServiceTestBase
{
    protected INotificationRepository NotificationRepo { get; } = Substitute.For<INotificationRepository>();
    protected IInvoiceRepository InvoiceRepo { get; } = Substitute.For<IInvoiceRepository>();
    protected IEmailService EmailService { get; } = Substitute.For<IEmailService>();

    protected Application.Services.NotificationService CreateService(NotificationSettings? settings = null)
    {
        settings ??= new NotificationSettings
        {
            SendEmails = false,
            ReceivableDays = [14, 7, 1],
            PayableDays = [10, 5, 1]
        };
        var options = Options.Create(settings);
        return new(NotificationRepo, InvoiceRepo, EmailService, options);
    }

    protected static Notification CreateNotification(
        NotificationType type = NotificationType.InvoiceDueReminder,
        string? data = null,
        bool isRead = false,
        bool isEmailSent = false,
        List<NotificationInvoice>? invoices = null)
    {
        var notification = new Notification
        {
            Type = type,
            Data = data ?? """{"daysBeforeDue":7,"invoiceType":"Receivable"}""",
            IsRead = isRead,
            IsEmailSent = isEmailSent,
            CreatedAt = DateTime.UtcNow,
            NotificationInvoices = invoices ?? []
        };
        SetEntityId(notification, Guid.NewGuid());
        return notification;
    }

    protected static Invoice CreateInvoice(
        string companyName = "Acme Corp",
        string? companyEmail = "acme@test.com",
        DateTime? dueDate = null,
        InvoiceType type = InvoiceType.Receivable)
    {
        var companyId = Guid.NewGuid();
        var company = new Company
        {
            Name = companyName,
            Email = companyEmail,
            Type = CompanyType.Client,
            IsActive = true
        };
        SetEntityId(company, companyId);

        var invoice = new Invoice
        {
            InvoiceNumber = "INV-001",
            CompanyId = companyId,
            Company = company,
            Type = type,
            Status = InvoiceStatus.Confirmed,
            TotalAmount = 5000m,
            AmountPaid = 0m,
            DueDate = dueDate ?? DateTime.UtcNow.AddDays(7),
            IssueDate = DateTime.UtcNow.AddDays(-30)
        };
        SetEntityId(invoice, Guid.NewGuid());
        return invoice;
    }

    protected static NotificationInvoice CreateNotificationInvoice(
        Guid notificationId,
        Invoice invoice) => new()
    {
        Id = Guid.NewGuid(),
        NotificationId = notificationId,
        InvoiceId = invoice.Id,
        Invoice = invoice
    };

    protected static void SetEntityId(object entity, Guid id)
    {
        var prop = entity.GetType().GetProperty("Id")!;
        prop.SetValue(entity, id);
    }
}
