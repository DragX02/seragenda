// Import ASP.NET Core authorization attributes
using Microsoft.AspNetCore.Authorization;
// Import base MVC/API controller types and result helpers
using Microsoft.AspNetCore.Mvc;
// Import Entity Framework Core for async database operations
using Microsoft.EntityFrameworkCore;
// Import project models and services
using seragenda.Models;
using seragenda.Services;
// Import cryptography for random code generation
using System.Security.Cryptography;

namespace seragenda.Controllers
{
    // Marks this class as an API controller with automatic model binding and validation
    [ApiController]
    // All routes in this controller are prefixed with /api/admin
    [Route("api/[controller]")]
    // Restricts all endpoints in this controller to users with the ADMIN role
    [Authorize(Roles = "ADMIN")]
    /// <summary>
    /// Provides administrative endpoints for managing license keys.
    /// Only accessible to users with the ADMIN system role.
    /// Supports listing, creating, revoking, reactivating, and deleting licenses.
    /// </summary>
    public class AdminController : ControllerBase
    {
        // Entity Framework database context for querying and persisting license data
        private readonly AgendaContext _context;

        /// <summary>
        /// Constructor — receives the database context via dependency injection.
        /// </summary>
        /// <param name="context">The EF Core database context for the agenda database</param>
        public AdminController(AgendaContext context)
        {
            _context = context;
        }

        // GET /api/admin/licenses
        // Returns all licenses with their current status derived from IsActive, ExpiresAt, and AssignedUserId
        [HttpGet("licenses")]
        /// <summary>
        /// Retrieves all license records ordered by creation date (most recent first).
        /// Each license includes a computed human-readable status string.
        /// </summary>
        public async Task<IActionResult> GetLicenses()
        {
            var licenses = await _context.Licenses
                // Eagerly load the navigation property so we can access the assigned user's email
                .Include(l => l.AssignedUser)
                // Most recently created licenses appear first
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new
                {
                    l.Id,
                    l.Code,    // This is the SHA-256 hash stored in the database; the plain code is never persisted
                    l.Label,
                    l.IsActive,
                    l.CreatedAt,
                    l.ExpiresAt,
                    l.AssignedAt,
                    // Show the email of the user who activated the license, or null if unused
                    AssignedEmail = l.AssignedUser != null ? l.AssignedUser.Email : null,
                    // Derive a status string:
                    // "Révoquée" if manually deactivated,
                    // "Expirée" if past the expiry date,
                    // "Utilisée" if assigned to a user,
                    // "Disponible" if active and not yet assigned
                    Status = !l.IsActive ? "Révoquée"
                           : l.ExpiresAt.HasValue && l.ExpiresAt.Value < DateTime.UtcNow ? "Expirée"
                           : l.AssignedUserId != null ? "Utilisée"
                           : "Disponible"
                })
                .ToListAsync();

            return Ok(licenses);
        }

        // POST /api/admin/licenses
        // Creates a new license with an optional custom code and optional expiry date
        [HttpPost("licenses")]
        /// <summary>
        /// Creates a new license key.
        /// If no code is provided in the request body, a random 10-character alphanumeric code is generated.
        /// The plain code is returned only once in this response; only its SHA-256 hash is stored.
        /// </summary>
        /// <param name="dto">The license creation parameters (optional code, optional label, optional expiry)</param>
        public async Task<IActionResult> CreateLicense([FromBody] CreateLicenseDto dto)
        {
            try
            {
                string plainCode; // The human-readable license key (returned to admin, never stored)
                string codeHash;  // The SHA-256 hash of the license key (stored in the database)

                if (string.IsNullOrWhiteSpace(dto.Code))
                {
                    // No code was provided — generate a unique random one
                    // Loop until we find a code whose hash does not already exist in the database
                    do
                    {
                        plainCode = GenerateCode();
                        // Hash the candidate code before querying — computed outside LINQ to avoid EF translation issues
                        codeHash = LicenseHelper.HashCode(plainCode);
                    }
                    while (await _context.Licenses.AnyAsync(l => l.Code == codeHash));
                }
                else
                {
                    // Use the admin-supplied code, normalized to uppercase
                    plainCode = dto.Code.Trim().ToUpper();
                    codeHash  = LicenseHelper.HashCode(plainCode);
                    // Reject if a license with the same hash already exists
                    if (await _context.Licenses.AnyAsync(l => l.Code == codeHash))
                        return BadRequest(new { message = "Ce code existe déjà" });
                }

                // Build the new license entity; only the hash is persisted, never the plain code
                var license = new License
                {
                    Code      = codeHash,
                    Label     = dto.Label?.Trim(),
                    IsActive  = true,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = dto.ExpiresAt  // Null means the license never expires
                };

                _context.Licenses.Add(license);
                await _context.SaveChangesAsync();

                // Return the plain code exactly once so the admin can transmit it to the end user.
                // After this response, the plain code is gone — only the hash remains in the DB.
                return Ok(new { license.Id, Code = plainCode, license.Label, license.IsActive, license.CreatedAt, license.ExpiresAt });
            }
            catch (Exception)
            {
                // Catch unexpected database or hashing errors and return a generic 500
                return StatusCode(500, new { message = "Erreur lors de la création de la licence" });
            }
        }

