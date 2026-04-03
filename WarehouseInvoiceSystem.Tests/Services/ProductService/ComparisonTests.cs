namespace WarehouseInvoiceSystem.Tests.Services.ProductService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Application.DTOs.Product;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;

public class ComparisonTests : ProductServiceTestBase
{
    // ── GetPartnerComparisonAsync ────────────────────────────────────────────

    [Fact]
    public async Task PartnerComparison_Individuals_QueriesPurchaseNotes()
    {
        var productId = Guid.NewGuid();
        var individualId = Guid.NewGuid();
        var pnId = Guid.NewGuid();

        var line = new PurchaseNoteLine
        {
            PurchaseNoteId = pnId,
            ProductId = productId,
            Quantity = 20,
            UnitPrice = 30m,
            PurchaseNote = new PurchaseNote
            {
                IndividualId = individualId,
                WarehouseId = Guid.NewGuid(),
                Individual = new Individual { FirstName = "John", LastName = "Doe" },
                Warehouse = new Warehouse { Name = "WH1" }
            },
            Product = CreateEntity()
        };
        SetEntityId(line, Guid.NewGuid());

        PurchaseNoteRepo.GetLineItemsByProductIdAsync(productId, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new[] { line });
        var service = CreateService();

        var result = await service.GetPartnerComparisonAsync(productId, PartnerComparisonMode.Individuals, null, null, null);

        result.Should().HaveCount(1);
        result[0].PartnerId.Should().Be(individualId);
        result[0].TotalQuantity.Should().Be(20);
        result[0].DocumentCount.Should().Be(1);
    }

    [Fact]
    public async Task PartnerComparison_Vendors_QueriesPayableInvoices()
    {
        var productId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        var line = CreateComparisonInvoiceLine(productId, invoiceId, companyId, InvoiceType.Payable, 10, 50m);
        InvoiceRepo.GetLineItemsByProductIdAsync(productId, InvoiceType.Payable, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new[] { line });
        var service = CreateService();

        var result = await service.GetPartnerComparisonAsync(productId, PartnerComparisonMode.Vendors, null, null, null);

        result.Should().HaveCount(1);
        result[0].PartnerId.Should().Be(companyId);
        result[0].TotalQuantity.Should().Be(10);
    }

    [Fact]
    public async Task PartnerComparison_Clients_QueriesReceivableInvoices()
    {
        var productId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        var line = CreateComparisonInvoiceLine(productId, invoiceId, companyId, InvoiceType.Receivable, 5, 100m);
        InvoiceRepo.GetLineItemsByProductIdAsync(productId, InvoiceType.Receivable, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new[] { line });
        var service = CreateService();

        var result = await service.GetPartnerComparisonAsync(productId, PartnerComparisonMode.Clients, null, null, null);

        result.Should().HaveCount(1);
        result[0].PartnerId.Should().Be(companyId);
    }

    [Fact]
    public async Task PartnerComparison_FiltersByPartnerIds()
    {
        var productId = Guid.NewGuid();
        var includedId = Guid.NewGuid();
        var excludedId = Guid.NewGuid();

        var line1 = new PurchaseNoteLine
        {
            PurchaseNoteId = Guid.NewGuid(),
            ProductId = productId,
            Quantity = 10,
            UnitPrice = 30m,
            PurchaseNote = new PurchaseNote
            {
                IndividualId = includedId,
                WarehouseId = Guid.NewGuid(),
                Individual = new Individual { FirstName = "Included", LastName = "Person" },
                Warehouse = new Warehouse { Name = "WH1" }
            },
            Product = CreateEntity()
        };
        SetEntityId(line1, Guid.NewGuid());

        var line2 = new PurchaseNoteLine
        {
            PurchaseNoteId = Guid.NewGuid(),
            ProductId = productId,
            Quantity = 5,
            UnitPrice = 20m,
            PurchaseNote = new PurchaseNote
            {
                IndividualId = excludedId,
                WarehouseId = Guid.NewGuid(),
                Individual = new Individual { FirstName = "Excluded", LastName = "Person" },
                Warehouse = new Warehouse { Name = "WH1" }
            },
            Product = CreateEntity()
        };
        SetEntityId(line2, Guid.NewGuid());

        PurchaseNoteRepo.GetLineItemsByProductIdAsync(productId, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new[] { line1, line2 });
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

        // Purchase note lines (incoming from individuals)
        var pnLine = new PurchaseNoteLine
        {
            PurchaseNoteId = Guid.NewGuid(),
            ProductId = product1.Id,
            Quantity = 10,
            UnitPrice = 30m,
            Product = product1,
            PurchaseNote = new PurchaseNote
            {
                IndividualId = Guid.NewGuid(),
                WarehouseId = Guid.NewGuid(),
                Warehouse = new Warehouse { Name = "WH1" }
            }
        };
        SetEntityId(pnLine, Guid.NewGuid());

        // Payable invoice lines (incoming from vendors)
        var payLine = CreateComparisonInvoiceLine(product2.Id, Guid.NewGuid(), Guid.NewGuid(), InvoiceType.Payable, 5, 40m);
        payLine.Product = product2;

        // Receivable invoice lines (outgoing to clients)
        var recLine = CreateComparisonInvoiceLine(product1.Id, Guid.NewGuid(), Guid.NewGuid(), InvoiceType.Receivable, 8, 100m);
        recLine.Product = product1;

        PurchaseNoteRepo.GetLineItemsByProductIdsAsync(productIds, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new[] { pnLine });
        InvoiceRepo.GetLineItemsByProductIdsAsync(productIds, null, null, null, InvoiceType.Payable, Arg.Any<CancellationToken>())
            .Returns(new[] { payLine });
        InvoiceRepo.GetLineItemsByProductIdsAsync(productIds, null, null, null, InvoiceType.Receivable, Arg.Any<CancellationToken>())
            .Returns(new[] { recLine });
        var service = CreateService();

        var result = await service.GetProductComparisonAsync(productIds, null, null, null);

        result.Should().HaveCount(2);

        var p1Result = result.First(r => r.ProductId == product1.Id);
        p1Result.IncomingQuantity.Should().Be(10); // from purchase note
        p1Result.OutgoingQuantity.Should().Be(8); // from receivable invoice

        var p2Result = result.First(r => r.ProductId == product2.Id);
        p2Result.IncomingQuantity.Should().Be(5); // from payable invoice
        p2Result.OutgoingQuantity.Should().Be(0); // no receivable
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static InvoiceLine CreateComparisonInvoiceLine(Guid productId, Guid invoiceId, Guid companyId,
        InvoiceType type, int quantity, decimal unitPrice)
    {
        var invoice = new Invoice
        {
            InvoiceNumber = "INV-001",
            CompanyId = companyId,
            WarehouseId = Guid.NewGuid(),
            Type = type,
            Status = InvoiceStatus.Confirmed,
            Company = new Company { Name = "Test Company", Email = "test@test.com" },
            Warehouse = new Warehouse { Name = "WH1" },
            LineItems = []
        };
        SetEntityId(invoice, invoiceId);

        var line = new InvoiceLine
        {
            InvoiceId = invoiceId,
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            TaxRate = 0,
            DiscountPercentage = 0,
            Invoice = invoice,
            Product = new Product { Code = "P001", Name = "Test", Unit = "kg" }
        };
        SetEntityId(line, Guid.NewGuid());
        return line;
    }
}
