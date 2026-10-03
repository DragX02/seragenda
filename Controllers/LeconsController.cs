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
    public class LeconsController : ControllerBase
    {
        private readonly AgendaContext _context;

        public LeconsController(AgendaContext context)
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

        private const int MaxPhases = 30;

        [HttpGet]
        public async Task<IActionResult> GetLecons()
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var lecons = await _context.Lecons
                    .AsNoTracking()
                    .Include(l => l.Phases)
                    .Where(l => l.IdUserFk == userId.Value)
                    .OrderByDescending(l => l.ModifiedAt)
                    .ToListAsync();

                return Ok(lecons.Select(Projeter));
            }
            catch (Exception ex)
            {
                return StatusCode(500, MessageErreur(ex));
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetLecon(int id)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var lecon = await _context.Lecons
                    .AsNoTracking()
                    .Include(l => l.Phases)
                    .FirstOrDefaultAsync(l => l.Id == id && l.IdUserFk == userId.Value);

                if (lecon == null) return NotFound();

                return Ok(Projeter(lecon));
            }
            catch (Exception ex)
            {
                return StatusCode(500, MessageErreur(ex));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] Lecon lecon)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            var refus = Assainir(lecon, userId.Value);
            if (refus != null) return BadRequest(new { message = refus });

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                Lecon cible;

                if (lecon.Id == 0)
                {
                    cible = new Lecon
                    {
                        IdUserFk  = userId.Value,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Lecons.Add(cible);
                }
                else
                {
                    var existante = await _context.Lecons
                        .Include(l => l.Phases)
                        .FirstOrDefaultAsync(l => l.Id == lecon.Id && l.IdUserFk == userId.Value);

                    if (existante == null) return NotFound();

                    _context.LeconPhases.RemoveRange(existante.Phases);
                    cible = existante;
                }

                cible.Titre         = lecon.Titre;
                cible.Enseignant    = lecon.Enseignant;
                cible.Duree         = lecon.Duree;
                cible.NombreSeances = lecon.NombreSeances;
                cible.Niveaux       = lecon.Niveaux;
                cible.Competences   = lecon.Competences;
                cible.IdViseeFk     = lecon.IdViseeFk;
                cible.ModifiedAt    = DateTime.UtcNow;

                int ordre = 1;
                foreach (var phase in lecon.Phases)
                {
                    cible.Phases.Add(new LeconPhase
                    {
                        Ordre    = ordre++,
                        Intitule = phase.Intitule,
                        Temps    = phase.Temps
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    cible.Id,
                    cible.Titre,
                    cible.Enseignant,
                    cible.Duree,
                    cible.NombreSeances,
                    cible.Niveaux,
                    cible.Competences,
                    cible.IdViseeFk,
                    cible.CreatedAt,
                    cible.ModifiedAt,
                    Phases = cible.Phases
                        .OrderBy(p => p.Ordre)
                        .Select(p => new { p.Id, p.Ordre, p.Intitule, p.Temps })
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = MessageErreur(ex) });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var lecon = await _context.Lecons
                    .FirstOrDefaultAsync(l => l.Id == id && l.IdUserFk == userId.Value);

                if (lecon == null) return NotFound();

                _context.Lecons.Remove(lecon);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, MessageErreur(ex));
            }
        }

        private static object Projeter(Lecon l) => new
        {
            l.Id,
            l.Titre,
            l.Enseignant,
            l.Duree,
            l.NombreSeances,
            l.Niveaux,
            l.Competences,
            l.IdViseeFk,
            l.CreatedAt,
            l.ModifiedAt,
            Phases = l.Phases
                .OrderBy(p => p.Ordre)
                .Select(p => new { p.Id, p.Ordre, p.Intitule, p.Temps })
        };

        private static string? Assainir(Lecon lecon, int userId)
        {
            lecon.IdUserFk = userId;

            lecon.Titre = Texte(lecon.Titre, 200);
            if (lecon.Titre.Length == 0) return "Le titre de la leçon est obligatoire.";

            lecon.Enseignant  = Texte(lecon.Enseignant, 150);
            lecon.Duree       = Texte(lecon.Duree, 100);
            lecon.Niveaux     = Texte(lecon.Niveaux, 200);
            lecon.Competences = Texte(lecon.Competences, 4000, multiligne: true);

            if (lecon.IdViseeFk == 0) lecon.IdViseeFk = null;

            if (lecon.NombreSeances < 1)   lecon.NombreSeances = 1;
            if (lecon.NombreSeances > 100) lecon.NombreSeances = 100;

            lecon.Phases ??= new List<LeconPhase>();

            if (lecon.Phases.Count > MaxPhases)
                return $"Une leçon ne peut pas dépasser {MaxPhases} phases.";

            foreach (var phase in lecon.Phases)
            {
                phase.Intitule = Texte(phase.Intitule, 1000, multiligne: true);
                phase.Temps    = Texte(phase.Temps, 50);
            }

            return null;
        }

        private static string Texte(string? valeur, int maximum, bool multiligne = false)
        {
            var t = (valeur ?? string.Empty).Trim();

            t = System.Text.RegularExpressions.Regex.Replace(
                t,
                @"<(script|style|iframe|object|embed)[^>]*>.*?<\/\1>",
                string.Empty,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase |
                System.Text.RegularExpressions.RegexOptions.Singleline);

            t = System.Text.RegularExpressions.Regex.Replace(t, "<[^>]*>", string.Empty);

            if (!multiligne) t = t.Replace("\r", " ").Replace("\n", " ").Trim();
            else             t = t.Replace("\r\n", "\n").Replace("\r", "\n");

            return t.Length > maximum ? t[..maximum] : t;
        }

        private static string MessageErreur(Exception ex)
        {
            var detail = ex.InnerException?.Message ?? ex.Message;
            return $"Erreur base de donnees : {detail}";
        }
    }
}
