using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace quickLink.Services
{
    public sealed class EncryptionService
    {
        #region Constants

        // Simple AES encryption with a fixed key (for demo purposes)
        // In production, use Windows Data Protection API or user-specific keys
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("QuickLink2024Key"); // 16 bytes for AES-128
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("QuickLink2024IV!"); // 16 bytes

        #endregion

        #region Public Methods

        public string Encrypt(string? plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText ?? string.Empty;

            try
            {
                using var aes = Aes.Create();
                aes.Key = Key;
                aes.IV = IV;

                using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
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
                // Fallback to plain text if encryption fails
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
                using var aes = Aes.Create();
                aes.Key = Key;
                aes.IV = IV;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
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
                // Failed to decrypt - data might be corrupted
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
                // Try to decode as Base64
                Convert.FromBase64String(text);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        #endregion
    }
}
