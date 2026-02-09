namespace WarehouseInvoiceSystem.Domain.Common
{
    public abstract class AuditableEntity : Entity
    {
        public DateTime CreatedAt { get; set; }
        public DateTime? DeletedOn { get; set; }

        public bool IsDeleted => DeletedOn.HasValue;
    }
}
