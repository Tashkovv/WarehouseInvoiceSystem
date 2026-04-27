namespace WarehouseInvoiceSystem.Domain.Queries.Results
{
    public record ProductSummary(
        Guid ProductId,
        int DocumentCount,
        decimal TotalQuantity,
        decimal TotalAmount);
}
