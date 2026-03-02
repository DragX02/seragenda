// Import ASP.NET Core authorization attributes
using Microsoft.AspNetCore.Authorization;
// Import base MVC/API controller types and result helpers
using Microsoft.AspNetCore.Mvc;
// Import Entity Framework Core for async database operations
using Microsoft.EntityFrameworkCore;
// Import project models
using seragenda.Models;
// Import Claims support for reading the current user's identity from the JWT
using System.Security.Claims;

namespace seragenda.Controllers
{
    // All routes in this controller are prefixed with /api/notes
    [Route("api/[controller]")]
    // Marks this class as an API controller
    [ApiController]
    // Requires a valid JWT token on all endpoints — unauthenticated requests are rejected
    [Authorize]
    /// <summary>
    /// Manages personal timed notes (agenda entries) for the authenticated user.
    /// Each note belongs to a single calendar day and occupies a specific hour slot (6–22).
    /// Notes support HTML-stripped plain-text content up to 2000 characters.
    /// </summary>
    public class NotesController : ControllerBase
    {
        // Entity Framework database context for reading and writing UserNote records
        private readonly AgendaContext _context;

        /// <summary>
        /// Constructor — receives the database context via dependency injection.
        /// </summary>
        /// <param name="context">The EF Core database context</param>
        public NotesController(AgendaContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Resolves the integer primary key of the currently authenticated user
        /// by looking up their email address (stored as the JWT Name claim) in the database.
        /// Returns null if the claim is missing or the user record cannot be found.
        /// </summary>
        /// <returns>The user's IdUser, or null if not found</returns>
        private async Task<int?> GetUserId()
        {
            // The Name claim was set to the user's email at login time
            var email = User.FindFirst(ClaimTypes.Name)?.Value;
            if (email == null) return null;
            var user = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == email);
            return user?.IdUser;
        }

        // GET /api/notes/date/{date}
        // Returns all notes for the current user on the specified calendar date
        [HttpGet("date/{date}")]
        /// <summary>
        /// Retrieves all notes belonging to the current user on a specific date,
        /// ordered by hour (earliest first).
        /// </summary>
        /// <param name="date">The target date, parsed from the route segment</param>
        public async Task<IActionResult> GetNotesForDate(DateTime date)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            // Compute the inclusive start and exclusive end of the target calendar day
            var dayStart = date.Date;                  // Midnight at the start of the day
            var dayEnd   = dayStart.AddDays(1);        // Midnight at the start of the next day

            // Fetch notes that fall within this day window, ordered by their hour slot
            var notes = await _context.UserNotes
                .Where(n => n.IdUserFk == userId && n.Date >= dayStart && n.Date < dayEnd)
                .OrderBy(n => n.Hour)
                .ToListAsync();

            return Ok(notes);
        }

