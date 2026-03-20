namespace WarehouseInvoiceSystem.Domain.Queries.Results
{
    public class DayPaymentSummaryResult
    {
        public int TotalCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal ReceivedAmount { get; set; }
        public decimal PaidOutAmount { get; set; }
    }
}
