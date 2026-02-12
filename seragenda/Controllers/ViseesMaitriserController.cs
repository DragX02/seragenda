using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using seragenda.Models;

namespace seragenda.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ViseesMaitriserController : ControllerBase
{
    private readonly AgendaContext _context;

    public ViseesMaitriserController(AgendaContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get(int page = 1, int limit = 50)
    {
        var total = await _context.ViseesMaitrisers.CountAsync();

       
        var donnees = await _context.ViseesMaitrisers
            .OrderBy(v => v.IdViseesMaitriser) 
            .Skip((page - 1) * limit)          
            .Take(limit)                       
            .Select(v => new
            {
                v.IdViseesMaitriser,
                v.NomViseesMaitriser
            })
            .AsNoTracking() 
            .ToListAsync();

        return Ok(new
        {
            TotalItems = total,
            CurrentPage = page,
            PageSize = limit,
            Data = donnees
        });
    }
}