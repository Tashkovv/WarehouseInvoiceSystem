namespace WarehouseInvoiceSystem.Domain.Queries
{
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public class GetIndividualsQuery : PagedQuery
    {
        public bool? IsActive { get; set; }
        public bool SearchByNameOnly { get; set; }
    }
}
