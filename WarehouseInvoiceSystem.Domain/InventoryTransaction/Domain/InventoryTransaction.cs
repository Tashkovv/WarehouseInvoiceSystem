namespace WarehouseInvoiceSystem.Domain.InventoryTransaction.Domain
{
    using WarehouseInvoiceSystem.Domain.Common;
    using WarehouseInvoiceSystem.Domain.InventoryTransaction.Enums;

    public class InventoryTransaction : AuditableEntity
    {
        public Guid ProductId { get; set; }

        public Guid WarehouseId { get; set; }

        public InventoryTransactionType Type { get; set; }

        public decimal Quantity { get; set; }  // always positive

        public Guid? SourceDocumentId { get; set; }

        public string? SourceDocumentType { get; set; }  // Examples: "Invoice", "ManualAdjustment"

        public string? Note { get; set; }
    }
}
