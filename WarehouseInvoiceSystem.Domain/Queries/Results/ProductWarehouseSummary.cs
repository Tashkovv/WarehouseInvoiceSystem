namespace WarehouseInvoiceSystem.Domain.Queries.Results
{
    public record ProductWarehouseSummary(
        Guid WarehouseId,
        int Count,
        decimal TotalQuantity,
        decimal TotalAmount,
        decimal AvgUnitPrice);
}
