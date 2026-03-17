namespace WarehouseInvoiceSystem.Application.Services
{
    using System.Security.Cryptography;
    using System.Text;
    using Microsoft.Extensions.Options;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Application.Settings;

    /// <summary>
    /// AES-256 CBC encryption service.
    /// A fresh random IV is generated on every Encrypt call and prepended to
    /// the ciphertext before base64-encoding, so the output is self-contained.
    /// Decrypt splits the IV back out before decrypting.
    /// </summary>
    public class EncryptionService(IOptions<EncryptionSettings> settings) : IEncryptionService
    {
        // Key must be exactly 32 bytes for AES-256.
        // SHA-256 the configured string so any 32-char ASCII value works directly,
        // and shorter/longer keys are also accepted without crashing at startup.
        private readonly byte[] _key = SHA256.HashData(
            Encoding.UTF8.GetBytes(settings.Value.Key));

        public string Encrypt(string plainText)
        {
            ArgumentException.ThrowIfNullOrEmpty(plainText);

            using Aes aes = Aes.Create();
            aes.Key = _key;
            aes.GenerateIV(); // 16-byte random IV

            using MemoryStream ms = new();
            ms.Write(aes.IV, 0, aes.IV.Length); // prepend IV

            using (CryptoStream cs = new(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            using (StreamWriter sw = new(cs))
                sw.Write(plainText);

            return Convert.ToBase64String(ms.ToArray());
        }

        public string Decrypt(string cipherText)
        {
            ArgumentException.ThrowIfNullOrEmpty(cipherText);

            byte[] fullBytes = Convert.FromBase64String(cipherText);

            using Aes aes = Aes.Create();
            aes.Key = _key;

            // First 16 bytes are the IV
            byte[] iv = new byte[aes.BlockSize / 8];
            byte[] cipher = new byte[fullBytes.Length - iv.Length];
            Buffer.BlockCopy(fullBytes, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(fullBytes, iv.Length, cipher, 0, cipher.Length);
            aes.IV = iv;

            using MemoryStream ms = new(cipher);
            using CryptoStream cs = new(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using StreamReader sr = new(cs);
            return sr.ReadToEnd();
        }
    }
}