using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppAPI.Extensions;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppServices.Interfaces;

namespace SuperAppAPI.Controllers
{
    [ApiController]
    [Route("api/wiki")]
    [Authorize]
    public class WikiController : ControllerBase
    {
        private readonly IWikiService _wikiService;
        private readonly ILogger<WikiController> _logger;

        public WikiController(IWikiService wikiService, ILogger<WikiController> logger)
        {
            _wikiService = wikiService ?? throw new ArgumentNullException(nameof(wikiService));
            _logger      = logger      ?? throw new ArgumentNullException(nameof(logger));
        }

        private int? GetUserId()
        {
            var claim = User.GetUserId();
            return string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id) ? null : id;
        }

        /// <summary>Returns all keywords + infos for the current user</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized();

                var result = await _wikiService.GetAllAsync(userId.Value);
                return Ok(new ResultOptions { Success = true, Object = result, Status = 200 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting wiki data");
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        /// <summary>Create a new keyword</summary>
        [HttpPost("keywords")]
        public async Task<IActionResult> CreateKeyword([FromBody] WikiCreateKeywordRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var userId = GetUserId();
                if (userId == null) return Unauthorized();

                request.UserId = userId.Value;
                var result = await _wikiService.CreateKeywordAsync(request);
                return result.Success
                    ? StatusCode(result.Status ?? 201, result)
                    : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating wiki keyword");
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        /// <summary>Create or update an info entry (id present = update)</summary>
        [HttpPost("infos")]
        public async Task<IActionResult> UpsertInfo([FromBody] WikiUpsertInfoRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var userId = GetUserId();
                if (userId == null) return Unauthorized();

                request.UserId = userId.Value;
                var result = await _wikiService.UpsertInfoAsync(request);
                return result.Success
                    ? StatusCode(result.Status ?? 200, result)
                    : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting wiki info");
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        /// <summary>Soft-delete an info entry</summary>
        [HttpDelete("infos/{id:int}")]
        public async Task<IActionResult> DeleteInfo(int id)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized();

                var result = await _wikiService.SoftDeleteInfoAsync(id, userId.Value);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 404, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting wiki info {Id}", id);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        /// <summary>Restore a soft-deleted info entry</summary>
        [HttpPost("infos/{id:int}/restore")]
        public async Task<IActionResult> RestoreInfo(int id)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized();

                var result = await _wikiService.RestoreInfoAsync(id, userId.Value);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 404, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring wiki info {Id}", id);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        /// <summary>Soft-delete a keyword and remove all its info links</summary>
        [HttpDelete("keywords/{id:int}")]
        public async Task<IActionResult> DeleteKeyword(int id)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized();

                var result = await _wikiService.SoftDeleteKeywordAsync(id, userId.Value);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 404, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting wiki keyword {Id}", id);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        /// <summary>Update keyword icon and synonyms</summary>
        [HttpPatch("keywords/{id:int}")]
        public async Task<IActionResult> UpdateKeyword(int id, [FromBody] WikiUpdateKeywordRequest request)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized();

                request.UserId = userId.Value;
                var result = await _wikiService.UpdateKeywordMetaAsync(id, request);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating wiki keyword {Id}", id);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        /// <summary>Persist a keyword's pinned graph position</summary>
        [HttpPost("keywords/{id:int}/position")]
        public async Task<IActionResult> SavePosition(int id, [FromBody] WikiSavePinnedPositionRequest request)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized();

                request.UserId = userId.Value;
                var result = await _wikiService.SavePinnedPositionAsync(id, request);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 404, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving position for keyword {Id}", id);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        /// <summary>Record a keyword interaction (view / read / edit)</summary>
        [HttpPost("keywords/{id:int}/interact/{type}")]
        public async Task<IActionResult> Interact(int id, string type)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized();

                var result = await _wikiService.IncrementInteractionAsync(id, userId.Value, type);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 400, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording interaction for keyword {Id}", id);
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }

        /// <summary>
        /// Full rescan: rebuild all keyword↔info links from scratch by text matching.
        /// Use after bulk edits, keyword renames, or synonym changes made outside the UI.
        /// </summary>
        [HttpPost("rescan")]
        public async Task<IActionResult> RescanAllLinks()
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized();

                var result = await _wikiService.RescanAllLinksAsync(userId.Value);
                return result.Success ? Ok(result) : StatusCode(result.Status ?? 500, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rescanning all wiki links");
                return StatusCode(500, new ResultOptions { Success = false, Message = "An error occurred", Status = 500 });
            }
        }
    }
}
