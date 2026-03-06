namespace WarehouseInvoiceSystem.Application.Queries
{
    using WarehouseInvoiceSystem.Application.DTOs.Common;

    public class GetProductsQuery : PagedQuery
    {
        public bool? IsActive { get; set; }
    }
}
