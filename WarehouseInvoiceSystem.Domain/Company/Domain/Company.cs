namespace WarehouseInvoiceSystem.Domain.Company.Domain
{
    using WarehouseInvoiceSystem.Domain.Company.Enums;
    using WarehouseInvoiceSystem.Domain.Invoice.Domain;

    public class Company
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public CompanyType Type { get; set; }
        public string? ContactPerson { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? TaxId { get; set; }
        public int PaymentTermsDays { get; set; } = 30; // Net 30 default
        public decimal? CreditLimit { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? DeletedOn { get; set; }

        // Navigation properties
        public ICollection<Invoice> Invoices { get; set; } = [];
    }
}
