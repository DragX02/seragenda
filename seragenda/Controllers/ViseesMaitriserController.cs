// Import ASP.NET Core authorization attributes
using Microsoft.AspNetCore.Authorization;
// Import base MVC/API controller types and result helpers
using Microsoft.AspNetCore.Mvc;
// Import Entity Framework Core for async database queries and count operations
using Microsoft.EntityFrameworkCore;
// Import project models (ViseesMaitriser)
using seragenda.Models;

// File-scoped namespace declaration (C# 10+ style)
namespace seragenda.Controllers;

// All routes in this controller are prefixed with /api/ViseesMaitriser
[Route("api/[controller]")]
// Marks this class as an API controller
[ApiController]
// All endpoints require a valid JWT token
[Authorize]
/// <summary>
/// Provides paginated read access to the ViseesMaitriser (mastery targets) reference table.
/// ViseesMaitriser records represent high-level educational outcomes or mastery goals
/// defined in the curriculum and referenced throughout the lesson planning workflow.
/// </summary>
public class ViseesMaitriserController : ControllerBase
{
    // Entity Framework database context for querying ViseesMaitriser records
    private readonly AgendaContext _context;

    /// <summary>
    /// Constructor — receives the database context via dependency injection.
    /// </summary>
    /// <param name="context">The EF Core database context</param>
    public ViseesMaitriserController(AgendaContext context)
    {
        _context = context;
    }

    // GET /api/ViseesMaitriser?page=1&limit=50
    // Returns a paginated list of mastery target records
    [HttpGet]
    /// <summary>
    /// Retrieves a paginated page of ViseesMaitriser records.
    /// Returns the total item count alongside the current page data so the client
    /// can render pagination controls without a separate COUNT call.
    /// </summary>
    /// <param name="page">1-based page number (defaults to 1)</param>
    /// <param name="limit">Maximum number of records per page (defaults to 50)</param>
    public async Task<IActionResult> Get(int page = 1, int limit = 50)
    {
        // Count total records for pagination metadata (runs as a single COUNT query)
        var total = await _context.ViseesMaitrisers.CountAsync();

        // Fetch the requested page of data
        var donnees = await _context.ViseesMaitrisers
            // Sort by primary key to ensure a stable, deterministic order across pages
            .OrderBy(v => v.IdViseesMaitriser)
            // Skip rows from previous pages
            .Skip((page - 1) * limit)
            // Take only the current page's worth of rows
            .Take(limit)
            // Project to an anonymous type with only the fields the client needs
            .Select(v => new
            {
                v.IdViseesMaitriser,
                v.NomViseesMaitriser
            })
            // AsNoTracking() tells EF not to track these entities in the change tracker,
            // improving performance for read-only queries
            .AsNoTracking()
            .ToListAsync();

        // Return pagination metadata together with the page data
        return Ok(new
        {
            TotalItems  = total,   // Total number of records across all pages
            CurrentPage = page,    // The page that was requested
            PageSize    = limit,   // The maximum number of items per page
            Data        = donnees  // The actual records for this page
        });
    }
}
