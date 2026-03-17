namespace WarehouseInvoiceSystem.Application.DTOs.Dashboard
{
    public class PartnerAttentionDto
    {
        public Guid PartnerId { get; set; }
        public string PartnerName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }
}
