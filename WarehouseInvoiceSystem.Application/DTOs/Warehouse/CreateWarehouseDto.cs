namespace WarehouseInvoiceSystem.Application.DTOs.Warehouse
{
    public class CreateWarehouseDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
    }
}