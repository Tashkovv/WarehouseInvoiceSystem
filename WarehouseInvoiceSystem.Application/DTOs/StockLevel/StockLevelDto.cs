namespace WarehouseInvoiceSystem.Application.DTOs.StockLevel
{
    public class StockLevelDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductUnit { get; set; } = string.Empty;
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal ReservedQuantity { get; set; }
        public decimal AvailableQuantity { get; set; }
        public decimal? MinimumQuantity { get; set; }
        public decimal? ReorderPoint { get; set; }
        public DateTime LastRestockedAt { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalValue => Quantity * UnitPrice;
        public bool IsDefault { get; set; }
    }
}