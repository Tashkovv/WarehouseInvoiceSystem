namespace WarehouseInvoiceSystem.Domain.Entities
{
    using WarehouseInvoiceSystem.Domain.Common;

    public class Tenant : AuditableEntity
    {
        public string CompanyName { get; set; } = string.Empty;
        public string? OperatorName { get; set; }              
        public string? Address { get; set; }                   
        public string? Phone { get; set; }                     
        public string? Website { get; set; }                   
        public string? Email { get; set; }                     
        public string? EmailPasswordEncrypted { get; set; }    
        public byte[]? LogoData { get; set; }                  
        public string? LogoMimeType { get; set; }              
    }
}