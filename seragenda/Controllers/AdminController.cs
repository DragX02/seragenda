using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using seragenda.Models;
using seragenda.Services;
using System.Security.Cryptography;

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
            try
            {
                string plainCode;
                string codeHash;

                if (string.IsNullOrWhiteSpace(dto.Code))
                {
                    // Génère un code unique — hash calculé AVANT la requête LINQ
                    do
                    {
                        plainCode = GenerateCode();
                        codeHash = LicenseHelper.HashCode(plainCode);
                    }
                    while (await _context.Licenses.AnyAsync(l => l.Code == codeHash));
                }
                else
                {
                    plainCode = dto.Code.Trim().ToUpper();
                    codeHash = LicenseHelper.HashCode(plainCode);
                    if (await _context.Licenses.AnyAsync(l => l.Code == codeHash))
                        return BadRequest(new { message = "Ce code existe déjà" });
                }

                var license = new License
                {
                    Code = codeHash,
                    Label = dto.Label?.Trim(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = dto.ExpiresAt
                };

                _context.Licenses.Add(license);
                await _context.SaveChangesAsync();

                // Le plainCode est retourné UNE SEULE FOIS à l'admin pour le transmettre au tiers
                return Ok(new { license.Id, Code = plainCode, license.Label, license.IsActive, license.CreatedAt, license.ExpiresAt });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.InnerException?.Message ?? ex.Message });
            }
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
            var bytes = RandomNumberGenerator.GetBytes(10);
            return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
        }
    }

    public class CreateLicenseDto
    {
        public string? Code { get; set; }       // Si vide, code aléatoire généré
        public string? Label { get; set; }       // Ex: "PROF-DUPONT"
        public DateTime? ExpiresAt { get; set; } // Null = pas d'expiration
    }
}
