namespace WarehouseInvoiceSystem.Domain.Queries.Results
{
    public class VatPeriodSummaryResult
    {
        public decimal OutputVatTotal { get; set; }
        public decimal InputVatTotal { get; set; }
        public decimal OutputBaseTotal { get; set; }
        public decimal InputBaseTotal { get; set; }
        public int OutputDocCount { get; set; }
        public int InputDocCount { get; set; }

        public decimal NetVat => OutputVatTotal - InputVatTotal;
    }
}
