using System.Security.Cryptography;
using System.Text;

namespace IdpUtilities
{
    public static class EmailHashUtil
    {
        public static string HashEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email cannot be null or whitespace.", nameof(email));
            }

            var normalizedEmail = email.Trim().ToLowerInvariant();
            
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(normalizedEmail));
            
            return Convert.ToHexString(hashBytes);
        }
    }
}