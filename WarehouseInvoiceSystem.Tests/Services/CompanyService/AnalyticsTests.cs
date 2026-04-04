namespace WarehouseInvoiceSystem.Tests.Services.CompanyService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Enums;
using WarehouseInvoiceSystem.Domain.Queries.Results;

public class AnalyticsTests : CompanyServiceTestBase
{
    [Fact]
    public async Task Analytics_EmptyData_ReturnsEmptyDto()
    {
        var id = Guid.NewGuid();
        InvoiceRepo.GetCompanyAnalyticsDataAsync(id, Arg.Any<CancellationToken>())
            .Returns(BuildAnalyticsResult());
        var service = CreateService();

        var result = await service.GetCompanyAnalyticsAsync(id);

        result.ReceivableTotalCount.Should().Be(0);
        result.PayableTotalCount.Should().Be(0);
        result.RecentInvoices.Should().BeEmpty();
        result.MostTradedProduct.Should().BeNull();
    }

    [Fact]
    public async Task Analytics_ReceivableCounts_Correct()
    {
        var id = Guid.NewGuid();
        var statRows = new List<CompanyInvoiceStatRow>
        {
            BuildStatRow(InvoiceType.Receivable, InvoiceStatus.Paid, count: 5),
            BuildStatRow(InvoiceType.Receivable, InvoiceStatus.Confirmed, count: 3),
            BuildStatRow(InvoiceType.Receivable, InvoiceStatus.PartiallyPaid, count: 2),
            BuildStatRow(InvoiceType.Receivable, InvoiceStatus.Overdue, count: 1),
            BuildStatRow(InvoiceType.Receivable, InvoiceStatus.Cancelled, count: 4)
        };
        InvoiceRepo.GetCompanyAnalyticsDataAsync(id, Arg.Any<CancellationToken>())
            .Returns(BuildAnalyticsResult(statRows: statRows));
        var service = CreateService();

        var result = await service.GetCompanyAnalyticsAsync(id);

        // Active = all except Cancelled and Draft → Paid+Confirmed+PartiallyPaid+Overdue = 11
        result.ReceivableTotalCount.Should().Be(11);
        result.ReceivablePaidCount.Should().Be(5);
        // Open = Confirmed+PartiallyPaid+Overdue = 6
        result.ReceivableOpenCount.Should().Be(6);
        result.ReceivableOverdueCount.Should().Be(1);
        result.ReceivableCancelledCount.Should().Be(4);
    }

    [Fact]
    public async Task Analytics_ReceivableAmounts_Correct()
    {
        var id = Guid.NewGuid();
        var statRows = new List<CompanyInvoiceStatRow>
        {
            // Paid: total 5000, amountPaid 5000
            BuildStatRow(InvoiceType.Receivable, InvoiceStatus.Paid, count: 2,
                totalAmount: 5000m, amountPaid: 5000m, amountDue: 0m),
            // Open (Confirmed): total 3000, amountPaid 500, due 2500
            BuildStatRow(InvoiceType.Receivable, InvoiceStatus.Confirmed, count: 1,
                totalAmount: 3000m, amountPaid: 500m, amountDue: 2500m),
            // Open (Overdue): total 2000, amountPaid 200, due 1800
            BuildStatRow(InvoiceType.Receivable, InvoiceStatus.Overdue, count: 1,
                totalAmount: 2000m, amountPaid: 200m, amountDue: 1800m),
            // Cancelled: total 1000
            BuildStatRow(InvoiceType.Receivable, InvoiceStatus.Cancelled, count: 1,
                totalAmount: 1000m, amountPaid: 0m, amountDue: 0m)
        };
        InvoiceRepo.GetCompanyAnalyticsDataAsync(id, Arg.Any<CancellationToken>())
            .Returns(BuildAnalyticsResult(statRows: statRows));
        var service = CreateService();

        var result = await service.GetCompanyAnalyticsAsync(id);

        // Active total = 5000+3000+2000 = 10000
        result.ReceivableTotalAmount.Should().Be(10000m);
        // Bug 1 fix: PaidAmount should come from recPaid only (not all active)
        result.ReceivablePaidAmount.Should().Be(5000m);
        // Open AmountDue = 2500+1800 = 4300
        result.ReceivableAmountDue.Should().Be(4300m);
        // Overdue AmountDue = 1800
        result.ReceivableOverdueAmountDue.Should().Be(1800m);
        // Cancelled amount
        result.ReceivableCancelledAmount.Should().Be(1000m);
    }

