namespace WarehouseInvoiceSystem.Application.DTOs.Company
{
    using WarehouseInvoiceSystem.Domain.Company.Enums;

    public class CreateCompanyDto
    {
        public string Name { get; set; } = string.Empty;
        public CompanyType Type { get; set; }
        public string? ContactPerson { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? TaxId { get; set; }
        public int PaymentTermsDays { get; set; } = 30;
        public decimal CreditLimit { get; set; }
    }
}
