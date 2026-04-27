namespace WarehouseInvoiceSystem.Domain.Queries.Results
{
    public record PartnerSummary(
        Guid PartnerId,
        string PartnerName,
        int DocumentCount,
        decimal TotalQuantity,
        decimal TotalAmount,
        decimal AvgUnitPrice);
}
