namespace WarehouseInvoiceSystem.Tests.Services.CompanyService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Entities;

public class BalanceTests : CompanyServiceTestBase
{
    [Fact]
    public async Task GetBalance_Found_CalculatesNetBalance()
    {
        var entity = CreateEntity("Balance Co");
        CompanyRepo.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>()).Returns(entity);
        CompanyRepo.GetTotalOwedByCompanyAsync(entity.Id, Arg.Any<CancellationToken>()).Returns(3000m);
        CompanyRepo.GetTotalOwedToCompanyAsync(entity.Id, Arg.Any<CancellationToken>()).Returns(5000m);
        var service = CreateService();

        var result = await service.GetCompanyBalanceAsync(entity.Id);

        result.CompanyId.Should().Be(entity.Id);
        result.CompanyName.Should().Be("Balance Co");
        result.TotalOwedByUs.Should().Be(3000m);
        result.TotalOwedToUs.Should().Be(5000m);
        result.NetBalance.Should().Be(2000m); // 5000 - 3000
    }

    [Fact]
    public async Task GetBalance_NotFound_ThrowsKeyNotFound()
    {
        var id = Guid.NewGuid();
        CompanyRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Company?)null);
        var service = CreateService();

        await service.Invoking(s => s.GetCompanyBalanceAsync(id))
            .Should().ThrowAsync<KeyNotFoundException>();
    }
}
