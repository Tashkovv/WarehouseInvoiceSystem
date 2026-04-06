namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Infrastructure.Data;

    public class TenantRepository(IDbContextFactory<ApplicationDbContext> factory, IAuditContextService auditContext)
        : BaseRepository(factory, auditContext), ITenantRepository
    {
        /// <summary>
        /// Returns the singleton Tenant row. If no row exists yet (e.g. on a fresh
        /// database before the migration seed runs), one is created automatically
        /// so callers never receive null.
        /// </summary>
        public Task<Tenant> GetAsync(CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                Tenant? tenant = await context.Tenants.FirstOrDefaultAsync(ct);

                if (tenant is null)
                {
                    tenant = new Tenant { CompanyName = "My Company" };
                    context.Tenants.Add(tenant);
                    await SaveAsync(context, ct);
                }

                return tenant;
            }, ct);

        public Task UpdateAsync(Tenant tenant, CancellationToken ct = default) =>
            WithContextAsync(async context =>
            {
                Tenant? tracked = await context.Tenants.FindAsync(tenant.Id)
                    ?? throw new KeyNotFoundException("Tenant record not found.");

                context.Entry(tracked).CurrentValues.SetValues(tenant);

                // Byte arrays are reference-compared by EF — mark explicitly modified
                // so the logo column is included in the UPDATE even when the array
                // reference changes but the content appears identical.
                context.Entry(tracked).Property(t => t.LogoData).IsModified = true;

                await SaveAsync(context, ct);
            }, ct);
    }
}