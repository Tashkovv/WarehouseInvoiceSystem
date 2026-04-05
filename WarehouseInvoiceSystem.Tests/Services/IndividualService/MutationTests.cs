namespace WarehouseInvoiceSystem.Tests.Services.IndividualService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Entities;

public class MutationTests : IndividualServiceTestBase
{
    [Fact]
    public async Task Create_MapsAllDtoFieldsToEntity()
    {
        var dto = BuildCreateDto("Maria", "Petrova", "1112223334445");
        IndividualRepo.IdentificationNumberExistsAsync(dto.IdentificationNumber, Arg.Any<Guid?>()).Returns(false);
        var service = CreateService();

        await service.CreateIndividualAsync(dto);

        await IndividualRepo.Received(1).CreateAsync(Arg.Is<Individual>(i =>
            i.FirstName == "Maria" &&
            i.LastName == "Petrova" &&
            i.IdentificationNumber == "1112223334445" &&
            i.Address == "456 Orchard Ln" &&
            i.Phone == "+389 71 333444" &&
            i.Email == "jane@orchard.com" &&
            i.BankAccount == "MK07300000000054321" &&
            i.IsActive == true));
    }

    [Fact]
    public async Task Create_DuplicateIdentificationNumber_ThrowsInvalidOperation()
    {
        var dto = BuildCreateDto(identificationNumber: "DUPLICATE123");
        IndividualRepo.IdentificationNumberExistsAsync("DUPLICATE123", Arg.Any<Guid?>()).Returns(true);
        var service = CreateService();

        await service.Invoking(s => s.CreateIndividualAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task Update_Found_MapsAllFields()
    {
        var entity = CreateEntity();
        IndividualRepo.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>()).Returns(entity);
        IndividualRepo.IdentificationNumberExistsAsync(Arg.Any<string>(), entity.Id).Returns(false);
        var dto = BuildUpdateDto("Updated", "Name", "9999999999999");
        var service = CreateService();

        await service.UpdateIndividualAsync(entity.Id, dto);

        await IndividualRepo.Received(1).UpdateAsync(Arg.Is<Individual>(i =>
            i.FirstName == "Updated" &&
            i.LastName == "Name" &&
            i.IdentificationNumber == "9999999999999" &&
            i.Address == "789 Updated St" &&
            i.Phone == "+389 72 555666" &&
            i.Email == "updated@test.com" &&
            i.BankAccount == "MK07300000000099999" &&
            i.IsActive == true));
    }

    [Fact]
    public async Task Update_NotFound_ThrowsKeyNotFound()
    {
        var id = Guid.NewGuid();
        IndividualRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Individual?)null);
        var service = CreateService();

        await service.Invoking(s => s.UpdateIndividualAsync(id, BuildUpdateDto()))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Update_DuplicateIdentificationNumber_ThrowsInvalidOperation()
    {
        var entity = CreateEntity();
        IndividualRepo.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>()).Returns(entity);
        IndividualRepo.IdentificationNumberExistsAsync("DUPLICATE123", entity.Id).Returns(true);
        var dto = BuildUpdateDto(identificationNumber: "DUPLICATE123");
        var service = CreateService();

        await service.Invoking(s => s.UpdateIndividualAsync(entity.Id, dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task Delete_DelegatesToRepository()
    {
        var id = Guid.NewGuid();
        IndividualRepo.DeleteAsync(id).Returns(true);
        var service = CreateService();

        var result = await service.DeleteIndividualAsync(id);

        result.Should().BeTrue();
        await IndividualRepo.Received(1).DeleteAsync(id);
    }
}
