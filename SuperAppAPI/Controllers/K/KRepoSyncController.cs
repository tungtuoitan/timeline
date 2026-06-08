using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppModels.DTOs;
using SuperAppServices.Interfaces;

namespace SuperAppAPI.Controllers.K
{
    [ApiController]
    [Route("api/k/repo-sync")]
    [Authorize]
    public class KRepoSyncController : BaseAuthController
    {
        private readonly IKRepoSyncService _syncService;
        private readonly ILogger<KRepoSyncController> _logger;

        public KRepoSyncController(IKRepoSyncService syncService, ILogger<KRepoSyncController> logger)
        {
            _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
            _logger      = logger      ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET /api/k/repo-sync/status
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized();
                var result = await _syncService.GetStatusAsync(userId.Value);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetStatus failed");
                return StatusCode(500, ResultOptions.Fail("An error occurred", 500));
            }
        }

        // POST /api/k/repo-sync/config
        [HttpPost("config")]
        public async Task<IActionResult> SaveConfig([FromBody] KRepoSyncConfigRequest request)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized();
                var result = await _syncService.SaveConfigAsync(userId.Value, request.RepoUrl, request.Branch, request.Pat);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 400, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SaveConfig failed");
                return StatusCode(500, ResultOptions.Fail("An error occurred", 500));
            }
        }

        // POST /api/k/repo-sync/push
        [HttpPost("push")]
        public async Task<IActionResult> Push()
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized();
                var result = await _syncService.PushToRepoAsync(userId.Value);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 400, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Push failed");
                return StatusCode(500, ResultOptions.Fail("An error occurred", 500));
            }
        }

        // POST /api/k/repo-sync/pull
        [HttpPost("pull")]
        public async Task<IActionResult> Pull()
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized();
                var result = await _syncService.PullFromRepoAsync(userId.Value);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 400, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pull failed");
                return StatusCode(500, ResultOptions.Fail("An error occurred", 500));
            }
        }

        // GET /api/k/repo-sync/diff
        [HttpGet("diff")]
        public async Task<IActionResult> GetDiff()
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized();
                var result = await _syncService.GetDiffAsync(userId.Value);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetDiff failed");
                return StatusCode(500, ResultOptions.Fail("An error occurred", 500));
            }
        }

        // POST /api/k/repo-sync/force-update  — force overwrites remote with DB content
        [HttpPost("force-update")]
        public async Task<IActionResult> ForceUpdate()
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized();
                var result = await _syncService.ForceUpdateRemoteAsync(userId.Value);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 400, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ForceUpdate failed");
                return StatusCode(500, ResultOptions.Fail("An error occurred", 500));
            }
        }

        // GET /api/k/repo-sync/compare  — compare remote repo vs DB, no writes
        [HttpGet("compare")]
        public async Task<IActionResult> Compare()
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized();
                var result = await _syncService.GetCompareDiffAsync(userId.Value);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Compare failed");
                return StatusCode(500, ResultOptions.Fail("An error occurred", 500));
            }
        }

        // POST /api/k/repo-sync/retry  — clears conflict status and retries push
        [HttpPost("retry")]
        public async Task<IActionResult> Retry()
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized();
                var result = await _syncService.ResetConflictAndRetryAsync(userId.Value);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 400, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Retry failed");
                return StatusCode(500, ResultOptions.Fail("An error occurred", 500));
            }
        }
    }

    public class KRepoSyncConfigRequest
    {
        public string RepoUrl { get; set; } = "";
        public string Branch  { get; set; } = "main";
        /// <summary>Empty string = keep existing PAT in DB unchanged.</summary>
        public string Pat     { get; set; } = "";
    }
}
