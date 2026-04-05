namespace WarehouseInvoiceSystem.Application.DTOs.User
{
    public class LoginResultDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public UserDto? User { get; set; }
    }
}
