namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Application.DTOs.Product;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public interface IProductService
    {
        Task<IEnumerable<ProductDto>> GetAllProductsAsync();
        Task<PagedResult<ProductDto>> GetPagedAsync(GetProductsQuery query);
        Task<IEnumerable<ProductDto>> GetProductsByIdsAsync(List<Guid> productIds);
        Task<IEnumerable<ProductDto>> GetActiveProductsAsync();
        Task<ProductDto?> GetProductByIdAsync(Guid id);
        Task<ProductDto?> GetProductByCodeAsync(string code);
        /// <summary>
        /// Returns full product analytics + transaction history in a single coordinated call.
        /// Prefer this over calling GetProductAnalyticsAsync and GetProductTransactionHistoryAsync separately.
        /// </summary>
        Task<ProductDetailsDto> GetProductDetailsAsync(Guid productId);

        /// <summary>Thin wrapper around GetProductDetailsAsync. Kept for backward compatibility.</summary>
        Task<ProductAnalyticsDto> GetProductAnalyticsAsync(Guid productId);

        /// <summary>Kept for backward compatibility. Use GetProductDetailsAsync when possible.</summary>
        Task<ProductTransactionHistoryDto> GetProductTransactionHistoryAsync(Guid productId);
        Task<ProductDto> CreateProductAsync(CreateProductDto createDto);
        Task<ProductDto> UpdateProductAsync(Guid id, UpdateProductDto updateDto);
        Task<bool> SetActiveStatusAsync(Guid id, bool isActive);
        Task<bool> DeleteProductAsync(Guid id);
    }
}