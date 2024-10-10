using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace FrirendZoneHub.Server.Utils
{
    public class EncryptionHelper
    {
        // 32 tecken för AES-256 (256 bitar)
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("7CboWDwyMfsUsBXgi0fNa2UBt38Z4uM6");
        private readonly ILogger<EncryptionHelper> _logger;

        public EncryptionHelper(ILogger<EncryptionHelper> logger)
        {
            _logger = logger;
        }

        public string Encrypt(string plainText)
        {
            _logger.LogInformation($"Encrypting message: {plainText}");
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.GenerateIV(); // Slumpmässig IV
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    // Skriv IV till streamen
                    msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);

                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }

                    // Returnera IV + Ciphertext som Base64-sträng
                    var result = Convert.ToBase64String(msEncrypt.ToArray());
                    _logger.LogInformation($"Encrypted message: {result}");
                    return result;
                }
            }
        }

        public string Decrypt(string cipherText)
        {
            _logger.LogInformation($"Decrypting message: {cipherText}");
            byte[] fullCipher = Convert.FromBase64String(cipherText);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;

                // Första 16 byten är IV
                byte[] iv = new byte[16];
                Array.Copy(fullCipher, 0, iv, 0, iv.Length);
                aesAlg.IV = iv;

                // Resterande är ciphertext
                int cipherTextLength = fullCipher.Length - iv.Length;
                byte[] cipherBytes = new byte[cipherTextLength];
                Array.Copy(fullCipher, iv.Length, cipherBytes, 0, cipherTextLength);

                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherBytes))
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                {
                    string plaintext = srDecrypt.ReadToEnd();
                    _logger.LogInformation($"Decrypted message: {plaintext}");
                    return plaintext;
                }
            }
        }
    }
}
