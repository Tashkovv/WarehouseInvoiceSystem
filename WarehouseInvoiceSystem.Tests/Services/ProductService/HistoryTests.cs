namespace WarehouseInvoiceSystem.Tests.Services.ProductService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;
using WarehouseInvoiceSystem.Domain.Queries;
using WarehouseInvoiceSystem.Domain.Queries.Common;
using WarehouseInvoiceSystem.Domain.Queries.Results;

public class HistoryTests : ProductServiceTestBase
{
    [Fact]
    public async Task Sold_RoutesToInvoiceRepository()
    {
        var query = new GetProductHistoryQuery { ProductId = Guid.NewGuid(), Purchased = false, Page = 1, PageSize = 10 };
        var warehouseId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        var invoice = new Invoice
        {
            InvoiceNumber = "INV-001",
            CompanyId = Guid.NewGuid(),
            WarehouseId = warehouseId,
            Type = InvoiceType.Receivable,
            Status = InvoiceStatus.Confirmed,
            IssueDate = DateTime.Today,
            Company = new Company { Name = "Client Co", Email = "c@c.com" },
            Warehouse = new Warehouse { Name = "Main" },
            LineItems = []
        };
        SetEntityId(invoice, invoiceId);

        var line = new InvoiceLine
        {
            InvoiceId = invoiceId,
            ProductId = query.ProductId,
            Quantity = 5,
            UnitPrice = 100m,
            TaxRate = 0,
            DiscountPercentage = 0,
            Invoice = invoice,
            Product = CreateEntity()
        };

        InvoiceRepo.GetPagedLineItemsByProductIdAsync(query, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<InvoiceLine>
            {
                Items = [line],
                TotalCount = 1,
                Page = 1,
                PageSize = 10
            });
        var service = CreateService();

        var result = await service.GetPagedProductHistoryAsync(query);

        result.Items.Should().HaveCount(1);
        result.Items[0].DocumentNumber.Should().Be("INV-001");
        result.Items[0].DocumentType.Should().Be("Invoice");
        result.Items[0].PartyName.Should().Be("Client Co");
        result.Items[0].Quantity.Should().Be(5);
    }

    [Fact]
    public async Task Purchased_RoutesToPurchasedHistoryView()
    {
        var query = new GetProductHistoryQuery { ProductId = Guid.NewGuid(), Purchased = true, Page = 1, PageSize = 10 };
        var warehouseId = Guid.NewGuid();

        InvoiceRepo.GetPagedPurchasedHistoryAsync(query, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ProductPurchaseHistoryView>
            {
                Items =
                [
                    new ProductPurchaseHistoryView
                    {
                        ProductId = query.ProductId,
                        Date = DateTime.Today,
                        DocumentNumber = "PN-001",
                        DocumentUrl = "/purchase-notes/abc",
                        PartyName = "John Doe",
                        WarehouseId = warehouseId,
                        WarehouseName = "Main",
                        Quantity = 20,
                        UnitPrice = 50m,
                        TotalPrice = 1000m
                    }
                ],
                TotalCount = 1,
                Page = 1,
                PageSize = 10
            });
        var service = CreateService();

        var result = await service.GetPagedProductHistoryAsync(query);

        result.Items.Should().HaveCount(1);
        var row = result.Items[0];
        row.DocumentNumber.Should().Be("PN-001");
        row.DocumentType.Should().Be("PurchaseNote");
        row.PartyName.Should().Be("John Doe");
        row.Quantity.Should().Be(20);
    }

    [Fact]
    public async Task Purchased_InvoiceUrl_SetsDocumentTypeToInvoice()
    {
        var query = new GetProductHistoryQuery { ProductId = Guid.NewGuid(), Purchased = true, Page = 1, PageSize = 10 };

        InvoiceRepo.GetPagedPurchasedHistoryAsync(query, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ProductPurchaseHistoryView>
            {
                Items =
                [
                    new ProductPurchaseHistoryView
                    {
                        DocumentUrl = "/invoices/abc",
                        DocumentNumber = "PAY-001",
                        PartyName = "Vendor",
                        WarehouseId = Guid.NewGuid(),
                        WarehouseName = "WH",
                        Quantity = 10,
                        UnitPrice = 30m,
                        TotalPrice = 300m
                    }
                ],
                TotalCount = 1,
                Page = 1,
                PageSize = 10
            });
        var service = CreateService();

        var result = await service.GetPagedProductHistoryAsync(query);

        result.Items[0].DocumentType.Should().Be("Invoice");
    }

    [Fact]
    public async Task Totals_Purchased_RoutesToPurchasedTotals()
    {
        var query = new GetProductHistoryQuery { ProductId = Guid.NewGuid(), Purchased = true };
        InvoiceRepo.GetPurchasedHistoryTotalsAsync(query, Arg.Any<CancellationToken>())
            .Returns((100m, 5000m));
        var service = CreateService();

        var (totalQty, totalAmt) = await service.GetProductHistoryTotalsAsync(query);

        totalQty.Should().Be(100m);
        totalAmt.Should().Be(5000m);
    }

    [Fact]
    public async Task Totals_Sold_RoutesToSoldTotals()
    {
        var query = new GetProductHistoryQuery { ProductId = Guid.NewGuid(), Purchased = false };
        InvoiceRepo.GetSoldHistoryTotalsAsync(query, Arg.Any<CancellationToken>())
            .Returns((50m, 10000m));
        var service = CreateService();

        var (totalQty, totalAmt) = await service.GetProductHistoryTotalsAsync(query);

        totalQty.Should().Be(50m);
        totalAmt.Should().Be(10000m);
    }
}
