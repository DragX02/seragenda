using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace seragenda.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class AccessController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AccessController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("validate")]
        public IActionResult Validate([FromBody] AccessCodeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Code))
                return BadRequest(new { message = "Code requis" });

            var validCodes = _configuration.GetSection("AccessCodes").Get<List<string>>() ?? new();

            if (validCodes.Contains(dto.Code.Trim(), StringComparer.OrdinalIgnoreCase))
                return Ok(new { valid = true });

            return BadRequest(new { message = "Code invalide" });
        }
    }

    public class AccessCodeDto
    {
        public string Code { get; set; } = string.Empty;
    }
}
