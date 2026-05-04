using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppAPI.Extensions;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;
using SuperAppServices.Interfaces;

namespace SuperAppAPI.Controllers.Registry
{
    /// <summary>
    /// Controller for managing standard registry/configuration operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StandardRegistryController : ControllerBase
    {
        private readonly IStandardRegistryService _service;
        private readonly ILogger<StandardRegistryController> _logger;

        public StandardRegistryController(
            IStandardRegistryService service,
            ILogger<StandardRegistryController> logger)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get authenticated user email from JWT claims
        /// </summary>
        private string? GetAuthenticatedUserEmail()
        {
            return User.GetUserEmail();
        }

        /// <summary>
        /// Gets standard registry entries with optional type filtering
        /// </summary>
        /// <param name="type">Optional registry type to filter by. If not provided, returns all registries.</param>
        /// <param name="showAll">If true, returns all entries including inactive ones. If false, returns only active entries. Default: false</param>
        /// <returns>List of standard registry entries matching the criteria</returns>
        /// <response code="200">Registry entries retrieved successfully</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [Route("getStandardRegistryByType")]
        [ProducesResponseType(typeof(List<StandardRegistry>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStandardRegistries([FromQuery] string? type = null, [FromQuery] bool showAll = false)
        {
            try
            {
                var userEmail = GetAuthenticatedUserEmail();
                if (string.IsNullOrEmpty(userEmail))
                {
                    _logger.LogWarning("Failed to extract user email from token");
                    return Unauthorized(new { Message = "Invalid token claims" });
                }

                _logger.LogInformation("Getting standard registries for user: {UserEmail}, Type: {Type}, ShowAll: {ShowAll}",
                    userEmail, type ?? "ALL", showAll);

                var registries = await _service.GetStandardRegistries(type, showAll);

                _logger.LogInformation("Successfully retrieved {RegistryCount} standard registries for user: {UserEmail}, Type: {Type}",
                    registries?.Count ?? 0, userEmail, type ?? "ALL");

                return Ok(registries ?? new List<StandardRegistry>());
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument provided for get standard registries");
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt for standard registries");
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while retrieving standard registries for type: {Type}", type ?? "ALL");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Message = "An error occurred while retrieving standard registries" });
            }
        }

        /// <summary>
        /// Sets the default checklist template for a taskType registry entry.
        /// Saves the template text into json_detail as { "checklistTemplate": "..." }
        /// </summary>
        [HttpPost]
        [Route("setChecklistTemplate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SetChecklistTemplate([FromBody] SetChecklistTemplateRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var userEmail = GetAuthenticatedUserEmail();
                if (string.IsNullOrEmpty(userEmail)) return Unauthorized(new { Message = "Invalid token claims" });

                _logger.LogInformation("Setting checklist template for taskType: {TaskTypeCode}, User: {UserEmail}",
                    request.TaskTypeCode, userEmail);

                await _service.SetChecklistTemplateAsync(request.TaskTypeCode, request.Template);

                return Ok(new { success = true, message = "Checklist template saved." });
            }
            catch (KeyNotFoundException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting checklist template for taskType: {TaskTypeCode}", request.TaskTypeCode);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred." });
            }
        }
    }
}
