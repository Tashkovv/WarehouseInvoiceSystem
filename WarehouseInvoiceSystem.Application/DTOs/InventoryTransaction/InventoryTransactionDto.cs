namespace WarehouseInvoiceSystem.Application.DTOs.InventoryTransaction
{
    using WarehouseInvoiceSystem.Domain.Enums;

    public class InventoryTransactionDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductUnit { get; set; } = string.Empty;
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public InventoryTransactionType Type { get; set; }
        public decimal Quantity { get; set; }
        public Guid? SourceDocumentId { get; set; }
        public string? SourceDocumentType { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}