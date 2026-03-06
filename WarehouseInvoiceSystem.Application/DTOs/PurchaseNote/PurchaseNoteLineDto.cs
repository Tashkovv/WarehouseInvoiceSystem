namespace WarehouseInvoiceSystem.Application.DTOs.PurchaseNote
{
    public class PurchaseNoteLineDto
    {
        public Guid Id { get; set; }
        public Guid PurchaseNoteId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductUnit { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
    }
}