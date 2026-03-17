namespace WarehouseInvoiceSystem.Domain.Queries.Results
{
    public class ProductMovementResult
    {
        public Guid ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
