using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using seragenda.Models;

namespace seragenda.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "ADMIN")]
    public class AdminController : ControllerBase
    {
        private readonly AgendaContext _context;

        public AdminController(AgendaContext context)
        {
            _context = context;
        }

        // GET /api/admin/licenses
        [HttpGet("licenses")]
        public async Task<IActionResult> GetLicenses()
        {
            var licenses = await _context.Licenses
                .Include(l => l.AssignedUser)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new
                {
                    l.Id,
                    l.Code,
                    l.Label,
                    l.IsActive,
                    l.CreatedAt,
                    l.ExpiresAt,
                    l.AssignedAt,
                    AssignedEmail = l.AssignedUser != null ? l.AssignedUser.Email : null,
                    Status = !l.IsActive ? "Révoquée"
                           : l.ExpiresAt.HasValue && l.ExpiresAt.Value < DateTime.UtcNow ? "Expirée"
                           : l.AssignedUserId != null ? "Utilisée"
                           : "Disponible"
                })
                .ToListAsync();

            return Ok(licenses);
        }

        // POST /api/admin/licenses
        [HttpPost("licenses")]
        public async Task<IActionResult> CreateLicense([FromBody] CreateLicenseDto dto)
        {
            var code = string.IsNullOrWhiteSpace(dto.Code)
                ? GenerateCode()
                : dto.Code.Trim().ToUpper();

            if (await _context.Licenses.AnyAsync(l => l.Code.ToLower() == code.ToLower()))
                return BadRequest(new { message = "Ce code existe déjà" });

            var license = new License
            {
                Code = code,
                Label = dto.Label?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = dto.ExpiresAt
            };

            _context.Licenses.Add(license);
            await _context.SaveChangesAsync();

            return Ok(new { license.Id, license.Code, license.Label, license.IsActive, license.CreatedAt, license.ExpiresAt });
        }

        // PUT /api/admin/licenses/{id}/revoke
        [HttpPut("licenses/{id}/revoke")]
        public async Task<IActionResult> Revoke(int id)
        {
            var license = await _context.Licenses.FindAsync(id);
            if (license == null) return NotFound();

            license.IsActive = false;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Licence révoquée" });
        }

        // PUT /api/admin/licenses/{id}/reactivate
        [HttpPut("licenses/{id}/reactivate")]
        public async Task<IActionResult> Reactivate(int id)
        {
            var license = await _context.Licenses.FindAsync(id);
            if (license == null) return NotFound();

            license.IsActive = true;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Licence réactivée" });
        }

        // DELETE /api/admin/licenses/{id}
        [HttpDelete("licenses/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var license = await _context.Licenses.FindAsync(id);
            if (license == null) return NotFound();

            _context.Licenses.Remove(license);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Licence supprimée" });
        }

        private static string GenerateCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var random = new Random();
            return new string(Enumerable.Range(0, 10).Select(_ => chars[random.Next(chars.Length)]).ToArray());
        }
    }

    public class CreateLicenseDto
    {
        public string? Code { get; set; }       // Si vide, code aléatoire généré
        public string? Label { get; set; }       // Ex: "PROF-DUPONT"
        public DateTime? ExpiresAt { get; set; } // Null = pas d'expiration
    }
}