    [Fact]
    public async Task Analytics_PayableCounts_Correct()
    {
        var id = Guid.NewGuid();
        var statRows = new List<CompanyInvoiceStatRow>
        {
            BuildStatRow(InvoiceType.Payable, InvoiceStatus.Paid, count: 4),
            BuildStatRow(InvoiceType.Payable, InvoiceStatus.Confirmed, count: 2),
            BuildStatRow(InvoiceType.Payable, InvoiceStatus.Overdue, count: 3),
            BuildStatRow(InvoiceType.Payable, InvoiceStatus.Cancelled, count: 1)
        };
        InvoiceRepo.GetCompanyAnalyticsDataAsync(id, Arg.Any<CancellationToken>())
            .Returns(BuildAnalyticsResult(statRows: statRows));
        var service = CreateService();

        var result = await service.GetCompanyAnalyticsAsync(id);

        result.PayableTotalCount.Should().Be(9);
        result.PayablePaidCount.Should().Be(4);
        result.PayableOpenCount.Should().Be(5);
        result.PayableOverdueCount.Should().Be(3);
        result.PayableCancelledCount.Should().Be(1);
    }

    [Fact]
    public async Task Analytics_PayableAmounts_Correct()
    {
        var id = Guid.NewGuid();
        var statRows = new List<CompanyInvoiceStatRow>
        {
            BuildStatRow(InvoiceType.Payable, InvoiceStatus.Paid, count: 1,
                totalAmount: 4000m, amountPaid: 4000m, amountDue: 0m),
            BuildStatRow(InvoiceType.Payable, InvoiceStatus.Confirmed, count: 1,
                totalAmount: 2000m, amountPaid: 300m, amountDue: 1700m),
            BuildStatRow(InvoiceType.Payable, InvoiceStatus.Overdue, count: 1,
                totalAmount: 1500m, amountPaid: 100m, amountDue: 1400m)
        };
        InvoiceRepo.GetCompanyAnalyticsDataAsync(id, Arg.Any<CancellationToken>())
            .Returns(BuildAnalyticsResult(statRows: statRows));
        var service = CreateService();

        var result = await service.GetCompanyAnalyticsAsync(id);

        result.PayableTotalAmount.Should().Be(7500m);
        // Bug 1 fix: PaidAmount from payPaid only
        result.PayablePaidAmount.Should().Be(4000m);
        result.PayableAmountDue.Should().Be(3100m);
        result.PayableOverdueAmountDue.Should().Be(1400m);
    }

    [Fact]
    public async Task Analytics_MostTradedProduct_MappedCorrectly()
    {
        var id = Guid.NewGuid();
        // Need at least one stat row or recent invoice to avoid early return
        var data = BuildAnalyticsResult(
            statRows: [BuildStatRow(InvoiceType.Receivable, InvoiceStatus.Paid)],
            mostTradedProductName: "Widget A",
            mostTradedProductQuantity: 150.5m,
            mostTradedProductUnit: "kg");
        InvoiceRepo.GetCompanyAnalyticsDataAsync(id, Arg.Any<CancellationToken>()).Returns(data);
        var service = CreateService();

        var result = await service.GetCompanyAnalyticsAsync(id);

        result.MostTradedProduct.Should().Be("Widget A");
        result.MostTradedProductQuantity.Should().Be(150.5m);
        result.MostTradedProductUnit.Should().Be("kg");
    }

    [Fact]
    public async Task Analytics_RecentInvoices_MappedCorrectly()
    {
        var id = Guid.NewGuid();
        var row = BuildRecentInvoiceRow(InvoiceType.Receivable, InvoiceStatus.Confirmed, 750m, 750m);
        var data = BuildAnalyticsResult(recentInvoices: [row]);
        InvoiceRepo.GetCompanyAnalyticsDataAsync(id, Arg.Any<CancellationToken>()).Returns(data);
        var service = CreateService();

        var result = await service.GetCompanyAnalyticsAsync(id);

        result.RecentInvoices.Should().HaveCount(1);
        var dto = result.RecentInvoices[0];
        dto.Id.Should().Be(row.Id);
        dto.InvoiceNumber.Should().Be("INV-001");
        dto.Type.Should().Be(InvoiceType.Receivable);
        dto.Status.Should().Be(InvoiceStatus.Confirmed);
        dto.TotalAmount.Should().Be(750m);
        dto.AmountDue.Should().Be(750m);
    }

    [Fact]
    public async Task Analytics_InvoiceDates_MappedCorrectly()
    {
        var id = Guid.NewGuid();
        var first = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var last = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);
        var data = BuildAnalyticsResult(
            statRows: [BuildStatRow(InvoiceType.Receivable, InvoiceStatus.Paid)],
            firstInvoiceDate: first,
            lastInvoiceDate: last);
        InvoiceRepo.GetCompanyAnalyticsDataAsync(id, Arg.Any<CancellationToken>()).Returns(data);
        var service = CreateService();

        var result = await service.GetCompanyAnalyticsAsync(id);

        result.FirstInvoiceDate.Should().Be(first);
        result.LastInvoiceDate.Should().Be(last);
    }
}
