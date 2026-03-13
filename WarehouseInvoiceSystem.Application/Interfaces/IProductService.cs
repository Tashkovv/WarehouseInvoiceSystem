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
        /// Returns full product analytics + transaction history + stock movements in a single coordinated call.
        /// </summary>
        Task<ProductDetailsDto> GetProductDetailsAsync(Guid productId);
        Task<PagedResult<ProductTransactionRowDto>> GetPagedProductHistoryAsync(GetProductHistoryQuery query);
        Task CreateProductAsync(CreateProductDto createDto);
        Task UpdateProductAsync(Guid id, UpdateProductDto updateDto);
        Task<bool> SetActiveStatusAsync(Guid id, bool isActive);
        Task<bool> DeleteProductAsync(Guid id);
    }
}