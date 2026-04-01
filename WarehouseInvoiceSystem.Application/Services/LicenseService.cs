namespace WarehouseInvoiceSystem.Application.Services
{
    using System.Globalization;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.Json;
    using Microsoft.Extensions.Logging;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Application.Settings;
    using WarehouseInvoiceSystem.Domain.Entities;

    /// <summary>
    /// Singleton license service. Validates the RSA-signed token from app-state.json,
    /// enforces hardware binding, expiry + grace period, and monotonic time guard.
    /// </summary>
    public class LicenseService(
        IAppStateService appState,
        IHardwareIdService hardwareIdService,
        ILogger<LicenseService> logger) : ILicenseService
    {
        private const string TokenKey = "LicenseToken";
        private const string LastSeenKey = "LicenseLastSeenUtc";
        private static readonly TimeSpan ClockDriftTolerance = TimeSpan.FromMinutes(5);

        public LicenseStatus Status { get; private set; } = LicenseStatus.NotActivated;
        public LicenseInfo? CurrentLicense { get; private set; }
        public string? LockReason { get; private set; }
        public int? GraceDaysRemaining { get; private set; }

        public string GetHardwareId() => hardwareIdService.GetHardwareId();

        public async Task ValidateAsync(CancellationToken ct = default)
        {
            try
            {
                string? token = await appState.GetStringAsync(TokenKey, ct);

                if (string.IsNullOrWhiteSpace(token))
                {
                    SetLocked(LicenseStatus.NotActivated, "LicenseNotActivated");
                    return;
                }

                // Split token: payload.signature
                int dotIndex = token.IndexOf('.');
                if (dotIndex < 0)
                {
                    SetLocked(LicenseStatus.Locked, "LicenseInvalidFormat");
                    return;
                }

                string payloadBase64Url = token[..dotIndex];
                string signatureBase64Url = token[(dotIndex + 1)..];

                // Verify RSA signature
                if (!VerifySignature(payloadBase64Url, signatureBase64Url))
                {
                    SetLocked(LicenseStatus.Locked, "LicenseInvalidSignature");
                    return;
                }

                // Deserialize payload
                byte[] payloadBytes = Base64UrlDecode(payloadBase64Url);
                LicensePayload? payload = JsonSerializer.Deserialize<LicensePayload>(payloadBytes);
                if (payload is null)
                {
                    SetLocked(LicenseStatus.Locked, "LicenseInvalidFormat");
                    return;
                }

                // Build LicenseInfo
                DateTime expiryDate = DateTime.ParseExact(payload.exp, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

                CurrentLicense = new LicenseInfo
                {
                    TenantId = payload.tid,
                    HardwareId = payload.hwid,
                    ExpiryDate = expiryDate,
                    GraceDays = payload.grace
                };

                // Hardware check
                string currentHwId = hardwareIdService.GetHardwareId();
                if (!string.Equals(payload.hwid, currentHwId, StringComparison.OrdinalIgnoreCase))
                {
                    SetLocked(LicenseStatus.Locked, "LicenseHardwareMismatch");
                    return;
                }

                // Monotonic time guard
                if (await IsClockTamperedAsync(ct))
                {
                    CurrentLicense.ClockTamperingDetected = true;
                    SetLocked(LicenseStatus.Locked, "LicenseClockTampering");
                    return;
                }

                // Update last seen
                await appState.SetStringAsync(LastSeenKey,
                    DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));

                // Check expiry + grace
                DateTime now = DateTime.UtcNow;
                DateTime graceEnd = expiryDate.AddDays(payload.grace);

                if (now.Date > graceEnd.Date)
                {
                    SetLocked(LicenseStatus.Locked, "LicenseExpired");
                    return;
                }

                if (now.Date > expiryDate.Date)
                {
                    int remaining = (graceEnd.Date - now.Date).Days;
                    GraceDaysRemaining = remaining;
                    Status = LicenseStatus.Warning;
                    LockReason = null;
                    return;
                }

                // All good
                Status = LicenseStatus.Active;
                LockReason = null;
                GraceDaysRemaining = null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "License validation failed");
                SetLocked(LicenseStatus.Locked, "LicenseValidationError");
            }
        }

        public async Task<bool> ActivateAsync(string token, CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                    return false;

                // Validate the token before storing
                string? previousToken = await appState.GetStringAsync(TokenKey, ct);
                await appState.SetStringAsync(TokenKey, token.Trim());
                await ValidateAsync(ct);

                if (Status is LicenseStatus.Active or LicenseStatus.Warning)
                    return true;

                // Token invalid — restore previous state
                await appState.SetStringAsync(TokenKey, previousToken ?? "");
                await ValidateAsync(ct);
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "License activation failed");
                return false;
            }
        }

        private void SetLocked(LicenseStatus status, string reason)
        {
            Status = status;
            LockReason = reason;
            GraceDaysRemaining = null;
        }

        private async Task<bool> IsClockTamperedAsync(CancellationToken ct)
        {
            string? lastSeenStr = await appState.GetStringAsync(LastSeenKey, ct);
            if (lastSeenStr is null)
                return false; // First run — no baseline yet

            if (!DateTime.TryParse(lastSeenStr, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out DateTime lastSeen))
                return false;

            return DateTime.UtcNow < lastSeen - ClockDriftTolerance;
        }

        private static bool VerifySignature(string payloadBase64Url, string signatureBase64Url)
        {
            if (string.Equals(LicenseSettings.PublicKeyBase64, "PLACEHOLDER", StringComparison.Ordinal))
                return false;

            try
            {
                using RSA rsa = RSA.Create();
                rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(LicenseSettings.PublicKeyBase64), out _);
                byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadBase64Url);
                byte[] signatureBytes = Base64UrlDecode(signatureBase64Url);
                return rsa.VerifyData(payloadBytes, signatureBytes,
                    HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            }
            catch
            {
                return false;
            }
        }

        private static byte[] Base64UrlDecode(string input)
        {
            string base64 = input.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }

        private sealed record LicensePayload(string tid, string hwid, string exp, int grace);
    }
}
