using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppAPI.Exceptions;
using SuperAppAPI.Extensions;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using SuperAppServices.Interfaces;
using SuperAppModels.Time;

namespace SuperAppAPI.Controllers.K
{
    [ApiController]
    [Route("api/k")]
    [Authorize]
    public class KController : BaseAuthController
    {
        private readonly IKKnowledgeService _knowledgeService;
        private readonly IKNodeService _nodeService;
        private readonly IKMarkdownImportService _markdownImportService;
        private readonly IKQuestionService _questionService;
        private readonly IKSyncEventPublisher _syncPublisher;
        private readonly ILogger<KController> _logger;

        public KController(
            IKKnowledgeService knowledgeService,
            IKNodeService nodeService,
            IKMarkdownImportService markdownImportService,
            IKQuestionService questionService,
            IKSyncEventPublisher syncPublisher,
            ILogger<KController> logger)
        {
            _knowledgeService      = knowledgeService      ?? throw new ArgumentNullException(nameof(knowledgeService));
            _nodeService           = nodeService           ?? throw new ArgumentNullException(nameof(nodeService));
            _markdownImportService = markdownImportService ?? throw new ArgumentNullException(nameof(markdownImportService));
            _questionService       = questionService       ?? throw new ArgumentNullException(nameof(questionService));
            _syncPublisher         = syncPublisher         ?? throw new ArgumentNullException(nameof(syncPublisher));
            _logger                = logger                ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET /api/k/orphan-questions
        [HttpGet("orphan-questions")]
        public async Task<IActionResult> GetOrphanQuestions()
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized("User ID not found in token");
                var result = await _questionService.GetOrphanQuestionsAsync(userId.Value);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orphan questions");
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        // PATCH /api/k/orphan-questions
        [HttpPatch("orphan-questions")]
        public async Task<IActionResult> UpdateOrphanQuestions([FromBody] KUpdateQuestionsRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var userId = GetUserId();
                if (userId == null) return Unauthorized("User ID not found in token");
                var result = await _questionService.UpdateOrphanQuestionsAsync(userId.Value, request);
                if (result.Success) _syncPublisher.NotifyChanged(userId.Value);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating orphan questions");
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        // PATCH /api/k/questions/{id}/node  — move question to a different node (or make orphan)
        [HttpPatch("questions/{id:int}/node")]
        public async Task<IActionResult> MoveQuestion(int id, [FromBody] KMoveQuestionRequest request)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized("User ID not found in token");
                var result = await _questionService.MoveQuestionAsync(id, request.NodeId, userId.Value);
                if (result.Success) _syncPublisher.NotifyChanged(userId.Value);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving question {Id}", id);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
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
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized("User ID not found in token");

                var filter = new FilterOptions
                {
                    StatusCodes = !string.IsNullOrEmpty(statusCode)
                        ? statusCode.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList()
                        : null,
                    DeletedAt   = deletedAt,
                    CreatedFrom = TimeParsing.ParseInstantLenient(createdAtFrom),
                    CreatedTo   = TimeParsing.ParseInstantLenientEnd(createdAtTo)
                };

                var result = await _knowledgeService.GetAllKnowledgesAsync(userId.Value, filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all knowledges");
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred while retrieving knowledges", Status = 500 });
            }
        }

        /// <summary>Creates a new knowledge base</summary>
        [HttpPost]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateKnowledge([FromBody] KUpsertKnowledgeRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = GetUserId();
                if (userId == null) return Unauthorized("User ID not found in token");

                request.UserId = userId.Value;
                var result = await _knowledgeService.CreateKnowledgeAsync(request);
                if (result.Success) _syncPublisher.NotifyChanged(userId.Value);
                return result.Success ? StatusCode(201, result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating knowledge");
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred while creating knowledge", Status = 500 });
            }
        }

        /// <summary>Updates an existing knowledge base (name, description, image)</summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateKnowledge(int id, [FromBody] KUpsertKnowledgeRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = GetUserId();
                if (userId == null) return Unauthorized("User ID not found in token");

                request.UserId = userId.Value;
                var result = await _knowledgeService.UpdateKnowledgeAsync(id, request);
                if (result.Success) _syncPublisher.NotifyChanged(userId.Value);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating knowledge {Id}", id);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred while updating knowledge", Status = 500 });
            }
        }

        /// <summary>Soft-deletes a knowledge base</summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> SoftDeleteKnowledge(int id)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized("User ID not found in token");

                var result = await _knowledgeService.SoftDeleteKnowledgeAsync(id, userId.Value);
                if (result.Success) _syncPublisher.NotifyChanged(userId.Value);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting knowledge {Id}", id);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred while deleting knowledge", Status = 500 });
            }
        }

        /// <summary>
        /// Gets knowledge tree — flat list of nodes.
        /// Frontend builds hierarchy from parentId.
        /// </summary>
        [HttpGet("{knowledgeId}/tree")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetKnowledgeTree(int knowledgeId)
        {
            try
            {
                if (knowledgeId <= 0)
                    return BadRequest(new ResultOptions { Success = false, Message = "Knowledge ID must be a positive integer", Status = 400 });

                var userId = GetUserId();
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
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ResultOptions { Success = false, Message = ex.Message, Status = 404 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving knowledge tree for knowledge {KnowledgeId}", knowledgeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred while retrieving the knowledge tree", Status = 500 });
            }
        }

        /// <summary>Deletes nodes (and descendants) by k.node IDs</summary>
        [HttpDelete("{knowledgeId}/nodes")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteNodes(int knowledgeId, [FromBody] KDeleteNodesRequest request)
        {
            try
            {
                if (knowledgeId <= 0)
                    return BadRequest(new ResultOptions { Success = false, Message = "Knowledge ID must be a positive integer", Status = 400 });

                if (!ModelState.IsValid)
                    return BadRequest(new ResultOptions { Success = false, Message = "Invalid request data", Object = ModelState, Status = 400 });

                var userId = GetUserId();
                if (userId == null) return Unauthorized("User ID not found in token");

                _logger.LogInformation("Deleting {Count} nodes in knowledge {KnowledgeId}", request.NodeIds.Count, knowledgeId);

                var result = await _knowledgeService.DeleteNodesAsync(knowledgeId, userId.Value, request);
                if (result.Success) _syncPublisher.NotifyChanged(userId.Value);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting nodes in knowledge {KnowledgeId}", knowledgeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred while deleting nodes", Status = 500 });
            }
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
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (requests == null || !requests.Any())
                    return BadRequest("At least one node request is required");

                var userId = GetUserId();
                if (userId == null) return Unauthorized("User ID not found in token");

                var userEmail = GetUserEmail();

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
                if (response.Success) _syncPublisher.NotifyChanged(userId.Value);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting nodes in knowledge {KnowledgeId}", knowledgeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred while processing node requests", Status = 500 });
            }
        }

        /// <summary>
        /// Structured test markdown import (parsed on frontend).
        /// Creates question nodes under parentNodeId, one KTestEntity per ## section,
        /// links question nodes to their test via KTestNodeEntity.
        /// Orphan questions (no ## parent) are created as nodes only.
        /// </summary>
        [HttpPost("{knowledgeId}/import-test-markdown")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> ImportTestMarkdown(int knowledgeId, [FromBody] KImportTestMarkdownRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = GetUserId();
                if (userId == null) return Unauthorized("User ID not found in token");

                if (request.Tests.Count == 0 && request.OrphanQuestions.Count == 0)
                    return BadRequest(new ResultOptions { Success = false, Message = "No tests or questions provided.", Status = 400 });

                var testsCreated = await _markdownImportService.ImportTestMarkdownAsync(knowledgeId, userId.Value, request);
                _syncPublisher.NotifyChanged(userId.Value);
                return Ok(new ResultOptions
                {
                    Success = true,
                    Message = $"Created {testsCreated} tests",
                    Object  = testsCreated,
                    Status  = 200,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Test markdown import failed for knowledge {KnowledgeId}", knowledgeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "Import failed. Please try again.", Status = 500 });
            }
        }

        /// <summary>
        /// AI-powered markdown → nodes import.
        /// Parses free-form markdown, creates nodes under parentNodeId with statusCode = "draft".
        /// </summary>
        [HttpPost("{knowledgeId}/nodes/import-markdown")]
        [ProducesResponseType(typeof(ResultOptions), StatusCodes.Status200OK)]
        public async Task<IActionResult> ImportMarkdown(int knowledgeId, [FromBody] KImportMarkdownRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (string.IsNullOrWhiteSpace(request.Markdown))
                    return BadRequest(new ResultOptions { Success = false, Message = "Markdown content is required", Status = 400 });

                var userId = GetUserId();
                if (userId == null) return Unauthorized("User ID not found in token");

                var nodes = await _markdownImportService.ImportAsync(knowledgeId, userId.Value, request);
                _syncPublisher.NotifyChanged(userId.Value);
                return Ok(new ResultOptions
                {
                    Success = true,
                    Message = $"Imported {nodes.Count} nodes as draft",
                    Object  = nodes,
                    Status  = 200
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ResultOptions { Success = false, Message = ex.Message, Status = 400 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Markdown import failed for knowledge {KnowledgeId}", knowledgeId);
                return StatusCode(500, new ResultOptions { Success = false, Message = "Import failed. Please try again.", Status = 500 });
            }
        }
    }
}
