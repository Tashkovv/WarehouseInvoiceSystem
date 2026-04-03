namespace WarehouseInvoiceSystem.Tests.Services.PurchaseNoteService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;
using WarehouseInvoiceSystem.Domain.Queries;
using WarehouseInvoiceSystem.Domain.Queries.Common;

public class QueryTests : PurchaseNoteServiceTestBase
{
    [Fact]
    public async Task GetAll_DelegatesToRepository()
    {
        var entities = new[] { CreateEntity(PurchaseNoteStatus.Draft) };
        PurchaseNoteRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(entities);
        var service = CreateService();

        var result = await service.GetAllPurchaseNotesAsync();

        result.Should().HaveCount(1);
        await PurchaseNoteRepo.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPaged_DelegatesToRepository()
    {
        var query = new GetPurchaseNotesQuery { Page = 1, PageSize = 10 };
        var pagedResult = new PagedResult<PurchaseNote>
        {
            Items = [CreateEntity(PurchaseNoteStatus.Paid)],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };
        PurchaseNoteRepo.GetPagedAsync(query, Arg.Any<CancellationToken>()).Returns(pagedResult);
        var service = CreateService();

        var result = await service.GetPagedAsync(query);

        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAllFiltered_SetsPageSizeToMaxValue()
    {
        var query = new GetPurchaseNotesQuery { Page = 1, PageSize = 10, Search = "test" };
        var pagedResult = new PagedResult<PurchaseNote>
        {
            Items = [],
            TotalCount = 0,
            Page = 1,
            PageSize = int.MaxValue
        };
        PurchaseNoteRepo.GetPagedAsync(
            Arg.Is<GetPurchaseNotesQuery>(q => q.PageSize == int.MaxValue && q.Page == 1),
            Arg.Any<CancellationToken>()).Returns(pagedResult);
        var service = CreateService();

        await service.GetAllFilteredAsync(query);

        await PurchaseNoteRepo.Received(1).GetPagedAsync(
            Arg.Is<GetPurchaseNotesQuery>(q => q.PageSize == int.MaxValue),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_Found_ReturnsDto()
    {
        var note = CreateEntity(PurchaseNoteStatus.Pending);
        SetupEntityLookup(note.Id, note);
        var service = CreateService();

        var result = await service.GetPurchaseNoteByIdAsync(note.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(note.Id);
        result.NoteNumber.Should().Be(note.NoteNumber);
    }

    [Fact]
    public async Task GetById_NotFound_ReturnsNull()
    {
        var id = Guid.NewGuid();
        PurchaseNoteRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((PurchaseNote?)null);
        var service = CreateService();

        var result = await service.GetPurchaseNoteByIdAsync(id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByNoteNumber_DelegatesToRepository()
    {
        var note = CreateEntity(PurchaseNoteStatus.Draft);
        PurchaseNoteRepo.GetByNoteNumberAsync("OB-000001", Arg.Any<CancellationToken>()).Returns(note);
        var service = CreateService();

        var result = await service.GetPurchaseNoteByNumberAsync("OB-000001");

        result.Should().NotBeNull();
        result!.NoteNumber.Should().Be("OB-000001");
    }

    [Fact]
    public async Task GetByIndividual_DelegatesToRepository()
    {
        var individualId = Guid.NewGuid();
        PurchaseNoteRepo.GetByIndividualIdAsync(individualId, Arg.Any<CancellationToken>())
            .Returns(new[] { CreateEntity(PurchaseNoteStatus.Draft) });
        var service = CreateService();

        var result = await service.GetPurchaseNotesByIndividualAsync(individualId);

        result.Should().HaveCount(1);
        await PurchaseNoteRepo.Received(1).GetByIndividualIdAsync(individualId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByDateRange_DelegatesToRepository()
    {
        var from = DateTime.Today.AddDays(-7);
        var to = DateTime.Today;
        PurchaseNoteRepo.GetByDateRangeAsync(from, to, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<PurchaseNote>());
        var service = CreateService();

        var result = await service.GetPurchaseNotesByDateRangeAsync(from, to);

        result.Should().BeEmpty();
        await PurchaseNoteRepo.Received(1).GetByDateRangeAsync(from, to, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByStatus_DelegatesToRepository()
    {
        PurchaseNoteRepo.GetByStatusAsync(PurchaseNoteStatus.Pending, Arg.Any<CancellationToken>())
            .Returns(new[] { CreateEntity(PurchaseNoteStatus.Pending) });
        var service = CreateService();

        var result = await service.GetPurchaseNotesByStatusAsync(PurchaseNoteStatus.Pending);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetRecent_DelegatesToRepository()
    {
        PurchaseNoteRepo.GetRecentAsync(5, Arg.Any<CancellationToken>())
            .Returns(new[] { CreateEntity(PurchaseNoteStatus.Paid) });
        var service = CreateService();

        var result = await service.GetRecentAsync(5);

        result.Should().HaveCount(1);
        await PurchaseNoteRepo.Received(1).GetRecentAsync(5, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetOutstandingPosition_ReturnsCorrectTuple()
    {
        PurchaseNoteRepo.GetOutstandingPositionAsync(Arg.Any<CancellationToken>())
            .Returns((3, 15000m));
        var service = CreateService();

        var (count, amount) = await service.GetOutstandingPositionAsync();

        count.Should().Be(3);
        amount.Should().Be(15000m);
    }
}
