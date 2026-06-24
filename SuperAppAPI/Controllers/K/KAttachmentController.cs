using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperAppAPI.Extensions;
using SuperAppDataRepositories.Data;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Responses;
using SuperAppModels.Models;
using SuperAppServices.Interfaces;

namespace SuperAppAPI.Controllers.K
{
    [ApiController]
    [Route("api/k/attachments")]
    [Authorize]
    public class KAttachmentController : BaseAuthController
    {
        private readonly ApplicationDbContext _db;
        private readonly IKSyncEventPublisher _syncPublisher;
        private readonly ILogger<KAttachmentController> _logger;

        public KAttachmentController(
            ApplicationDbContext db,
            IKSyncEventPublisher syncPublisher,
            ILogger<KAttachmentController> logger)
        {
            _db            = db;
            _syncPublisher = syncPublisher;
            _logger        = logger;
        }

        // GET /api/k/attachments — all attachments for the current user
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();
            try
            {
                var atts = await _db.KAttachments
                    .Where(a => a.UserId == userId.Value && a.DeletedAt == null)
                    .OrderBy(a => a.SortOrder).ThenBy(a => a.Title)
                    .Select(a => new KAttachmentResponse
                    {
                        Id       = a.Id,
                        Title    = a.Title,
                        Type     = a.Type,
                        Language = a.Language,
                        Content  = a.Content,
                        SortOrder = a.SortOrder,
                    })
                    .ToListAsync();
                return Ok(new ResultOptions { Success = true, Object = atts });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAll attachments failed for user {UserId}", userId);
                return StatusCode(500, ResultOptions.Fail("Failed to load attachments", 500));
            }
        }

        // GET /api/k/attachments/node/{nodeId}
        [HttpGet("node/{nodeId:int}")]
        public async Task<IActionResult> GetForNode(int nodeId)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();
            try
            {
                var result = await LoadLinkedAttachments("node", nodeId);
                return Ok(new ResultOptions { Success = true, Object = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetForNode attachments failed for node {NodeId}", nodeId);
                return StatusCode(500, ResultOptions.Fail("Failed to load attachments", 500));
            }
        }

        // POST /api/k/attachments/node/{nodeId}/{attachmentId}
        [HttpPost("node/{nodeId:int}/{attachmentId:int}")]
        public async Task<IActionResult> LinkToNode(int nodeId, int attachmentId)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();
            try
            {
                await UpsertLink("node", nodeId, attachmentId);
                _syncPublisher.NotifyChanged(userId.Value);
                return Ok(new ResultOptions { Success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LinkToNode failed node={NodeId} att={AttId}", nodeId, attachmentId);
                return StatusCode(500, ResultOptions.Fail("Failed to link attachment", 500));
            }
        }

        // DELETE /api/k/attachments/node/{nodeId}/{attachmentId}
        [HttpDelete("node/{nodeId:int}/{attachmentId:int}")]
        public async Task<IActionResult> UnlinkFromNode(int nodeId, int attachmentId)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();
            try
            {
                await RemoveLink("node", nodeId, attachmentId);
                _syncPublisher.NotifyChanged(userId.Value);
                return Ok(new ResultOptions { Success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UnlinkFromNode failed node={NodeId} att={AttId}", nodeId, attachmentId);
                return StatusCode(500, ResultOptions.Fail("Failed to unlink attachment", 500));
            }
        }

        // POST /api/k/attachments/question/{questionId}/{attachmentId}
        [HttpPost("question/{questionId:int}/{attachmentId:int}")]
        public async Task<IActionResult> LinkToQuestion(int questionId, int attachmentId)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();
            try
            {
                await UpsertLink("question", questionId, attachmentId);
                _syncPublisher.NotifyChanged(userId.Value);
                return Ok(new ResultOptions { Success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LinkToQuestion failed q={QId} att={AttId}", questionId, attachmentId);
                return StatusCode(500, ResultOptions.Fail("Failed to link attachment", 500));
            }
        }

        // DELETE /api/k/attachments/question/{questionId}/{attachmentId}
        [HttpDelete("question/{questionId:int}/{attachmentId:int}")]
        public async Task<IActionResult> UnlinkFromQuestion(int questionId, int attachmentId)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();
            try
            {
                await RemoveLink("question", questionId, attachmentId);
                _syncPublisher.NotifyChanged(userId.Value);
                return Ok(new ResultOptions { Success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UnlinkFromQuestion failed q={QId} att={AttId}", questionId, attachmentId);
                return StatusCode(500, ResultOptions.Fail("Failed to unlink attachment", 500));
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private async Task<List<KAttachmentResponse>> LoadLinkedAttachments(string entityType, int entityId)
        {
            var links = await _db.KAttachmentLinks
                .Where(l => l.EntityType == entityType && l.EntityId == entityId)
                .ToListAsync();
            if (links.Count == 0) return [];
            var attIds = links.Select(l => l.AttachmentId).ToList();
            return await _db.KAttachments
                .Where(a => attIds.Contains(a.Id) && a.DeletedAt == null)
                .OrderBy(a => a.SortOrder).ThenBy(a => a.Title)
                .Select(a => new KAttachmentResponse
                {
                    Id        = a.Id,
                    Title     = a.Title,
                    Type      = a.Type,
                    Language  = a.Language,
                    Content   = a.Content,
                    SortOrder = a.SortOrder,
                })
                .ToListAsync();
        }

        private async Task UpsertLink(string entityType, int entityId, int attachmentId)
        {
            var exists = await _db.KAttachmentLinks.AnyAsync(l =>
                l.AttachmentId == attachmentId &&
                l.EntityType == entityType &&
                l.EntityId == entityId);
            if (exists) return;
            _db.KAttachmentLinks.Add(new KAttachmentLinkEntity
            {
                AttachmentId = attachmentId,
                EntityType   = entityType,
                EntityId     = entityId,
                CreatedAt    = DateTime.UtcNow,
            });
            await _db.SaveChangesAsync();
        }

        private async Task RemoveLink(string entityType, int entityId, int attachmentId)
        {
            var link = await _db.KAttachmentLinks.FirstOrDefaultAsync(l =>
                l.AttachmentId == attachmentId &&
                l.EntityType == entityType &&
                l.EntityId == entityId);
            if (link == null) return;
            _db.KAttachmentLinks.Remove(link);
            await _db.SaveChangesAsync();
        }
    }
}
