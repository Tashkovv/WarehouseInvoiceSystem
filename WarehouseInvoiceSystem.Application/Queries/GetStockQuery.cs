namespace WarehouseInvoiceSystem.Application.Queries
{
    using WarehouseInvoiceSystem.Application.DTOs.Common;

    public class GetStockQuery : PagedQuery
    {
        public Guid? WarehouseId { get; set; }
        public Guid? ProductId { get; set; }
        public bool? IsLowStock { get; set; }
    }
}