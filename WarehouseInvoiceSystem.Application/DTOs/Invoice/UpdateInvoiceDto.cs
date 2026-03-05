namespace WarehouseInvoiceSystem.Application.DTOs.Invoice
{
    using WarehouseInvoiceSystem.Domain.Enums;

    public class UpdateInvoiceDto
    {
        public Guid CompanyId { get; set; }
        public Guid WarehouseId { get; set; }
        public InvoiceType Type { get; set; }
        public InvoiceStatus Status { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public string? Notes { get; set; }
        public List<UpdateInvoiceLineDto> LineItems { get; set; } = new();
    }
}
