namespace WarehouseInvoiceSystem.Domain.Warehouse.Domain
{
    using WarehouseInvoiceSystem.Domain.Common;

    public class Warehouse : AuditableEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }

        public bool IsDefault { get; set; }
    }
}
