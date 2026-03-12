namespace WarehouseInvoiceSystem.Application.DTOs.Product
{
    /// <summary>
    /// Full product detail payload returned by GetProductDetailsAsync.
    /// Contains stock data and pre-aggregated purchase/sale summaries per warehouse.
    /// All heavy aggregation is done in the service — no raw document lists cross this boundary.
    /// </summary>
    public class ProductDetailsDto
    {
        // ── Stock ─────────────────────────────────────────────────────────────
        public decimal TotalStockAcrossWarehouses { get; set; }
        public List<WarehouseStockDto> StockByWarehouse { get; set; } = [];
        public bool HasLowStock { get; set; }

        // ── Profitability ─────────────────────────────────────────────────────
        public decimal CurrentSellingPrice { get; set; }
        public decimal GrossMarginPercentage { get; set; }
        public decimal AveragePurchasePrice { get; set; }

        // ── Per-warehouse transaction summaries ───────────────────────────────
        /// <summary>Aggregated purchase totals per warehouse (purchase notes + payable invoices).</summary>
        public List<WarehouseTransactionSummaryDto> PurchasedByWarehouse { get; set; } = [];

        /// <summary>Aggregated sale totals per warehouse (receivable invoices).</summary>
        public List<WarehouseTransactionSummaryDto> SoldByWarehouse { get; set; } = [];

        // ── Global totals (across all warehouses) ─────────────────────────────
        public int TotalPurchasedCount { get; set; }
        public decimal TotalPurchasedQuantity { get; set; }
        public decimal TotalPurchasedAmount { get; set; }
        public int TotalSoldCount { get; set; }
        public decimal TotalSoldQuantity { get; set; }
        public decimal TotalSoldAmount { get; set; }
        public decimal TotalProfit { get; set; }
    }

    public class WarehouseTransactionSummaryDto
    {
        public Guid WarehouseId { get; set; }
        public int Count { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageUnitPrice { get; set; }
    }
}