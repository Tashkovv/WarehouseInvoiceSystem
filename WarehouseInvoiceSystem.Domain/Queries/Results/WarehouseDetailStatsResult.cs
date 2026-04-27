namespace WarehouseInvoiceSystem.Domain.Queries.Results
{
    public record WarehouseDetailStatsResult(
        int ProductCount,
        int LowStockCount,
        decimal TotalValue);
}
