using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using seragenda.Models;
using seragenda.Services;
using System.Security.Claims;

namespace seragenda.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccessController : ControllerBase
    {
        private readonly AgendaContext _context;

        public AccessController(AgendaContext context)
        {
            _context = context;
        }

        // POST /api/access/validate  (utilisateur connecté, valide son code de licence)
        [HttpPost("validate")]
        [Authorize]
        public async Task<IActionResult> Validate([FromBody] AccessCodeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Code))
                return BadRequest(new { message = "Code requis" });

            var codeHash = LicenseHelper.HashCode(dto.Code);
            var license = await _context.Licenses
                .FirstOrDefaultAsync(l => l.Code == codeHash);

            if (license == null || !license.IsActive)
                return BadRequest(new { message = "Code invalide ou révoqué" });

            if (license.ExpiresAt.HasValue && license.ExpiresAt.Value < DateTime.UtcNow)
                return BadRequest(new { message = "Licence expirée" });

            // Associer la licence à l'utilisateur connecté (si pas déjà assignée à quelqu'un d'autre)
            if (license.AssignedUserId == null)
            {
                var email = User.FindFirst(ClaimTypes.Name)?.Value;
                var user = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == email);
                if (user != null)
                {
                    license.AssignedUserId = user.IdUser;
                    license.AssignedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }

            // On retourne le code en clair (fourni par l'utilisateur), pas le hash stocké
            return Ok(new { valid = true, code = dto.Code.Trim().ToUpper() });
        }

        // GET /api/access/check  (vérifie que la licence de l'utilisateur est toujours active)
        [HttpGet("check")]
        [Authorize]
        public async Task<IActionResult> Check([FromQuery] string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest(new { valid = false, message = "Code manquant" });

            var codeHash = LicenseHelper.HashCode(code);
            var license = await _context.Licenses
                .FirstOrDefaultAsync(l => l.Code == codeHash);

            if (license == null || !license.IsActive)
                return Ok(new { valid = false, message = "Licence révoquée" });

            if (license.ExpiresAt.HasValue && license.ExpiresAt.Value < DateTime.UtcNow)
                return Ok(new { valid = false, message = "Licence expirée" });

            return Ok(new { valid = true });
        }
    }

    public class AccessCodeDto
    {
        public string Code { get; set; } = string.Empty;
    }
}
