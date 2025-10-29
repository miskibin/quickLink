using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace quickLink.Services
{
public class EncryptionService
    {
        // Simple AES encryption with a fixed key (for demo purposes)
        // In production, use Windows Data Protection API or user-specific keys
   private static readonly byte[] Key = Encoding.UTF8.GetBytes("QuickLink2024Key"); // 16 bytes for AES-128
   private static readonly byte[] IV = Encoding.UTF8.GetBytes("QuickLink2024IV!"); // 16 bytes

        public string Encrypt(string plainText)
      {
      if (string.IsNullOrEmpty(plainText))
    return plainText;

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
catch
   {
    return plainText; // Fallback to plain text on error
       }
   }

    public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
     return cipherText;

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
  catch
  {
         return cipherText; // Fallback to cipher text on error
          }
        }
    }
}
