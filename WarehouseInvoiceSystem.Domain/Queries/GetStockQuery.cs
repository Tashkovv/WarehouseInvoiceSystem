namespace WarehouseInvoiceSystem.Domain.Queries
{
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public class GetStockQuery : PagedQuery
    {
        public Guid? WarehouseId { get; set; }
        public Guid? ProductId { get; set; }
        public bool? IsLowStock { get; set; }
    }
}