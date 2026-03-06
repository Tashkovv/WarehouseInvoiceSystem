namespace WarehouseInvoiceSystem.Domain.Interfaces
{
    using WarehouseInvoiceSystem.Domain.Entities;

    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<IEnumerable<Product>> GetByIdsAsync(List<Guid> ids);
        Task<IEnumerable<Product>> GetActiveProductsAsync();
        Task<Product?> GetByIdAsync(Guid id);
        Task<Product?> GetByCodeAsync(string code);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> AllExistAsync(IEnumerable<Guid> ids);
        Task<bool> CodeExistsAsync(string code, Guid? excludeId = null);
        Task<Product> CreateAsync(Product product);
        Task<Product> UpdateAsync(Product product);
        Task<bool> DeleteAsync(Guid id);
    }
}
