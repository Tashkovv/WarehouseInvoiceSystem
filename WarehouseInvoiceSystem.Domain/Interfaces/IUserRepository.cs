namespace WarehouseInvoiceSystem.Domain.Interfaces
{
    using WarehouseInvoiceSystem.Domain.Entities;

    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
        Task<IEnumerable<User>> GetAllAsync(CancellationToken ct = default);
        Task<bool> AnyUsersExistAsync(CancellationToken ct = default);
        Task CreateAsync(User user);
        Task UpdateAsync(User user);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> SetActiveStatusAsync(Guid id, bool isActive, CancellationToken ct = default);
    }
}
