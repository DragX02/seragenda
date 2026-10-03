using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using seragenda.Models;
using seragenda.Services;
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

        public const int MaxJoursPlage = 200;

        [HttpGet("range")]
        public async Task<IActionResult> GetNotesForRange([FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            if ((end - start).TotalDays > MaxJoursPlage) return BadRequest("Plage trop grande.");

            var notes = await _context.UserNotes
                .Where(n => n.IdUserFk == userId && n.Date >= start.Date && n.Date <= end.Date)
                .OrderBy(n => n.Date)
                .ThenBy(n => n.Hour)
                .ToListAsync();

            return Ok(notes);
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] UserNote note)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            var refus = Assainir(note, userId.Value);
            if (refus != null) return BadRequest(refus);

            if (note.Id == 0)
            {
                note.CreatedAt  = DateTime.UtcNow;
                note.ModifiedAt = DateTime.UtcNow;
                _context.UserNotes.Add(note);
            }
            else
            {
                var existing = await _context.UserNotes
                    .FirstOrDefaultAsync(n => n.Id == note.Id && n.IdUserFk == userId);
                if (existing == null) return NotFound();

                if (ReportNote.Cible(note.Content) == null &&
                    ReportNote.Cible(existing.Content) is DateTime dejaReportee)
                {
                    note.Content = ReportNote.Marquer(note.Content, dejaReportee);
                }

                existing.Content       = note.Content;
                existing.Titre         = note.Titre;
                existing.Hour        = note.Hour;
                existing.EndHour       = note.EndHour;
                existing.Minute        = note.Minute;
                existing.EndMinute     = note.EndMinute;
                existing.IdViseeFk     = note.IdViseeFk;
                existing.ViseeContexte = note.ViseeContexte;
                existing.ModifiedAt    = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(note);
        }

        private static string? Assainir(UserNote note, int userId)
        {
            note.Date = new DateTime(note.Date.Year, note.Date.Month, note.Date.Day, 0, 0, 0, DateTimeKind.Unspecified);

            note.Content = note.Content?.Trim() ?? string.Empty;

            note.Content = System.Text.RegularExpressions.Regex.Replace(
                note.Content,
                @"<(script|style|iframe|object|embed)[^>]*>.*?<\/\1>",
                string.Empty,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase |
                System.Text.RegularExpressions.RegexOptions.Singleline);

            note.Content = System.Text.RegularExpressions.Regex.Replace(note.Content, "<[^>]*>", string.Empty);

            if (note.Content.Length > ReportNote.MaxContenu)
                note.Content = note.Content[..ReportNote.MaxContenu];

            if (note.Hour < 8 || note.Hour > 17) return "Heure de début invalide.";

            note.Minute    = NormalizeMinute(note.Minute);
            note.EndMinute = NormalizeMinute(note.EndMinute);

            int startTotal = note.Hour * 60 + note.Minute;
            int endTotal   = note.EndHour * 60 + note.EndMinute;
            if (note.EndHour < 9 || note.EndHour > 18 || endTotal <= startTotal)
            {
                note.EndHour   = note.Hour + 1;
                note.EndMinute = note.Minute;
            }

            if (note.EndHour == 18) note.EndMinute = 0;

            note.IdUserFk = userId;

            if (note.IdViseeFk == 0) note.IdViseeFk = null;

            if (!string.IsNullOrWhiteSpace(note.ViseeContexte))
            {
                var ctx = System.Text.RegularExpressions.Regex.Replace(note.ViseeContexte, "<[^>]*>", string.Empty).Trim();
                note.ViseeContexte = ctx.Length > 2000 ? ctx[..2000] : (ctx.Length == 0 ? null : ctx);
            }
            else
            {
                note.ViseeContexte = null;
            }

            if (!string.IsNullOrWhiteSpace(note.Titre))
            {
                var titre = System.Text.RegularExpressions.Regex.Replace(note.Titre, "<[^>]*>", string.Empty).Trim();
                note.Titre = titre.Length > 150 ? titre[..150] : (titre.Length == 0 ? null : titre);
            }
            else
            {
                note.Titre = null;
            }

            return null;
        }

        private static int NormalizeMinute(int minute)
        {
            if (minute < 0) minute = 0;
            if (minute > 59) minute = 59;
            return (minute / 5) * 5;
        }

        private const int MaxCopies = 500;

        [HttpPost("copier")]
        public async Task<IActionResult> Copier([FromBody] CopierNotesDto dto)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            var ids       = (dto.IdsNotes ?? new()).Distinct().ToList();
            var modeles   = dto.Modeles ?? new();
            var decalages = (dto.Decalages ?? new()).Distinct().ToList();

            if (ids.Count == 0 && modeles.Count == 0)
                return BadRequest(new { message = "Aucune leçon à copier." });
            if (decalages.Count == 0)
                return BadRequest(new { message = "Aucune date de destination." });

            if (dto.Marquer && decalages.Count != 1)
                return BadRequest(new { message = "Un report ne vise qu'une seule date." });

            if (dto.Marquer && modeles.Count > 0)
                return BadRequest(new { message = "Un report part d'une leçon enregistrée." });

            if ((ids.Count + modeles.Count) * decalages.Count > MaxCopies)
                return BadRequest(new { message = $"Trop de copies demandées (maximum {MaxCopies})." });

            var sources = ids.Count == 0
                ? new List<UserNote>()
                : await _context.UserNotes
                    .Where(n => n.IdUserFk == userId.Value && ids.Contains(n.Id))
                    .ToListAsync();

            if (ids.Count > 0 && sources.Count == 0)
                return NotFound(new { message = "Aucune leçon trouvée." });

            foreach (var modele in modeles)
            {
                var refus = Assainir(modele, userId.Value);
                if (refus != null) return BadRequest(new { message = refus });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                int copiees = 0;

                void Copie(UserNote source, string contenu, int decalage)
                {
                    var cible = source.Date.Date.AddDays(decalage);

                    _context.UserNotes.Add(new UserNote
                    {
                        IdUserFk      = userId.Value,
                        Date          = new DateTime(cible.Year, cible.Month, cible.Day,
                                                     0, 0, 0, DateTimeKind.Unspecified),
                        Hour          = source.Hour,
                        Minute        = source.Minute,
                        EndHour       = source.EndHour,
                        EndMinute     = source.EndMinute,
                        Content       = contenu,
                        Titre         = source.Titre,
                        IdViseeFk     = source.IdViseeFk,
                        ViseeContexte = source.ViseeContexte,
                        CreatedAt     = DateTime.UtcNow,
                        ModifiedAt    = DateTime.UtcNow
                    });

                    copiees++;
                }

                foreach (var source in sources)
                {
                    var contenu = ReportNote.Texte(source.Content);

                    foreach (var decalage in decalages) Copie(source, contenu, decalage);

                    if (dto.Marquer)
                    {
                        source.Content    = ReportNote.Marquer(source.Content,
                                                               source.Date.Date.AddDays(decalages[0]));
                        source.ModifiedAt = DateTime.UtcNow;
                    }
                }

                foreach (var modele in modeles)
                {
                    var contenu = ReportNote.Texte(modele.Content);
                    foreach (var decalage in decalages) Copie(modele, contenu, decalage);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { Copiees = copiees, Sources = sources.Count + modeles.Count });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var detail = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = $"La copie a échoué, rien n'a été enregistré : {detail}" });
            }
        }

        public class CopierNotesDto
        {
            public List<int> IdsNotes { get; set; } = new();

            public List<UserNote> Modeles { get; set; } = new();

            public List<int> Decalages { get; set; } = new();

            public bool Marquer { get; set; }
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
