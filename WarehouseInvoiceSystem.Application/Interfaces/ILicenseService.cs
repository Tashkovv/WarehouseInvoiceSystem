namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Domain.Entities;

    public enum LicenseStatus
    {
        NotActivated,
        Active,
        Warning,
        Locked
    }

    public interface ILicenseService
    {
        event Action? OnStatusChanged;

        LicenseStatus Status { get; }
        LicenseInfo? CurrentLicense { get; }
        string? LockReason { get; }
        int? GraceDaysRemaining { get; }
        string GetHardwareId();

        Task ValidateAsync(CancellationToken ct = default);
        Task<bool> ActivateAsync(string token, CancellationToken ct = default);
    }
}
