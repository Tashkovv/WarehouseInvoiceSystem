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
        public const string PublicKeyBase64 = "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA4hmqYdSkiGiTNtFtabSXhcs1AA7q+QoLBCoTQBlDspbb9Br+QM3WN5M4gHVU9nvStdgB/C72Swu5WHVwWmHZiZJEXVxin2MWSyBDfqOp3UwEgPo20sW01qX0b/wXApw6kOCALBhEnw3oSNBJa2k1BTl4Uc4T7SIeCfCGckgTQsQNg6FRMq23+cmcqLy7WS/q4JdDFTJU+c9vFuBXKqjXzygOj9eyu5n+kkanbDwpcf1DVZ7juXTC6Jg3Ki7g/NA9I4BOYPSOy2Av4dpoe9UojYWuLwMj9tfr7fOhCOxZC/22Dr851IO95HJeEA8ywVndlnq1BlQgBnjGyMEQQ6ya3QIDAQAB";
    }
}
