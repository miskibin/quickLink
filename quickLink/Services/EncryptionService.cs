using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace quickLink.Services
{
    public sealed class EncryptionService
    {
        // Simple AES encryption with a fixed key (for demo purposes)
        // In production, use Windows Data Protection API or user-specific keys
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("QuickLink2024Key"); // 16 bytes for AES-128
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("QuickLink2024IV!"); // 16 bytes

        // Reuse Aes instance (thread-safe for creating transforms)
        private static readonly Aes AesInstance = CreateAes();

        private static Aes CreateAes()
        {
            var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = IV;
            return aes;
        }

        public string Encrypt(string? plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText ?? string.Empty;

            try
            {
                using var encryptor = AesInstance.CreateEncryptor();
                using var msEncrypt = new MemoryStream();
                using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                using (var swEncrypt = new StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(plainText);
                }

                return Convert.ToBase64String(msEncrypt.ToArray());
            }
            catch (CryptographicException)
            {
                System.Diagnostics.Debug.WriteLine("Warning: Failed to encrypt data. Storing as plain text.");
                return plainText;
            }
        }

        public string Decrypt(string? cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText ?? string.Empty;

            try
            {
                using var decryptor = AesInstance.CreateDecryptor();
                using var msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText));
                using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                using var srDecrypt = new StreamReader(csDecrypt);

                return srDecrypt.ReadToEnd();
            }
            catch (FormatException)
            {
                // Not Base64 encoded - likely plain text
                return cipherText;
            }
            catch (CryptographicException)
            {
                System.Diagnostics.Debug.WriteLine("Warning: Failed to decrypt data. Returning cipher text.");
                return cipherText;
            }
        }

        public bool IsEncrypted(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            try
            {
                Convert.FromBase64String(text);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
