namespace WarehouseInvoiceSystem.Domain.Queries
{
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public class GetProductsQuery : PagedQuery
    {
        public bool? IsActive { get; set; }
    }
}
