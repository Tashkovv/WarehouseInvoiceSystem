namespace WarehouseInvoiceSystem.Application.DTOs.Product
{
    public class UpdateProductDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal DefaultPrice { get; set; }
        public bool IsActive { get; set; }
    }
}