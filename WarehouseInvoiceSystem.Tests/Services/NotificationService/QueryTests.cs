namespace WarehouseInvoiceSystem.Tests.Services.NotificationService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;

public class QueryTests : NotificationServiceTestBase
{
    [Fact]
    public async Task GetUnreadCount_DelegatesToRepository()
    {
        NotificationRepo.GetUnreadCountAsync(Arg.Any<CancellationToken>()).Returns(5);
        var service = CreateService();

        var result = await service.GetUnreadCountAsync();

        result.Should().Be(5);
    }

    [Fact]
    public async Task GetRecent_MapsNotificationsWithInvoices()
    {
        var invoice = CreateInvoice("Test Co", "test@co.com");
        var notification = CreateNotification(
            NotificationType.InvoiceDueReminder,
            """{"daysBeforeDue":7}""",
            isRead: true,
            isEmailSent: true);
        notification.NotificationInvoices = [CreateNotificationInvoice(notification.Id, invoice)];

        NotificationRepo.GetRecentAsync(20, Arg.Any<CancellationToken>()).Returns([notification]);
        var service = CreateService();

        var result = await service.GetRecentNotificationsAsync();

        result.Should().HaveCount(1);
        var dto = result[0];
        dto.Id.Should().Be(notification.Id);
        dto.Type.Should().Be(NotificationType.InvoiceDueReminder);
        dto.Data.Should().Be("""{"daysBeforeDue":7}""");
        dto.IsRead.Should().BeTrue();
        dto.IsEmailSent.Should().BeTrue();
        dto.Invoices.Should().HaveCount(1);

        var invDto = dto.Invoices[0];
        invDto.InvoiceId.Should().Be(invoice.Id);
        invDto.InvoiceNumber.Should().Be("INV-001");
        invDto.CompanyName.Should().Be("Test Co");
        invDto.TotalAmount.Should().Be(5000m);
        invDto.DueDate.Should().Be(invoice.DueDate);
    }

    [Fact]
    public async Task MarkAsRead_DelegatesToRepository()
    {
        var id = Guid.NewGuid();
        var service = CreateService();

        await service.MarkAsReadAsync(id);

        await NotificationRepo.Received(1).MarkAsReadAsync(id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkAllAsRead_DelegatesToRepository()
    {
        var service = CreateService();

        await service.MarkAllAsReadAsync();

        await NotificationRepo.Received(1).MarkAllAsReadAsync(Arg.Any<CancellationToken>());
    }
}
