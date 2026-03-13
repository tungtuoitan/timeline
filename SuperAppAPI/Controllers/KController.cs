using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppAPI.Exceptions;
using SuperAppAPI.Extensions;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using SuperAppServices.Interfaces;

namespace SuperAppAPI.Controllers
{
    [ApiController]
    [Route("api/k")]
    [Authorize]
    public class KController : ControllerBase
    {
        private readonly IKKnowledgeService _knowledgeService;
        private readonly IKNodeService _nodeService;
        private readonly ILogger<KController> _logger;

        public KController(
            IKKnowledgeService knowledgeService,
            IKNodeService nodeService,
            ILogger<KController> logger)
        {
            _knowledgeService = knowledgeService ?? throw new ArgumentNullException(nameof(knowledgeService));
            _nodeService      = nodeService      ?? throw new ArgumentNullException(nameof(nodeService));
            _logger           = logger           ?? throw new ArgumentNullException(nameof(logger));
        }

        private int? GetAuthenticatedUserId()
        {
            var claim = User.GetUserId();
            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var userId))
                return null;
            return userId;
        }

        /// <summary>Gets all knowledge bases for the current user</summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<KKnowledgeSummary>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllKnowledges(
            [FromQuery] string? statusCode    = null,
            [FromQuery] string? deletedAt     = null,
            [FromQuery] string? createdAtFrom = null,
            [FromQuery] string? createdAtTo   = null)
        {
            var userId = GetAuthenticatedUserId();
            if (userId == null) return Unauthorized("User ID not found in token");

            var filter = new FilterOptions
            {
                StatusCodes = !string.IsNullOrEmpty(statusCode)
                    ? statusCode.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList()
                    : null,
                DeletedAt   = deletedAt,
                CreatedFrom = !string.IsNullOrEmpty(createdAtFrom) && DateTime.TryParse(createdAtFrom, out var from) ? from : null,
                CreatedTo   = !string.IsNullOrEmpty(createdAtTo)   && DateTime.TryParse(createdAtTo,   out var to)   ? to   : null
            };

            var result = await _knowledgeService.GetAllKnowledgesAsync(userId.Value, filter);
            return Ok(result);
        }

        /// <summary>Creates a new knowledge base</summary>
        [HttpPost]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateKnowledge([FromBody] KUpsertKnowledgeRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetAuthenticatedUserId();
            if (userId == null) return Unauthorized("User ID not found in token");

            request.UserId = userId.Value;
            var result = await _knowledgeService.CreateKnowledgeAsync(request);
            return result.Success ? StatusCode(201, result) : StatusCode(result.Status ?? 500, result);
        }

        /// <summary>Updates an existing knowledge base (name, description, image)</summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateKnowledge(int id, [FromBody] KUpsertKnowledgeRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetAuthenticatedUserId();
            if (userId == null) return Unauthorized("User ID not found in token");

            request.UserId = userId.Value;
            var result = await _knowledgeService.UpdateKnowledgeAsync(id, request);
            return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
        }

        /// <summary>Soft-deletes a knowledge base</summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> SoftDeleteKnowledge(int id)
        {
            var userId = GetAuthenticatedUserId();
            if (userId == null) return Unauthorized("User ID not found in token");

            var result = await _knowledgeService.SoftDeleteKnowledgeAsync(id, userId.Value);
            return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
        }

        /// <summary>
        /// Gets knowledge tree — flat list of nodes.
        /// Frontend builds hierarchy from parentId.
        /// </summary>
        [HttpGet("{knowledgeId}/tree")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetKnowledgeTree(int knowledgeId)
        {
            if (knowledgeId <= 0)
                throw new BadRequestException("Knowledge ID must be a positive integer");

            var userId = GetAuthenticatedUserId();
            if (userId == null) return Unauthorized("User ID not found in token");

            _logger.LogInformation("Retrieving knowledge tree for knowledgeId: {KnowledgeId}, userId: {UserId}",
                knowledgeId, userId.Value);

            var tree = await _knowledgeService.GetKnowledgeTreeAsync(knowledgeId, userId.Value);

            _logger.LogInformation("Retrieved {Count} nodes for knowledgeId: {KnowledgeId}",
                tree.FlatData?.Count ?? 0, knowledgeId);

            return Ok(new ResultOptions
            {
                Success = true,
                Message = "Knowledge tree retrieved successfully",
                Status  = StatusCodes.Status200OK,
                Object  = tree
            });
        }

        /// <summary>Deletes nodes (and descendants) by k.node IDs</summary>
        [HttpDelete("{knowledgeId}/nodes")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteNodes(int knowledgeId, [FromBody] KDeleteNodesRequest request)
        {
            if (knowledgeId <= 0)
                return BadRequest(new ResultOptions { Success = false, Message = "Knowledge ID must be a positive integer", Status = 400 });

            if (!ModelState.IsValid)
                return BadRequest(new ResultOptions { Success = false, Message = "Invalid request data", Object = ModelState, Status = 400 });

            var userId = GetAuthenticatedUserId();
            if (userId == null) return Unauthorized("User ID not found in token");

            _logger.LogInformation("Deleting {Count} nodes in knowledge {KnowledgeId}", request.NodeIds.Count, knowledgeId);

            var result = await _knowledgeService.DeleteNodesAsync(knowledgeId, userId.Value, request);
            return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
        }

        /// <summary>
        /// Batch upsert nodes with explicit actions.
        /// Actions: Create, Update, Move, MoveCross, Delete, Restore
        /// </summary>
        [HttpPost("{knowledgeId}/nodes/batch")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpsertNodes(
            int knowledgeId,
            [FromBody] List<KUpsertNodeRequest> requests)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (requests == null || !requests.Any())
                return BadRequest("At least one node request is required");

            var userId = GetAuthenticatedUserId();
            if (userId == null) return Unauthorized("User ID not found in token");

            var userEmail = User.GetUserEmail();

            foreach (var req in requests)
            {
                if (req.Action != KNodeAction.MoveCross)
                    req.KnowledgeId = knowledgeId;

                req.UserId    = userId.Value;
                req.CreatedBy = userEmail;
            }

            _logger.LogInformation("Processing {Count} node requests (actions: {Actions}) for knowledge: {KnowledgeId}",
                requests.Count,
                string.Join(", ", requests.Select(r => r.Action.ToString())),
                knowledgeId);

            var response = await _nodeService.UpsertNodesAsync(requests, userId.Value, knowledgeId);
            return Ok(response);
        }
    }
}
