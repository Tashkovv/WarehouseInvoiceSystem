namespace WarehouseInvoiceSystem.Application.DTOs.Product
{
    public class ProductTransactionHistoryDto
    {
        public List<ProductTransactionRowDto> Purchased { get; set; } = [];
        public List<ProductTransactionRowDto> Sold { get; set; } = [];
    }

    public class ProductTransactionRowDto
    {
        public DateTime Date { get; set; }
        public string DocumentNumber { get; set; } = string.Empty;
        public string DocumentUrl { get; set; } = string.Empty;
        public string? DocumentType { get; set; }
        public string PartyName { get; set; } = string.Empty;
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
