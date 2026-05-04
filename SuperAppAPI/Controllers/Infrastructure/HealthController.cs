using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppAPI.Extensions;

namespace SuperAppAPI.Controllers.Infrastructure
{
    /// <summary>
    /// Controller for health check and monitoring endpoints
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;

        public HealthController(ILogger<HealthController> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Public health check endpoint - no authentication required
        /// </summary>
        /// <returns>Basic system health status</returns>
        /// <response code="200">System is healthy</response>
        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public ActionResult GetHealth()
        {
            try
            {
                var healthInfo = new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                    version = "1.0.0",
                    uptime = DateTime.UtcNow.Subtract(System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime())
                };

                _logger.LogDebug("Health check requested - system healthy");
                return Ok(healthInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during health check");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new 
                { 
                    status = "unhealthy",
                    timestamp = DateTime.UtcNow,
                    error = "Health check failed"
                });
            }
        }
    }
}