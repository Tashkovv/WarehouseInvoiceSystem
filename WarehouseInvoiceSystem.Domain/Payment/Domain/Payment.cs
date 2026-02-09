namespace WarehouseInvoiceSystem.Domain.Payment.Domain
{
    using WarehouseInvoiceSystem.Domain.Common;
    using WarehouseInvoiceSystem.Domain.Invoice.Domain;
    using WarehouseInvoiceSystem.Domain.Payment.Enums;

    public class Payment : AuditableEntity
    {
        public Guid InvoiceId { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Notes { get; set; }
        public string? RecordedBy { get; set; }

        // Navigation property
        public Invoice Invoice { get; set; } = null!;
    }
}
