namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Application.DTOs.Vat;

    public interface IVatService
    {
        Task<VatCurrentPeriodDto?> GetCurrentPeriodSummaryAsync(CancellationToken ct = default);
        Task<VatCurrentPeriodDto?> GetRangeSummaryAsync(DateTime from, DateTime to, string label, CancellationToken ct = default);
        Task<IEnumerable<VatPeriodHistoryItemDto>> GetPeriodHistoryAsync(int count, CancellationToken ct = default);
        Task<VatThresholdDto> GetThresholdProgressAsync(CancellationToken ct = default);
        Task<bool> IsVatRegisteredAsync(CancellationToken ct = default);
    }
}
