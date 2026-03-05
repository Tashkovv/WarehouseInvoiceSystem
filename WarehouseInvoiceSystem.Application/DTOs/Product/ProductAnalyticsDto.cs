namespace WarehouseInvoiceSystem.Application.DTOs.Product
{
    public class ProductAnalyticsDto
    {
        // Stock Information
        public decimal TotalStockAcrossWarehouses { get; set; }
        public decimal TotalStockValue { get; set; }
        public List<WarehouseStockDto> StockByWarehouse { get; set; } = [];
        public bool HasLowStock { get; set; }
        public bool IsOutOfStock { get; set; }

        // Sales Analytics (Receivable Invoices - Outbound)
        public int TotalUnitsSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public DateTime? LastSaleDate { get; set; }
        public decimal AverageSaleQuantity { get; set; }
        public string? TopCustomer { get; set; }
        public decimal TopCustomerQuantity { get; set; }

        // Purchase Analytics (Purchase Notes - Inbound)
        public int TotalUnitsPurchased { get; set; }
        public decimal TotalPurchaseCost { get; set; }
        public DateTime? LastPurchaseDate { get; set; }
        public decimal AveragePurchaseQuantity { get; set; }
        public string? TopSupplier { get; set; }
        public decimal TopSupplierQuantity { get; set; }

        // Profitability
        public decimal AveragePurchasePrice { get; set; }
        public decimal CurrentSellingPrice { get; set; }
        public decimal GrossMarginPercentage { get; set; }
        public decimal EstimatedProfitIfSoldAll { get; set; }

        // Movement
        public int TotalTransactions { get; set; }
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