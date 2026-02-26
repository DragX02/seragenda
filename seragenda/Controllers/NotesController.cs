using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using seragenda.Models;
using System.Security.Claims;

namespace seragenda.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotesController : ControllerBase
    {
        private readonly AgendaContext _context;

        public NotesController(AgendaContext context)
        {
            _context = context;
        }

        private async Task<int?> GetUserId()
        {
            var email = User.FindFirst(ClaimTypes.Name)?.Value;
            if (email == null) return null;
            var user = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == email);
            return user?.IdUser;
        }

        [HttpGet("date/{date}")]
        public async Task<IActionResult> GetNotesForDate(DateTime date)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            var dayStart = date.Date;
            var dayEnd   = dayStart.AddDays(1);
            var notes = await _context.UserNotes
                .Where(n => n.IdUserFk == userId && n.Date >= dayStart && n.Date < dayEnd)
                .OrderBy(n => n.Hour)
                .ToListAsync();

            return Ok(notes);
        }

        [HttpGet("range")]
        public async Task<IActionResult> GetNotesForRange([FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            if ((end - start).TotalDays > 62) return BadRequest("Plage trop grande.");

            var notes = await _context.UserNotes
                .Where(n => n.IdUserFk == userId && n.Date >= start.Date && n.Date <= end.Date)
                .OrderBy(n => n.Date).ThenBy(n => n.Hour)
                .ToListAsync();

            return Ok(notes);
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] UserNote note)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            // Normaliser la date : supprimer le décalage horaire éventuel (Year/Month/Day uniquement)
            note.Date = new DateTime(note.Date.Year, note.Date.Month, note.Date.Day, 0, 0, 0, DateTimeKind.Unspecified);

            // Sanitize content : trim, supprimer le contenu des balises script/style puis les balises restantes
            note.Content = note.Content?.Trim() ?? string.Empty;
            note.Content = System.Text.RegularExpressions.Regex.Replace(
                note.Content, @"<(script|style|iframe|object|embed)[^>]*>.*?<\/\1>",
                string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            note.Content = System.Text.RegularExpressions.Regex.Replace(note.Content, "<[^>]*>", string.Empty);
            if (note.Content.Length > 2000) note.Content = note.Content[..2000];

            // Validate hours
            if (note.Hour < 6 || note.Hour > 22) return BadRequest("Heure de début invalide.");
            if (note.EndHour <= note.Hour || note.EndHour > 23) note.EndHour = note.Hour + 1;

            note.IdUserFk = userId.Value;

            if (note.Id == 0)
            {
                note.CreatedAt = DateTime.UtcNow;
                note.ModifiedAt = DateTime.UtcNow;
                _context.UserNotes.Add(note);
            }
            else
            {
                var existing = await _context.UserNotes
                    .FirstOrDefaultAsync(n => n.Id == note.Id && n.IdUserFk == userId);
                if (existing == null) return NotFound();

                existing.Content = note.Content;
                existing.Hour = note.Hour;
                existing.EndHour = note.EndHour;
                existing.ModifiedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(note);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            var note = await _context.UserNotes
                .FirstOrDefaultAsync(n => n.Id == id && n.IdUserFk == userId);
            if (note == null) return NotFound();

            _context.UserNotes.Remove(note);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
