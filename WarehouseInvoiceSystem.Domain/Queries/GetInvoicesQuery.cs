namespace WarehouseInvoiceSystem.Domain.Queries
{
    using WarehouseInvoiceSystem.Domain.Queries.Common;
    using WarehouseInvoiceSystem.Domain.Enums;

    public class GetInvoicesQuery : PagedQuery
    {
        /// <summary>Single status filter — kept for backward compat with existing callers.</summary>
        public InvoiceStatus? Status { get; set; }

        /// <summary>Multi-status filter used by the list page. When set, Status is ignored.</summary>
        public List<InvoiceStatus>? Statuses { get; set; }

        public InvoiceType? Type { get; set; }
        public Guid? CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public decimal? AmountMin { get; set; }
        public decimal? AmountMax { get; set; }
        public DateTime? IssueDateFrom { get; set; }
        public DateTime? IssueDateTo { get; set; }
        public DateTime? DueDateFrom { get; set; }
        public DateTime? DueDateTo { get; set; }
    }
}