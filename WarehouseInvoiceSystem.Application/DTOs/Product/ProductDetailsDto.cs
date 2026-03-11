namespace WarehouseInvoiceSystem.Application.DTOs.Product
{
    /// <summary>
    /// Full product detail payload returned by GetProductDetailsAsync.
    /// Combines what was previously two separate service calls (analytics + transaction history)
    /// into a single coordinated fetch.
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

        // ── Transaction history ───────────────────────────────────────────────
        public List<ProductTransactionRowDto> Purchased { get; set; } = [];
        public List<ProductTransactionRowDto> Sold { get; set; } = [];

        // ── Computed ──────────────────────────────────────────────────────────

        /// <summary>Total revenue from all sold lines minus total cost from all purchased lines.</summary>
        public decimal TotalProfit => Sold.Sum(r => r.TotalPrice) - Purchased.Sum(r => r.TotalPrice);

        /// <summary>Average unit price across all purchase lines. Returns 0 when there are no purchases.</summary>
        public decimal AveragePurchasePrice =>
            Purchased.Count > 0 ? Purchased.Average(r => r.UnitPrice) : 0;
    }
}
