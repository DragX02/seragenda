using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using seragenda.Models;

namespace seragenda.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ValuesController : ControllerBase
{
    private readonly AgendaContext _context;

    public ValuesController(AgendaContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var donnees = await _context.CalendrierScolaires
                                    .OrderBy(d => d.DateDebut)
                                    .ToListAsync();
        return Ok(donnees);
    }
}