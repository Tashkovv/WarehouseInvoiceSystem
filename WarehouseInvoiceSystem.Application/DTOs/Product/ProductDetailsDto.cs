namespace WarehouseInvoiceSystem.Application.DTOs.Product
{
    /// <summary>
    /// Stock-only payload returned by GetProductDetailsAsync. Profitability and transaction
    /// totals are period-scoped and live in ProductTransactionSummaryDto.
    /// </summary>
    public class ProductDetailsDto
    {
        public decimal TotalStockAcrossWarehouses { get; set; }
        public List<WarehouseStockDto> StockByWarehouse { get; set; } = [];
        public bool HasLowStock { get; set; }
    }

    public class WarehouseTransactionSummaryDto
    {
        public Guid WarehouseId { get; set; }
        public int Count { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageUnitPrice { get; set; }
    }

    /// <summary>
    /// Period-scoped aggregate returned by GetProductTransactionSummaryAsync.
    /// Drives both the Profitability and Transaction History sections on the product page.
    /// </summary>
    public class ProductTransactionSummaryDto
    {
        public List<WarehouseTransactionSummaryDto> PurchasedByWarehouse { get; set; } = [];
        public List<WarehouseTransactionSummaryDto> SoldByWarehouse { get; set; } = [];
        public int TotalPurchasedCount { get; set; }
        public decimal TotalPurchasedQuantity { get; set; }
        public decimal TotalPurchasedAmount { get; set; }
        public int TotalSoldCount { get; set; }
        public decimal TotalSoldQuantity { get; set; }
        public decimal TotalSoldAmount { get; set; }
        public decimal AveragePurchasePrice { get; set; }
        public decimal AverageSellingPrice { get; set; }
    }
}
