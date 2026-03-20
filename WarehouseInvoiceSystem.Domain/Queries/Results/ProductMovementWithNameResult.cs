namespace WarehouseInvoiceSystem.Domain.Queries.Results
{
    public class ProductMovementWithNameResult
    {
        public Guid ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
