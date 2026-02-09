namespace WarehouseInvoiceSystem.Domain.Common
{
    public abstract class AuditableEntity : Entity
    {
        public DateTime CreatedAt { get; protected set; }
        public DateTime? DeletedOn { get; protected set; }

        public bool IsDeleted => DeletedOn.HasValue;
    }
}
