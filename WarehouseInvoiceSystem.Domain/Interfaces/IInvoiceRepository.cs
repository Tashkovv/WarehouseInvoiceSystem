namespace WarehouseInvoiceSystem.Domain.Interfaces
{
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;
    using WarehouseInvoiceSystem.Domain.Queries.Results;

    public interface IInvoiceRepository
    {
        Task<IEnumerable<Invoice>> GetAllAsync(CancellationToken ct = default);
        Task<PagedResult<Invoice>> GetPagedAsync(GetInvoicesQuery query, CancellationToken ct = default);

        Task<IEnumerable<Invoice>> GetByCompanyIdAsync(Guid companyId, CancellationToken ct = default);

        Task<IEnumerable<Invoice>> GetByTypeAsync(InvoiceType type, CancellationToken ct = default);

        Task<IEnumerable<Invoice>> GetByStatusAsync(InvoiceStatus status, CancellationToken ct = default);

        Task<IEnumerable<Invoice>> GetOverdueInvoicesAsync(CancellationToken ct = default);
        Task<Invoice?> GetByIdAsync(Guid id, CancellationToken ct = default);

        Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken ct = default);

        Task<IEnumerable<InvoiceLine>> GetLineItemsByProductIdAsync(Guid productId, CancellationToken ct = default);
        Task<IEnumerable<InvoiceLine>> GetLineItemsByProductIdAsync(Guid productId, InvoiceType? type, Guid? warehouseId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default);
        Task<IEnumerable<InvoiceLine>> GetLineItemsByProductIdsAsync(List<Guid> productIds, Guid? warehouseId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default);

        Task<PagedResult<InvoiceLine>> GetPagedLineItemsByProductIdAsync(GetProductHistoryQuery query, CancellationToken ct = default);

        Task<Guid> CreateAsync(Invoice invoice);
        Task<Invoice> UpdateAsync(Invoice invoice);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

        Task<string> GenerateInvoiceNumberAsync(InvoiceType type, CancellationToken ct = default);

        Task<(int total, int paid, int unpaid, int overdue)> GetPayableInvoiceCountsAsync(CancellationToken ct = default);
        Task<(decimal totalAmount, decimal totalPaid, decimal totalDue)> GetPayableInvoiceTotalsAsync(CancellationToken ct = default);

        // ── Dashboard targeted queries ────────────────────────────────────────────
        Task<IEnumerable<Invoice>> GetRecentAsync(int count, CancellationToken ct = default);
        Task<IEnumerable<Invoice>> GetByIssueDateAsync(DateTime date, CancellationToken ct = default);
        Task<IEnumerable<Invoice>> GetByIssueDateMonthAsync(int year, int month, CancellationToken ct = default);
        Task<InvoiceOutstandingResult> GetOutstandingPositionAsync(CancellationToken ct = default);
        Task<IEnumerable<PartnerSummaryResult>> GetTopClientsByRevenueAsync(DateTime from, DateTime to, int topCount, CancellationToken ct = default);
        Task<IEnumerable<PartnerSummaryResult>> GetTopPayableVendorsBySpendAsync(DateTime from, DateTime to, int topCount, CancellationToken ct = default);
        Task<IEnumerable<PartnerSummaryResult>> GetOverdueClientSummariesAsync(CancellationToken ct = default);
        Task<IEnumerable<PartnerSummaryResult>> GetUnpaidPayableCompanySummariesAsync(CancellationToken ct = default);
        Task<IEnumerable<PartnerSummaryResult>> GetOverduePayableCompanySummariesAsync(CancellationToken ct = default);
        Task<IEnumerable<ProductMovementResult>> GetProductMovementByWarehouseAsync(Guid warehouseId, InvoiceType type, DateTime from, DateTime to, CancellationToken ct = default);
        Task<IEnumerable<ProductMovementWithNameResult>> GetTopProductMovementByWarehouseAsync(Guid warehouseId, InvoiceType type, DateTime from, DateTime to, int top, CancellationToken ct = default);

        Task<CompanyAnalyticsResult> GetCompanyAnalyticsDataAsync(Guid companyId, CancellationToken ct = default);

        Task<PagedResult<ProductPurchaseHistoryView>> GetPagedPurchasedHistoryAsync(GetProductHistoryQuery query, CancellationToken ct = default);

        // ── Dashboard aggregates ──────────────────────────────────────────────────
        Task<DayIssueSummaryResult> GetDayIssueSummaryAsync(DateTime date, CancellationToken ct = default);
        Task<IEnumerable<Invoice>> GetTopOverdueReceivablesAsync(Guid? warehouseId, int top, CancellationToken ct = default);
        Task<InvoicePeriodSummaryResult> GetDayInvoiceSummaryAsync(DateTime date, CancellationToken ct = default);
        Task<InvoicePeriodSummaryResult> GetMonthInvoiceSummaryAsync(int year, int month, CancellationToken ct = default);
        Task<InvoicePeriodSummaryResult> GetYearInvoiceSummaryAsync(int year, CancellationToken ct = default);

        // ── Notification queries ────────────────────────────────────────────────
        Task<List<Invoice>> GetInvoicesDueInDaysAsync(int days, InvoiceType type, CancellationToken ct = default);
    }
}