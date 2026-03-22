namespace WarehouseInvoiceSystem.Application.DTOs.Product
{
    public record ProductComparisonDto(
        Guid ProductId,
        string ProductName,
        string ProductCode,
        string ProductUnit,
        decimal IncomingQuantity,
        decimal OutgoingQuantity,
        decimal IncomingAmount,
        decimal OutgoingAmount,
        int DocumentCount
    );
}
