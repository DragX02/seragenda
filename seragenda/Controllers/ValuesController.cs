// Import ASP.NET Core authorization attributes
using Microsoft.AspNetCore.Authorization;
// Import base MVC/API controller types and result helpers
using Microsoft.AspNetCore.Mvc;
// Import Entity Framework Core for async database queries
using Microsoft.EntityFrameworkCore;
// Import project models (CalendrierScolaire)
using seragenda.Models;

// File-scoped namespace declaration (C# 10+ style)
namespace seragenda.Controllers;

// All routes in this controller are prefixed with /api/values
[Route("api/[controller]")]
// Marks this class as an API controller
[ApiController]
// All endpoints require a valid JWT token
[Authorize]
/// <summary>
/// Exposes the school calendar (academic year events and holidays) to authenticated users.
/// The data is sourced from the CalendrierScolaire table, which is populated
/// by the ScolaireScraper background service.
/// </summary>
public class ValuesController : ControllerBase
{
    // Entity Framework database context for querying the school calendar table
    private readonly AgendaContext _context;

    /// <summary>
    /// Constructor — receives the database context via dependency injection.
    /// </summary>
    /// <param name="context">The EF Core database context</param>
    public ValuesController(AgendaContext context)
    {
        _context = context;
    }

    // GET /api/values
    // Returns all school calendar entries ordered by start date (ascending)
    [HttpGet]
    /// <summary>
    /// Retrieves all school calendar entries (holidays, back-to-school dates, public holidays, etc.),
    /// ordered chronologically by their start date.
    /// </summary>
    public async Task<IActionResult> Get()
    {
        // Fetch all calendar entries sorted so the client receives them in date order
        var donnees = await _context.CalendrierScolaires
                                    .OrderBy(d => d.DateDebut)
                                    .ToListAsync();
        return Ok(donnees);
    }
}
