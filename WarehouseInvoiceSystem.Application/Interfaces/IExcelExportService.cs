namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Application.DTOs.Invoice;

    public interface IExcelExportService
    {
        Task<byte[]> ExportInvoiceForPrintingAsync(int invoiceId);
        Task<byte[]> ExportInvoicesToExcelAsync(List<InvoiceDto> invoicesToExport);
        Task<byte[]> ExportInvoicesByDateRangeAsync(List<InvoiceDto> invoicesToExport, DateTime startDate, DateTime endDate);
    }
}
