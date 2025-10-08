using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace UserProfileDataRepositories
{
    public static class Helpers
    {

        public static DateTime DateOnlyToDateTime(DateOnly dateOnly)
        {
            return dateOnly.ToDateTime(TimeOnly.MinValue); // TimeOnly.MinValue = 00:00:00
        }

        public static string HashPassword(string password)
        {
            // Generate a salt
            byte[] salt = RandomNumberGenerator.GetBytes(16); // 128-bit salt

            // Derive a 256-bit subkey (hash) using PBKDF2
            var hash = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            byte[] hashBytes = hash.GetBytes(32); // 256-bit

            // Combine salt + hash into one string (Base64)
            byte[] result = new byte[1 + salt.Length + hashBytes.Length];
            result[0] = 0x01; // version
            Buffer.BlockCopy(salt, 0, result, 1, salt.Length);
            Buffer.BlockCopy(hashBytes, 0, result, 1 + salt.Length, hashBytes.Length);

            return Convert.ToBase64String(result);

        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            // Decode Base64 string back to byte[]
            byte[] hashBytes = Convert.FromBase64String(hashedPassword);

            // Read version (optional, in case you add other formats later)
            if (hashBytes[0] != 0x01)
                throw new NotSupportedException("Unknown hash version");

            // Extract salt and hash
            byte[] salt = new byte[16];
            Buffer.BlockCopy(hashBytes, 1, salt, 0, salt.Length);

            byte[] storedHash = new byte[32];
            Buffer.BlockCopy(hashBytes, 1 + salt.Length, storedHash, 0, storedHash.Length);

            // Hash the input password using the same salt
            var hash = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            byte[] computedHash = hash.GetBytes(32);

            // Compare the computed hash with the stored one (constant-time comparison)
            return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
        }


    }
}
