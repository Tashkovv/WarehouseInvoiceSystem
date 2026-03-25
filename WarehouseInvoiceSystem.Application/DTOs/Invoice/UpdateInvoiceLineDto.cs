namespace WarehouseInvoiceSystem.Application.DTOs.Invoice
{
    public class UpdateInvoiceLineDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxRate { get; set; }
        public decimal DiscountPercentage { get; set; }
    }
}
