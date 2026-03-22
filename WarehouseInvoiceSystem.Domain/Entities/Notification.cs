namespace WarehouseInvoiceSystem.Domain.Entities
{
    using WarehouseInvoiceSystem.Domain.Common;
    using WarehouseInvoiceSystem.Domain.Enums;

    public class Notification : AuditableEntity
    {
        public NotificationType Type { get; set; }
        public string? Data { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public bool IsEmailSent { get; set; }
        public DateTime? EmailSentAt { get; set; }

        // Navigation — populated for invoice-related notification types
        public ICollection<NotificationInvoice> NotificationInvoices { get; set; } = [];
    }
}
