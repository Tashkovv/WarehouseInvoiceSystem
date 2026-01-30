namespace WarehouseInvoiceSystem.Application.DTOs.Invoice
{
    using WarehouseInvoiceSystem.Domain.Enums;

    public class CreateInvoiceDto
    {
        public int CompanyId { get; set; }
        public InvoiceType Type { get; set; }
        public DateTime IssueDate { get; set; } = DateTime.Now;
        public DateTime DueDate { get; set; }
        public string? Notes { get; set; }
        public List<CreateInvoiceLineDto> LineItems { get; set; } = new();
    }
}
