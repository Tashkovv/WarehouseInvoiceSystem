namespace WarehouseInvoiceSystem.Domain.Entities
{
    using WarehouseInvoiceSystem.Domain.Common;

    public class Product : AuditableEntity
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public string Unit { get; set; } = string.Empty;
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<InvoiceLine> InvoiceLines { get; set; } = [];
        public ICollection<StockLevel> StockLevels { get; set; } = [];
        public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = [];
    }
}
