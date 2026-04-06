namespace WarehouseInvoiceSystem.Application.DTOs.User
{
    using WarehouseInvoiceSystem.Domain.Enums;

    public class CreateUserDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.User;
    }
}
