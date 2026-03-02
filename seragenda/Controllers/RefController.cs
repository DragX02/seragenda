// Import ASP.NET Core authorization attributes
using Microsoft.AspNetCore.Authorization;
// Import base MVC/API controller types and result helpers
using Microsoft.AspNetCore.Mvc;
// Import Entity Framework Core for async database queries
using Microsoft.EntityFrameworkCore;

namespace seragenda.Controllers
{
    // Routes in this controller are prefixed with /api/ref (explicit path, not using [controller] token)
    [Route("api/ref")]
    // Marks this class as an API controller
    [ApiController]
    // All endpoints require a valid JWT token
    [Authorize]
    /// <summary>
    /// Provides read-only reference data used to populate cascading selection lists on the client.
    /// Exposes three related lookups: courses (subjects), levels (year groups), and domains.
    /// The intended usage is:
    ///   1. Load all courses → user picks one.
    ///   2. Load levels for the selected course → user picks one.
    ///   3. Load domains for the selected course + level → user picks one.
    /// </summary>
    public class RefController : ControllerBase
    {
        // Entity Framework database context for querying reference tables
        private readonly AgendaContext _context;

        /// <summary>
        /// Constructor — receives the database context via dependency injection.
        /// </summary>
        /// <param name="context">The EF Core database context</param>
        public RefController(AgendaContext context)
        {
            _context = context;
        }

        // GET /api/ref/cours
        // Returns all available courses (subjects), sorted alphabetically by name
        [HttpGet("cours")]
        /// <summary>
        /// Retrieves all course (subject) records from the database.
        /// Returns only the fields needed for display and subsequent lookups:
        /// the unique code, the display name, and the agenda colour.
        /// </summary>
        public async Task<IActionResult> GetCours()
        {
            var cours = await _context.Cours
                // Sort alphabetically so the client dropdown is ordered
                .OrderBy(c => c.NomCours)
                // Project only the columns the client needs; avoids over-fetching
                .Select(c => new { c.CodeCours, c.NomCours, c.CouleurAgenda })
                .ToListAsync();

            return Ok(cours);
        }

        // GET /api/ref/niveaux/{codeCours}
        // Returns the distinct teaching levels available for a given course code
        [HttpGet("niveaux/{codeCours}")]
        /// <summary>
        /// Retrieves all educational levels (year groups) associated with a specific course.
        /// The level list is derived from the many-to-many linking table CoursNiveau.
        /// Duplicates are removed with Distinct() in case multiple professors teach the same level.
        /// </summary>
        /// <param name="codeCours">The unique code of the course (e.g., "MATH", "FR")</param>
        public async Task<IActionResult> GetNiveaux(string codeCours)
        {
            var niveaux = await _context.CoursNiveaus
                // Filter linking records where the associated course matches the requested code
                .Where(cn => cn.IdCoursFkNavigation.CodeCours == codeCours)
                // Navigate through the linking table to the Niveau entity
                .Select(cn => cn.IdNiveauFkNavigation)
                // Remove duplicates that arise from multiple professors teaching the same level
                .Distinct()
                // Sort by the level code for a consistent order
                .OrderBy(n => n.CodeNiveau)
                // Project only the fields needed for display and further lookups
                .Select(n => new { n.CodeNiveau, n.NomNiveau })
                .ToListAsync();

            return Ok(niveaux);
        }

        // GET /api/ref/domaines/{codeCours}/{codeNiveau}
        // Returns the domains for a given course + level combination
        [HttpGet("domaines/{codeCours}/{codeNiveau}")]
        /// <summary>
        /// Retrieves all pedagogical domains for a specific course and level combination.
        /// Domains are specific curriculum areas within a course at a given level
        /// (e.g., "Algebra" within "Mathematics" at "3rd year").
        /// </summary>
        /// <param name="codeCours">The unique code of the course</param>
        /// <param name="codeNiveau">The unique code of the educational level</param>
        public async Task<IActionResult> GetDomaines(string codeCours, string codeNiveau)
        {
            var domaines = await _context.CoursNiveaus
                // Find linking records that match both the course and the level
                .Where(cn =>
                    cn.IdCoursFkNavigation.CodeCours   == codeCours &&
                    cn.IdNiveauFkNavigation.CodeNiveau == codeNiveau)
                // Flatten the collection of Domaine children from each matching CoursNiveau record
                .SelectMany(cn => cn.Domaines)
                // Sort alphabetically for the dropdown
                .OrderBy(d => d.Nom)
                // Project only the ID and display name
                .Select(d => new { d.IdDom, d.Nom })
                .ToListAsync();

            return Ok(domaines);
        }
    }
}
