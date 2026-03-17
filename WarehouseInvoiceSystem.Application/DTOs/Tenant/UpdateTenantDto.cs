namespace WarehouseInvoiceSystem.Application.DTOs.Tenant
{
    public class UpdateTenantDto
    {
        public string CompanyName { get; set; } = string.Empty;
        public string? OperatorName { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Website { get; set; }
        public string? Email { get; set; }

        /// <summary>
        /// Plain-text Gmail app password from the Profile form.
        /// Null or empty means "leave the existing encrypted password unchanged".
        /// TenantService encrypts this before persisting.
        /// </summary>
        public string? EmailPassword { get; set; }
    }
}