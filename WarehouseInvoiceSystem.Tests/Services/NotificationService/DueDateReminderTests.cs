namespace WarehouseInvoiceSystem.Tests.Services.NotificationService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Application.Settings;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;

public class DueDateReminderTests : NotificationServiceTestBase
{
    [Fact]
    public async Task Generate_NoThresholds_DoesNothing()
    {
        var settings = new NotificationSettings
        {
            SendEmails = false,
            ReceivableDays = null,
            PayableDays = null
        };
        var service = CreateService(settings);

        await service.GenerateAndSendDueDateRemindersAsync();

        await InvoiceRepo.DidNotReceive().GetInvoicesDueInDaysAsync(
            Arg.Any<int>(), Arg.Any<InvoiceType>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Generate_ReceivableThreshold_CreatesNotification()
    {
        var settings = new NotificationSettings { SendEmails = false, ReceivableDays = [7], PayableDays = null };
        var invoices = new List<Invoice> { CreateInvoice() };
        InvoiceRepo.GetInvoicesDueInDaysAsync(7, InvoiceType.Receivable, Arg.Any<CancellationToken>())
            .Returns(invoices);
        NotificationRepo.ExistsTodayAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        NotificationRepo.CreateWithInvoicesAsync(Arg.Any<Notification>(), Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());
        var service = CreateService(settings);

        await service.GenerateAndSendDueDateRemindersAsync();

        await NotificationRepo.Received(1).CreateWithInvoicesAsync(
            Arg.Is<Notification>(n => n.Type == NotificationType.InvoiceDueReminder),
            Arg.Is<List<Guid>>(ids => ids.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Generate_NoInvoicesDue_SkipsThreshold()
    {
        var settings = new NotificationSettings { SendEmails = false, ReceivableDays = [7], PayableDays = null };
        InvoiceRepo.GetInvoicesDueInDaysAsync(7, InvoiceType.Receivable, Arg.Any<CancellationToken>())
            .Returns(new List<Invoice>());
        var service = CreateService(settings);

        await service.GenerateAndSendDueDateRemindersAsync();

        await NotificationRepo.DidNotReceive().CreateWithInvoicesAsync(
            Arg.Any<Notification>(), Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Generate_AlreadyExistsToday_SkipsThreshold()
    {
        var settings = new NotificationSettings { SendEmails = false, ReceivableDays = [7], PayableDays = null };
        var invoices = new List<Invoice> { CreateInvoice() };
        InvoiceRepo.GetInvoicesDueInDaysAsync(7, InvoiceType.Receivable, Arg.Any<CancellationToken>())
            .Returns(invoices);
        NotificationRepo.ExistsTodayAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        var service = CreateService(settings);

        await service.GenerateAndSendDueDateRemindersAsync();

        await NotificationRepo.DidNotReceive().CreateWithInvoicesAsync(
            Arg.Any<Notification>(), Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Generate_ReceivableWithSendEmails_SendsEmail()
    {
        var settings = new NotificationSettings { SendEmails = true, ReceivableDays = [7], PayableDays = null };
        var invoice = CreateInvoice("Acme", "acme@test.com");
        InvoiceRepo.GetInvoicesDueInDaysAsync(7, InvoiceType.Receivable, Arg.Any<CancellationToken>())
            .Returns(new List<Invoice> { invoice });
        NotificationRepo.ExistsTodayAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        NotificationRepo.CreateWithInvoicesAsync(Arg.Any<Notification>(), Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());
        EmailService.SendDueDateReminderEmailAsync(
            7, "Acme", "acme@test.com", Arg.Any<List<Invoice>>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var service = CreateService(settings);

        await service.GenerateAndSendDueDateRemindersAsync();

        await EmailService.Received(1).SendDueDateReminderEmailAsync(
            7, "Acme", "acme@test.com", Arg.Any<List<Invoice>>(), Arg.Any<CancellationToken>());
        await NotificationRepo.Received(1).MarkEmailSentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Generate_PayableThreshold_DoesNotSendEmail()
    {
        var settings = new NotificationSettings { SendEmails = true, ReceivableDays = null, PayableDays = [5] };
        var invoice = CreateInvoice("Vendor", "vendor@test.com", type: InvoiceType.Payable);
        InvoiceRepo.GetInvoicesDueInDaysAsync(5, InvoiceType.Payable, Arg.Any<CancellationToken>())
            .Returns(new List<Invoice> { invoice });
        NotificationRepo.ExistsTodayAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        NotificationRepo.CreateWithInvoicesAsync(Arg.Any<Notification>(), Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());
        var service = CreateService(settings);

        await service.GenerateAndSendDueDateRemindersAsync();

        // Notification created but no email sent for payable
        await NotificationRepo.Received(1).CreateWithInvoicesAsync(
            Arg.Any<Notification>(), Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>());
        await EmailService.DidNotReceive().SendDueDateReminderEmailAsync(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<List<Invoice>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Generate_MultipleThresholds_ProcessesEach()
    {
        var settings = new NotificationSettings { SendEmails = false, ReceivableDays = [14, 7], PayableDays = null };
        var invoice = CreateInvoice();
        InvoiceRepo.GetInvoicesDueInDaysAsync(Arg.Any<int>(), InvoiceType.Receivable, Arg.Any<CancellationToken>())
            .Returns(new List<Invoice> { invoice });
        NotificationRepo.ExistsTodayAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        NotificationRepo.CreateWithInvoicesAsync(Arg.Any<Notification>(), Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());
        var service = CreateService(settings);

        await service.GenerateAndSendDueDateRemindersAsync();

        // Should create a notification for each threshold
        await NotificationRepo.Received(2).CreateWithInvoicesAsync(
            Arg.Any<Notification>(), Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>());
    }
}
