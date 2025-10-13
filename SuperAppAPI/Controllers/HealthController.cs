using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperAppAPI.Extensions;

namespace SuperAppAPI.Controllers
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

        /// <summary>
        /// Protected health check endpoint - requires authentication
        /// </summary>
        /// <returns>Detailed system health status for authenticated users</returns>
        /// <response code="200">System is healthy with user context</response>
        /// <response code="401">Unauthorized - invalid or missing token</response>
        /// <response code="503">Service unavailable</response>
        //[Authorize] // TEMPORARY: Authorization disabled for development
        [HttpGet("secure")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public ActionResult GetSecureHealth()
        {
            try
            {
                // TEMPORARY: Using mock data while auth is disabled
                var userEmail = User.GetUserEmail() ?? "hoanhtungle@gmail.com";
                var userId = User.GetUserId() ?? "1";

                var secureHealthInfo = new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                    version = "1.0.0",
                    uptime = DateTime.UtcNow.Subtract(System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime()),
                    authentication = new
                    {
                        isAuthenticated = User.Identity?.IsAuthenticated ?? false,
                        userEmail = userEmail,
                        userId = userId,
                        tokenClaims = User.Claims.Count(),
                        authDisabled = true // TEMPORARY: Indicate auth is disabled
                    },
                    system = new
                    {
                        machineName = Environment.MachineName,
                        osVersion = Environment.OSVersion.ToString(),
                        processorCount = Environment.ProcessorCount,
                        workingSet = Environment.WorkingSet,
                        gcMemory = GC.GetTotalMemory(false)
                    }
                };

                _logger.LogInformation("Secure health check requested by user: {UserEmail}", userEmail);
                return Ok(secureHealthInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during secure health check");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new 
                { 
                    status = "unhealthy",
                    timestamp = DateTime.UtcNow,
                    error = "Secure health check failed"
                });
            }
        }

        /// <summary>
        /// Readiness probe endpoint for Kubernetes/container orchestration
        /// </summary>
        /// <returns>Simple ready status</returns>
        /// <response code="200">Service is ready</response>
        /// <response code="503">Service is not ready</response>
        [AllowAnonymous]
        [HttpGet("ready")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public ActionResult GetReadiness()
        {
            try
            {
                // TODO: Add actual readiness checks (database connectivity, external services, etc.)
                // For now, just return ready if the service is running

                var readinessInfo = new
                {
                    status = "ready",
                    timestamp = DateTime.UtcNow,
                    checks = new
                    {
                        database = "healthy", // TODO: Add actual DB ping
                        services = "healthy"  // TODO: Add actual service checks
                    }
                };

                _logger.LogDebug("Readiness check requested - service ready");
                return Ok(readinessInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during readiness check");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new 
                { 
                    status = "not ready",
                    timestamp = DateTime.UtcNow,
                    error = "Readiness check failed"
                });
            }
        }

        /// <summary>
        /// Liveness probe endpoint for Kubernetes/container orchestration
        /// </summary>
        /// <returns>Simple alive status</returns>
        /// <response code="200">Service is alive</response>
        [AllowAnonymous]
        [HttpGet("live")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public ActionResult GetLiveness()
        {
            // Liveness should be simple - if we can respond, we're alive
            var livenessInfo = new
            {
                status = "alive",
                timestamp = DateTime.UtcNow
            };

            return Ok(livenessInfo);
        }
    }
}