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
        /// Returns full product analytics + transaction history + stock movements in a single coordinated call.
        /// </summary>
        Task<ProductDetailsDto> GetProductDetailsAsync(Guid productId, CancellationToken ct = default);
        Task<PagedResult<ProductTransactionRowDto>> GetPagedProductHistoryAsync(GetProductHistoryQuery query, CancellationToken ct = default);
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