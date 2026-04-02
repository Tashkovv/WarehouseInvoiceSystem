namespace WarehouseInvoiceSystem.Application.DTOs.PurchaseNote
{
    public class CreatePurchaseNoteLineDto
    {
        public Guid ProductId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal GrossQuantity { get; set; }
        public decimal KaloPercentage { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}