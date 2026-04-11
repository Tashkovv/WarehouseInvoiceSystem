namespace WarehouseInvoiceSystem.Application.Models
{
    using WarehouseInvoiceSystem.Application.DTOs.InventoryTransaction;
    using WarehouseInvoiceSystem.Application.DTOs.Invoice;
    using WarehouseInvoiceSystem.Application.DTOs.Product;
    using WarehouseInvoiceSystem.Application.DTOs.PurchaseNote;
    using WarehouseInvoiceSystem.Application.DTOs.StockLevel;
    using WarehouseInvoiceSystem.Application.Interfaces;

    public static class ExportColumnDefinitions
    {
        public static IReadOnlyList<ExportColumn<InvoiceDto>> InvoiceColumns(ILocalizationService t) =>
        [
            new(t.GetString("InvoiceNumber"), x => x.InvoiceNumber),
            new(t.GetString("Company"), x => x.CompanyName),
            new(t.GetString("Type"), x => t.GetString(x.Type.ToString())),
            new(t.GetString("Status"), x => t.GetString(x.Status.ToString())),
            new(t.GetString("IssueDate"), x => x.IssueDate, ExportColumnType.Date),
            new(t.GetString("DueDate"), x => x.DueDate, ExportColumnType.Date),
            new(t.GetString("Subtotal"), x => x.SubTotal, ExportColumnType.Currency, IncludeInTotals: true),
            new(t.GetString("Tax"), x => x.TaxAmount, ExportColumnType.Currency, IncludeInTotals: true),
            new(t.GetString("Total"), x => x.TotalAmount, ExportColumnType.Currency, IncludeInTotals: true),
            new(t.GetString("AmountPaid"), x => x.AmountPaid, ExportColumnType.Currency, IncludeInTotals: true),
            new(t.GetString("AmountDue"), x => x.AmountDue, ExportColumnType.Currency, IncludeInTotals: true),
        ];

        public static IReadOnlyList<ExportColumn<PurchaseNoteDto>> PurchaseNoteColumns(ILocalizationService t) =>
        [
            new(t.GetString("NoteNumber"), x => x.NoteNumber),
            new(t.GetString("Individual"), x => x.IndividualFullName),
            new(t.GetString("Status"), x => t.GetString(x.Status.ToString())),
            new(t.GetString("PurchaseDate"), x => x.PurchaseDate, ExportColumnType.Date),
            new(t.GetString("Warehouse"), x => x.WarehouseName),
            new(t.GetString("Total"), x => x.TotalAmount, ExportColumnType.Currency, IncludeInTotals: true),
        ];

        public static IReadOnlyList<ExportColumn<StockLevelDto>> StockLevelColumns(ILocalizationService t) =>
        [
            new(t.GetString("Product"), x => $"{x.ProductCode} - {x.ProductName}"),
            new(t.GetString("Quantity"), x => $"{x.Quantity:N0} {x.ProductUnit}"),
            new(t.GetString("LastRestocked"), x => x.LastRestockedAt, ExportColumnType.Date),
            new(t.GetString("MinimumQuantity"), x => x.MinimumQuantity, ExportColumnType.Number),
            new(t.GetString("UnitPrice"), x => x.UnitPrice, ExportColumnType.Currency),
            new(t.GetString("TotalValue"), x => x.TotalValue, ExportColumnType.Currency, IncludeInTotals: true),
        ];

        public static IReadOnlyList<ExportColumn<InventoryTransactionDto>> TransactionColumns(ILocalizationService t, string productUnit) =>
        [
            new(t.GetString("CreatedAt"), x => x.CreatedAt, ExportColumnType.Date),
            new(t.GetString("Warehouse"), x => x.WarehouseName),
            new(t.GetString("Quantity"), x => $"{x.Quantity:N0} {productUnit}"),
            new(t.GetString("Type"), x => t.GetString(x.Type.ToString())),
            new(t.GetString("Note"), x => x.Note),
        ];

        public static IReadOnlyList<ExportColumn<PartnerComparisonDto>> PartnerComparisonColumns(ILocalizationService t, string productUnit) =>
        [
            new(t.GetString("Partner"), x => x.PartnerName),
            new(t.GetString("Quantity"), x => $"{x.TotalQuantity:N0} {productUnit}"),
            new(t.GetString("Amount"), x => x.TotalAmount, ExportColumnType.Currency),
            new(t.GetString("AveragePrice"), x => x.AverageUnitPrice, ExportColumnType.Currency),
            new(t.GetString("Documents"), x => x.DocumentCount, ExportColumnType.Number),
        ];

        public static IReadOnlyList<ExportColumn<ProductComparisonDto>> ProductComparisonColumns(ILocalizationService t) =>
        [
            new(t.GetString("Product"), x => $"{x.ProductCode} — {x.ProductName}"),
            new(t.GetString("Incoming"), x => $"{x.IncomingQuantity:N0} {x.ProductUnit}"),
            new(t.GetString("IncomingAmount"), x => x.IncomingAmount, ExportColumnType.Currency),
            new(t.GetString("Outgoing"), x => $"{x.OutgoingQuantity:N0} {x.ProductUnit}"),
            new(t.GetString("OutgoingAmount"), x => x.OutgoingAmount, ExportColumnType.Currency),
            new(t.GetString("Documents"), x => x.DocumentCount, ExportColumnType.Number),
        ];
    }
}
