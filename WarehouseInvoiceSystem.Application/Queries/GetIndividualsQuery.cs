namespace WarehouseInvoiceSystem.Application.Queries
{
    using WarehouseInvoiceSystem.Application.DTOs.Common;

    public class GetIndividualsQuery : PagedQuery
    {
        public bool? IsActive { get; set; }
    }
}
