namespace WarehouseInvoiceSystem.Application.Services
{
    using WarehouseInvoiceSystem.Application.DTOs.Tenant;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Interfaces;

    public class TenantService(
        ITenantRepository tenantRepository,
        IEncryptionService encryptionService) : ITenantService
    {
        public async Task<TenantDto> GetAsync(CancellationToken ct = default)
        {
            Tenant tenant = await tenantRepository.GetAsync(ct);
            return MapToDto(tenant);
        }

        public async Task UpdateAsync(UpdateTenantDto dto, CancellationToken ct = default)
        {
            Tenant tenant = await tenantRepository.GetAsync(ct);

            tenant.CompanyName = dto.CompanyName;
            tenant.OperatorName = dto.OperatorName;
            tenant.Address = dto.Address;
            tenant.Phone = dto.Phone;
            tenant.Website = dto.Website;
            tenant.Email = dto.Email;

            // Only replace the stored password when the caller supplies a new one.
            // An empty or null EmailPassword means "leave the existing value alone".
            if (!string.IsNullOrWhiteSpace(dto.EmailPassword))
                tenant.EmailPasswordEncrypted = encryptionService.Encrypt(dto.EmailPassword);

            await tenantRepository.UpdateAsync(tenant, ct);
        }

        public async Task UpdateLogoAsync(byte[] logoData, string mimeType, CancellationToken ct = default)
        {
            Tenant tenant = await tenantRepository.GetAsync(ct);
            tenant.LogoData = logoData;
            tenant.LogoMimeType = mimeType;
            await tenantRepository.UpdateAsync(tenant, ct);
        }

        public async Task RemoveLogoAsync(CancellationToken ct = default)
        {
            Tenant tenant = await tenantRepository.GetAsync(ct);
            tenant.LogoData = null;
            tenant.LogoMimeType = null;
            await tenantRepository.UpdateAsync(tenant, ct);
        }

        // ── Mapping ───────────────────────────────────────────────────────────

        private static TenantDto MapToDto(Tenant tenant) => new()
        {
            Id = tenant.Id,
            CompanyName = tenant.CompanyName,
            OperatorName = tenant.OperatorName,
            Address = tenant.Address,
            Phone = tenant.Phone,
            Website = tenant.Website,
            Email = tenant.Email,
            HasEmailPassword = !string.IsNullOrEmpty(tenant.EmailPasswordEncrypted),
            LogoData = tenant.LogoData,
            LogoMimeType = tenant.LogoMimeType,
        };
    }
}