        // GET /api/notes/range?start=...&end=...
        // Returns all notes for the current user within a date range (max 62 days to prevent abuse)
        [HttpGet("range")]
        /// <summary>
        /// Retrieves all notes belonging to the current user within a date range.
        /// The range is capped at 62 days to prevent excessively large responses.
        /// Results are sorted by date and then by hour within each day.
        /// </summary>
        /// <param name="start">First day of the range (inclusive)</param>
        /// <param name="end">Last day of the range (inclusive)</param>
        public async Task<IActionResult> GetNotesForRange([FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            // Reject ranges longer than approximately two months to avoid resource exhaustion
            if ((end - start).TotalDays > 62) return BadRequest("Plage trop grande.");

            // Fetch notes that fall within [start.Date, end.Date] inclusive
            var notes = await _context.UserNotes
                .Where(n => n.IdUserFk == userId && n.Date >= start.Date && n.Date <= end.Date)
                .OrderBy(n => n.Date)  // Sort by date first (chronological order across days)
                .ThenBy(n => n.Hour)   // Then by hour within each day
                .ToListAsync();

            return Ok(notes);
        }

        // POST /api/notes
        // Creates a new note (Id == 0) or updates an existing one (non-zero Id)
        [HttpPost]
        /// <summary>
        /// Creates or updates a note entry.
        /// Applies server-side sanitization to the content (strips dangerous HTML tags).
        /// Enforces valid hour ranges (6–22 for start, 7–23 for end).
        /// The date is normalized to midnight to prevent timezone-offset issues.
        /// </summary>
        /// <param name="note">The note data submitted by the client</param>
        public async Task<IActionResult> Save([FromBody] UserNote note)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            // Strip the time component from the date and mark it as timezone-unspecified.
            // This prevents UTC vs. local time offset from shifting the note to a different calendar day.
            note.Date = new DateTime(note.Date.Year, note.Date.Month, note.Date.Day, 0, 0, 0, DateTimeKind.Unspecified);

            // --- Content sanitization ---
            // Trim leading/trailing whitespace and ensure the field is not null
            note.Content = note.Content?.Trim() ?? string.Empty;

            // First pass: remove the inner content of dangerous block-level elements
            // (script, style, iframe, object, embed) including their tags
            note.Content = System.Text.RegularExpressions.Regex.Replace(
                note.Content,
                @"<(script|style|iframe|object|embed)[^>]*>.*?<\/\1>",
                string.Empty,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase |
                System.Text.RegularExpressions.RegexOptions.Singleline);

            // Second pass: strip all remaining HTML tags (e.g., <b>, <p>, <a href="...">)
            note.Content = System.Text.RegularExpressions.Regex.Replace(note.Content, "<[^>]*>", string.Empty);

            // Enforce a 2000-character maximum to prevent storage abuse
            if (note.Content.Length > 2000) note.Content = note.Content[..2000];

            // --- Hour validation ---
            // The agenda grid starts at 6 and ends at 22; reject start hours outside this range
            if (note.Hour < 6 || note.Hour > 22) return BadRequest("Heure de début invalide.");

            // EndHour must be strictly after Hour and within the grid boundary (max 23)
            // If invalid, clamp EndHour to one hour after the start
            if (note.EndHour <= note.Hour || note.EndHour > 23) note.EndHour = note.Hour + 1;

            // Force the owner to the currently authenticated user
            note.IdUserFk = userId.Value;

            if (note.Id == 0)
            {
                // New note — record both creation and modification timestamps
                note.CreatedAt  = DateTime.UtcNow;
                note.ModifiedAt = DateTime.UtcNow;
                _context.UserNotes.Add(note);
            }
            else
            {
                // Existing note — verify it belongs to the current user before updating
                var existing = await _context.UserNotes
                    .FirstOrDefaultAsync(n => n.Id == note.Id && n.IdUserFk == userId);
                if (existing == null) return NotFound();

                // Update only the content and timing fields; creation timestamp is immutable
                existing.Content    = note.Content;
                existing.Hour       = note.Hour;
                existing.EndHour    = note.EndHour;
                existing.ModifiedAt = DateTime.UtcNow;
            }

            // Persist the insert or update
            await _context.SaveChangesAsync();
            return Ok(note);
        }

        // DELETE /api/notes/{id}
        // Permanently removes a note owned by the current user
        [HttpDelete("{id}")]
        /// <summary>
        /// Deletes a note by its ID.
        /// Verifies that the note belongs to the requesting user before deletion.
        /// </summary>
        /// <param name="id">The primary key of the note to delete</param>
        public async Task<IActionResult> Delete(int id)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            // Find the note that matches both the given ID and the current user's ID
            // (prevents users from deleting other users' notes by guessing IDs)
            var note = await _context.UserNotes
                .FirstOrDefaultAsync(n => n.Id == id && n.IdUserFk == userId);
            if (note == null) return NotFound();

            // Remove the note entity and persist the deletion
            _context.UserNotes.Remove(note);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
