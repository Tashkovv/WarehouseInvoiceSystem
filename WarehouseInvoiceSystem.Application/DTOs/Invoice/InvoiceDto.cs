namespace WarehouseInvoiceSystem.Application.DTOs.Invoice
{
    using WarehouseInvoiceSystem.Domain.Enums;

    public class InvoiceDto
    {
        public Guid Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyEmail { get; set; } = string.Empty;
        public InvoiceType Type { get; set; }
        public InvoiceStatus Status { get; set; }
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal AmountDue { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<InvoiceLineDto> LineItems { get; set; } = [];
    }
}
