namespace WarehouseInvoiceSystem.Tests.Services.IndividualService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Enums;
using WarehouseInvoiceSystem.Domain.Queries.Results;

public class AnalyticsTests : IndividualServiceTestBase
{
    [Fact]
    public async Task Analytics_EmptyData_ReturnsEmptyDto()
    {
        var id = Guid.NewGuid();
        PurchaseNoteRepo.GetIndividualAnalyticsDataAsync(id, Arg.Any<CancellationToken>())
            .Returns(BuildAnalyticsResult());
        var service = CreateService();

        var result = await service.GetIndividualAnalyticsAsync(id);

        result.TotalPurchaseNotes.Should().Be(0);
        result.TotalAmount.Should().Be(0);
        result.RecentPurchaseNotes.Should().BeEmpty();
        result.MostPurchasedProduct.Should().BeNull();
    }

    [Fact]
    public async Task Analytics_Counts_Correct()
    {
        var id = Guid.NewGuid();
        var statRows = new List<IndividualNoteStatRow>
        {
            BuildStatRow(PurchaseNoteStatus.Paid, count: 5),
            BuildStatRow(PurchaseNoteStatus.Pending, count: 3),
            BuildStatRow(PurchaseNoteStatus.Cancelled, count: 2)
        };
        PurchaseNoteRepo.GetIndividualAnalyticsDataAsync(id, Arg.Any<CancellationToken>())
            .Returns(BuildAnalyticsResult(statRows: statRows));
        var service = CreateService();

        var result = await service.GetIndividualAnalyticsAsync(id);

        // Active = all except Cancelled and Draft → Paid + Pending = 8
        result.TotalPurchaseNotes.Should().Be(8);
        result.PaidCount.Should().Be(5);
        result.UnpaidCount.Should().Be(3);
        result.CancelledCount.Should().Be(2);
    }

    [Fact]
    public async Task Analytics_Amounts_Correct()
    {
        var id = Guid.NewGuid();
        var statRows = new List<IndividualNoteStatRow>
        {
            BuildStatRow(PurchaseNoteStatus.Paid, count: 2, totalAmount: 5000m),
            BuildStatRow(PurchaseNoteStatus.Pending, count: 1, totalAmount: 3000m),
            BuildStatRow(PurchaseNoteStatus.Cancelled, count: 1, totalAmount: 1000m)
        };
        PurchaseNoteRepo.GetIndividualAnalyticsDataAsync(id, Arg.Any<CancellationToken>())
            .Returns(BuildAnalyticsResult(statRows: statRows));
        var service = CreateService();

        var result = await service.GetIndividualAnalyticsAsync(id);

        // Active total = 5000 + 3000 = 8000
        result.TotalAmount.Should().Be(8000m);
        result.PaidAmount.Should().Be(5000m);
        result.UnpaidAmount.Should().Be(3000m);
        result.CancelledAmount.Should().Be(1000m);
    }

    [Fact]
    public async Task Analytics_PurchaseDates_MappedCorrectly()
    {
        var id = Guid.NewGuid();
        var first = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var last = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc);
        var data = BuildAnalyticsResult(
            statRows: [BuildStatRow(PurchaseNoteStatus.Paid)],
            firstPurchaseDate: first,
            lastPurchaseDate: last);
        PurchaseNoteRepo.GetIndividualAnalyticsDataAsync(id, Arg.Any<CancellationToken>()).Returns(data);
        var service = CreateService();

        var result = await service.GetIndividualAnalyticsAsync(id);

        result.FirstPurchaseDate.Should().Be(first);
        result.LastPurchaseDate.Should().Be(last);
    }

    [Fact]
    public async Task Analytics_MostPurchasedProduct_MappedCorrectly()
    {
        var id = Guid.NewGuid();
        var data = BuildAnalyticsResult(
            statRows: [BuildStatRow(PurchaseNoteStatus.Paid)],
            mostPurchasedProductName: "Apples",
            mostPurchasedProductQuantity: 2500.5m,
            mostPurchasedProductUnit: "kg");
        PurchaseNoteRepo.GetIndividualAnalyticsDataAsync(id, Arg.Any<CancellationToken>()).Returns(data);
        var service = CreateService();

        var result = await service.GetIndividualAnalyticsAsync(id);

        result.MostPurchasedProduct.Should().Be("Apples");
        result.MostPurchasedProductQuantity.Should().Be(2500.5m);
        result.MostPurchasedProductUnit.Should().Be("kg");
    }

    [Fact]
    public async Task Analytics_MostPurchasedProduct_NullWhenNoData()
    {
        var id = Guid.NewGuid();
        var data = BuildAnalyticsResult(
            statRows: [BuildStatRow(PurchaseNoteStatus.Paid)],
            mostPurchasedProductName: null);
        PurchaseNoteRepo.GetIndividualAnalyticsDataAsync(id, Arg.Any<CancellationToken>()).Returns(data);
        var service = CreateService();

        var result = await service.GetIndividualAnalyticsAsync(id);

        result.MostPurchasedProduct.Should().BeNull();
        result.MostPurchasedProductQuantity.Should().Be(0);
        result.MostPurchasedProductUnit.Should().BeNull();
    }

    [Fact]
    public async Task Analytics_RecentNotes_MappedCorrectly()
    {
        var id = Guid.NewGuid();
        var row = BuildRecentNoteRow(PurchaseNoteStatus.Pending, 750m);
        var data = BuildAnalyticsResult(recentNotes: [row]);
        PurchaseNoteRepo.GetIndividualAnalyticsDataAsync(id, Arg.Any<CancellationToken>()).Returns(data);
        var service = CreateService();

        var result = await service.GetIndividualAnalyticsAsync(id);

        result.RecentPurchaseNotes.Should().HaveCount(1);
        var dto = result.RecentPurchaseNotes[0];
        dto.Id.Should().Be(row.Id);
        dto.NoteNumber.Should().Be("PN-001");
        dto.Status.Should().Be(PurchaseNoteStatus.Pending);
        dto.TotalAmount.Should().Be(750m);
    }
}
