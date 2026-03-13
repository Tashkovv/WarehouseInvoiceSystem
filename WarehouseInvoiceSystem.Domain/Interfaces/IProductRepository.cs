namespace WarehouseInvoiceSystem.Domain.Interfaces
{
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<PagedResult<Product>> GetPagedAsync(GetProductsQuery query);
        Task<IEnumerable<Product>> GetByIdsAsync(List<Guid> ids);
        Task<IEnumerable<Product>> GetActiveProductsAsync();
        Task<Product?> GetByIdAsync(Guid id);
        Task<Product?> GetByCodeAsync(string code);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> AllExistAsync(IEnumerable<Guid> ids);
        Task<bool> CodeExistsAsync(string code, Guid? excludeId = null);
        Task CreateAsync(Product product);
        Task UpdateAsync(Product product);
        Task<bool> SetActiveStatusAsync(Guid id, bool isActive);
        Task<bool> DeleteAsync(Guid id);
    }
}