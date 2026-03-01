using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace seragenda.Controllers
{
    /// <summary>
    /// Données de référence : cours, niveaux, domaines.
    /// Utilisé pour alimenter les sélections en cascade côté client.
    /// </summary>
    [Route("api/ref")]
    [ApiController]
    [Authorize]
    public class RefController : ControllerBase
    {
        private readonly AgendaContext _context;

        public RefController(AgendaContext context)
        {
            _context = context;
        }

        // GET /api/ref/cours
        // Retourne tous les cours disponibles
        [HttpGet("cours")]
        public async Task<IActionResult> GetCours()
        {
            var cours = await _context.Cours
                .OrderBy(c => c.NomCours)
                .Select(c => new { c.CodeCours, c.NomCours, c.CouleurAgenda })
                .ToListAsync();

            return Ok(cours);
        }

        // GET /api/ref/niveaux/{codeCours}
        // Retourne les niveaux disponibles pour un cours donné
        [HttpGet("niveaux/{codeCours}")]
        public async Task<IActionResult> GetNiveaux(string codeCours)
        {
            var niveaux = await _context.CoursNiveaus
                .Where(cn => cn.IdCoursFkNavigation.CodeCours == codeCours)
                .Select(cn => cn.IdNiveauFkNavigation)
                .Distinct()
                .OrderBy(n => n.CodeNiveau)
                .Select(n => new { n.CodeNiveau, n.NomNiveau })
                .ToListAsync();

            return Ok(niveaux);
        }

        // GET /api/ref/domaines/{codeCours}/{codeNiveau}
        // Retourne les domaines pour un cours + niveau donné
        [HttpGet("domaines/{codeCours}/{codeNiveau}")]
        public async Task<IActionResult> GetDomaines(string codeCours, string codeNiveau)
        {
            var domaines = await _context.CoursNiveaus
                .Where(cn =>
                    cn.IdCoursFkNavigation.CodeCours == codeCours &&
                    cn.IdNiveauFkNavigation.CodeNiveau == codeNiveau)
                .SelectMany(cn => cn.Domaines)
                .OrderBy(d => d.Nom)
                .Select(d => new { d.IdDom, d.Nom })
                .ToListAsync();

            return Ok(domaines);
        }
    }
}
