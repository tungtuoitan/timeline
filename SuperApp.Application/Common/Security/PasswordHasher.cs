using System.Security.Cryptography;

namespace SuperApp.Application.Common.Security
{
    public static class PasswordHasher
    {
        public static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);

            var hash = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            byte[] hashBytes = hash.GetBytes(32);

            byte[] result = new byte[1 + salt.Length + hashBytes.Length];
            result[0] = 0x01;
            Buffer.BlockCopy(salt, 0, result, 1, salt.Length);
            Buffer.BlockCopy(hashBytes, 0, result, 1 + salt.Length, hashBytes.Length);

            return Convert.ToBase64String(result);
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(hashedPassword))
                return false;

            try
            {
                byte[] hashBytes = Convert.FromBase64String(hashedPassword);

                if (hashBytes[0] != 0x01)
                    return false;

                byte[] salt = new byte[16];
                Buffer.BlockCopy(hashBytes, 1, salt, 0, salt.Length);

                byte[] storedHash = new byte[32];
                Buffer.BlockCopy(hashBytes, 1 + salt.Length, storedHash, 0, storedHash.Length);

                var hash = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
                byte[] computedHash = hash.GetBytes(32);

                return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
            }
            catch
            {
                return false;
            }
        }
    }
}
