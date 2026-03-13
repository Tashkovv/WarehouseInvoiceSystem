namespace WarehouseInvoiceSystem.Domain.Interfaces
{
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;

    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync(CancellationToken ct = default);
        Task<PagedResult<Product>> GetPagedAsync(GetProductsQuery query, CancellationToken ct = default);

        Task<IEnumerable<Product>> GetByIdsAsync(List<Guid> ids, CancellationToken ct = default);

        Task<IEnumerable<Product>> GetActiveProductsAsync(CancellationToken ct = default);
        Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);

        Task<Product?> GetByCodeAsync(string code, CancellationToken ct = default);

        Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

        Task<bool> AllExistAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
        Task<bool> CodeExistsAsync(string code, Guid? excludeId = null);
        Task CreateAsync(Product product);
        Task UpdateAsync(Product product);
        Task<bool> SetActiveStatusAsync(Guid id, bool isActive);
        Task<bool> DeleteAsync(Guid id);
    }
}