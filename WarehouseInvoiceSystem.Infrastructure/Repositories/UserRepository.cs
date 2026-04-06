namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Infrastructure.Data;

    public class UserRepository(IDbContextFactory<ApplicationDbContext> factory, IAuditContextService auditContext)
        : BaseRepository(factory, auditContext), IUserRepository
    {
        public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            WithContextAsync(context =>
                All<User>(context).FirstOrDefaultAsync(u => u.Id == id, ct));

        public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
            WithContextAsync(context =>
                All<User>(context).FirstOrDefaultAsync(u => u.Username == username, ct));

        public Task<IEnumerable<User>> GetAllAsync(CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                return (IEnumerable<User>)await All<User>(context)
                    .OrderBy(u => u.Username)
                    .ToListAsync(ct);
            });

        public Task<bool> AnyUsersExistAsync(CancellationToken ct = default) =>
            WithContextAsync(context =>
                All<User>(context).AnyAsync(ct));

        public Task CreateAsync(User user) =>
            WithContextAsync(async context =>
            {
                user.CreatedAt = DateTime.UtcNow;
                context.Users.Add(user);
                await SaveAsync(context);
            });

        public Task UpdateAsync(User user) =>
            WithContextAsync(async context =>
            {
                User? tracked = await context.Users.FindAsync(user.Id)
                    ?? throw new KeyNotFoundException($"User {user.Id} not found");
                context.Entry(tracked).CurrentValues.SetValues(user);
                await SaveAsync(context);
            });

        public Task<bool> DeleteAsync(Guid id) =>
            WithContextAsync(async context =>
            {
                User? user = await context.Users
                    .Where(u => u.Id == id)
                    .SingleOrDefaultAsync();

                if (user == null)
                    return false;

                user.IsActive = false;
                user.DeletedOn = DateTime.UtcNow;
                await SaveAsync(context);
                return true;
            });

        public Task<bool> SetActiveStatusAsync(Guid id, bool isActive, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                User? user = await AllTracked<User>(context)
                    .FirstOrDefaultAsync(u => u.Id == id, ct);

                if (user is null)
                    return false;

                user.IsActive = isActive;
                await SaveAsync(context, ct);
                return true;
            });
    }
}
