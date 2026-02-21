namespace WarehouseInvoiceSystem.Application.DTOs.PurchaseNote
{
    public class UpdatePurchaseNoteLineDto
    {
        public Guid ProductId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}