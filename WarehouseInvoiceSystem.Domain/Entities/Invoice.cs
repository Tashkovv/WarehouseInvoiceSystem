namespace WarehouseInvoiceSystem.Domain.Entities
{
    using WarehouseInvoiceSystem.Domain.Common;
    using WarehouseInvoiceSystem.Domain.Enums;

    public class Invoice : AuditableEntity
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public Guid CompanyId { get; set; }
        public Guid WarehouseId { get; set; }
        public InvoiceType Type { get; set; }
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
        public DateTime IssueDate { get; set; } = DateTime.UtcNow;
        public DateTime DueDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal AmountDue => TotalAmount - AmountPaid;
        public string? Notes { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }

        // Navigation properties
        public Company Company { get; set; } = null!;
        public ICollection<InvoiceLine> LineItems { get; set; } = [];
        public ICollection<Payment> Payments { get; set; } = [];
        public Warehouse Warehouse { get; set; } = null!;
    }
}
