using WarehouseInvoiceSystem.Domain.Common;

namespace WarehouseInvoiceSystem.Domain.User.Domain
{
    public class User : AuditableEntity
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "Viewer"; // Admin, Manager, Accountant, Viewer
        public bool IsActive { get; set; } = true;
        public DateTime? LastLogin { get; set; }
    }
}
