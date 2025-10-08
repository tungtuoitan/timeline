using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SuperAppAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        /// <summary>
        /// Health check endpoint - no authentication required
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(200)]
        public ActionResult GetHealth()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
            });
        }

        /// <summary>
        /// Protected health check - requires authentication
        /// </summary>
        // [Authorize]  // Temporarily commented out
        [HttpGet("secure")]
        [ProducesResponseType(200)]
        public ActionResult GetSecureHealth()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value;

            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                authenticatedUser = userId,
                isAuthenticated = User.Identity?.IsAuthenticated ?? false
            });
        }
    }
}