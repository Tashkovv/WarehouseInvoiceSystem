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
        public const string PublicKeyBase64 = "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAvwfIYZcZa6wtWYmgzSj5xHtAuXqUmxzy7/BrK8krLiYpr2Luvkdaxl6lnxXdrrTgJb9JebtTXX1dxJOr0idA5DFgZiAn/7LdVMvX3/ppycvL5blqKg/RDeFpnxRMDN16h84D3cybVaAb9aK4BZ3cm2w346DR6c8PioUJkdphvlInlL3X5ljpt3dEKRFpPhgtDbLo9P33E6PYDIoIcFwnyCpinIGkYzqAoNRC7Lhah5hLLA4KjS0D61tfPUq9Re7TVf9eVhn2wAW/ZRxfxu/pv1HD+oLW+u/j75tovfqlMkxjnCsdpjGyAf6dql5Ll3uKXHWadz2gJn9Hleqe5MS9JQIDAQAB";

        public const string ServerUrl = "https://wis-license-server.fly.dev/";
    }
}
