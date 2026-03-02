// Import ASP.NET Core authorization attributes
using Microsoft.AspNetCore.Authorization;
// Import base MVC/API controller types and result helpers
using Microsoft.AspNetCore.Mvc;
// Import Entity Framework Core for async database queries
using Microsoft.EntityFrameworkCore;
// Import project models and the license hashing helper
using seragenda.Models;
using seragenda.Services;
// Import Claims support for extracting the current user's identity from the JWT
using System.Security.Claims;

namespace seragenda.Controllers
{
    // Marks this class as an API controller (enables auto model binding, validation attributes, etc.)
    [ApiController]
    // All routes in this controller are prefixed with /api/access
    [Route("api/[controller]")]
    /// <summary>
    /// Handles license key validation and status checking for end users.
    /// Authenticated users submit their license code here to gain or verify access to the application.
    /// </summary>
    public class AccessController : ControllerBase
    {
        // Entity Framework database context for querying license and user records
        private readonly AgendaContext _context;

        /// <summary>
        /// Constructor — receives the database context via dependency injection.
        /// </summary>
        /// <param name="context">The EF Core database context for the agenda database</param>
        public AccessController(AgendaContext context)
        {
            _context = context;
        }

        // POST /api/access/validate
        // An authenticated user submits their license code to activate access
        [HttpPost("validate")]
        // Requires a valid JWT token — only logged-in users may validate a license
        [Authorize]
        /// <summary>
        /// Validates a license code submitted by the currently authenticated user.
        /// If the license is valid and unassigned, it is linked to the requesting user.
        /// Returns the normalised plain code on success so the client can cache it locally.
        /// </summary>
        /// <param name="dto">DTO containing the license code entered by the user</param>
        public async Task<IActionResult> Validate([FromBody] AccessCodeDto dto)
        {
            // Reject empty or whitespace-only codes immediately
            if (string.IsNullOrWhiteSpace(dto.Code))
                return BadRequest(new { message = "Code requis" });

            // Hash the submitted code so we can compare against the hashed value stored in the database.
            // The plain code is never stored — only its SHA-256 hash is persisted.
            var codeHash = LicenseHelper.HashCode(dto.Code);

            // Look up the license by its hashed code
            var license = await _context.Licenses
                .FirstOrDefaultAsync(l => l.Code == codeHash);

            // Reject if no matching license exists or if it has been manually revoked
            if (license == null || !license.IsActive)
                return BadRequest(new { message = "Code invalide ou révoqué" });

            // Reject if the license has passed its optional expiry date
            if (license.ExpiresAt.HasValue && license.ExpiresAt.Value < DateTime.UtcNow)
                return BadRequest(new { message = "Licence expirée" });

            // If the license has not yet been assigned to anyone, link it to the current user
            if (license.AssignedUserId == null)
            {
                // Read the current user's email from the JWT Name claim
                var email = User.FindFirst(ClaimTypes.Name)?.Value;
                // Load the full user entity to get the integer primary key
                var user  = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == email);
                if (user != null)
                {
                    // Assign the license to this user and record when the assignment happened
                    license.AssignedUserId = user.IdUser;
                    license.AssignedAt     = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }
            // If the license is already assigned (possibly to this same user), we still return success.
            // The client should check /api/access/check to verify ongoing validity.

            // Return the normalised plain code (uppercased and trimmed) — not the stored hash
            return Ok(new { valid = true, code = dto.Code.Trim().ToUpper() });
        }

        // GET /api/access/check?code=...
        // Allows the client to verify that a previously validated license is still active
        [HttpGet("check")]
        // Requires a valid JWT token — only logged-in users may check a license
        [Authorize]
        /// <summary>
        /// Checks whether a given license code is still valid (active and not expired).
        /// Used by the client to periodically re-verify access without re-submitting the full validation flow.
        /// </summary>
        /// <param name="code">The plain license code to check (passed as a query string parameter)</param>
        public async Task<IActionResult> Check([FromQuery] string code)
        {
            // Reject if no code was provided in the query string
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest(new { valid = false, message = "Code manquant" });

            // Hash the submitted code to compare against the stored hash
            var codeHash = LicenseHelper.HashCode(code);

            // Look up the license record by hash
            var license = await _context.Licenses
                .FirstOrDefaultAsync(l => l.Code == codeHash);

            // Return valid=false if the license was revoked or does not exist
            if (license == null || !license.IsActive)
                return Ok(new { valid = false, message = "Licence révoquée" });

            // Return valid=false if the license has expired
            if (license.ExpiresAt.HasValue && license.ExpiresAt.Value < DateTime.UtcNow)
                return Ok(new { valid = false, message = "Licence expirée" });

            // License is active and within its validity window
            return Ok(new { valid = true });
        }
    }

    /// <summary>
    /// Data Transfer Object for the POST /api/access/validate endpoint.
    /// Carries the plain license code entered by the end user.
    /// </summary>
    public class AccessCodeDto
    {
        // The license code as entered by the user (before normalisation or hashing)
        public string Code { get; set; } = string.Empty;
    }
}
