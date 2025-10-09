using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;
using System.Text;

namespace SuperAppAPI.Helpers
{
    /// <summary>
    /// Security helper utilities for password hashing, token generation, and other security operations
    /// </summary>
    public static class SecurityHelper
    {
        private const int SaltSize = 16; // 128 bits
        private const int HashSize = 32; // 256 bits
        private const int Iterations = 100000; // OWASP recommendation (2023)

        /// <summary>
        /// Hashes a password using PBKDF2 with SHA256
        /// This method generates a random salt and combines it with the hash
        /// </summary>
        /// <param name="password">Plain text password to hash</param>
        /// <returns>Base64 encoded string containing version + salt + hash</returns>
        /// <exception cref="ArgumentException">Thrown when password is null or empty</exception>
        public static string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));

            // Generate a random salt
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

            // Generate hash using PBKDF2 with SHA256
            var hash = new Rfc2898DeriveBytes(
                password, 
                salt, 
                Iterations, 
                HashAlgorithmName.SHA256);
            byte[] hashBytes = hash.GetBytes(HashSize);

            // Combine version + salt + hash
            byte[] result = new byte[1 + SaltSize + HashSize];
            result[0] = 0x01; // Version identifier for future compatibility
            Buffer.BlockCopy(salt, 0, result, 1, SaltSize);
            Buffer.BlockCopy(hashBytes, 0, result, 1 + SaltSize, HashSize);

            return Convert.ToBase64String(result);
        }

        /// <summary>
        /// Verifies a password against a stored hash
        /// Uses constant-time comparison to prevent timing attacks
        /// </summary>
        /// <param name="password">Plain text password to verify</param>
        /// <param name="hashedPassword">Stored password hash from database</param>
        /// <returns>True if password matches, false otherwise</returns>
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            if (string.IsNullOrWhiteSpace(hashedPassword))
                return false;

            try
            {
                // Decode the stored hash
                byte[] hashBytes = Convert.FromBase64String(hashedPassword);

                // Validate format (version + salt + hash)
                if (hashBytes.Length != 1 + SaltSize + HashSize)
                    return false;

                // Check version (for future compatibility)
                if (hashBytes[0] != 0x01)
                    return false;

                // Extract salt and stored hash
                byte[] salt = new byte[SaltSize];
                Buffer.BlockCopy(hashBytes, 1, salt, 0, SaltSize);

                byte[] storedHash = new byte[HashSize];
                Buffer.BlockCopy(hashBytes, 1 + SaltSize, storedHash, 0, HashSize);

                // Compute hash of provided password with same salt
                var hash = new Rfc2898DeriveBytes(
                    password, 
                    salt, 
                    Iterations, 
                    HashAlgorithmName.SHA256);
                byte[] computedHash = hash.GetBytes(HashSize);

                // Use constant-time comparison to prevent timing attacks
                return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
            }
            catch (Exception)
            {
                // Invalid format or other error - return false
                return false;
            }
        }

        /// <summary>
        /// Generates a cryptographically secure random string for tokens, session IDs, etc.
        /// </summary>
        /// <param name="length">Length of the random string (default: 32)</param>
        /// <returns>Base64 encoded random string</returns>
        public static string GenerateSecureRandomString(int length = 32)
        {
            if (length <= 0)
                throw new ArgumentException("Length must be positive", nameof(length));

            byte[] randomBytes = RandomNumberGenerator.GetBytes(length);
            return Convert.ToBase64String(randomBytes);
        }

        /// <summary>
        /// Generates a secure random token for refresh tokens, API keys, etc.
        /// </summary>
        /// <returns>URL-safe Base64 encoded token</returns>
        public static string GenerateRefreshToken()
        {
            byte[] randomBytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(randomBytes)
                .TrimEnd('=') // Remove padding
                .Replace('+', '-')
                .Replace('/', '_'); // Make URL-safe
        }

        /// <summary>
        /// Sanitizes input string to prevent XSS and injection attacks
        /// </summary>
        /// <param name="input">Input string to sanitize</param>
        /// <returns>Sanitized string</returns>
        public static string SanitizeInput(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Remove potentially dangerous characters
            var sanitized = input
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("&", "&amp;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#x27;")
                .Replace("/", "&#x2F;");

            return sanitized.Trim();
        }

        /// <summary>
        /// Validates that an email address has a safe format
        /// </summary>
        /// <param name="email">Email address to validate</param>
        /// <returns>True if email format is valid and safe</returns>
        public static bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            if (email.Length > 254) // RFC 5321 limit
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Generates a secure hash of sensitive data for logging purposes
        /// This allows logging user identifiers without exposing actual values
        /// </summary>
        /// <param name="sensitiveData">Sensitive data to hash</param>
        /// <returns>SHA256 hash of the data for safe logging</returns>
        public static string GetLoggingSafeHash(string? sensitiveData)
        {
            if (string.IsNullOrEmpty(sensitiveData))
                return "empty";

            using var sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sensitiveData));
            return Convert.ToHexString(hashBytes)[..8]; // First 8 characters for brevity
        }
    }
}