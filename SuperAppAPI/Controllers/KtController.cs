using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppAPI.Extensions;
using SuperAppModels.DTOs.Requests;
using SuperAppServices.Interfaces;

namespace SuperAppAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class KtController : ControllerBase
    {
        private readonly IKtService _service;
        private readonly ILogger<KtController> _logger;

        public KtController(IKtService service, ILogger<KtController> logger)
        {
            _service = service;
            _logger = logger;
        }

        private int GetAuthenticatedUserId() => int.Parse(User.GetUserId() ?? "0");

        // ── Knowledge ────────────────────────────────────────────────────────

        /// <summary>GET /api/kt/knowledges?rootsOnly=true&searchText=&deletedAt=null</summary>
        [HttpGet("knowledges")]
        public async Task<IActionResult> GetKnowledges(
            [FromQuery] string? searchText,
            [FromQuery] int? parentId,
            [FromQuery] bool rootsOnly = false,
            [FromQuery] string? deletedAt = "null")
        {
            var userId = GetAuthenticatedUserId();
            _logger.LogInformation("GetKnowledges userId={UserId} rootsOnly={RootsOnly}", userId, rootsOnly);

            var filter = new KtKnowledgeFilterOptions
            {
                UserId = userId,
                SearchText = searchText,
                ParentId = parentId,
                RootsOnly = rootsOnly,
                DeletedAt = deletedAt,
            };

            var result = await _service.GetKnowledgesAsync(filter);
            return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
        }

        /// <summary>POST /api/kt/knowledges/batch</summary>
        [HttpPost("knowledges/batch")]
        public async Task<IActionResult> UpsertKnowledges([FromBody] List<UpsertKtKnowledgeRequest> requests)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetAuthenticatedUserId();
            requests.ForEach(r => r.UserId = userId);

            _logger.LogInformation("UpsertKnowledges userId={UserId} count={Count}", userId, requests.Count);

            var result = await _service.UpsertKnowledgesAsync(requests);
            return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
        }

        // ── Cards ─────────────────────────────────────────────────────────────

        /// <summary>GET /api/kt/cards?knowledgeId=1&deletedAt=null</summary>
        [HttpGet("cards")]
        public async Task<IActionResult> GetCards(
            [FromQuery] int? knowledgeId,
            [FromQuery] string? searchText,
            [FromQuery] string? deletedAt = "null")
        {
            var userId = GetAuthenticatedUserId();
            _logger.LogInformation("GetCards userId={UserId} knowledgeId={KnowledgeId}", userId, knowledgeId);

            var filter = new KtCardFilterOptions
            {
                UserId = userId,
                KnowledgeId = knowledgeId,
                SearchText = searchText,
                DeletedAt = deletedAt,
            };

            var result = await _service.GetCardsAsync(filter);
            return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
        }

        /// <summary>POST /api/kt/cards/batch</summary>
        [HttpPost("cards/batch")]
        public async Task<IActionResult> UpsertCards([FromBody] List<UpsertKtCardRequest> requests)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetAuthenticatedUserId();
            requests.ForEach(r => r.UserId = userId);

            _logger.LogInformation("UpsertCards userId={UserId} count={Count}", userId, requests.Count);

            var result = await _service.UpsertCardsAsync(requests);
            return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
        }
    }
}
