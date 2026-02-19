namespace WarehouseInvoiceSystem.Domain.Entities
{
    using WarehouseInvoiceSystem.Domain.Common;

    public class StockLevel : AuditableEntity
    {
        public Guid ProductId { get; set; }
        public Guid WarehouseId { get; set; }

        public decimal Quantity { get; set; }
        public decimal ReservedQuantity { get; set; } = 0m;
        public decimal AvailableQuantity => Quantity - ReservedQuantity;

        public decimal? MinimumQuantity { get; set; }
        public decimal? ReorderPoint { get; set; }
        public DateTime LastRestockedAt { get; set; }

        public Product Product { get; set; } = null!;
        public Warehouse Warehouse { get; set; } = null!;
    }
}
