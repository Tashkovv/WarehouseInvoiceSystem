namespace WarehouseInvoiceSystem.Domain.Entities
{
    using WarehouseInvoiceSystem.Domain.Common;

    public class InvoiceLine : AuditableEntity
    {
        public Guid InvoiceId { get; set; }
        public Guid ProductId { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxRate { get; set; } = 0m; // Percentage (e.g., 10 for 10%)
        public decimal Amount => Quantity * UnitPrice;
        public decimal TaxAmount => Amount * (TaxRate / 100);
        public decimal TotalAmount => Amount + TaxAmount;

        // Navigation property
        public Invoice Invoice { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
