namespace WarehouseInvoiceSystem.Domain.Entities
{
    using WarehouseInvoiceSystem.Domain.Common;

    public class PurchaseNoteLine : AuditableEntity
    {
        public Guid PurchaseNoteId { get; set; }
        public Guid ProductId { get; set; }

        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount => Quantity * UnitPrice;

        // Navigation properties
        public PurchaseNote PurchaseNote { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}