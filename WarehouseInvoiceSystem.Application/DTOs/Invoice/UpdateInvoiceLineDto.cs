namespace WarehouseInvoiceSystem.Application.DTOs.Invoice
{
    public class UpdateInvoiceLineDto
    {
        public int? Id { get; set; } // Null for new items
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxRate { get; set; }
    }
}
