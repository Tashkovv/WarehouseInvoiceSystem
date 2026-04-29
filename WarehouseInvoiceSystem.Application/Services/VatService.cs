namespace WarehouseInvoiceSystem.Application.Services
{
    using WarehouseInvoiceSystem.Application.DTOs.Vat;
    using WarehouseInvoiceSystem.Application.Helpers;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries.Results;

    public class VatService(
        ITenantRepository tenantRepository,
        IInvoiceRepository invoiceRepository) : IVatService
    {
        public async Task<VatCurrentPeriodDto?> GetCurrentPeriodSummaryAsync(CancellationToken ct = default)
        {
            var tenant = await tenantRepository.GetAsync(ct);
            if (!tenant.VatRegistered)
                return null;

            DateTime asOf = DateTime.UtcNow;
            (DateTime from, DateTime to, string label) = VatPeriodCalculator.GetCurrentPeriod(tenant.VatPayerPeriod, asOf);
            DateTime deadline = VatPeriodCalculator.GetFilingDeadline(to);
            int daysUntil = Math.Max(0, (deadline.Date - asOf.Date).Days);

            VatPeriodSummaryResult summary = await invoiceRepository.GetVatPeriodSummaryAsync(from, to, ct);

            return new VatCurrentPeriodDto
            {
                PeriodLabel      = label,
                PeriodFrom       = from,
                PeriodTo         = to,
                FilingDeadline   = deadline,
                DaysUntilDeadline = daysUntil,
                OutputVat        = summary.OutputVatTotal,
                InputVat         = summary.InputVatTotal,
                NetVat           = summary.NetVat,
                OutputBase       = summary.OutputBaseTotal,
                InputBase        = summary.InputBaseTotal,
                OutputDocCount   = summary.OutputDocCount,
                InputDocCount    = summary.InputDocCount,
            };
        }

        public async Task<IEnumerable<VatPeriodHistoryItemDto>> GetPeriodHistoryAsync(
            int count, CancellationToken ct = default)
        {
            var tenant = await tenantRepository.GetAsync(ct);
            if (!tenant.VatRegistered)
                return Enumerable.Empty<VatPeriodHistoryItemDto>();

            DateTime asOf = DateTime.UtcNow;
            IEnumerable<(DateTime From, DateTime To, string Label)> periods =
                VatPeriodCalculator.GetPastPeriods(tenant.VatPayerPeriod, count, asOf);

            // Fetch each period in parallel — typically 8 periods, each is a fast DB-side SUM
            IEnumerable<Task<VatPeriodHistoryItemDto>> tasks = periods.Select(async p =>
            {
                VatPeriodSummaryResult summary = await invoiceRepository.GetVatPeriodSummaryAsync(p.From, p.To, ct);
                DateTime deadline = VatPeriodCalculator.GetFilingDeadline(p.To);
                return new VatPeriodHistoryItemDto
                {
                    PeriodLabel    = p.Label,
                    PeriodFrom     = p.From,
                    PeriodTo       = p.To,
                    FilingDeadline = deadline,
                    OutputVat      = summary.OutputVatTotal,
                    InputVat       = summary.InputVatTotal,
                    NetVat         = summary.NetVat,
                    OutputDocCount = summary.OutputDocCount,
                    InputDocCount  = summary.InputDocCount,
                };
            });

            return await Task.WhenAll(tasks);
        }

        public async Task<VatCurrentPeriodDto?> GetRangeSummaryAsync(
            DateTime from, DateTime to, string label, CancellationToken ct = default)
        {
            var tenant = await tenantRepository.GetAsync(ct);
            if (!tenant.VatRegistered)
                return null;

            VatPeriodSummaryResult summary = await invoiceRepository.GetVatPeriodSummaryAsync(from, to, ct);
            DateTime deadline = VatPeriodCalculator.GetFilingDeadline(to);
            int daysUntil = Math.Max(0, (deadline.Date - DateTime.UtcNow.Date).Days);

            return new VatCurrentPeriodDto
            {
                PeriodLabel       = label,
                PeriodFrom        = from,
                PeriodTo          = to,
                FilingDeadline    = deadline,
                DaysUntilDeadline = daysUntil,
                OutputVat         = summary.OutputVatTotal,
                InputVat          = summary.InputVatTotal,
                NetVat            = summary.NetVat,
                OutputBase        = summary.OutputBaseTotal,
                InputBase         = summary.InputBaseTotal,
                OutputDocCount    = summary.OutputDocCount,
                InputDocCount     = summary.InputDocCount,
            };
        }

        public async Task<bool> IsVatRegisteredAsync(CancellationToken ct = default)
        {
            var tenant = await tenantRepository.GetAsync(ct);
            return tenant.VatRegistered;
        }

        public async Task<VatThresholdDto> GetThresholdProgressAsync(CancellationToken ct = default)
        {
            int year = DateTime.UtcNow.Year;
            decimal ytd = await invoiceRepository.GetYtdReceivableTotalAsync(year, ct);
            return new VatThresholdDto
            {
                Year       = year,
                YtdRevenue = ytd,
                Threshold  = 2_000_000m,
            };
        }
    }
}
