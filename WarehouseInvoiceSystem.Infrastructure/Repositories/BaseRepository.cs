namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Domain.Common;
    using WarehouseInvoiceSystem.Infrastructure.Data;

    /// <summary>
    /// Base class for all repositories.
    /// Uses IDbContextFactory to create a fresh DbContext per operation.
    /// This avoids concurrency issues in Blazor Server where multiple
    /// UI events may trigger parallel queries.
    /// </summary>
    public abstract class BaseRepository
    {
        private readonly IDbContextFactory<ApplicationDbContext> _factory;

        protected BaseRepository(IDbContextFactory<ApplicationDbContext> factory)
        {
            _factory = factory;
        }

        protected async Task<TResult> WithContextAsync<TResult>(
            Func<ApplicationDbContext, Task<TResult>> action,
            CancellationToken ct = default)
        {
            await using var context = _factory.CreateDbContext();
            ct.ThrowIfCancellationRequested();
            return await action(context);
        }

        protected async Task WithContextAsync(
            Func<ApplicationDbContext, Task> action,
            CancellationToken ct = default)
        {
            await using var context = _factory.CreateDbContext();
            ct.ThrowIfCancellationRequested();
            await action(context);
        }

        protected async Task<TResult> WithTransactionAsync<TResult>(
            Func<ApplicationDbContext, Task<TResult>> action)
        {
            await using var context = _factory.CreateDbContext();
            await using var tx = await context.Database.BeginTransactionAsync();

            var result = await action(context);

            await context.SaveChangesAsync();
            await tx.CommitAsync();

            return result;
        }

        // ── Query helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// No-tracking queryable for entities that only use DeletedOn (no IsActive).
        /// Filters out soft-deleted rows.
        /// </summary>
        protected static IQueryable<T> All<T>(ApplicationDbContext context)
            where T : AuditableEntity =>
            context.Set<T>()
                .Where(e => e.DeletedOn == null)
                .AsNoTracking();

        /// <summary>
        /// Tracking queryable filtered to non-deleted rows.
        /// Use in write operations where EF must detect changes.
        /// </summary>
        protected static IQueryable<T> AllTracked<T>(ApplicationDbContext context)
            where T : AuditableEntity =>
            context.Set<T>()
                .Where(e => e.DeletedOn == null);

        /// <summary>
        /// No-tracking queryable that includes soft-deleted rows.
        /// Use only when necessary (e.g., restore or delete operations).
        /// </summary>
        protected static IQueryable<T> AllIncludingDeleted<T>(ApplicationDbContext context)
            where T : AuditableEntity =>
            context.Set<T>()
                .AsNoTracking();

        // ── Write helpers ──────────────────────────────────────────────────────────

        protected static void Insert<T>(ApplicationDbContext context, T entity)
            where T : AuditableEntity =>
            context.Set<T>().Add(entity);

        protected static void Update<T>(ApplicationDbContext context, T entity)
            where T : AuditableEntity =>
            context.Set<T>().Update(entity);

        protected static Task<int> SaveAsync(ApplicationDbContext context, CancellationToken ct = default) =>
            context.SaveChangesAsync(ct);
    }
}