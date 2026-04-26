namespace WarehouseInvoiceSystem.Tests.Services.InventoryService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;
using WarehouseInvoiceSystem.Domain.Queries;
using WarehouseInvoiceSystem.Domain.Queries.Common;

public class TransactionQueryTests : InventoryServiceTestBase
{
    [Fact]
    public async Task GetAllTransactions_MapsEntitiesToDtos()
    {
        var entities = new[] { CreateTransaction(), CreateTransaction() };
        TransactionRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(entities);
        var service = CreateService();

        var result = await service.GetAllTransactionsAsync();

        result.Should().HaveCount(2);
        result.First().ProductCode.Should().Be("P001");
        result.First().WarehouseName.Should().Be("WH1");
    }

    [Fact]
    public async Task GetTransactionsByProduct_DelegatesToRepository()
    {
        var productId = Guid.NewGuid();
        var entities = new[] { CreateTransaction(productId: productId) };
        TransactionRepo.GetByProductIdAsync(productId, Arg.Any<CancellationToken>()).Returns(entities);
        var service = CreateService();

        var result = await service.GetTransactionsByProductAsync(productId);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPagedTransactionsByProduct_DelegatesToRepository()
    {
        var query = new GetInventoryTransactionsQuery { ProductId = Guid.NewGuid(), Page = 1, PageSize = 10 };
        var pagedResult = new PagedResult<InventoryTransaction>
        {
            Items = [CreateTransaction()],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };
        TransactionRepo.GetPagedByProductAsync(query, Arg.Any<CancellationToken>()).Returns(pagedResult);
        var service = CreateService();

        var result = await service.GetPagedTransactionsByProductAsync(query);

        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAllFiltered_DoesNotMutateInputQuery()
    {
        var query = new GetInventoryTransactionsQuery
        {
            ProductId = Guid.NewGuid(),
            Page = 3,
            PageSize = 25
        };
        TransactionRepo.GetPagedByProductAsync(Arg.Any<GetInventoryTransactionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<InventoryTransaction>
            {
                Items = [],
                TotalCount = 0,
                Page = 1,
                PageSize = int.MaxValue
            });
        var service = CreateService();

        await service.GetAllFilteredTransactionsAsync(query);

        // Bug 1 fix: original query should not be mutated
        query.Page.Should().Be(3);
        query.PageSize.Should().Be(25);
    }

    [Fact]
    public async Task GetAllFiltered_PassesUnpagedQueryToRepository()
    {
        var query = new GetInventoryTransactionsQuery
        {
            ProductId = Guid.NewGuid(),
            WarehouseId = Guid.NewGuid(),
            Types = [InventoryTransactionType.Inbound],
            Page = 5,
            PageSize = 20
        };
        TransactionRepo.GetPagedByProductAsync(Arg.Any<GetInventoryTransactionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<InventoryTransaction>
            {
                Items = [],
                TotalCount = 0,
                Page = 1,
                PageSize = int.MaxValue
            });
        var service = CreateService();

        await service.GetAllFilteredTransactionsAsync(query);

        // The repo should receive a query with Page=1, PageSize=int.MaxValue, but same filters
        await TransactionRepo.Received(1).GetPagedByProductAsync(
            Arg.Is<GetInventoryTransactionsQuery>(q =>
                q.Page == 1 &&
                q.PageSize == int.MaxValue &&
                q.ProductId == query.ProductId &&
                q.WarehouseId == query.WarehouseId &&
                q.Types != null && q.Types.Contains(InventoryTransactionType.Inbound)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMovementTotals_DelegatesToRepositoryAndReturnsTotals()
    {
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        TransactionRepo.GetMovementTotalsAsync(productId, warehouseId, Arg.Any<CancellationToken>())
            .Returns((150m, 40m));
        var service = CreateService();

        var (incoming, outgoing) = await service.GetMovementTotalsAsync(productId, warehouseId);

        incoming.Should().Be(150m);
        outgoing.Should().Be(40m);
    }

    [Fact]
    public async Task GetMovementTotals_NullWarehouseId_PassedThrough()
    {
        var productId = Guid.NewGuid();
        TransactionRepo.GetMovementTotalsAsync(productId, null, Arg.Any<CancellationToken>())
            .Returns((200m, 80m));
        var service = CreateService();

        var (incoming, outgoing) = await service.GetMovementTotalsAsync(productId, null);

        incoming.Should().Be(200m);
        outgoing.Should().Be(80m);
        await TransactionRepo.Received(1).GetMovementTotalsAsync(productId, null, Arg.Any<CancellationToken>());
    }
}
