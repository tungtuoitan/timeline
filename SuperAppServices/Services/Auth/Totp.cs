using System.Security.Cryptography;
using System.Text;

namespace SuperAppServices.Services.Auth
{
    /// <summary>
    /// TOTP (RFC 6238: HMAC-SHA1, 30-second steps, 6 digits) — what every authenticator app speaks.
    /// Pure functions (clock passed in) so they are unit-tested against the RFC vectors. TungRoot #1489.
    /// </summary>
    public static class Totp
    {
        public const int StepSeconds = 30;
        public const int Digits = 6;
        /// <summary>Accept the previous and next step too (phone clock drift).</summary>
        public const int Window = 1;
        private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        /// <summary>160-bit random secret, base32 (no padding).</summary>
        public static string GenerateSecret() => Base32Encode(RandomNumberGenerator.GetBytes(20));

        public static long StepAt(DateTime utc) => new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)).ToUnixTimeSeconds() / StepSeconds;

        /// <summary>Code for one time step (RFC 4226 dynamic truncation).</summary>
        public static string Code(byte[] key, long step, int digits = Digits)
        {
            var counter = BitConverter.GetBytes(step);
            if (BitConverter.IsLittleEndian) Array.Reverse(counter);
            using var hmac = new HMACSHA1(key);
            var hash = hmac.ComputeHash(counter);
            var offset = hash[^1] & 0x0f;
            var binary = ((hash[offset] & 0x7f) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
            var mod = (int)Math.Pow(10, digits);
            return (binary % mod).ToString().PadLeft(digits, '0');
        }

        /// <summary>
        /// Step of the matching code within ±Window of now, or null. Steps at or before
        /// <paramref name="lastUsedStep"/> are refused (a code works once). Constant-time compare.
        /// </summary>
        public static long? Verify(string base32Secret, string? code, DateTime nowUtc, long? lastUsedStep)
        {
            var clean = (code ?? "").Replace(" ", "");
            if (clean.Length != Digits || !clean.All(char.IsDigit)) return null;
            var key = Base32Decode(base32Secret);
            var now = StepAt(nowUtc);
            for (var step = now - Window; step <= now + Window; step++)
            {
                if (lastUsedStep.HasValue && step <= lastUsedStep.Value) continue;
                if (CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Code(key, step)), Encoding.ASCII.GetBytes(clean)))
                    return step;
            }
            return null;
        }

        /// <summary>otpauth:// link for the QR code.</summary>
        public static string OtpAuthUri(string base32Secret, string account, string issuer = "SuperApp")
            => $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(account)}?secret={base32Secret}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits={Digits}&period={StepSeconds}";

        /// <summary>
        /// Lock after a wrong code (Tung's rule 06/10): the 3rd wrong code in a row locks 10 minutes,
        /// the 5th and every later one 1 day. Null = no lock.
        /// </summary>
        public static DateTime? LockAfterFailure(int failedCount, DateTime nowUtc) => failedCount switch
        {
            >= 5 => nowUtc.AddDays(1),
            3 => nowUtc.AddMinutes(10),
            _ => null
        };

        public static string Base32Encode(byte[] data)
        {
            var sb = new StringBuilder((data.Length * 8 + 4) / 5);
            int buffer = 0, bits = 0;
            foreach (var b in data)
            {
                buffer = (buffer << 8) | b;
                bits += 8;
                while (bits >= 5)
                {
                    sb.Append(Base32Alphabet[(buffer >> (bits - 5)) & 31]);
                    bits -= 5;
                }
            }
            if (bits > 0) sb.Append(Base32Alphabet[(buffer << (5 - bits)) & 31]);
            return sb.ToString();
        }

        public static byte[] Base32Decode(string base32)
        {
            var s = base32.Trim().TrimEnd('=').Replace(" ", "").ToUpperInvariant();
            var output = new List<byte>(s.Length * 5 / 8);
            int buffer = 0, bits = 0;
            foreach (var c in s)
            {
                var v = Base32Alphabet.IndexOf(c);
                if (v < 0) throw new FormatException("Invalid base32 character");
                buffer = (buffer << 5) | v;
                bits += 5;
                if (bits >= 8)
                {
                    output.Add((byte)((buffer >> (bits - 8)) & 0xff));
                    bits -= 8;
                }
            }
            return output.ToArray();
        }
    }
}
