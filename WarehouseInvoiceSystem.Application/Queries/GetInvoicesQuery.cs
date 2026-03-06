namespace WarehouseInvoiceSystem.Application.Queries
{
    using WarehouseInvoiceSystem.Application.DTOs.Common;
    using WarehouseInvoiceSystem.Domain.Enums;

    public class GetInvoicesQuery : PagedQuery
    {
        public InvoiceStatus? Status { get; set; }
        public InvoiceType? Type { get; set; }
        public string? CompanyName { get; set; }
        public decimal? AmountMin { get; set; }
        public decimal? AmountMax { get; set; }
        public DateTime? IssueDateFrom { get; set; }
        public DateTime? IssueDateTo { get; set; }
        public DateTime? DueDateFrom { get; set; }
        public DateTime? DueDateTo { get; set; }
    }
}
