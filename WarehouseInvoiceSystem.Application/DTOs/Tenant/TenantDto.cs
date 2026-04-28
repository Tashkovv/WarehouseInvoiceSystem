namespace WarehouseInvoiceSystem.Application.DTOs.Tenant
{
    using WarehouseInvoiceSystem.Domain.Enums;

    public class TenantDto
    {
        public Guid Id { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? OperatorName { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? TaxId { get; set; }
        public string? Embs { get; set; }
        public string? BankAccount { get; set; }
        public string? BankName { get; set; }
        public string? BankBranch { get; set; }
        public string? Email { get; set; }

        /// <summary>
        /// True when an encrypted password is stored — the raw value is never
        /// sent to the client.
        /// </summary>
        public bool HasEmailPassword { get; set; }

        public byte[]? LogoData { get; set; }
        public string? LogoMimeType { get; set; }

        public bool VatRegistered { get; set; }
        public VatPayerPeriod VatPayerPeriod { get; set; }
        public DateTime? VatRegistrationDate { get; set; }
    }
}