namespace WarehouseInvoiceSystem.Application.DTOs.PurchaseNote
{
    public class CreatePurchaseNoteLineDto
    {
        public Guid ProductId { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}