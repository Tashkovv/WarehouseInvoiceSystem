namespace WarehouseInvoiceSystem.Application.Interfaces
{
    public interface IAuditContextService
    {
        string? CurrentUsername { get; }
        void SetUsername(string username);
    }
}
