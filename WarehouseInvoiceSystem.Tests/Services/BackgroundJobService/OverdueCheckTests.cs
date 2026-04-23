namespace WarehouseInvoiceSystem.Tests.Services.BackgroundJobService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;

public class OverdueCheckTests : BackgroundJobServiceTestBase
{
    // ── CheckAndUpdateOverdueInvoicesAsync ────────────────────────────────────

    [Fact]
    public async Task NoEligibleInvoices_ReturnsEmptyList()
    {
        InvoiceRepo.GetOverdueEligibleAsync(Arg.Any<CancellationToken>()).Returns([]);
        var service = CreateService();

        var result = await service.CheckAndUpdateOverdueInvoicesAsync();

        result.Should().BeEmpty();
        await InvoiceSvc.DidNotReceive().MarkAsOverdueAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task SingleEligibleInvoice_MarksOverdueAndReturnsId()
    {
        var invoice = BuildInvoice();
        InvoiceRepo.GetOverdueEligibleAsync(Arg.Any<CancellationToken>()).Returns([invoice]);
        var service = CreateService();

        var result = await service.CheckAndUpdateOverdueInvoicesAsync();

        result.Should().ContainSingle().Which.Should().Be(invoice.Id);
        await InvoiceSvc.Received(1).MarkAsOverdueAsync(invoice.Id);
    }

    [Fact]
    public async Task MultipleEligibleInvoices_AllMarkedAndIdsReturned()
    {
        var invoices = new[] { BuildInvoice(), BuildInvoice(), BuildInvoice() };
        InvoiceRepo.GetOverdueEligibleAsync(Arg.Any<CancellationToken>()).Returns([.. invoices]);
        var service = CreateService();

        var result = await service.CheckAndUpdateOverdueInvoicesAsync();

        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(invoices.Select(i => i.Id));
        await InvoiceSvc.Received(3).MarkAsOverdueAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task CancellationToken_PassedToRepository()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken capturedCt = default;
        InvoiceRepo.GetOverdueEligibleAsync(Arg.Do<CancellationToken>(ct => capturedCt = ct))
                   .Returns([]);
        var service = CreateService();

        await service.CheckAndUpdateOverdueInvoicesAsync(cts.Token);

        capturedCt.Should().Be(cts.Token);
    }

    // ── GenerateAndSendDueDateRemindersAsync ──────────────────────────────────

    [Fact]
    public async Task DueDateReminders_DelegatesToNotificationService()
    {
        var service = CreateService();

        await service.GenerateAndSendDueDateRemindersAsync();

        await NotificationSvc.Received(1).GenerateAndSendDueDateRemindersAsync(Arg.Any<CancellationToken>());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Invoice BuildInvoice()
    {
        var invoice = new Invoice
        {
            InvoiceNumber = "INV-000001",
            Status = InvoiceStatus.Confirmed,
            DueDate = DateTime.Today.AddDays(-1),
            TotalAmount = 1000m
        };
        typeof(Domain.Common.Entity)
            .GetProperty(nameof(Domain.Common.Entity.Id))!
            .SetValue(invoice, Guid.NewGuid());
        return invoice;
    }
}
