namespace WarehouseInvoiceSystem.Tests.Services.InvoiceService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;
using WarehouseInvoiceSystem.Domain.Queries;
using WarehouseInvoiceSystem.Domain.Queries.Common;
using WarehouseInvoiceSystem.Domain.Queries.Results;

public class QueryTests : InvoiceServiceTestBase
{
    [Fact]
    public async Task GetAll_DelegatesToRepository()
    {
        var entities = new[] { CreateEntity(InvoiceStatus.Draft) };
        InvoiceRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(entities);
        var service = CreateService();

        var result = await service.GetAllInvoicesAsync();

        result.Should().HaveCount(1);
        await InvoiceRepo.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPaged_DelegatesToRepository()
    {
        var query = new GetInvoicesQuery { Page = 1, PageSize = 10 };
        var pagedResult = new PagedResult<Invoice>
        {
            Items = [CreateEntity(InvoiceStatus.Confirmed)],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };
        InvoiceRepo.GetPagedAsync(query, Arg.Any<CancellationToken>()).Returns(pagedResult);
        var service = CreateService();

        var result = await service.GetPagedAsync(query);

        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAllFiltered_SetsPageSizeToMaxValue()
    {
        var query = new GetInvoicesQuery { Page = 1, PageSize = 10, Search = "test" };
        var pagedResult = new PagedResult<Invoice>
        {
            Items = [],
            TotalCount = 0,
            Page = 1,
            PageSize = int.MaxValue
        };
        InvoiceRepo.GetPagedAsync(
            Arg.Is<GetInvoicesQuery>(q => q.PageSize == int.MaxValue && q.Page == 1),
            Arg.Any<CancellationToken>()).Returns(pagedResult);
        var service = CreateService();

        await service.GetAllFilteredAsync(query);

        await InvoiceRepo.Received(1).GetPagedAsync(
            Arg.Is<GetInvoicesQuery>(q => q.PageSize == int.MaxValue),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_Found_ReturnsDto()
    {
        var invoice = CreateEntity(InvoiceStatus.Confirmed);
        SetupEntityLookup(invoice.Id, invoice);
        var service = CreateService();

        var result = await service.GetInvoiceByIdAsync(invoice.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(invoice.Id);
        result.InvoiceNumber.Should().Be(invoice.InvoiceNumber);
    }

    [Fact]
    public async Task GetById_NotFound_ReturnsNull()
    {
        var id = Guid.NewGuid();
        InvoiceRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Invoice?)null);
        var service = CreateService();

        var result = await service.GetInvoiceByIdAsync(id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByInvoiceNumber_DelegatesToRepository()
    {
        var invoice = CreateEntity(InvoiceStatus.Draft);
        InvoiceRepo.GetByInvoiceNumberAsync("INV-000001", Arg.Any<CancellationToken>()).Returns(invoice);
        var service = CreateService();

        var result = await service.GetInvoiceByNumberAsync("INV-000001");

        result.Should().NotBeNull();
        result!.InvoiceNumber.Should().Be("INV-000001");
    }

    [Fact]
    public async Task GetByCompany_DelegatesToRepository()
    {
        var companyId = Guid.NewGuid();
        InvoiceRepo.GetByCompanyIdAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(new[] { CreateEntity(InvoiceStatus.Draft) });
        var service = CreateService();

        var result = await service.GetInvoicesByCompanyAsync(companyId);

        result.Should().HaveCount(1);
        await InvoiceRepo.Received(1).GetByCompanyIdAsync(companyId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByType_DelegatesToRepository()
    {
        InvoiceRepo.GetByTypeAsync(InvoiceType.Receivable, Arg.Any<CancellationToken>())
            .Returns(new[] { CreateEntity(InvoiceStatus.Confirmed) });
        var service = CreateService();

        var result = await service.GetInvoicesByTypeAsync(InvoiceType.Receivable);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByStatus_DelegatesToRepository()
    {
        InvoiceRepo.GetByStatusAsync(InvoiceStatus.Overdue, Arg.Any<CancellationToken>())
            .Returns(new[] { CreateEntity(InvoiceStatus.Overdue) });
        var service = CreateService();

        var result = await service.GetInvoicesByStatusAsync(InvoiceStatus.Overdue);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetOverdue_DelegatesToRepository()
    {
        InvoiceRepo.GetOverdueInvoicesAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { CreateEntity(InvoiceStatus.Overdue) });
        var service = CreateService();

        var result = await service.GetOverdueInvoicesAsync();

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetOutstandingPosition_DelegatesToRepository()
    {
        var expected = new InvoiceOutstandingResult
        {
            ReceivableCount = 5,
            ReceivableAmount = 10000m,
            PayableCount = 3,
            PayableAmount = 5000m
        };
        InvoiceRepo.GetOutstandingPositionAsync(Arg.Any<CancellationToken>()).Returns(expected);
        var service = CreateService();

        var result = await service.GetOutstandingPositionAsync();

        result.ReceivableCount.Should().Be(5);
        result.PayableCount.Should().Be(3);
    }

    [Fact]
    public async Task GetPayableInvoiceSummary_MapsTwoRepoCallsToDto()
    {
        InvoiceRepo.GetPayableInvoiceCountsAsync(Arg.Any<CancellationToken>())
            .Returns((10, 6, 3, 1));
        InvoiceRepo.GetPayableInvoiceTotalsAsync(Arg.Any<CancellationToken>())
            .Returns((50000m, 30000m, 20000m));
        var service = CreateService();

        var result = await service.GetPayableInvoiceSummaryAsync();

        result.TotalInvoices.Should().Be(10);
        result.PaidInvoices.Should().Be(6);
        result.UnpaidInvoices.Should().Be(3);
        result.OverdueInvoices.Should().Be(1);
        result.TotalAmount.Should().Be(50000m);
        result.TotalPaid.Should().Be(30000m);
        result.TotalDue.Should().Be(20000m);
    }
}
