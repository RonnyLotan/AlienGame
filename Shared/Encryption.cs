using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public static class Encryption
    {
        private const int AesKeySize = 256;

        private const int RsaKeySize = 2048;

        public static (string privateKey, string publicKey) GenerateRsaKeyPair()
        {
            using (var rsa = new RSACryptoServiceProvider(RsaKeySize))
            {
                try
                {
                    var privateKeyStr = Convert.ToBase64String(rsa.ExportCspBlob(true));
                    var publicKeyStr = Convert.ToBase64String(rsa.ExportCspBlob(false));

                    return (privateKeyStr, publicKeyStr);
                }
                finally
                {
                    rsa.PersistKeyInCsp = false;
                }
            }
        }

        public static string RsaEncrypt(string data, string publicKeyBase64)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                try
                {
                    rsa.ImportCspBlob(Convert.FromBase64String(publicKeyBase64));

                    byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                    byte[] encryptedData = rsa.Encrypt(dataBytes, false);

                    return Convert.ToBase64String(encryptedData);
                }
                finally
                {
                    rsa.PersistKeyInCsp = false;
                }
            }
        }
        public static string RsaDecrypt(string encryptedDataBase64, string privateKeyBase64)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                try
                {
                    rsa.ImportCspBlob(Convert.FromBase64String(privateKeyBase64));

                    byte[] encryptedData = Convert.FromBase64String(encryptedDataBase64);
                    byte[] decryptedData = rsa.Decrypt(encryptedData, false);

                    return Encoding.UTF8.GetString(decryptedData);
                }
                finally
                {
                    rsa.PersistKeyInCsp = false;
                }
            }
        }


        public static string GenerateAesKey()
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = AesKeySize;
                aes.GenerateKey();
                return Convert.ToBase64String(aes.Key);
            }
        }
        public static string AesEncrypt(string data, string keyBase64)
        {
            byte[] key = Convert.FromBase64String(keyBase64);

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                aes.GenerateIV();
                byte[] iv = aes.IV;

                using (var encryptor = aes.CreateEncryptor())
                using (var memoryStream = new MemoryStream())
                {
                    memoryStream.Write(iv, 0, iv.Length);

                    using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    using (var writer = new StreamWriter(cryptoStream))
                    {
                        writer.Write(data);
                    }

                    return Convert.ToBase64String(memoryStream.ToArray());
                }
            }
        }
        public static string AesDecrypt(string encryptedDataBase64, string keyBase64)
        {
            byte[] encryptedData = Convert.FromBase64String(encryptedDataBase64);
            byte[] key = Convert.FromBase64String(keyBase64);

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                byte[] iv = new byte[aes.BlockSize / 8];
                Array.Copy(encryptedData, 0, iv, 0, iv.Length);
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                using (var memoryStream = new MemoryStream())
                {
                    using (var cryptoStream = new CryptoStream(
                        new MemoryStream(encryptedData, iv.Length, encryptedData.Length - iv.Length),
                        decryptor, CryptoStreamMode.Read))
                    using (var reader = new StreamReader(cryptoStream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }
        public static string ComputeHash(string password, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + salt));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        public static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            Random random = new Random();
            char[] result = new char[length];

            for (int i = 0; i < length; i++)
            {
                result[i] = chars[random.Next(chars.Length)];
            }

            return new string(result);
        }
    }
}
