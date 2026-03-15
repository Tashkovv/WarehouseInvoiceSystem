namespace WarehouseInvoiceSystem.Application.DTOs.PurchaseNote
{
    using WarehouseInvoiceSystem.Domain.Enums;

    public class PurchaseNoteDto
    {
        public Guid Id { get; set; }
        public string NoteNumber { get; set; } = string.Empty;
        public Guid IndividualId { get; set; }
        public string IndividualFullName { get; set; } = string.Empty;
        public string IndividualIdentificationNumber { get; set; } = string.Empty;
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TotalAmount { get; set; }
        public PurchaseNoteStatus Status { get; set; }
        public DateTime? PaidDate { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<PurchaseNoteLineDto> LineItems { get; set; } = [];
    }
}