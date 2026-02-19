namespace WarehouseInvoiceSystem.Application.DTOs.Invoice
{
    using WarehouseInvoiceSystem.Domain.Enums;

    public class CreateInvoiceDto
    {
        public Guid CompanyId { get; set; }
        public InvoiceType Type { get; set; }
        public DateTime IssueDate { get; set; } = DateTime.UtcNow;
        public DateTime DueDate { get; set; }
        public string? Notes { get; set; }
        public List<CreateInvoiceLineDto> LineItems { get; set; } = new();
    }
}
