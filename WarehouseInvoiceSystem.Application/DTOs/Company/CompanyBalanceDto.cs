namespace WarehouseInvoiceSystem.Application.DTOs.Company
{
    public class CompanyBalanceDto
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public decimal TotalOwedByUs { get; set; }  // We owe them (Payables)
        public decimal TotalOwedToUs { get; set; }  // They owe us (Receivables)
        public decimal NetBalance { get; set; }      // Positive = they owe us, Negative = we owe them
    }
}
