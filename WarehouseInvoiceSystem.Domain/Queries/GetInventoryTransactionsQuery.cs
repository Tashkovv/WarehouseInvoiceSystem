namespace WarehouseInvoiceSystem.Domain.Queries
{
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public class GetInventoryTransactionsQuery : PagedQuery
    {
        public Guid ProductId { get; set; }
        public Guid? WarehouseId { get; set; }

        /// <summary>
        /// Filter by one or more transaction types.
        /// When null or empty all types are included.
        /// </summary>
        public List<InventoryTransactionType>? Types { get; set; }

        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}