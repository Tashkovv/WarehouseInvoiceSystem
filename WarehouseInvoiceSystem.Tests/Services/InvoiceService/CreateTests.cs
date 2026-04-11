namespace WarehouseInvoiceSystem.Tests.Services.InvoiceService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Application.DTOs.InventoryTransaction;
using WarehouseInvoiceSystem.Application.DTOs.Invoice;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;

public class CreateTests : InvoiceServiceTestBase
{
    [Fact]
    public async Task AlwaysCreatesAsDraft()
    {
        var dto = BuildCreateDto();
        SetupValidCreate(dto);
        var service = CreateService();

        await service.CreateInvoiceAsync(dto);

        await InvoiceRepo.Received(1).CreateAsync(Arg.Is<Invoice>(inv =>
            inv.Status == InvoiceStatus.Draft && inv.AmountPaid == 0));
    }

    [Fact]
    public async Task ReturnsCreatedId()
    {
        var dto = BuildCreateDto();
        SetupValidCreate(dto);
        var expectedId = Guid.NewGuid();
        InvoiceRepo.CreateAsync(Arg.Any<Invoice>()).Returns(expectedId);
        var service = CreateService();

        var result = await service.CreateInvoiceAsync(dto);

        result.Should().Be(expectedId);
    }

    [Fact]
    public async Task CalculatesTotalsCorrectly()
    {
        var dto = BuildCreateDto();
        SetupValidCreate(dto);
        var service = CreateService();

        await service.CreateInvoiceAsync(dto);

        // Line: Qty=10, UnitPrice=100 → Amount=1000
        // Discount: 1000 * 5/100 = 50
        // Tax: (1000 - 50) * 18/100 = 171
        // TotalAmount: (1000 - 50) + 171 = 1121
        await InvoiceRepo.Received(1).CreateAsync(Arg.Is<Invoice>(inv =>
            inv.SubTotal == 1000m &&
            inv.DiscountTotal == 50m &&
            inv.TaxAmount == 171m &&
            inv.TotalAmount == 1121m));
    }

    [Fact]
    public async Task MultipleLineItems_CalculatesTotalsCorrectly()
    {
        var dto = new CreateInvoiceDto
        {
            CompanyId = Guid.NewGuid(),
            WarehouseId = Guid.NewGuid(),
            Type = InvoiceType.Receivable,
            IssueDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(30),
            LineItems =
            [
                new CreateInvoiceLineDto
                {
                    ProductId = Guid.NewGuid(), Description = "A",
                    Quantity = 10, UnitPrice = 100m, TaxRate = 0m, DiscountPercentage = 0m
                },
                new CreateInvoiceLineDto
                {
                    ProductId = Guid.NewGuid(), Description = "B",
                    Quantity = 5, UnitPrice = 200m, TaxRate = 10m, DiscountPercentage = 0m
                }
            ]
        };
        SetupValidCreate(dto);
        var service = CreateService();

        await service.CreateInvoiceAsync(dto);

        // Line A: 10*100=1000, discount=0, tax=0, total=1000
        // Line B: 5*200=1000, discount=0, tax=1000*10/100=100, total=1100
        // Totals: SubTotal=2000, Discount=0, Tax=100, Total=2100
        await InvoiceRepo.Received(1).CreateAsync(Arg.Is<Invoice>(inv =>
            inv.SubTotal == 2000m &&
            inv.DiscountTotal == 0m &&
            inv.TaxAmount == 100m &&
            inv.TotalAmount == 2100m));
    }

    [Fact]
    public async Task GeneratesInvoiceNumber_PassesType()
    {
        var dto = BuildCreateDto(InvoiceType.Payable);
        SetupValidCreate(dto);
        InvoiceRepo.GenerateInvoiceNumberAsync(InvoiceType.Payable, Arg.Any<CancellationToken>())
            .Returns("PAY-000001");
        var service = CreateService();

        await service.CreateInvoiceAsync(dto);

        await InvoiceRepo.Received(1).CreateAsync(Arg.Is<Invoice>(inv =>
            inv.InvoiceNumber == "PAY-000001"));
    }

    [Fact]
    public async Task SetsCreatedAtOnLineItems()
    {
        var dto = BuildCreateDto();
        SetupValidCreate(dto);
        var before = DateTime.UtcNow;
        var service = CreateService();

        await service.CreateInvoiceAsync(dto);

        await InvoiceRepo.Received(1).CreateAsync(Arg.Is<Invoice>(inv =>
            inv.LineItems.All(li => li.CreatedAt >= before)));
    }

    [Fact]
    public async Task InvalidCompany_ThrowsKeyNotFound()
    {
        var dto = BuildCreateDto();
        CompanyRepo.GetByIdAsync(dto.CompanyId, Arg.Any<CancellationToken>()).Returns((Company?)null);
        var service = CreateService();

        await service.Invoking(s => s.CreateInvoiceAsync(dto))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task InactiveCompany_ThrowsInvalidOperation()
    {
        var dto = BuildCreateDto();
        CompanyRepo.GetByIdAsync(dto.CompanyId, Arg.Any<CancellationToken>())
            .Returns(new Company { Name = "Test Company", Email = "test@test.com", IsActive = false });
        var service = CreateService();

        await service.Invoking(s => s.CreateInvoiceAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CompanyInactiveCannotCreate");
    }

    [Fact]
    public async Task InvalidWarehouse_ThrowsKeyNotFound()
    {
        var dto = BuildCreateDto();
        CompanyRepo.GetByIdAsync(dto.CompanyId, Arg.Any<CancellationToken>())
            .Returns(new Company { Name = "Test Company", Email = "test@test.com", IsActive = true });
        WarehouseRepo.ExistsAsync(dto.WarehouseId, Arg.Any<CancellationToken>()).Returns(false);
        var service = CreateService();

        await service.Invoking(s => s.CreateInvoiceAsync(dto))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task InvalidProduct_ThrowsKeyNotFound()
    {
        var dto = BuildCreateDto();
        CompanyRepo.GetByIdAsync(dto.CompanyId, Arg.Any<CancellationToken>())
            .Returns(new Company { Name = "Test Company", Email = "test@test.com", IsActive = true });
        WarehouseRepo.ExistsAsync(dto.WarehouseId, Arg.Any<CancellationToken>()).Returns(true);
        ProductRepo.AllExistAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>()).Returns(false);
        var service = CreateService();

        await service.Invoking(s => s.CreateInvoiceAsync(dto))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task EmptyLineItems_ThrowsInvalidOperation()
    {
        var dto = new CreateInvoiceDto
        {
            CompanyId = Guid.NewGuid(),
            WarehouseId = Guid.NewGuid(),
            Type = InvoiceType.Receivable,
            IssueDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(30),
            LineItems = []
        };
        var service = CreateService();

        await service.Invoking(s => s.CreateInvoiceAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DoesNotCreateInventoryTransactions()
    {
        var dto = BuildCreateDto();
        SetupValidCreate(dto);
        var service = CreateService();

        await service.CreateInvoiceAsync(dto);

        await InventoryService.DidNotReceive().CreateBatchAsync(
            Arg.Any<Guid>(), Arg.Any<IEnumerable<CreateInventoryTransactionDto>>());
    }
}
