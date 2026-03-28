namespace WarehouseInvoiceSystem.Application.Services
{
    using System.Management;
    using System.Runtime.Versioning;
    using System.Security.Cryptography;
    using System.Text;
    using WarehouseInvoiceSystem.Application.Interfaces;

    /// <summary>
    /// Generates a SHA256 hardware fingerprint from motherboard serial + OS MachineGuid.
    /// Singleton — the fingerprint is computed once and cached.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class HardwareIdService : IHardwareIdService
    {
        private string? _cachedId;

        public string GetHardwareId()
        {
            if (_cachedId is not null)
                return _cachedId;

            string motherboard = GetMotherboardSerial();
            string machineGuid = GetMachineGuid();

            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(motherboard + machineGuid));
            _cachedId = Convert.ToHexStringLower(hash);
            return _cachedId;
        }

        private static string GetMotherboardSerial()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
                foreach (ManagementObject obj in searcher.Get())
                {
                    string? serial = obj["SerialNumber"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(serial))
                        return serial.Trim();
                }
            }
            catch
            {
                // WMI unavailable — fall through to empty string
            }

            return "";
        }

        private static string GetMachineGuid()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Cryptography");
                return key?.GetValue("MachineGuid")?.ToString() ?? "";
            }
            catch
            {
                return "";
            }
        }
    }
}
