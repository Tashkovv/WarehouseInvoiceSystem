namespace WarehouseInvoiceSystem.Domain.Entities
{
    using WarehouseInvoiceSystem.Domain.Common;
    using WarehouseInvoiceSystem.Domain.Enums;

    public class PurchaseNote : AuditableEntity
    {
        public string NoteNumber { get; set; } = string.Empty;
        public Guid IndividualId { get; set; }
        public Guid? WarehouseId { get; set; }

        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

        public decimal SubTotal { get; set; }
        public decimal TotalAmount { get; set; }

        public PurchaseNoteStatus Status { get; set; } = PurchaseNoteStatus.Draft;
        public DateTime? PaidDate { get; set; }

        public string? Notes { get; set; }

        // Navigation properties
        public Individual Individual { get; set; } = null!;
        public Warehouse? Warehouse { get; set; }
        public ICollection<PurchaseNoteLine> LineItems { get; set; } = [];
    }
}