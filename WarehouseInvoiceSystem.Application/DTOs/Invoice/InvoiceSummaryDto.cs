namespace WarehouseInvoiceSystem.Application.DTOs.Invoice
{
    public class InvoiceSummaryDto
    {
        public int TotalInvoices { get; set; }
        public int PaidInvoices { get; set; }
        public int UnpaidInvoices { get; set; }
        public int OverdueInvoices { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalDue { get; set; }
    }
}
