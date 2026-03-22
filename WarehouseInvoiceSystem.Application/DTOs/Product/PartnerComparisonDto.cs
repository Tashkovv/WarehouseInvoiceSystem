namespace WarehouseInvoiceSystem.Application.DTOs.Product
{
    public record PartnerComparisonDto(
        Guid PartnerId,
        string PartnerName,
        decimal TotalQuantity,
        decimal TotalAmount,
        decimal AverageUnitPrice,
        int DocumentCount
    );

    public enum PartnerComparisonMode { Individuals, Vendors, Clients }
}
