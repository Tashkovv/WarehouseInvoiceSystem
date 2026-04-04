namespace WarehouseInvoiceSystem.Tests.Services.CompanyService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Queries.Results;

public class PartnerCountsTests : CompanyServiceTestBase
{
    [Fact]
    public async Task GetPartnerCounts_DelegatesToRepository()
    {
        var expected = new PartnerCountsResult
        {
            Total = 50,
            Active = 40,
            Clients = 30,
            Vendors = 20
        };
        CompanyRepo.GetPartnerCountsAsync(Arg.Any<CancellationToken>()).Returns(expected);
        var service = CreateService();

        var result = await service.GetPartnerCountsAsync();

        result.Should().BeSameAs(expected);
    }
}
