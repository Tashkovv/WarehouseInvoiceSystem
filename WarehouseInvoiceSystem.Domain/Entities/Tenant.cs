namespace WarehouseInvoiceSystem.Domain.Entities
{
    using WarehouseInvoiceSystem.Domain.Common;
    using WarehouseInvoiceSystem.Domain.Enums;

    public class Tenant : AuditableEntity
    {
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
        public string? EmailPasswordEncrypted { get; set; }    
        public byte[]? LogoData { get; set; }
        public string? LogoMimeType { get; set; }

        // VAT / ДДВ configuration
        public bool VatRegistered { get; set; }
        public VatPayerPeriod VatPayerPeriod { get; set; } = VatPayerPeriod.Quarterly;
        public DateTime? VatRegistrationDate { get; set; }
    }
}