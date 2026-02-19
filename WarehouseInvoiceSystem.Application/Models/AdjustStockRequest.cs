namespace WarehouseInvoiceSystem.Application.Models
{
    public class AdjustStockRequest
    {
        public Guid ProductId { get; set; }
        public Guid WarehouseId { get; set; }
        public decimal QuantityChange { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
