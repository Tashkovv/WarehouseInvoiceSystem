namespace WarehouseInvoiceSystem.Tests.Services.InvoiceService;

using NSubstitute;
using WarehouseInvoiceSystem.Application.DTOs.Invoice;
using WarehouseInvoiceSystem.Application.Interfaces;
using WarehouseInvoiceSystem.Application.Services;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;
using WarehouseInvoiceSystem.Domain.Interfaces;

public abstract class InvoiceServiceTestBase
{
    protected readonly IInvoiceRepository InvoiceRepo = Substitute.For<IInvoiceRepository>();
    protected readonly ICompanyRepository CompanyRepo = Substitute.For<ICompanyRepository>();
    protected readonly IWarehouseRepository WarehouseRepo = Substitute.For<IWarehouseRepository>();
    protected readonly IProductRepository ProductRepo = Substitute.For<IProductRepository>();
    protected readonly IInventoryService InventoryService = Substitute.For<IInventoryService>();
    protected readonly ILocalizationService LocalizationService = Substitute.For<ILocalizationService>();
    protected readonly IInventoryTransactionRepository TransactionRepo = Substitute.For<IInventoryTransactionRepository>();

    protected InvoiceService CreateService() =>
        new(InvoiceRepo, CompanyRepo, WarehouseRepo, ProductRepo, InventoryService, LocalizationService, TransactionRepo);

    protected static CreateInvoiceDto BuildCreateDto(InvoiceType type = InvoiceType.Receivable) => new()
    {
        CompanyId = Guid.NewGuid(),
        WarehouseId = Guid.NewGuid(),
        Type = type,
        IssueDate = DateTime.Today,
        DueDate = DateTime.Today.AddDays(30),
        LineItems =
        [
            new CreateInvoiceLineDto
            {
                ProductId = Guid.NewGuid(),
                Description = "Test Product",
                Quantity = 10,
                UnitPrice = 100m,
                TaxRate = 18m,
                DiscountPercentage = 5m
            }
        ]
    };

    protected static UpdateInvoiceDto BuildUpdateDto() => new()
    {
        CompanyId = Guid.NewGuid(),
        WarehouseId = Guid.NewGuid(),
        Type = InvoiceType.Receivable,
        IssueDate = DateTime.Today,
        DueDate = DateTime.Today.AddDays(30),
        Notes = "Updated notes",
        LineItems =
        [
            new UpdateInvoiceLineDto
            {
                Id = Guid.Empty,
                ProductId = Guid.NewGuid(),
                Description = "New Line",
                Quantity = 5,
                UnitPrice = 200m,
                TaxRate = 18m,
                DiscountPercentage = 0m
            }
        ]
    };

    protected static Invoice CreateEntity(InvoiceStatus status, InvoiceType type = InvoiceType.Receivable, bool withLines = true)
    {
        var invoice = new Invoice
        {
            InvoiceNumber = "INV-000001",
            CompanyId = Guid.NewGuid(),
            WarehouseId = Guid.NewGuid(),
            Type = type,
            Status = status,
            IssueDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(30),
            SubTotal = 1000m,
            DiscountTotal = 50m,
            TaxAmount = 171m,
            TotalAmount = 1121m,
            AmountPaid = 0m,
            Company = new Company { Name = "Test Company", Email = "test@test.com" },
            Warehouse = new Warehouse { Name = "Main" },
            LineItems = []
        };
        SetEntityId(invoice, Guid.NewGuid());

        if (withLines)
        {
            var line = new InvoiceLine
            {
                InvoiceId = invoice.Id,
                ProductId = Guid.NewGuid(),
                Description = "Test Product",
                Quantity = 10,
                UnitPrice = 100m,
                TaxRate = 18m,
                DiscountPercentage = 5m,
                Product = new Product { Code = "P001", Name = "Test Product", Unit = "pcs" }
            };
            SetEntityId(line, Guid.NewGuid());
            invoice.LineItems.Add(line);
        }

        return invoice;
    }

    protected static void SetEntityId(Domain.Common.Entity entity, Guid id)
    {
        typeof(Domain.Common.Entity)
            .GetProperty(nameof(Domain.Common.Entity.Id))!
            .SetValue(entity, id);
    }

    protected void SetupValidCreate(CreateInvoiceDto dto)
    {
        CompanyRepo.GetByIdAsync(dto.CompanyId, Arg.Any<CancellationToken>())
            .Returns(new Company { Name = "Test Company", Email = "test@test.com", IsActive = true });
        WarehouseRepo.ExistsAsync(dto.WarehouseId, Arg.Any<CancellationToken>()).Returns(true);
        ProductRepo.AllExistAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>()).Returns(true);
        InvoiceRepo.GenerateInvoiceNumberAsync(dto.Type, Arg.Any<CancellationToken>()).Returns("INV-000001");
        InvoiceRepo.CreateAsync(Arg.Any<Invoice>()).Returns(ci => ci.Arg<Invoice>().Id);
    }

    protected void SetupEntityLookup(Guid id, Invoice entity)
    {
        InvoiceRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(entity);
        // UpdateAsync returns the same entity
        InvoiceRepo.UpdateAsync(Arg.Any<Invoice>()).Returns(ci => ci.Arg<Invoice>());
    }

    protected void SetupValidUpdate(UpdateInvoiceDto dto)
    {
        CompanyRepo.ExistsAsync(dto.CompanyId, Arg.Any<CancellationToken>()).Returns(true);
        WarehouseRepo.ExistsAsync(dto.WarehouseId, Arg.Any<CancellationToken>()).Returns(true);
        ProductRepo.AllExistAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>()).Returns(true);
    }
}
