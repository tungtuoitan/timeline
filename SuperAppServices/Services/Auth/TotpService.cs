using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins.Auth;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Responses.Auth;
using SuperAppModels.Models.Auth;
using SuperAppServices.Interfaces.Auth;

namespace SuperAppServices.Services.Auth
{
    public class TotpService : ITotpService
    {
        private readonly IUserTotpRepository _repository;
        private readonly ILogger<TotpService> _logger;
        private readonly byte[] _unlockKey;

        public TotpService(IUserTotpRepository repository, IConfiguration configuration, ILogger<TotpService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            var jwtKey = configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(jwtKey)) throw new InvalidOperationException("Jwt:Key is not configured");
            _unlockKey = UnlockToken.DeriveKey(jwtKey);
        }

        public async Task<ResultOptions> GetStatusAsync(int userId)
        {
            var totp = await _repository.GetAsync(userId);
            var now = DateTime.UtcNow;
            return Ok(new TotpStatusDto
            {
                Enabled = totp?.Secret != null,
                LockedUntil = totp?.LockedUntil > now ? totp.LockedUntil : null
            });
        }

        public async Task<ResultOptions> SetupAsync(int userId)
        {
            var totp = await _repository.GetAsync(userId) ?? new UserTotp { UserId = userId };
            if (totp.Secret != null) return Fail(409, "TOTP đã bật — tắt trước khi cài lại");

            totp.PendingSecret = Totp.GenerateSecret();
            await _repository.SaveAsync(totp);
            var account = await _repository.GetEmailAsync(userId) ?? $"user-{userId}";
            _logger.LogInformation("[TOTP] setup-started | UserId={UserId}", userId);
            return Ok(new TotpSetupDto { Secret = totp.PendingSecret, OtpauthUri = Totp.OtpAuthUri(totp.PendingSecret, account) });
        }

        public async Task<ResultOptions> ConfirmAsync(int userId, string code)
        {
            var totp = await _repository.GetAsync(userId);
            if (totp?.PendingSecret == null) return Fail(404, "Chưa bắt đầu cài TOTP");
            return await CheckAsync(totp, totp.PendingSecret, code, onSuccess: step =>
            {
                totp.Secret = totp.PendingSecret;
                totp.PendingSecret = null;
                totp.EnabledAt = DateTime.UtcNow;
                _logger.LogInformation("[TOTP] enabled | UserId={UserId}", userId);
                return IssueToken(userId);
            });
        }

        public async Task<ResultOptions> UnlockAsync(int userId, string code)
        {
            var totp = await _repository.GetAsync(userId);
            if (totp?.Secret == null) return Fail(404, "TOTP chưa bật");
            return await CheckAsync(totp, totp.Secret, code, onSuccess: _ => IssueToken(userId));
        }

        public async Task<ResultOptions> DisableAsync(int userId, string code)
        {
            var totp = await _repository.GetAsync(userId);
            if (totp?.Secret == null) return Fail(404, "TOTP chưa bật");
            return await CheckAsync(totp, totp.Secret, code, onSuccess: _ =>
            {
                totp.Secret = null;
                totp.PendingSecret = null;
                totp.EnabledAt = null;
                _logger.LogInformation("[TOTP] disabled | UserId={UserId}", userId);
                return new ResultOptions { Success = true, Message = "Đã tắt TOTP", Status = 200 };
            });
        }

        public bool IsUnlocked(int userId, string? unlockToken) => UnlockToken.IsValid(_unlockKey, unlockToken, userId, DateTime.UtcNow);

        /// <summary>Lock check → verify → success (reset counter, remember step) or failure (count, maybe lock).</summary>
        private async Task<ResultOptions> CheckAsync(UserTotp totp, string secret, string code, Func<long, ResultOptions> onSuccess)
        {
            var now = DateTime.UtcNow;
            if (totp.LockedUntil > now) return Locked(totp.LockedUntil.Value);

            var step = Totp.Verify(secret, code, now, totp.LastUsedStep);
            if (step == null)
            {
                totp.FailedCount++;
                totp.LockedUntil = Totp.LockAfterFailure(totp.FailedCount, now) ?? totp.LockedUntil;
                await _repository.SaveAsync(totp);
                _logger.LogWarning("[TOTP] wrong-code | UserId={UserId} | FailedCount={FailedCount} | LockedUntil={LockedUntil}", totp.UserId, totp.FailedCount, totp.LockedUntil);
                return totp.LockedUntil > now ? Locked(totp.LockedUntil.Value) : Fail(400, "Mã không đúng");
            }

            totp.LastUsedStep = step;
            totp.FailedCount = 0;
            totp.LockedUntil = null;
            var result = onSuccess(step.Value);
            await _repository.SaveAsync(totp);
            return result;
        }

        private ResultOptions IssueToken(int userId)
        {
            var expires = DateTime.UtcNow.Add(UnlockToken.Lifetime);
            return Ok(new UnlockTokenDto { Token = UnlockToken.Issue(_unlockKey, userId, expires), ExpiresAt = expires });
        }

        private static ResultOptions Ok(object obj) => new() { Success = true, Message = "OK", Object = obj, Status = 200 };
        private static ResultOptions Fail(int status, string message) => new() { Success = false, Message = message, Status = status };
        private static ResultOptions Locked(DateTime until) =>
            new() { Success = false, Message = "Sai mã quá nhiều lần — tạm khoá", Object = new TotpStatusDto { Enabled = true, LockedUntil = until }, Status = 423 };
    }
}
