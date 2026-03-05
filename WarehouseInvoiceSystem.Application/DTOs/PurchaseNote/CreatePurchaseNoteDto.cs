namespace WarehouseInvoiceSystem.Application.DTOs.PurchaseNote
{
    public class CreatePurchaseNoteDto
    {
        public Guid IndividualId { get; set; }
        public Guid WarehouseId { get; set; }
        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }
        public bool MarkAsPaid { get; set; }
        public List<CreatePurchaseNoteLineDto> LineItems { get; set; } = [];
    }
}