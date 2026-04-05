namespace WarehouseInvoiceSystem.Tests.Services.IndividualService;

using FluentAssertions;
using NSubstitute;

public class ActiveStatusAndCountsTests : IndividualServiceTestBase
{
    [Fact]
    public async Task SetActiveStatus_DelegatesToRepository()
    {
        var id = Guid.NewGuid();
        IndividualRepo.SetActiveStatusAsync(id, false).Returns(true);
        var service = CreateService();

        var result = await service.SetActiveStatusAsync(id, false);

        result.Should().BeTrue();
        await IndividualRepo.Received(1).SetActiveStatusAsync(id, false);
    }

    [Fact]
    public async Task GetCounts_DelegatesToRepository()
    {
        IndividualRepo.GetCountsAsync(Arg.Any<CancellationToken>()).Returns((50, 40));
        var service = CreateService();

        var (total, active) = await service.GetCountsAsync();

        total.Should().Be(50);
        active.Should().Be(40);
    }
}
