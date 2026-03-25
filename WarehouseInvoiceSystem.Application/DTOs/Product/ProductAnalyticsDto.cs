namespace WarehouseInvoiceSystem.Application.DTOs.Product
{
    public class ProductAnalyticsDto
    {
        // Stock Information
        public decimal TotalStockAcrossWarehouses { get; set; }
        public List<WarehouseStockDto> StockByWarehouse { get; set; } = [];
        public bool HasLowStock { get; set; }

        // Profitability
        public decimal AverageSellingPrice { get; set; }
        public decimal GrossMarginPercentage { get; set; }

        // Movement
        public List<RecentTransactionDto> RecentTransactions { get; set; } = [];
    }

    public class WarehouseStockDto
    {
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal ReservedQuantity { get; set; }
        public decimal AvailableQuantity { get; set; }
        public decimal? MinimumQuantity { get; set; }
        public decimal? ReorderPoint { get; set; }
        public DateTime? LastRestockedAt { get; set; }
    }

    public class RecentTransactionDto
    {
        public DateTime Date { get; set; }
        public string Type { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string? SourceDocument { get; set; }
        public string? Note { get; set; }
    }
}