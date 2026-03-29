using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppAPI.Extensions;
using SuperAppModels.DTOs.Requests;
using SuperAppServices.Interfaces;

namespace SuperAppAPI.Controllers
{
    /// <summary>
    /// Manages Task Flow canvas data: arbitrary edges with notes and saved node positions.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FlowController : ControllerBase
    {
        private readonly IFlowService _flowService;
        private readonly ILogger<FlowController> _logger;

        public FlowController(IFlowService flowService, ILogger<FlowController> logger)
        {
            _flowService = flowService;
            _logger = logger;
        }

        private int? GetUserId()
        {
            var claim = User.GetUserId();
            return string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id) ? null : id;
        }

        // ── Edges ─────────────────────────────────────────────────────────────

        /// <summary>Get all non-deleted flow edges for the authenticated user.</summary>
        [HttpGet("edges")]
        public async Task<IActionResult> GetEdges()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();
            var result = await _flowService.GetEdgesAsync(userId.Value);
            return Ok(result);
        }

        /// <summary>
        /// Batch upsert flow edges.
        /// Pass deletedAt to soft-delete an existing edge.
        /// </summary>
        [HttpPost("edges/batch")]
        public async Task<IActionResult> UpsertEdges([FromBody] List<UpsertFlowEdgeRequest> requests)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (requests == null || !requests.Any()) return BadRequest("At least one edge required");

            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            _logger.LogInformation("Upserting {Count} flow edges for user {UserId}", requests.Count, userId.Value);
            var result = await _flowService.UpsertEdgesAsync(requests, userId.Value);
            return Ok(result);
        }

        // ── Node Positions ────────────────────────────────────────────────────

        /// <summary>Get saved node positions for the authenticated user.</summary>
        [HttpGet("positions")]
        public async Task<IActionResult> GetPositions(
            [FromQuery] string? nodeIds = null,
            [FromQuery] string? nodeType = null)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var ids = string.IsNullOrEmpty(nodeIds)
                ? null
                : nodeIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0)
                    .Where(id => id > 0)
                    .ToList();

            var result = await _flowService.GetNodePositionsAsync(userId.Value, ids, nodeType);
            return Ok(result);
        }

        /// <summary>Batch upsert node positions (insert or update by unique key).</summary>
        [HttpPost("positions/batch")]
        public async Task<IActionResult> UpsertPositions([FromBody] List<UpsertFlowNodePositionRequest> requests)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (requests == null || !requests.Any()) return BadRequest("At least one position required");

            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var result = await _flowService.UpsertNodePositionsAsync(requests, userId.Value);
            return Ok(result);
        }
    }
}
