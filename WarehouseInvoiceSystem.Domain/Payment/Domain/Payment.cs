namespace WarehouseInvoiceSystem.Domain.Payment.Domain
{
    using WarehouseInvoiceSystem.Domain.Invoice.Domain;
    using WarehouseInvoiceSystem.Domain.Payment.Enums;

    public class Payment
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Notes { get; set; }
        public string? RecordedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? DeletedOn { get; set; }

        // Navigation property
        public Invoice Invoice { get; set; } = null!;
    }
}
