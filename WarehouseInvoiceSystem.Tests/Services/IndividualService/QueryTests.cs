namespace WarehouseInvoiceSystem.Tests.Services.IndividualService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Queries;
using WarehouseInvoiceSystem.Domain.Queries.Common;

public class QueryTests : IndividualServiceTestBase
{
    [Fact]
    public async Task GetAll_MapsEntitiesToDtos()
    {
        var entities = new[] { CreateEntity("John", "Doe"), CreateEntity("Jane", "Smith") };
        IndividualRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(entities);
        var service = CreateService();

        var result = await service.GetAllIndividualsAsync();

        result.Should().HaveCount(2);
        var first = result.First();
        first.FirstName.Should().Be("John");
        first.LastName.Should().Be("Doe");
        first.FullName.Should().Be("John Doe");
        first.IdentificationNumber.Should().Be("1234567890123");
        first.Address.Should().Be("123 Farm Rd");
        first.Phone.Should().Be("+389 70 111222");
        first.Email.Should().Be("john@farm.com");
        first.BankAccount.Should().Be("MK07300000000012345");
        first.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetPaged_DelegatesToRepository()
    {
        var query = new GetIndividualsQuery { Page = 1, PageSize = 10 };
        var pagedResult = new PagedResult<Individual>
        {
            Items = [CreateEntity()],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };
        IndividualRepo.GetPagedAsync(query, Arg.Any<CancellationToken>()).Returns(pagedResult);
        var service = CreateService();

        var result = await service.GetPagedAsync(query);

        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetActive_DelegatesToRepository()
    {
        var entities = new[] { CreateEntity(isActive: true) };
        IndividualRepo.GetActiveIndividualsAsync(Arg.Any<CancellationToken>()).Returns(entities);
        var service = CreateService();

        var result = await service.GetActiveIndividualsAsync();

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetById_Found_ReturnsDto()
    {
        var entity = CreateEntity("Found", "Individual");
        IndividualRepo.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>()).Returns(entity);
        var service = CreateService();

        var result = await service.GetIndividualByIdAsync(entity.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
        result.FirstName.Should().Be("Found");
        result.LastName.Should().Be("Individual");
    }

    [Fact]
    public async Task GetById_NotFound_ReturnsNull()
    {
        var id = Guid.NewGuid();
        IndividualRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Individual?)null);
        var service = CreateService();

        var result = await service.GetIndividualByIdAsync(id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdentificationNumber_Found_ReturnsDto()
    {
        var entity = CreateEntity(identificationNumber: "5551234567890");
        IndividualRepo.GetByIdentificationNumberAsync("5551234567890", Arg.Any<CancellationToken>()).Returns(entity);
        var service = CreateService();

        var result = await service.GetIndividualByIdentificationNumberAsync("5551234567890");

        result.Should().NotBeNull();
        result!.IdentificationNumber.Should().Be("5551234567890");
    }
}
