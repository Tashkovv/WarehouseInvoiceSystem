namespace WarehouseInvoiceSystem.Application.DTOs.InventoryTransaction
{
    using WarehouseInvoiceSystem.Domain.Enums;

    public class CreateInventoryTransactionDto
    {
        public Guid ProductId { get; set; }
        public Guid WarehouseId { get; set; }
        public InventoryTransactionType Type { get; set; }
        public decimal Quantity { get; set; }
        public Guid? SourceDocumentId { get; set; }
        public string? SourceDocumentType { get; set; }
        public string? Note { get; set; }
    }
}