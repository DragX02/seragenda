using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using seragenda.Models;
using System.Security.Claims;

namespace seragenda.Controllers
{
    [Route("api/pagesgarde")]
    [ApiController]
    [Authorize]
    public class PagesGardeController : ControllerBase
    {
        private readonly AgendaContext _context;

        public PagesGardeController(AgendaContext context)
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

        private bool EstAdmin()
        {
            return User.IsInRole("ADMIN");
        }

        private object Projeter(PageGarde page, int userId)
        {
            bool modifiable = page.IdUserFk == userId || (page.PourTous && EstAdmin());
            return new
            {
                page.Id,
                page.Nom,
                page.Contenu,
                page.PourTous,
                EstAMoi = page.IdUserFk == userId,
                Modifiable = modifiable,
                page.ModifiedAt
            };
        }

        [HttpGet]
        public async Task<IActionResult> GetPages()
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var pages = await _context.PagesGarde
                    .AsNoTracking()
                    .Where(p => p.PourTous || p.IdUserFk == userId.Value)
                    .OrderBy(p => p.Nom)
                    .ToListAsync();

                var resultat = new List<object>();
                foreach (var p in pages)
                    resultat.Add(Projeter(p, userId.Value));

                return Ok(resultat);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erreur page_garde : " + ex.Message);
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPage(int id)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var page = await _context.PagesGarde
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == id && (p.PourTous || p.IdUserFk == userId.Value));

                if (page == null) return NotFound();

                return Ok(Projeter(page, userId.Value));
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erreur page_garde : " + ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] PageGarde page)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            string nom = (page.Nom ?? "").Trim();
            if (nom == "")
                return BadRequest(new { message = "Le nom est obligatoire." });
            if (nom.Length > 150)
                return BadRequest(new { message = "Le nom ne peut pas dépasser 150 caractères." });
            if (page.Contenu == null || page.Contenu.Length > 500000)
                return BadRequest(new { message = "Le contenu de la page est invalide ou trop grand." });

            bool pourTous = page.PourTous && EstAdmin();

            try
            {
                PageGarde cible;

                if (page.Id == 0)
                {
                    cible = new PageGarde();
                    cible.IdUserFk = userId.Value;
                    cible.CreatedAt = DateTime.UtcNow;
                    _context.PagesGarde.Add(cible);
                }
                else
                {
                    var existante = await _context.PagesGarde.FirstOrDefaultAsync(p => p.Id == page.Id);
                    if (existante == null) return NotFound();

                    bool modifiable = existante.IdUserFk == userId.Value || (existante.PourTous && EstAdmin());
                    if (!modifiable) return NotFound();

                    cible = existante;
                }

                cible.Nom = nom;
                cible.Contenu = page.Contenu;
                cible.PourTous = pourTous;
                cible.ModifiedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(Projeter(cible, userId.Value));
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erreur page_garde : " + ex.Message);
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var page = await _context.PagesGarde.FirstOrDefaultAsync(p => p.Id == id);
                if (page == null) return NotFound();

                bool modifiable = page.IdUserFk == userId.Value || (page.PourTous && EstAdmin());
                if (!modifiable) return NotFound();

                _context.PagesGarde.Remove(page);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erreur page_garde : " + ex.Message);
            }
        }
    }
}
