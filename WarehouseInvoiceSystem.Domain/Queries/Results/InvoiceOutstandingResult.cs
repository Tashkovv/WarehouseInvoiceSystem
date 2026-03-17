namespace WarehouseInvoiceSystem.Domain.Queries.Results
{
    public class InvoiceOutstandingResult
    {
        public int ReceivableCount { get; set; }
        public decimal ReceivableAmount { get; set; }
        public int OverdueReceivableCount { get; set; }
        public decimal OverdueReceivableAmount { get; set; }
        public int PayableCount { get; set; }
        public decimal PayableAmount { get; set; }
        public int TotalOverdueCount { get; set; }
    }
}
