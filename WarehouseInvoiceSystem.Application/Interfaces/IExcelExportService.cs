namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Application.DTOs.Invoice;
    using WarehouseInvoiceSystem.Application.DTOs.PurchaseNote;
    using WarehouseInvoiceSystem.Application.Models;

    public interface IExcelExportService
    {
        Task<byte[]> ExportListToExcelAsync<T>(IEnumerable<T> data, IReadOnlyList<ExportColumn<T>> columns, ExportListOptions options);
        Task<byte[]> ExportInvoiceForPrintingAsync(Guid invoiceId);
        Task<byte[]> ExportInvoiceForPrintingAsync(InvoiceDto invoice);
        Task<byte[]> ExportPurchaseNoteForPrintingAsync(PurchaseNoteDto purchaseNote);

        [Obsolete("Use ExportListToExcelAsync with ExportColumnDefinitions.InvoiceColumns instead")]
        Task<byte[]> ExportInvoicesToExcelAsync(IEnumerable<InvoiceDto> invoicesToExport);
        [Obsolete("Use ExportListToExcelAsync with ExportColumnDefinitions.InvoiceColumns instead")]
        Task<byte[]> ExportInvoicesByDateRangeAsync(List<InvoiceDto> invoicesToExport, DateTime startDate, DateTime endDate);
    }
}
