namespace WarehouseInvoiceSystem.Tests.Services.CompanyService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;
using WarehouseInvoiceSystem.Domain.Queries;
using WarehouseInvoiceSystem.Domain.Queries.Common;

public class QueryTests : CompanyServiceTestBase
{
    [Fact]
    public async Task GetAll_MapsEntitiesToDtos()
    {
        var entities = new[] { CreateEntity("A"), CreateEntity("B") };
        CompanyRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(entities);
        var service = CreateService();

        var result = await service.GetAllCompaniesAsync();

        result.Should().HaveCount(2);
        result.First().Name.Should().Be("A");
        result.Last().Name.Should().Be("B");
    }

    [Fact]
    public async Task GetPaged_DelegatesToRepository()
    {
        var query = new GetCompaniesQuery { Page = 1, PageSize = 10 };
        var pagedResult = new PagedResult<Company>
        {
            Items = [CreateEntity()],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };
        CompanyRepo.GetPagedAsync(query, Arg.Any<CancellationToken>()).Returns(pagedResult);
        var service = CreateService();

        var result = await service.GetPagedAsync(query);

        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetActive_DelegatesToRepository()
    {
        var entities = new[] { CreateEntity(isActive: true) };
        CompanyRepo.GetActiveCompaniesAsync(Arg.Any<CancellationToken>()).Returns(entities);
        var service = CreateService();

        var result = await service.GetActiveCompaniesAsync();

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByType_DelegatesToRepository()
    {
        var entities = new[] { CreateEntity(type: CompanyType.Vendor) };
        CompanyRepo.GetByTypeAsync(CompanyType.Vendor, Arg.Any<CancellationToken>()).Returns(entities);
        var service = CreateService();

        var result = await service.GetCompaniesByTypeAsync(CompanyType.Vendor);

        result.Should().HaveCount(1);
        result.First().Type.Should().Be(CompanyType.Vendor);
    }

    [Fact]
    public async Task GetById_Found_ReturnsDto()
    {
        var entity = CreateEntity("Found Co");
        CompanyRepo.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>()).Returns(entity);
        var service = CreateService();

        var result = await service.GetCompanyByIdAsync(entity.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
        result.Name.Should().Be("Found Co");
        result.ContactPerson.Should().Be("John Doe");
        result.Email.Should().Be("john@test.com");
        result.Phone.Should().Be("+389 70 123456");
        result.Address.Should().Be("Test Address 1");
        result.TaxId.Should().Be("MK1234567");
        result.PaymentTermsDays.Should().Be(30);
        result.CreditLimit.Should().Be(10000m);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetById_NotFound_ReturnsNull()
    {
        var id = Guid.NewGuid();
        CompanyRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Company?)null);
        var service = CreateService();

        var result = await service.GetCompanyByIdAsync(id);

        result.Should().BeNull();
    }
}
