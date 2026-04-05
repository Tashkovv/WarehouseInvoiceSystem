namespace WarehouseInvoiceSystem.Tests.Services.IndividualService;

using NSubstitute;
using WarehouseInvoiceSystem.Application.DTOs.Individual;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;
using WarehouseInvoiceSystem.Domain.Interfaces;
using WarehouseInvoiceSystem.Domain.Queries.Results;

public abstract class IndividualServiceTestBase
{
    protected IIndividualRepository IndividualRepo { get; } = Substitute.For<IIndividualRepository>();
    protected IPurchaseNoteRepository PurchaseNoteRepo { get; } = Substitute.For<IPurchaseNoteRepository>();

    protected Application.Services.IndividualService CreateService() => new(IndividualRepo, PurchaseNoteRepo);

    protected static Individual CreateEntity(
        string firstName = "John",
        string lastName = "Doe",
        string identificationNumber = "1234567890123",
        bool isActive = true)
    {
        var individual = new Individual
        {
            FirstName = firstName,
            LastName = lastName,
            IdentificationNumber = identificationNumber,
            Address = "123 Farm Rd",
            Phone = "+389 70 111222",
            Email = "john@farm.com",
            BankAccount = "MK07300000000012345",
            IsActive = isActive
        };
        SetEntityId(individual, Guid.NewGuid());
        return individual;
    }

    protected static CreateIndividualDto BuildCreateDto(
        string firstName = "Jane",
        string lastName = "Smith",
        string identificationNumber = "9876543210987") => new()
    {
        FirstName = firstName,
        LastName = lastName,
        IdentificationNumber = identificationNumber,
        Address = "456 Orchard Ln",
        Phone = "+389 71 333444",
        Email = "jane@orchard.com",
        BankAccount = "MK07300000000054321",
        IsActive = true
    };

    protected static UpdateIndividualDto BuildUpdateDto(
        string firstName = "Updated",
        string lastName = "Person",
        string identificationNumber = "1111111111111") => new()
    {
        FirstName = firstName,
        LastName = lastName,
        IdentificationNumber = identificationNumber,
        Address = "789 Updated St",
        Phone = "+389 72 555666",
        Email = "updated@test.com",
        BankAccount = "MK07300000000099999",
        IsActive = true
    };

    protected static IndividualNoteStatRow BuildStatRow(
        PurchaseNoteStatus status,
        int count = 1,
        decimal totalAmount = 1000m) => new()
    {
        Status = status,
        Count = count,
        TotalAmount = totalAmount
    };

    protected static IndividualRecentNoteRow BuildRecentNoteRow(
        PurchaseNoteStatus status = PurchaseNoteStatus.Pending,
        decimal totalAmount = 500m) => new()
    {
        Id = Guid.NewGuid(),
        NoteNumber = "PN-001",
        PurchaseDate = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc),
        TotalAmount = totalAmount,
        Status = status
    };

    protected static IndividualAnalyticsResult BuildAnalyticsResult(
        List<IndividualNoteStatRow>? statRows = null,
        List<IndividualRecentNoteRow>? recentNotes = null,
        string? mostPurchasedProductName = null,
        decimal mostPurchasedProductQuantity = 0m,
        string? mostPurchasedProductUnit = null,
        DateTime? firstPurchaseDate = null,
        DateTime? lastPurchaseDate = null) => new()
    {
        StatRows = statRows ?? [],
        RecentNotes = recentNotes ?? [],
        MostPurchasedProductName = mostPurchasedProductName,
        MostPurchasedProductQuantity = mostPurchasedProductQuantity,
        MostPurchasedProductUnit = mostPurchasedProductUnit,
        FirstPurchaseDate = firstPurchaseDate,
        LastPurchaseDate = lastPurchaseDate
    };

    protected static void SetEntityId(object entity, Guid id)
    {
        var prop = entity.GetType().GetProperty("Id")!;
        prop.SetValue(entity, id);
    }
}
