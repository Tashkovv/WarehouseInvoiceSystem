namespace WarehouseInvoiceSystem.Application.DTOs.PurchaseNote
{
    using WarehouseInvoiceSystem.Domain.Enums;

    public class UpdatePurchaseNoteDto
    {
        public Guid IndividualId { get; set; }
        public Guid WarehouseId { get; set; }
        public DateTime PurchaseDate { get; set; }
        public PurchaseNoteStatus Status { get; set; }
        public DateTime? PaidDate { get; set; }
        public string? Notes { get; set; }
        public List<UpdatePurchaseNoteLineDto> LineItems { get; set; } = [];
    }
}