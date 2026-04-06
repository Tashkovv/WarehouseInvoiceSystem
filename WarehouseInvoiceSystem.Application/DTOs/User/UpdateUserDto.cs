namespace WarehouseInvoiceSystem.Application.DTOs.User
{
    using WarehouseInvoiceSystem.Domain.Enums;

    public class UpdateUserDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool IsActive { get; set; }
    }
}
