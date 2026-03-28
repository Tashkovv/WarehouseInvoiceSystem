namespace WarehouseInvoiceSystem.Application.Settings
{
    /// <summary>
    /// Compile-time constants for license verification.
    /// The RSA public key is embedded here so clients cannot swap it via config files.
    /// The private key lives only on the license server.
    /// </summary>
    public static class LicenseSettings
    {
        // RSA public key (DER SubjectPublicKeyInfo, base64). Replace with real key after generating the key pair.
        public const string PublicKeyBase64 = "PLACEHOLDER";

        public const string ServerUrl = "https://your-license-server.fly.dev";
    }
}
