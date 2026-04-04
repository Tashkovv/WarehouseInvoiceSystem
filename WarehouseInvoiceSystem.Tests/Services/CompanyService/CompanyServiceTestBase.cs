namespace WarehouseInvoiceSystem.Tests.Services.CompanyService;

using NSubstitute;
using WarehouseInvoiceSystem.Application.DTOs.Company;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;
using WarehouseInvoiceSystem.Domain.Interfaces;
using WarehouseInvoiceSystem.Domain.Queries.Results;

public abstract class CompanyServiceTestBase
{
    protected ICompanyRepository CompanyRepo { get; } = Substitute.For<ICompanyRepository>();
    protected IInvoiceRepository InvoiceRepo { get; } = Substitute.For<IInvoiceRepository>();

    protected Application.Services.CompanyService CreateService() => new(CompanyRepo, InvoiceRepo);

    protected static Company CreateEntity(
        string name = "Test Company",
        CompanyType type = CompanyType.Client,
        bool isActive = true,
        string? taxId = "MK1234567",
        int paymentTermsDays = 30,
        decimal creditLimit = 10000m)
    {
        var company = new Company
        {
            Name = name,
            Type = type,
            IsActive = isActive,
            ContactPerson = "John Doe",
            Email = "john@test.com",
            Phone = "+389 70 123456",
            Address = "Test Address 1",
            TaxId = taxId,
            PaymentTermsDays = paymentTermsDays,
            CreditLimit = creditLimit
        };
        SetEntityId(company, Guid.NewGuid());
        return company;
    }

    protected static CreateCompanyDto BuildCreateDto(
        string name = "New Company",
        CompanyType type = CompanyType.Client,
        int paymentTermsDays = 30,
        decimal creditLimit = 5000m) => new()
    {
        Name = name,
        Type = type,
        ContactPerson = "Jane Doe",
        Email = "jane@test.com",
        Phone = "+389 71 654321",
        Address = "New Address 1",
        TaxId = "MK7654321",
        PaymentTermsDays = paymentTermsDays,
        CreditLimit = creditLimit
    };

    protected static UpdateCompanyDto BuildUpdateDto(
        string name = "Updated Company",
        CompanyType type = CompanyType.Vendor,
        bool isActive = true) => new()
    {
        Name = name,
        Type = type,
        ContactPerson = "Updated Person",
        Email = "updated@test.com",
        Phone = "+389 72 000000",
        Address = "Updated Address",
        TaxId = "MK0000000",
        PaymentTermsDays = 45,
        CreditLimit = 20000m,
        IsActive = isActive
    };

    protected static CompanyInvoiceStatRow BuildStatRow(
        InvoiceType type,
        InvoiceStatus status,
        int count = 1,
        decimal totalAmount = 1000m,
        decimal amountPaid = 0m,
        decimal amountDue = 1000m) => new()
    {
        Type = type,
        Status = status,
        Count = count,
        TotalAmount = totalAmount,
        AmountPaid = amountPaid,
        AmountDue = amountDue
    };

    protected static CompanyRecentInvoiceRow BuildRecentInvoiceRow(
        InvoiceType type = InvoiceType.Receivable,
        InvoiceStatus status = InvoiceStatus.Confirmed,
        decimal totalAmount = 500m,
        decimal amountDue = 500m) => new()
    {
        Id = Guid.NewGuid(),
        InvoiceNumber = "INV-001",
        Type = type,
        Status = status,
        IssueDate = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
        DueDate = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc),
        TotalAmount = totalAmount,
        AmountDue = amountDue
    };

    protected static CompanyAnalyticsResult BuildAnalyticsResult(
        List<CompanyInvoiceStatRow>? statRows = null,
        List<CompanyRecentInvoiceRow>? recentInvoices = null,
        string? mostTradedProductName = null,
        decimal mostTradedProductQuantity = 0m,
        string? mostTradedProductUnit = null,
        DateTime? firstInvoiceDate = null,
        DateTime? lastInvoiceDate = null) => new()
    {
        StatRows = statRows ?? [],
        RecentInvoices = recentInvoices ?? [],
        MostTradedProductName = mostTradedProductName,
        MostTradedProductQuantity = mostTradedProductQuantity,
        MostTradedProductUnit = mostTradedProductUnit,
        FirstInvoiceDate = firstInvoiceDate,
        LastInvoiceDate = lastInvoiceDate
    };

    protected static void SetEntityId(object entity, Guid id)
    {
        var prop = entity.GetType().GetProperty("Id")!;
        prop.SetValue(entity, id);
    }
}
