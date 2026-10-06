using System.Text;
using SuperAppServices.Services.Auth;
using Xunit;

namespace SuperAppServices.Tests
{
    /// <summary>TungRoot #1489 — TOTP (RFC 6238) + lock rule + unlock token.</summary>
    public class TotpTests
    {
        // RFC 6238 appendix B, SHA-1 seed "12345678901234567890"; 8-digit vectors, last 6 digits = 6-digit code
        private static readonly byte[] RfcKey = Encoding.ASCII.GetBytes("12345678901234567890");
        private static DateTime At(long unix) => DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;

        [Theory]
        [InlineData(59L, "94287082")]
        [InlineData(1111111109L, "07081804")]
        [InlineData(1111111111L, "14050471")]
        [InlineData(1234567890L, "89005924")]
        [InlineData(2000000000L, "69279037")]
        [InlineData(20000000000L, "65353130")]
        public void Code_MatchesRfc6238Vectors(long unix, string expected8)
        {
            var step = Totp.StepAt(At(unix));
            Assert.Equal(expected8, Totp.Code(RfcKey, step, 8));
            Assert.Equal(expected8[2..], Totp.Code(RfcKey, step));
        }

        [Fact]
        public void Base32_RoundTrip()
        {
            Assert.Equal("GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ", Totp.Base32Encode(RfcKey));
            Assert.Equal(RfcKey, Totp.Base32Decode("gezd gnbv gy3t qojq gezd gnbv gy3t qojq"));
            var secret = Totp.GenerateSecret();
            Assert.Equal(32, secret.Length);
            Assert.Equal(20, Totp.Base32Decode(secret).Length);
            Assert.Throws<FormatException>(() => Totp.Base32Decode("1NVALID"));
        }

        [Fact]
        public void Verify_WindowReplayAndFormat()
        {
            var secret = Totp.Base32Encode(RfcKey);
            var now = At(1234567890);
            var step = Totp.StepAt(now);
            var code = Totp.Code(RfcKey, step);

            Assert.Equal(step, Totp.Verify(secret, code, now, null));
            Assert.Equal(step, Totp.Verify(secret, code[..3] + " " + code[3..], now, null));
            Assert.Equal(step - 1, Totp.Verify(secret, Totp.Code(RfcKey, step - 1), now, null)); // phone 30 s behind
            Assert.Null(Totp.Verify(secret, Totp.Code(RfcKey, step - 2), now, null));           // too old
            Assert.Null(Totp.Verify(secret, code, now, step));                                   // used once already
            Assert.Null(Totp.Verify(secret, Totp.Code(RfcKey, step - 1), now, step));            // older than last used
            Assert.Null(Totp.Verify(secret, "12345", now, null));
            Assert.Null(Totp.Verify(secret, "abcdef", now, null));
            Assert.Null(Totp.Verify(secret, null, now, null));
        }

        [Fact]
        public void LockRule_ThreeWrongTenMinutes_FiveWrongOneDay()
        {
            var now = new DateTime(2026, 10, 6, 8, 0, 0, DateTimeKind.Utc);
            Assert.Null(Totp.LockAfterFailure(1, now));
            Assert.Null(Totp.LockAfterFailure(2, now));
            Assert.Equal(now.AddMinutes(10), Totp.LockAfterFailure(3, now));
            Assert.Null(Totp.LockAfterFailure(4, now));
            Assert.Equal(now.AddDays(1), Totp.LockAfterFailure(5, now));
            Assert.Equal(now.AddDays(1), Totp.LockAfterFailure(6, now));
        }

        [Fact]
        public void OtpAuthUri_HasSecretAndIssuer()
        {
            var uri = Totp.OtpAuthUri("ABC234", "tung@example.com");
            Assert.StartsWith("otpauth://totp/SuperApp:tung%40example.com?secret=ABC234&issuer=SuperApp", uri);
            Assert.Contains("digits=6", uri);
            Assert.Contains("period=30", uri);
        }

        [Fact]
        public void UnlockToken_UserExpiryAndSignature()
        {
            var key = UnlockToken.DeriveKey("jwt-key-for-tests-only-0123456789");
            var now = new DateTime(2026, 10, 6, 8, 0, 0, DateTimeKind.Utc);
            var token = UnlockToken.Issue(key, 42, now.AddMinutes(15));

            Assert.True(UnlockToken.IsValid(key, token, 42, now));
            Assert.False(UnlockToken.IsValid(key, token, 43, now), "other user");
            Assert.False(UnlockToken.IsValid(key, token, 42, now.AddMinutes(16)), "expired");
            Assert.False(UnlockToken.IsValid(UnlockToken.DeriveKey("other-key"), token, 42, now), "other key");
            var parts = token.Split('.');
            Assert.False(UnlockToken.IsValid(key, $"{parts[0]}.{long.Parse(parts[1]) + 3600}.{parts[2]}", 42, now), "tampered expiry");
            Assert.False(UnlockToken.IsValid(key, null, 42, now));
            Assert.False(UnlockToken.IsValid(key, "garbage", 42, now));
        }
    }
}
