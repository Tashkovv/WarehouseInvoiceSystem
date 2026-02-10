namespace WarehouseInvoiceSystem.Application.DTOs.Company
{
    using WarehouseInvoiceSystem.Domain.Company.Enums;

    public class UpdateCompanyDto
    {
        public string Name { get; set; } = string.Empty;
        public CompanyType Type { get; set; }
        public string? ContactPerson { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? TaxId { get; set; }
        public int PaymentTermsDays { get; set; }
        public decimal CreditLimit { get; set; }
        public bool IsActive { get; set; }
    }
}
