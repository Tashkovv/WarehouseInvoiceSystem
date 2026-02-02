namespace WarehouseInvoiceSystem.Domain.Entities
{
    public class InvoiceLine
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxRate { get; set; } = 0; // Percentage (e.g., 10 for 10%)
        public decimal Amount => Quantity * UnitPrice;
        public decimal TaxAmount => Amount * (TaxRate / 100);
        public decimal TotalAmount => Amount + TaxAmount;
        public DateTime? DeletedOn { get; set; }

        // Navigation property
        public Invoice Invoice { get; set; } = null!;
    }
}
