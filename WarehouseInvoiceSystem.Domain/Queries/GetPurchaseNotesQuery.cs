namespace WarehouseInvoiceSystem.Domain.Queries
{
    using WarehouseInvoiceSystem.Domain.Queries.Common;
    using WarehouseInvoiceSystem.Domain.Enums;

    public class GetPurchaseNotesQuery : PagedQuery
    {
        public PurchaseNoteStatus? Status { get; set; }
        public string? IndividualName { get; set; }
        public Guid? IndividualId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public decimal? AmountMin { get; set; }
        public decimal? AmountMax { get; set; }
    }
}