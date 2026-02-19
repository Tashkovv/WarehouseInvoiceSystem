namespace WarehouseInvoiceSystem.Domain.Entities
{
    using WarehouseInvoiceSystem.Domain.Common;

    public class Product : AuditableEntity
    {
        public string Code { get; set; } = string.Empty;  // SKU / internal code
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public string Unit { get; set; } = string.Empty;  // pcs, kg, l
        public decimal DefaultPrice { get; set; }

        public bool IsActive { get; set; }
    }
}
