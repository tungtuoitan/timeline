using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppModels.Mos;
using SuperAppDataRepositories.Ins;

namespace SuperAppAPI.Controllers
{
    //[Authorize]  // Temporarily commented out - Require authentication for all endpoints
    [ApiController]
    [Route("api/[controller]")]
    public class StandardRegistryController : ControllerBase
    {
        private readonly IStandardRegistryRepository _repository;
        private readonly ILogger<StandardRegistryController> _logger;

        public StandardRegistryController(
            IStandardRegistryRepository repository,
            ILogger<StandardRegistryController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// Get standard registry entries by type
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<StandardRegistry>), 200)]
        public async Task<ActionResult<List<StandardRegistry>>> GetStandardRegistries(
            [FromQuery] string? type = null)
        {
            _logger.LogInformation("Getting standard registries. Type: {Type}", type);

            var registries = await _repository.GetStandardRegistries(type);

            return Ok(registries);
        }
    }
}
