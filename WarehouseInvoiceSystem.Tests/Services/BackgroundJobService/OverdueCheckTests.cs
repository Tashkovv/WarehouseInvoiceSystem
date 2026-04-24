namespace WarehouseInvoiceSystem.Tests.Services.BackgroundJobService;

using FluentAssertions;
using NSubstitute;

public class OverdueCheckTests : BackgroundJobServiceTestBase
{
    // ── CheckAndUpdateOverdueInvoicesAsync ────────────────────────────────────

    [Fact]
    public async Task NoEligibleInvoices_ReturnsEmptyList()
    {
        InvoiceRepo.BulkMarkOverdueAsync(Arg.Any<CancellationToken>()).Returns([]);
        var service = CreateService();

        var result = await service.CheckAndUpdateOverdueInvoicesAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SingleEligibleInvoice_ReturnsId()
    {
        var id = Guid.NewGuid();
        InvoiceRepo.BulkMarkOverdueAsync(Arg.Any<CancellationToken>()).Returns([id]);
        var service = CreateService();

        var result = await service.CheckAndUpdateOverdueInvoicesAsync();

        result.Should().ContainSingle().Which.Should().Be(id);
    }

    [Fact]
    public async Task MultipleEligibleInvoices_AllIdsReturned()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        InvoiceRepo.BulkMarkOverdueAsync(Arg.Any<CancellationToken>()).Returns([.. ids]);
        var service = CreateService();

        var result = await service.CheckAndUpdateOverdueInvoicesAsync();

        result.Should().HaveCount(3).And.BeEquivalentTo(ids);
    }

    [Fact]
    public async Task CancellationToken_PassedToRepository()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken capturedCt = default;
        InvoiceRepo.BulkMarkOverdueAsync(Arg.Do<CancellationToken>(ct => capturedCt = ct))
                   .Returns([]);
        var service = CreateService();

        await service.CheckAndUpdateOverdueInvoicesAsync(cts.Token);

        capturedCt.Should().Be(cts.Token);
    }

    [Fact]
    public async Task BulkMarkOverdue_CalledExactlyOnce()
    {
        InvoiceRepo.BulkMarkOverdueAsync(Arg.Any<CancellationToken>()).Returns([]);
        var service = CreateService();

        await service.CheckAndUpdateOverdueInvoicesAsync();

        await InvoiceRepo.Received(1).BulkMarkOverdueAsync(Arg.Any<CancellationToken>());
    }

    // ── GenerateAndSendDueDateRemindersAsync ──────────────────────────────────

    [Fact]
    public async Task DueDateReminders_DelegatesToNotificationService()
    {
        var service = CreateService();

        await service.GenerateAndSendDueDateRemindersAsync();

        await NotificationSvc.Received(1).GenerateAndSendDueDateRemindersAsync(Arg.Any<CancellationToken>());
    }
}
