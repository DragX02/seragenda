using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace seragenda.Controllers
{
    [Route("api/compte")]
    [ApiController]
    [Authorize]
    public class CompteController : ControllerBase
    {
        private readonly AgendaContext _context;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public CompteController(AgendaContext context, IConfiguration config, IWebHostEnvironment env)
        {
            _context = context;
            _config = config;
            _env = env;
        }

        public class SuppressionCompte
        {
            public string Confirmation { get; set; } = "";
        }

        [HttpPost("supprimer")]
        public async Task<IActionResult> Supprimer([FromBody] SuppressionCompte demande)
        {
            var email = User.FindFirst(ClaimTypes.Name)?.Value;
            if (email == null) return Unauthorized();

            var user = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return Unauthorized();

            if ((demande.Confirmation ?? "").Trim().ToLower() != user.Email.ToLower())
                return BadRequest(new { message = "L'adresse email tapée ne correspond pas à ton compte." });

            if (user.RoleSysteme == "ADMIN")
                return BadRequest(new { message = "Un compte administrateur ne peut pas être supprimé depuis cette page." });

            bool lieAuReferentiel = await _context.CoursNiveaus.AnyAsync(c => c.IdProfFk == user.IdUser);
            if (lieAuReferentiel)
                return BadRequest(new { message = "Ton compte est lié au référentiel des cours : contacte un administrateur pour le supprimer." });

            int userId = user.IdUser;

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var idLecons = await _context.Lecons.Where(l => l.IdUserFk == userId).Select(l => l.Id).ToListAsync();
                await _context.LeconPhases.Where(p => idLecons.Contains(p.IdLeconFk)).ExecuteDeleteAsync();
                await _context.Lecons.Where(l => l.IdUserFk == userId).ExecuteDeleteAsync();
                await _context.UserNotes.Where(n => n.IdUserFk == userId).ExecuteDeleteAsync();
                await _context.UserCourses.Where(c => c.IdUserFk == userId).ExecuteDeleteAsync();
                await _context.UserConges.Where(c => c.IdUserFk == userId).ExecuteDeleteAsync();
                await _context.PagesGarde.Where(p => p.IdUserFk == userId).ExecuteDeleteAsync();
                await _context.Abonnements.Where(a => a.IdUserFk == userId).ExecuteDeleteAsync();
                await _context.Licenses.Where(l => l.AssignedUserId == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(l => l.AssignedUserId, (int?)null));
                await _context.Utilisateurs.Where(u => u.IdUser == userId).ExecuteDeleteAsync();

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "La suppression du compte a échoué : " + ex.Message);
            }

            try
            {
                string dossier = _config["ImagesGarde:DossierPerso"] ?? "ImagesPerso";
                if (!Path.IsPathRooted(dossier))
                    dossier = Path.Combine(_env.ContentRootPath, dossier);
                string dossierUser = Path.Combine(dossier, userId.ToString());
                if (Directory.Exists(dossierUser))
                    Directory.Delete(dossierUser, true);
            }
            catch
            {
            }

            return NoContent();
        }
    }
}
