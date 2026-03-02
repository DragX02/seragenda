// Import ASP.NET Core authorization attributes
using Microsoft.AspNetCore.Authorization;
// Import base MVC/API controller types and result helpers
using Microsoft.AspNetCore.Mvc;

namespace seragenda.Controllers
{
    // Marks this class as an API controller
    [ApiController]
    // Routes in this controller are prefixed with /api/health
    [Route("api/[controller]")]
    // Health check must be publicly accessible — no JWT required (used by uptime monitors, load balancers, etc.)
    [AllowAnonymous]
    /// <summary>
    /// Provides a lightweight health-check endpoint for infrastructure monitoring.
    /// Uptime monitors and load balancers can poll GET /api/health to verify
    /// that the API process is running and reachable.
    /// </summary>
    public class HealthController : ControllerBase
    {
        // GET /api/health
        // Returns a simple JSON payload confirming the API is online
        [HttpGet]
        /// <summary>
        /// Returns a JSON object with the current server status, UTC timestamp,
        /// server name, and API version number.
        /// A 200 OK response indicates the service is healthy.
        /// </summary>
        public IActionResult Get()
        {
            return Ok(new
            {
                // Human-readable status flag — always "online" when this code runs
                status    = "online",
                // Current UTC time so the caller can verify the server clock is reasonable
                timestamp = DateTime.UtcNow,
                // Identifies which application is responding (useful in multi-service deployments)
                server    = "AgendaProf API",
                // Semantic version of this API deployment
                version   = "1.0.0"
            });
        }
    }
}
