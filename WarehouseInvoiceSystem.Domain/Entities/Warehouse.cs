namespace WarehouseInvoiceSystem.Domain.Entities
{
    using WarehouseInvoiceSystem.Domain.Common;

    public class Warehouse : AuditableEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<StockLevel> StockLevels { get; set; } = [];
        public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = [];
    }
}