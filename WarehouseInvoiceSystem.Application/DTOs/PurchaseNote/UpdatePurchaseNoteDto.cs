namespace WarehouseInvoiceSystem.Application.DTOs.PurchaseNote
{
    public class UpdatePurchaseNoteDto
    {
        public Guid IndividualId { get; set; }
        public Guid WarehouseId { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string? Notes { get; set; }
        public List<UpdatePurchaseNoteLineDto> LineItems { get; set; } = [];
    }
}
