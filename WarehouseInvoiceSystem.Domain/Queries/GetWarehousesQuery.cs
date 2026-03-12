namespace WarehouseInvoiceSystem.Domain.Queries
{
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public class GetWarehousesQuery : PagedQuery
    {
        public bool? IsActive { get; set; }
    }
}