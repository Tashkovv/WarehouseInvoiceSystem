namespace WarehouseInvoiceSystem.Application.Queries
{
    using WarehouseInvoiceSystem.Application.DTOs.Common;
    using WarehouseInvoiceSystem.Domain.Enums;

    public class GetPurchaseNotesQuery : PagedQuery
    {
        public PurchaseNoteStatus? Status { get; set; }
        public string? IndividualName { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public decimal? AmountMin { get; set; }
        public decimal? AmountMax { get; set; }
    }
}
