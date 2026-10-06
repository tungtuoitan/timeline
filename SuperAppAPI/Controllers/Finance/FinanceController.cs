using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests.Finance;
using SuperAppServices.Interfaces.Finance;

namespace SuperAppAPI.Controllers.Finance
{
    /// <summary>Finance ledger, price cache and summary (TungRoot #1482).</summary>
    [ApiController]
    [Route("api/finance")]
    [Authorize]
    public class FinanceController : BaseAuthController
    {
        private readonly IFinanceService _service;

        private readonly SuperAppServices.Interfaces.Auth.ITotpService _totp;

        public FinanceController(IFinanceService service, SuperAppServices.Interfaces.Auth.ITotpService totp)
        {
            _service = service;
            _totp = totp;
        }

        /// <summary>GET /api/finance/transactions?from=yyyy-MM-dd&amp;to=&amp;account=&amp;kind=&amp;uncategorized=true&amp;limit=200 — newest first.</summary>
        [HttpGet("transactions")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTransactions([FromQuery] string? from = null, [FromQuery] string? to = null,
            [FromQuery] string? account = null, [FromQuery] string? kind = null, [FromQuery] bool uncategorized = false, [FromQuery] int? limit = null)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("User ID not found in token");
            return Reply(await _service.GetTransactionsAsync(userId.Value, from, to, account, kind, uncategorized, limit));
        }

        /// <summary>POST /api/finance/transactions — batch insert; (source, externalId) already stored → update source facts.</summary>
        [HttpPost("transactions")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpsertTransactions([FromBody] UpsertFinTransactionsRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var userId = GetUserId();
            if (userId == null) return Unauthorized("User ID not found in token");
            return Reply(await _service.UpsertTransactionsAsync(userId.Value, request));
        }

        /// <summary>PATCH /api/finance/transactions/{id} — classify (kind/category), note, counterparty, groupKey, valueVnd.</summary>
        [HttpPatch("transactions/{id:int}")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> PatchTransaction(int id, [FromBody] PatchFinTransactionRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var userId = GetUserId();
            if (userId == null) return Unauthorized("User ID not found in token");
            return Reply(await _service.PatchTransactionAsync(userId.Value, id, request));
        }

        /// <summary>DELETE /api/finance/transactions/{id} — soft delete.</summary>
        [HttpDelete("transactions/{id:int}")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("User ID not found in token");
            return Reply(await _service.DeleteTransactionAsync(userId.Value, id));
        }

        /// <summary>GET /api/finance/summary?from=yyyy-MM-dd&amp;to=&amp;interval=day|week|month — net worth series, months, holdings (VND).</summary>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSummary([FromQuery] string? from = null, [FromQuery] string? to = null, [FromQuery] string? interval = null)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("User ID not found in token");
            // Private homepage data: only with a fresh TOTP unlock token (TungRoot #1489)
            if (!_totp.IsUnlocked(userId.Value, Request.Headers[SuperAppServices.Services.Auth.UnlockToken.HeaderName].FirstOrDefault()))
                return StatusCode(StatusCodes.Status403Forbidden, new ResultOptions { Success = false, Status = 403, Message = "Cần mở khoá bằng mã TOTP" });
            return Reply(await _service.GetSummaryAsync(userId.Value, from, to, interval));
        }

        /// <summary>GET /api/finance/prices/latest — last cached date per (asset, quote).</summary>
        [HttpGet("prices/latest")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLatestPrices()
        {
            if (GetUserId() == null) return Unauthorized("User ID not found in token");
            return Reply(await _service.GetLatestPricesAsync());
        }

        /// <summary>PUT /api/finance/prices — upsert daily close prices (global cache, not per user).</summary>
        [HttpPut("prices")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpsertPrices([FromBody] UpsertFinPricesRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (GetUserId() == null) return Unauthorized("User ID not found in token");
            return Reply(await _service.UpsertPricesAsync(request));
        }

        private IActionResult Reply(ResultOptions result) => result.Status switch
        {
            400 => BadRequest(result),
            404 => NotFound(result),
            500 => StatusCode(500, result),
            _ => Ok(result),
        };
    }
}
