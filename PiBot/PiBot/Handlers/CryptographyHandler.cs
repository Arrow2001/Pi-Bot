using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace PiBot.Handlers
{
    public class CryptographyHandler
    {
        // https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aes?view=net-10.0 
        // might take a while getting this to work
        // https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.icryptotransform?view=net-10.0 (adding the documentation so i can access for other days)
        // https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.rfc2898derivebytes?view=net-10.0

        public static string EncryptData(string data, byte[] key, byte[] iv) // iv should be generated each time so it's always diffrent
        {
            using (Aes aes = Aes.Create()) 
            {
                aes.Key = key;
                aes.IV = iv;

                ICryptoTransform encryptor =  aes.CreateEncryptor(aes.Key, aes.IV);

                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write)) // seems messy
                    using (var sw = new StreamWriter(cs))
                    {
                        sw.Write(data);
                    }
                    return Convert.ToBase64String(ms.ToArray()); // still not sure on how all this works
                }
            }
        }

        // decryption (hopefully)
        public static string DecryptData(string cipheredText, byte[] key, byte[] iv)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key,aes.IV);

                using (var ms = new MemoryStream(Convert.FromBase64String(cipheredText)))// needs to convert it from the text it was encrypted in
                {
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
        }

        public static byte[] DeriveAKey(string password, byte[] salt)
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, 300000, HashAlgorithmName.SHA256))
            {
                return deriveBytes.GetBytes(32); // due to the 256 bit key
            }
        }
    }
}
