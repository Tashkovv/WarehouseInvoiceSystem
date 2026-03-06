namespace WarehouseInvoiceSystem.Domain.Queries
{
    using WarehouseInvoiceSystem.Domain.Queries.Common;
    using WarehouseInvoiceSystem.Domain.Enums;

    public class GetPaymentsQuery : PagedQuery
    {
        public Guid? InvoiceId { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}
