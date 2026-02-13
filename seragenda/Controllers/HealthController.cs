using Microsoft.AspNetCore.Mvc;

namespace seragenda.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                status = "online",
                timestamp = DateTime.UtcNow,
                server = "AgendaProf API",
                version = "1.0.0"
            });
        }
    }
}
