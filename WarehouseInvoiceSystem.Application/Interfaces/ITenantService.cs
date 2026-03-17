namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Application.DTOs.Tenant;

    public interface ITenantService
    {
        Task<TenantDto> GetAsync(CancellationToken ct = default);
        Task UpdateAsync(UpdateTenantDto dto, CancellationToken ct = default);
        Task UpdateLogoAsync(byte[] logoData, string mimeType, CancellationToken ct = default);
        Task RemoveLogoAsync(CancellationToken ct = default);
    }
}