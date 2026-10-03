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
    public class CongesController : ControllerBase
    {
        private readonly AgendaContext _context;

        public CongesController(AgendaContext context)
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

        [HttpGet]
        public async Task<IActionResult> GetConges()
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var conges = await _context.UserConges
                                           .Where(c => c.IdUserFk == userId.Value)
                                           .OrderBy(c => c.DateDebut)
                                           .ToListAsync();
                return Ok(conges);
            }
            catch (Exception ex)
            {
                return StatusCode(500, MessageErreur(ex));
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveConge([FromBody] UserConge conge)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            conge.Nom = (conge.Nom ?? string.Empty).Trim();

            conge.Nom = System.Text.RegularExpressions.Regex.Replace(conge.Nom, "<[^>]*>", string.Empty);

            if (conge.Nom.Length > 100) conge.Nom = conge.Nom[..100];
            if (string.IsNullOrWhiteSpace(conge.Nom)) return BadRequest("Le nom du conge est obligatoire.");

            conge.DateDebut = conge.DateDebut.Date;
            conge.DateFin   = conge.DateFin.Date;

            if (conge.DateFin < conge.DateDebut)
                return BadRequest("La date de fin doit etre posterieure ou egale a la date de debut.");

            if ((conge.DateFin - conge.DateDebut).TotalDays > 366)
                return BadRequest("La periode ne peut pas depasser un an.");

            conge.IdUserFk = userId.Value;

            if (conge.IdCalendrierFk == 0) conge.IdCalendrierFk = null;

            UserConge? existant = null;

            if (conge.Id > 0)
            {
                existant = await _context.UserConges
                                         .FirstOrDefaultAsync(c => c.Id == conge.Id && c.IdUserFk == userId.Value);
                if (existant == null) return NotFound();
            }
            else if (conge.IdCalendrierFk != null)
            {
                existant = await _context.UserConges
                                         .FirstOrDefaultAsync(c => c.IdUserFk == userId.Value
                                                                && c.IdCalendrierFk == conge.IdCalendrierFk);
            }

            if (existant != null)
            {
                existant.Nom       = conge.Nom;
                existant.DateDebut = conge.DateDebut;
                existant.DateFin   = conge.DateFin;
                existant.Masque    = conge.Masque;
            }
            else
            {
                _context.UserConges.Add(conge);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, MessageErreur(ex));
            }

            return Ok(existant ?? conge);
        }

        private static string MessageErreur(Exception ex)
        {
            var detail = ex.InnerException?.Message ?? ex.Message;
            return $"Erreur base de donnees : {detail}";
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteConge(int id)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var conge = await _context.UserConges
                                          .FirstOrDefaultAsync(c => c.Id == id && c.IdUserFk == userId.Value);
                if (conge == null) return NotFound();

                _context.UserConges.Remove(conge);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, MessageErreur(ex));
            }
        }
    }
}
