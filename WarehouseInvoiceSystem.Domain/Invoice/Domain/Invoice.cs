namespace WarehouseInvoiceSystem.Domain.Invoice.Domain
{
    using WarehouseInvoiceSystem.Domain.Company.Domain;
    using WarehouseInvoiceSystem.Domain.Invoice.Enums;
    using WarehouseInvoiceSystem.Domain.Payment.Domain;

    public class Invoice
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int CompanyId { get; set; }
        public InvoiceType Type { get; set; }
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
        public DateTime IssueDate { get; set; } = DateTime.Now;
        public DateTime DueDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal AmountDue => TotalAmount - AmountPaid;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? DeletedOn { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }

        // Navigation properties
        public Company Company { get; set; } = null!;
        public ICollection<InvoiceLine> LineItems { get; set; } = [];
        public ICollection<Payment> Payments { get; set; } = [];
    }
}
