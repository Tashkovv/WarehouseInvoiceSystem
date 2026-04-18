namespace WarehouseInvoiceSystem.Domain.Entities
{
    using WarehouseInvoiceSystem.Domain.Common;

    public class Individual : AuditableEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string IdentificationNumber { get; set; } = string.Empty;

        // Optional fields
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? BankAccount { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<PurchaseNote> PurchaseNotes { get; set; } = [];
    }
}