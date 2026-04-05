namespace WarehouseInvoiceSystem.Tests.Services.NotificationService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Application.Settings;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;

public class OverdueNotificationTests : NotificationServiceTestBase
{
    [Fact]
    public async Task CreateOverdue_CreatesNotificationWithInvoices()
    {
        var invoiceIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        NotificationRepo.ExistsTodayAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        NotificationRepo.CreateWithInvoicesAsync(Arg.Any<Notification>(), Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());
        var service = CreateService();

        await service.CreateOverdueNotificationAsync(invoiceIds);

        await NotificationRepo.Received(1).CreateWithInvoicesAsync(
            Arg.Is<Notification>(n => n.Type == NotificationType.InvoiceOverdue && n.Data!.Contains("\"count\":2")),
            Arg.Is<List<Guid>>(ids => ids.Count == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateOverdue_AlreadyExistsToday_ReturnsEarly()
    {
        var invoiceIds = new List<Guid> { Guid.NewGuid() };
        NotificationRepo.ExistsTodayAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        var service = CreateService();

        await service.CreateOverdueNotificationAsync(invoiceIds);

        await NotificationRepo.DidNotReceive().CreateWithInvoicesAsync(
            Arg.Any<Notification>(), Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateOverdue_WithSendEmails_SendsGroupedEmails()
    {
        var settings = new NotificationSettings { SendEmails = true, ReceivableDays = [], PayableDays = [] };
        var invoice1 = CreateInvoice("Company A", "a@test.com");
        var invoice2 = CreateInvoice("Company B", "b@test.com");
        var invoiceIds = new List<Guid> { invoice1.Id, invoice2.Id };

        NotificationRepo.ExistsTodayAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        NotificationRepo.CreateWithInvoicesAsync(Arg.Any<Notification>(), Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());
        InvoiceRepo.GetByIdAsync(invoice1.Id, Arg.Any<CancellationToken>()).Returns(invoice1);
        InvoiceRepo.GetByIdAsync(invoice2.Id, Arg.Any<CancellationToken>()).Returns(invoice2);
        EmailService.SendOverdueNotificationEmailAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<List<Invoice>>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var service = CreateService(settings);

        await service.CreateOverdueNotificationAsync(invoiceIds);

        // Two companies → two email calls (grouped by CompanyId)
        await EmailService.Received(2).SendOverdueNotificationEmailAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<List<Invoice>>(), Arg.Any<CancellationToken>());
        await NotificationRepo.Received(1).MarkEmailSentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateOverdue_EmailSkipsCompanyWithoutEmail()
    {
        var settings = new NotificationSettings { SendEmails = true, ReceivableDays = [], PayableDays = [] };
        var invoiceNoEmail = CreateInvoice("No Email Co", null);
        var invoiceWithEmail = CreateInvoice("Has Email Co", "has@test.com");
        var invoiceIds = new List<Guid> { invoiceNoEmail.Id, invoiceWithEmail.Id };

        NotificationRepo.ExistsTodayAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        NotificationRepo.CreateWithInvoicesAsync(Arg.Any<Notification>(), Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());
        InvoiceRepo.GetByIdAsync(invoiceNoEmail.Id, Arg.Any<CancellationToken>()).Returns(invoiceNoEmail);
        InvoiceRepo.GetByIdAsync(invoiceWithEmail.Id, Arg.Any<CancellationToken>()).Returns(invoiceWithEmail);
        EmailService.SendOverdueNotificationEmailAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<List<Invoice>>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var service = CreateService(settings);

        await service.CreateOverdueNotificationAsync(invoiceIds);

        // Only one email — the company without email is skipped
        await EmailService.Received(1).SendOverdueNotificationEmailAsync(
            "Has Email Co", "has@test.com", Arg.Any<List<Invoice>>(), Arg.Any<CancellationToken>());
    }
}