        // PUT /api/admin/licenses/{id}/revoke
        // Deactivates a license without deleting it — preserves audit history
        [HttpPut("licenses/{id}/revoke")]
        /// <summary>
        /// Revokes a license by setting its IsActive flag to false.
        /// Revoked licenses are rejected by the access validation endpoint.
        /// </summary>
        /// <param name="id">The primary key of the license to revoke</param>
        public async Task<IActionResult> Revoke(int id)
        {
            // Look up the license by its primary key
            var license = await _context.Licenses.FindAsync(id);
            // Return 404 if no license with this ID exists
            if (license == null) return NotFound();

            // Mark the license as inactive
            license.IsActive = false;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Licence révoquée" });
        }

        // PUT /api/admin/licenses/{id}/reactivate
        // Re-enables a previously revoked license
        [HttpPut("licenses/{id}/reactivate")]
        /// <summary>
        /// Reactivates a previously revoked license by setting its IsActive flag back to true.
        /// </summary>
        /// <param name="id">The primary key of the license to reactivate</param>
        public async Task<IActionResult> Reactivate(int id)
        {
            var license = await _context.Licenses.FindAsync(id);
            if (license == null) return NotFound();

            // Restore the active status
            license.IsActive = true;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Licence réactivée" });
        }

        // DELETE /api/admin/licenses/{id}
        // Permanently removes the license record from the database
        [HttpDelete("licenses/{id}")]
        /// <summary>
        /// Permanently deletes a license record.
        /// This action is irreversible; consider using revoke instead if audit history matters.
        /// </summary>
        /// <param name="id">The primary key of the license to delete</param>
        public async Task<IActionResult> Delete(int id)
        {
            var license = await _context.Licenses.FindAsync(id);
            if (license == null) return NotFound();

            // Remove the entity from the context and persist the deletion
            _context.Licenses.Remove(license);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Licence supprimée" });
        }

        /// <summary>
        /// Generates a random 10-character uppercase alphanumeric license code.
        /// Uses a visually unambiguous character set (no 0/O, 1/I, etc.) to prevent
        /// transcription errors when codes are shared verbally or printed.
        /// </summary>
        /// <returns>A random 10-character string drawn from the safe character set</returns>
        private static string GenerateCode()
        {
            // Safe character set: uppercase letters and digits, excluding visually ambiguous characters
            // (no 0/O, no 1/I) to make codes easy to read and type
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            // Generate 10 cryptographically random bytes using the OS RNG
            var bytes = RandomNumberGenerator.GetBytes(10);
            // Map each random byte to a character in the alphabet using modulo
            return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
        }
    }

    /// <summary>
    /// Data Transfer Object for the POST /api/admin/licenses endpoint.
    /// All fields are optional — sensible defaults are applied when they are omitted.
    /// </summary>
    public class CreateLicenseDto
    {
        // Optional: if null or empty, a random code is auto-generated
        public string? Code { get; set; }
        // Optional human-readable label to help the admin identify this license (e.g., "PROF-DUPONT")
        public string? Label { get; set; }
        // Optional expiry date/time in UTC; null means the license never expires
        public DateTime? ExpiresAt { get; set; }
    }
}
