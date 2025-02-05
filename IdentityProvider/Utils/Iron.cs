using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using IdentityProvider.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

/// <summary>This program is a C# implementation of @hapi/iron.</summary>
/// <remarks>
/// Encrypts and decrypts JSON objects into strings like the following.
/// <c>Fe26.<Version>*<Key Id>*<Encryption_Key_Salt>*<Encryption_IV>*<Encrypted_Payload>*<Expiry_Time>*<MAC_Key_Salt>*<MAC></c>
/// </remarks>
/// <see href="https://github.com/hapijs/iron">The original code is available at https://github.com/hapijs/iron.</see>
///
public class Iron
{
    public class Options
    {
        public int SaltBits { get; set; } = 256;
        public string Algorithm { get; set; } = "aes-256-cbc";
        public int Iterations { get; set; } = 1;
        public int MinPasswordLength { get; set; } = 32;
        public int Ttl { get; set; } = 0;
        public int TimestampSkewSec { get; set; } = 60;
        public int LocalTimeOffsetMsec { get; set; } = 0;
        public string Salt { get; set; }
        public string Iv { get; set; }
    }

    private static readonly string MacPrefix = "Fe26.2";

    public static async Task<string> Seal(State state, string password, Options options)
    {
        options = options ?? new Options();
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + options.LocalTimeOffsetMsec;

        var objectString = JsonSerializer.Serialize<State>(state);
        var key = await GenerateKey(password, options);
        var encrypted = EncryptStringToBytes_Aes(objectString, key.Key, key.Iv);

        var encryptedB64 = Convert.ToBase64String(encrypted);
        var iv = Convert.ToBase64String(key.Iv);
        var expiration = options.Ttl > 0 ? (now + options.Ttl).ToString() : "";
        var macBaseString = $"{MacPrefix}*{key.Salt}*{iv}*{encryptedB64}*{expiration}";

        var mac = await HmacWithPassword(password, options, macBaseString);

        return $"{macBaseString}*{mac.Salt}*{mac.Digest}";
    }

    public static async Task<State> Unseal<State>(string sealedData, string password, Options options)
    {
        options = options ?? new Options();
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + options.LocalTimeOffsetMsec;

        var parts = sealedData.Split('*');
        if (parts.Length != 7)
        {
            throw new Exception("Incorrect number of sealed components");
        }

        var macPrefix = parts[0];
        var encryptionSalt = parts[1];
        var encryptionIv = parts[2];
        var encryptedB64 = parts[3];
        var expiration = parts[4];
        var hmacSalt = parts[5];
        var hmac = parts[6];
        var macBaseString = $"{macPrefix}*{encryptionSalt}*{encryptionIv}*{encryptedB64}*{expiration}";

        if (macPrefix != MacPrefix)
        {
            throw new Exception("Wrong mac prefix");
        }

        if (!string.IsNullOrEmpty(expiration) && long.TryParse(expiration, out var exp) && exp <= (now - (options.TimestampSkewSec * 1000)))
        {
            throw new Exception("Expired seal");
        }

        var macOptions = new Options { Algorithm = options.Algorithm, Salt = hmacSalt };
        var mac = await HmacWithPassword(password, macOptions, macBaseString);

        if (!Cryptiles.FixedTimeComparison(mac.Digest, hmac))
        {
            throw new Exception("Bad hmac value");
        }

        var encrypted = Convert.FromBase64String(encryptedB64);
        var decryptOptions = new Options { Algorithm = options.Algorithm, Salt = encryptionSalt, Iv = encryptionIv };
        var key = await GenerateKey(password, decryptOptions);
        var decrypted = DecryptStringFromBytes_Aes(encrypted, key.Key, key.Iv);

        return JsonSerializer.Deserialize<State>(decrypted);
    }

    private static async Task<(byte[] Key, byte[] Iv, string Salt)> GenerateKey(string password, Options options)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new Exception("Empty password");
        }

        if (password.Length < options.MinPasswordLength)
        {
            throw new Exception($"Password string too short (min {options.MinPasswordLength} characters required)");
        }

        var salt = options.Salt ?? GenerateRandomSalt(options.SaltBits);
        var key = await Pbkdf2(password, salt, options.Iterations, options.Algorithm);
        var iv = string.IsNullOrEmpty(options.Iv) ? GenerateRandomIv(options.Algorithm) : Convert.FromBase64String(options.Iv);

        return (key, iv, salt);
    }

    private static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
    {
        using (var aesAlg = Aes.Create())
        {
            aesAlg.Key = Key;
            aesAlg.IV = IV;

            var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using (var msEncrypt = new MemoryStream())
            {
                using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (var swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }
                    return msEncrypt.ToArray();
                }
            }
        }
    }

    private static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
    {
        using (var aesAlg = Aes.Create())
        {
            aesAlg.Key = Key;
            aesAlg.IV = IV;

            var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using (var msDecrypt = new MemoryStream(cipherText))
            {
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (var srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
        }
    }

    private static async Task<byte[]> Pbkdf2(string password, string salt, int iterations, string algorithm)
    {
        using (var rfc2898DeriveBytes = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(salt), iterations, HashAlgorithmName.SHA256))
        {
            return rfc2898DeriveBytes.GetBytes(32);
        }
    }

    private static async Task<(string Digest, string Salt)> HmacWithPassword(string password, Options options, string data)
    {
        var key = await GenerateKey(password, options);
        using (var hmac = new HMACSHA256(key.Key))
        {
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            var digest = Convert.ToBase64String(hash).Replace('+', '-').Replace('/', '_').Replace("=", string.Empty);
            return (digest, key.Salt);
        }
    }

    private static string GenerateRandomSalt(int bits)
    {
        var salt = new byte[bits / 8];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }
        return Convert.ToBase64String(salt);
    }

    private static byte[] GenerateRandomIv(string algorithm)
    {
        var iv = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(iv);
        }
        return iv;
    }
}

public static class Cryptiles
{
    public static bool FixedTimeComparison(string a, string b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }

        var result = 0;
        for (var i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }
        return result == 0;
    }
}

