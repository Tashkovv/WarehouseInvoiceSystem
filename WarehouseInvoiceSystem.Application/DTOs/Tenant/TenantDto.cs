namespace WarehouseInvoiceSystem.Application.DTOs.Tenant
{
    public class TenantDto
    {
        public Guid Id { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? OperatorName { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Website { get; set; }
        public string? Email { get; set; }

        /// <summary>
        /// True when an encrypted password is stored — the raw value is never
        /// sent to the client.
        /// </summary>
        public bool HasEmailPassword { get; set; }

        public byte[]? LogoData { get; set; }
        public string? LogoMimeType { get; set; }
    }
}