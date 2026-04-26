namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Application.DTOs.Product;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public interface IProductService
    {
        Task<IEnumerable<ProductDto>> GetAllProductsAsync(CancellationToken ct = default);
        Task<PagedResult<ProductDto>> GetPagedAsync(GetProductsQuery query, CancellationToken ct = default);
        Task<IEnumerable<ProductDto>> GetProductsByIdsAsync(List<Guid> productIds, CancellationToken ct = default);
        Task<IEnumerable<ProductDto>> GetActiveProductsAsync(CancellationToken ct = default);
        Task<ProductDto?> GetProductByIdAsync(Guid id, CancellationToken ct = default);
        Task<ProductDto?> GetProductByCodeAsync(string code, CancellationToken ct = default);
        /// <summary>
        /// Returns stock-only details for a product. Profitability and transaction totals
        /// are period-scoped — fetch those via GetProductTransactionSummaryAsync.
        /// </summary>
        Task<ProductDetailsDto> GetProductDetailsAsync(Guid productId, CancellationToken ct = default);
        Task<ProductTransactionSummaryDto> GetProductTransactionSummaryAsync(Guid productId, DateTime? dateFrom = null, DateTime? dateTo = null, CancellationToken ct = default);
        Task<PagedResult<ProductTransactionRowDto>> GetPagedProductHistoryAsync(GetProductHistoryQuery query, CancellationToken ct = default);
        Task<(int Count, decimal TotalQuantity, decimal TotalAmount)> GetProductHistoryStatsAsync(GetProductHistoryQuery query, CancellationToken ct = default);
        Task CreateProductAsync(CreateProductDto createDto);
        Task UpdateProductAsync(Guid id, UpdateProductDto updateDto);
        Task<bool> SetActiveStatusAsync(Guid id, bool isActive);
        Task<bool> DeleteProductAsync(Guid id);
        Task<List<PartnerComparisonDto>> GetPartnerComparisonAsync(
            Guid productId,
            PartnerComparisonMode mode,
            Guid? warehouseId,
            DateTime? dateFrom,
            DateTime? dateTo,
            List<Guid>? partnerIds = null,
            CancellationToken ct = default);
        Task<List<ProductComparisonDto>> GetProductComparisonAsync(
            List<Guid> productIds,
            Guid? warehouseId,
            DateTime? dateFrom,
            DateTime? dateTo,
            CancellationToken ct = default);
    }
}