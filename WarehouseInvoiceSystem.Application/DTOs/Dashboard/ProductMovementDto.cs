namespace WarehouseInvoiceSystem.Application.DTOs.Dashboard
{
    public class ProductMovementDto
    {
        public Guid ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
