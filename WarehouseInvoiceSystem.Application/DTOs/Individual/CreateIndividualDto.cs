namespace WarehouseInvoiceSystem.Application.DTOs.Individual
{
    public class CreateIndividualDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string IdentificationNumber { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? BankAccount { get; set; }
        public bool IsActive { get; set; } = true;
    }
}