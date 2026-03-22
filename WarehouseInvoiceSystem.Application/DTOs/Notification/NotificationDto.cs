namespace WarehouseInvoiceSystem.Application.DTOs.Notification
{
    using WarehouseInvoiceSystem.Domain.Enums;

    public class NotificationDto
    {
        public Guid Id { get; set; }
        public NotificationType Type { get; set; }
        public string? Data { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public bool IsEmailSent { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<NotificationInvoiceDto> Invoices { get; set; } = [];
    }
}
