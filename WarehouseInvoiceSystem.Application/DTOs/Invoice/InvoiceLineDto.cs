namespace WarehouseInvoiceSystem.Application.DTOs.Invoice
{
    public class InvoiceLineDto
    {
        public Guid Id { get; set; }
        public Guid? ProductId { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxRate { get; set; }
        public decimal Amount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
