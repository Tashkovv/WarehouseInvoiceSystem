namespace WarehouseInvoiceSystem.Domain.Entities
{
    using WarehouseInvoiceSystem.Domain.Common;

    public class Individual : AuditableEntity
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string IdentificationNumber { get; set; } = string.Empty;

        // Optional fields
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? BankAccount { get; set; }

        public bool IsActive { get; set; } = true;

        // Computed property
        public string FullName => $"{FirstName} {LastName}";

        // Navigation properties
        public ICollection<PurchaseNote> PurchaseNotes { get; set; } = [];
    }
}