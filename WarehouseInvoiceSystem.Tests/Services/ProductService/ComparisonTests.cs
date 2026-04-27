namespace WarehouseInvoiceSystem.Tests.Services.ProductService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Application.DTOs.Product;
using WarehouseInvoiceSystem.Domain.Enums;
using WarehouseInvoiceSystem.Domain.Queries.Results;

public class ComparisonTests : ProductServiceTestBase
{
    // ── GetPartnerComparisonAsync ────────────────────────────────────────────

    [Fact]
    public async Task PartnerComparison_Individuals_QueriesPurchaseNotes()
    {
        var productId = Guid.NewGuid();
        var individualId = Guid.NewGuid();

        PurchaseNoteRepo.GetIndividualAggregatesForProductAsync(
            productId, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<PartnerSummary>
            {
                new(individualId, "John Doe", DocumentCount: 1, TotalQuantity: 20m, TotalAmount: 600m, AvgUnitPrice: 30m)
            });

        var service = CreateService();

        var result = await service.GetPartnerComparisonAsync(productId, PartnerComparisonMode.Individuals, null, null, null);

        result.Should().HaveCount(1);
        result[0].PartnerId.Should().Be(individualId);
        result[0].TotalQuantity.Should().Be(20m);
        result[0].DocumentCount.Should().Be(1);
    }

    [Fact]
    public async Task PartnerComparison_Vendors_QueriesPayableInvoices()
    {
        var productId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        InvoiceRepo.GetCompanyAggregatesForProductAsync(
            productId, InvoiceType.Payable, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<PartnerSummary>
            {
                new(companyId, "Vendor Corp", DocumentCount: 1, TotalQuantity: 10m, TotalAmount: 500m, AvgUnitPrice: 50m)
            });

        var service = CreateService();

        var result = await service.GetPartnerComparisonAsync(productId, PartnerComparisonMode.Vendors, null, null, null);

        result.Should().HaveCount(1);
        result[0].PartnerId.Should().Be(companyId);
        result[0].TotalQuantity.Should().Be(10m);
    }

    [Fact]
    public async Task PartnerComparison_Clients_QueriesReceivableInvoices()
    {
        var productId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        InvoiceRepo.GetCompanyAggregatesForProductAsync(
            productId, InvoiceType.Receivable, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<PartnerSummary>
            {
                new(companyId, "Client Corp", DocumentCount: 2, TotalQuantity: 5m, TotalAmount: 500m, AvgUnitPrice: 100m)
            });

        var service = CreateService();

        var result = await service.GetPartnerComparisonAsync(productId, PartnerComparisonMode.Clients, null, null, null);

        result.Should().HaveCount(1);
        result[0].PartnerId.Should().Be(companyId);
        result[0].DocumentCount.Should().Be(2);
    }

    [Fact]
    public async Task PartnerComparison_FiltersByPartnerIds_PassedToRepository()
    {
        var productId = Guid.NewGuid();
        var includedId = Guid.NewGuid();

        PurchaseNoteRepo.GetIndividualAggregatesForProductAsync(
            productId, null, Arg.Is<IEnumerable<Guid>?>(ids => ids != null && ids.Contains(includedId)),
            null, null, Arg.Any<CancellationToken>())
            .Returns(new List<PartnerSummary>
            {
                new(includedId, "Included Person", DocumentCount: 1, TotalQuantity: 10m, TotalAmount: 300m, AvgUnitPrice: 30m)
            });

        var service = CreateService();

        var result = await service.GetPartnerComparisonAsync(
            productId, PartnerComparisonMode.Individuals, null, null, null,
            partnerIds: [includedId]);

        result.Should().HaveCount(1);
        result[0].PartnerId.Should().Be(includedId);
    }

    [Fact]
    public async Task PartnerComparison_UnknownMode_ReturnsEmpty()
    {
        var service = CreateService();

        var result = await service.GetPartnerComparisonAsync(
            Guid.NewGuid(), (PartnerComparisonMode)999, null, null, null);

        result.Should().BeEmpty();
    }

    // ── GetProductComparisonAsync ────────────────────────────────────────────

    [Fact]
    public async Task ProductComparison_LessThan2Products_ReturnsEmpty()
    {
        var service = CreateService();

        var result = await service.GetProductComparisonAsync([Guid.NewGuid()], null, null, null);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ProductComparison_AggregatesAllThreeSources()
    {
        var product1 = CreateEntity();
        var product2 = CreateEntity();
        product2.Code = "P002";
        product2.Name = "Product 2";
        SetEntityId(product2, Guid.NewGuid());

        var productIds = new List<Guid> { product1.Id, product2.Id };

        ProductRepo.GetByIdsAsync(productIds, Arg.Any<CancellationToken>())
            .Returns(new[] { product1, product2 });

        // Purchase notes (incoming from individuals) → product1: 10 qty, 300 amt
        PurchaseNoteRepo.GetProductsPurchaseAggregatesAsync(productIds, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<ProductSummary>
            {
                new(product1.Id, DocumentCount: 1, TotalQuantity: 10m, TotalAmount: 300m)
            });

        // Payable invoices (incoming from vendors) → product2: 5 qty, 200 amt
        InvoiceRepo.GetProductsInvoiceAggregatesAsync(productIds, InvoiceType.Payable, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<ProductSummary>
            {
                new(product2.Id, DocumentCount: 1, TotalQuantity: 5m, TotalAmount: 200m)
            });

        // Receivable invoices (outgoing to clients) → product1: 8 qty, 800 amt
        InvoiceRepo.GetProductsInvoiceAggregatesAsync(productIds, InvoiceType.Receivable, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<ProductSummary>
            {
                new(product1.Id, DocumentCount: 1, TotalQuantity: 8m, TotalAmount: 800m)
            });

        var service = CreateService();

        var result = await service.GetProductComparisonAsync(productIds, null, null, null);

        result.Should().HaveCount(2);

        var p1Result = result.First(r => r.ProductId == product1.Id);
        p1Result.IncomingQuantity.Should().Be(10); // from purchase note
        p1Result.IncomingAmount.Should().Be(300m);
        p1Result.OutgoingQuantity.Should().Be(8); // from receivable invoice
        p1Result.OutgoingAmount.Should().Be(800m);
        p1Result.DocumentCount.Should().Be(2); // 1 PN + 1 receivable

        var p2Result = result.First(r => r.ProductId == product2.Id);
        p2Result.IncomingQuantity.Should().Be(5); // from payable invoice
        p2Result.IncomingAmount.Should().Be(200m);
        p2Result.OutgoingQuantity.Should().Be(0); // no receivable
        p2Result.DocumentCount.Should().Be(1); // 1 payable
    }

    [Fact]
    public async Task ProductComparison_SkipsProductsMissingFromRepository()
    {
        var product1 = CreateEntity();
        var unknownId = Guid.NewGuid();
        var productIds = new List<Guid> { product1.Id, unknownId };

        ProductRepo.GetByIdsAsync(productIds, Arg.Any<CancellationToken>())
            .Returns(new[] { product1 });

        PurchaseNoteRepo.GetProductsPurchaseAggregatesAsync(productIds, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<ProductSummary>());
        InvoiceRepo.GetProductsInvoiceAggregatesAsync(productIds, InvoiceType.Payable, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<ProductSummary>());
        InvoiceRepo.GetProductsInvoiceAggregatesAsync(productIds, InvoiceType.Receivable, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<ProductSummary>());

        var service = CreateService();

        var result = await service.GetProductComparisonAsync(productIds, null, null, null);

        result.Should().HaveCount(1);
        result[0].ProductId.Should().Be(product1.Id);
    }

}
