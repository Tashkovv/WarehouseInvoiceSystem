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
        Task<ProductAnalyticsDto> GetProductAnalyticsAsync(Guid productId);
        Task<ProductTransactionHistoryDto> GetProductTransactionHistoryAsync(Guid productId);
        Task<ProductDto> CreateProductAsync(CreateProductDto createDto);
        Task<ProductDto> UpdateProductAsync(Guid id, UpdateProductDto updateDto);
        Task<bool> DeleteProductAsync(Guid id);
    }
}