namespace WarehouseInvoiceSystem.Application.DTOs.Dashboard
{
    public class PartnerSummaryDto
    {
        public Guid PartnerId { get; set; }
        public string PartnerName { get; set; } = string.Empty;
        public int DocumentCount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
