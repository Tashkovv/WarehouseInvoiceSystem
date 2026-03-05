namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Application.DTOs.Product;

    public interface IProductService
    {
        Task<IEnumerable<ProductDto>> GetAllProductsAsync();
        Task<IEnumerable<ProductDto>> GetProductsByIdsAsync(List<Guid> productIds);
        Task<IEnumerable<ProductDto>> GetActiveProductsAsync();
        Task<ProductDto?> GetProductByIdAsync(Guid id);
        Task<ProductDto?> GetProductByCodeAsync(string code);
        Task<ProductAnalyticsDto> GetProductAnalyticsAsync(Guid productId);
        Task<ProductDto> CreateProductAsync(CreateProductDto createDto);
        Task<ProductDto> UpdateProductAsync(Guid id, UpdateProductDto updateDto);
        Task<bool> DeleteProductAsync(Guid id);
    }
}