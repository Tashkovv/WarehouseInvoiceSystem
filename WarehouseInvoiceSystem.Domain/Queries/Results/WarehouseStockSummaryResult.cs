namespace WarehouseInvoiceSystem.Domain.Queries.Results
{
    public class WarehouseStockSummaryResult
    {
        public int TotalProducts { get; set; }
        public int InStockCount { get; set; }
        public decimal TotalStockValue { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public int WarehouseCount { get; set; }
    }
}
