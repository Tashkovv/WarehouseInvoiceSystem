namespace WarehouseInvoiceSystem.Tests.Services.CompanyService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;

public class MutationTests : CompanyServiceTestBase
{
    [Fact]
    public async Task Create_MapsAllDtoFieldsToEntity()
    {
        var dto = BuildCreateDto("Acme Corp", CompanyType.Vendor, 60, 15000m);
        var service = CreateService();

        await service.CreateCompanyAsync(dto);

        await CompanyRepo.Received(1).CreateAsync(Arg.Is<Company>(c =>
            c.Name == "Acme Corp" &&
            c.Type == CompanyType.Vendor &&
            c.ContactPerson == "Jane Doe" &&
            c.Email == "jane@test.com" &&
            c.Phone == "+389 71 654321" &&
            c.Address == "New Address 1" &&
            c.TaxId == "MK7654321" &&
            c.PaymentTermsDays == 60 &&
            c.CreditLimit == 15000m &&
            c.IsActive == true));
    }

    [Fact]
    public async Task Update_Found_MapsAllFields()
    {
        var entity = CreateEntity();
        CompanyRepo.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>()).Returns(entity);
        var dto = BuildUpdateDto("Updated Name", CompanyType.Both, true);
        var service = CreateService();

        await service.UpdateCompanyAsync(entity.Id, dto);

        await CompanyRepo.Received(1).UpdateAsync(Arg.Is<Company>(c =>
            c.Name == "Updated Name" &&
            c.Type == CompanyType.Both &&
            c.ContactPerson == "Updated Person" &&
            c.Email == "updated@test.com" &&
            c.Phone == "+389 72 000000" &&
            c.Address == "Updated Address" &&
            c.TaxId == "MK0000000" &&
            c.PaymentTermsDays == 45 &&
            c.CreditLimit == 20000m &&
            c.IsActive == true));
    }

    [Fact]
    public async Task Update_NotFound_ThrowsKeyNotFound()
    {
        var id = Guid.NewGuid();
        CompanyRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Company?)null);
        var service = CreateService();

        await service.Invoking(s => s.UpdateCompanyAsync(id, BuildUpdateDto()))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Delete_DelegatesToRepository()
    {
        var id = Guid.NewGuid();
        CompanyRepo.DeleteAsync(id).Returns(true);
        var service = CreateService();

        var result = await service.DeleteCompanyAsync(id);

        result.Should().BeTrue();
        await CompanyRepo.Received(1).DeleteAsync(id);
    }

    [Fact]
    public async Task SetActiveStatus_Found_UpdatesAndReturnsTrue()
    {
        var entity = CreateEntity(isActive: true);
        CompanyRepo.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>()).Returns(entity);
        var service = CreateService();

        var result = await service.SetActiveStatusAsync(entity.Id, false);

        result.Should().BeTrue();
        await CompanyRepo.Received(1).UpdateAsync(Arg.Is<Company>(c => c.IsActive == false));
    }

    [Fact]
    public async Task SetActiveStatus_NotFound_ThrowsKeyNotFound()
    {
        var id = Guid.NewGuid();
        CompanyRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Company?)null);
        var service = CreateService();

        await service.Invoking(s => s.SetActiveStatusAsync(id, true))
            .Should().ThrowAsync<KeyNotFoundException>();
    }
}
