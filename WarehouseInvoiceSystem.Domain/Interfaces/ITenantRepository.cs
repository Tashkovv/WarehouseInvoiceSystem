namespace WarehouseInvoiceSystem.Domain.Interfaces
{
    using WarehouseInvoiceSystem.Domain.Entities;

    public interface ITenantRepository
    {
        Task<Tenant> GetAsync(CancellationToken ct = default);
        Task UpdateAsync(Tenant tenant, CancellationToken ct = default);
    }
}