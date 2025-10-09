using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppAPI.Extensions;
using SuperAppDataRepositories.Ins;
using SuperAppModels.Models;

namespace SuperAppAPI.Controllers
{
    /// <summary>
    /// Controller for managing standard registry/configuration operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Restore authorization for all endpoints - security critical!
    public class StandardRegistryController : ControllerBase
    {
        private readonly IStandardRegistryRepository _repository;
        private readonly ILogger<StandardRegistryController> _logger;

        public StandardRegistryController(
            IStandardRegistryRepository repository,
            ILogger<StandardRegistryController> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets standard registry entries with optional type filtering
        /// </summary>
        /// <param name="type">Optional registry type filter</param>
        /// <returns>List of standard registry entries</returns>
        /// <response code="200">Registry entries retrieved successfully</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<StandardRegistry>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStandardRegistries([FromQuery] string? type = null)
        {
            try
            {
                var userEmail = User.GetUserEmail();
                if (string.IsNullOrEmpty(userEmail))
                {
                    _logger.LogWarning("Failed to extract user email from token");
                    return Unauthorized(new { Message = "Invalid token claims" });
                }

                _logger.LogInformation("Getting standard registries for user: {UserEmail}, Type: {Type}", 
                    userEmail, type);

                var registries = await _repository.GetStandardRegistries(type);

                _logger.LogInformation("Successfully retrieved {RegistryCount} standard registries for user: {UserEmail}", 
                    registries?.Count ?? 0, userEmail);

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
                _logger.LogError(ex, "Unexpected error occurred while retrieving standard registries");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Message = "An error occurred while retrieving standard registries" });
            }
        }

        /// <summary>
        /// Gets a specific standard registry entry by ID
        /// </summary>
        /// <param name="id">Registry entry ID</param>
        /// <returns>Standard registry entry if found</returns>
        /// <response code="200">Registry entry retrieved successfully</response>
        /// <response code="400">Invalid registry ID</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="404">Registry entry not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(StandardRegistry), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStandardRegistryById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid registry ID provided: {RegistryId}", id);
                    return BadRequest(new { Message = "Registry ID must be a positive integer" });
                }

                var userEmail = User.GetUserEmail();
                if (string.IsNullOrEmpty(userEmail))
                {
                    _logger.LogWarning("Failed to extract user email from token");
                    return Unauthorized(new { Message = "Invalid token claims" });
                }

                _logger.LogInformation("Getting standard registry {RegistryId} for user: {UserEmail}", 
                    id, userEmail);

                // Get all registries and find the specific one by ID
                // TODO: Add a method to repository to get by ID directly for better performance
                var allRegistries = await _repository.GetStandardRegistries(null);
                var registry = allRegistries?.FirstOrDefault(r => r.Id == id);

                if (registry == null)
                {
                    _logger.LogWarning("Standard registry {RegistryId} not found for user: {UserEmail}", 
                        id, userEmail);
                    return NotFound(new { Message = $"Standard registry with ID {id} not found" });
                }

                _logger.LogInformation("Successfully retrieved standard registry {RegistryId} for user: {UserEmail}", 
                    id, userEmail);
                return Ok(registry);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument provided for get standard registry by ID: {RegistryId}", id);
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt for standard registry {RegistryId}", id);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while retrieving standard registry {RegistryId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Message = "An error occurred while retrieving the standard registry" });
            }
        }
    }
}
