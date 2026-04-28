namespace WarehouseInvoiceSystem.Application.DTOs.Vat
{
    public class VatCurrentPeriodDto
    {
        public string PeriodLabel { get; set; } = string.Empty;
        public DateTime PeriodFrom { get; set; }
        public DateTime PeriodTo { get; set; }
        public DateTime FilingDeadline { get; set; }
        public int DaysUntilDeadline { get; set; }

        public decimal OutputVat { get; set; }
        public decimal InputVat { get; set; }
        public decimal NetVat { get; set; }
        public decimal OutputBase { get; set; }
        public decimal InputBase { get; set; }
        public int OutputDocCount { get; set; }
        public int InputDocCount { get; set; }

        public bool IsLiability => NetVat >= 0;
    }
}
