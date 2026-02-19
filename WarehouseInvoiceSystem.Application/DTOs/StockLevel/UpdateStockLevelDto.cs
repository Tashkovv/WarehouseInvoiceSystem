namespace WarehouseInvoiceSystem.Application.DTOs.StockLevel
{
    public class UpdateStockLevelDto
    {
        public decimal Quantity { get; set; }
        public decimal? MinimumQuantity { get; set; }
        public decimal? ReorderPoint { get; set; }
    }
}