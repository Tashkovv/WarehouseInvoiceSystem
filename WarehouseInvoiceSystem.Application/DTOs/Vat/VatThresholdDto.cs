namespace WarehouseInvoiceSystem.Application.DTOs.Vat
{
    public class VatThresholdDto
    {
        public decimal YtdRevenue { get; set; }
        public decimal Threshold { get; set; } = 2_000_000m;
        public decimal Percentage => Threshold == 0 ? 0 : Math.Min(100m, Math.Round(YtdRevenue / Threshold * 100m, 1));
        public int Year { get; set; }
    }
}
