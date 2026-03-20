namespace WarehouseInvoiceSystem.Domain.Queries.Results
{
    public class InvoicePeriodSummaryResult
    {
        public int ReceivableCount { get; set; }
        public decimal ReceivableAmount { get; set; }
        public int PayableCount { get; set; }
        public decimal PayableAmount { get; set; }
    }
}
