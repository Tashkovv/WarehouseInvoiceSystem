namespace WarehouseInvoiceSystem.Tests.Services.NotificationService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;

public class LicenseNotificationTests : NotificationServiceTestBase
{
    [Fact]
    public async Task CreateLicenseExpiring_CreatesNotificationWithEmptyInvoices()
    {
        NotificationRepo.ExistsTodayAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        NotificationRepo.CreateWithInvoicesAsync(Arg.Any<Notification>(), Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());
        var service = CreateService();

        await service.CreateLicenseExpiringNotificationAsync(5);

        await NotificationRepo.Received(1).CreateWithInvoicesAsync(
            Arg.Is<Notification>(n =>
                n.Type == NotificationType.LicenseExpiring &&
                n.Data!.Contains("\"graceDaysRemaining\":5")),
            Arg.Is<List<Guid>>(ids => ids.Count == 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateLicenseExpiring_AlreadyExistsToday_ReturnsEarly()
    {
        NotificationRepo.ExistsTodayAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        var service = CreateService();

        await service.CreateLicenseExpiringNotificationAsync(3);

        await NotificationRepo.DidNotReceive().CreateWithInvoicesAsync(
            Arg.Any<Notification>(), Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>());
    }
}
