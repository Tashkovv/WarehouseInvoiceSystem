namespace WarehouseInvoiceSystem.Domain.Queries.Results
{
    /// <summary>
    /// Keyless entity mapped to the vw_product_purchase_history database view.
    /// The view UNIONs purchase-note lines (individual vendors) and payable invoice
    /// lines (company vendors) so the purchased history can be paged in SQL.
    /// </summary>
    public class ProductPurchaseHistoryView
    {
        public Guid ProductId { get; set; }
        public DateTime Date { get; set; }
        public string DocumentNumber { get; set; } = string.Empty;
        public string DocumentUrl { get; set; } = string.Empty;
        public string PartyName { get; set; } = string.Empty;
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public Guid? IndividualId { get; set; }
        public Guid? CompanyId { get; set; }
    }
}
