namespace WarehouseInvoiceSystem.Application.DTOs.Invoice
{
    public class CreateInvoiceLineDto
    {
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxRate { get; set; } = 0;
    }
}
