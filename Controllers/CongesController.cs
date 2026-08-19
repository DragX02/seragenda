// Importation des attributs d'autorisation ASP.NET Core
using Microsoft.AspNetCore.Authorization;
// Importation des types de contrôleur MVC/API de base et des helpers de résultat
using Microsoft.AspNetCore.Mvc;
// Importation d'Entity Framework Core pour les opérations asynchrones en base de données
using Microsoft.EntityFrameworkCore;
// Importation des modèles du projet
using seragenda.Models;
// Importation du support des Claims pour lire l'identité de l'utilisateur courant depuis le JWT
using System.Security.Claims;

namespace seragenda.Controllers
{
    // Toutes les routes de ce contrôleur sont préfixées par /api/conges
    [Route("api/[controller]")]
    // Marque cette classe comme contrôleur API
    [ApiController]
    // Nécessite un jeton JWT valide sur tous les points de terminaison
    [Authorize]
    // Gère les corrections personnelles du calendrier des congés scolaires.
    //
    // La table calendrier_scolaire est commune à tous et alimentée par le scraper ;
    // elle contient parfois des dates erronées. Chaque enseignant enregistre ici ses
    // propres corrections (dates rectifiées, congé masqué ou congé ajouté), qui ne
    // s'appliquent qu'à son calendrier. Le calendrier officiel n'est jamais modifié.
    public class CongesController : ControllerBase
    {
        // Contexte de base de données Entity Framework pour lire et écrire les enregistrements UserConge
        private readonly AgendaContext _context;

        // Constructeur — reçoit le contexte de base de données par injection de dépendances.
        public CongesController(AgendaContext context)
        {
            _context = context;
        }

        // Résout la clé primaire entière de l'utilisateur actuellement authentifié
        // en recherchant son adresse email (stockée comme claim Name du JWT) en base de données.
        // Retourne null si le claim est absent ou si l'enregistrement utilisateur est introuvable.
        private async Task<int?> GetUserId()
        {
            var email = User.FindFirst(ClaimTypes.Name)?.Value;
            if (email == null) return null;
            var user = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == email);
            return user?.IdUser;
        }

        // GET /api/conges
        // Retourne toutes les corrections de l'utilisateur courant, ordonnées par date de début.
        [HttpGet]
        public async Task<IActionResult> GetConges()
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            var conges = await _context.UserConges
                                       .Where(c => c.IdUserFk == userId.Value)
                                       .OrderBy(c => c.DateDebut)
                                       .ToListAsync();
            return Ok(conges);
        }

        // POST /api/conges
        // Crée une correction ou met à jour une correction existante (Id > 0).
        // Une seule correction par congé officiel : réenregistrer le même IdCalendrierFk
        // remplace la correction précédente plutôt que d'en empiler une seconde.
        [HttpPost]
        public async Task<IActionResult> SaveConge([FromBody] UserConge conge)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            // --- Assainissement du nom ---
            conge.Nom = (conge.Nom ?? string.Empty).Trim();

            // Suppression des balises HTML pour éviter toute injection à l'affichage
            conge.Nom = System.Text.RegularExpressions.Regex.Replace(conge.Nom, "<[^>]*>", string.Empty);

            if (conge.Nom.Length > 100) conge.Nom = conge.Nom[..100];
            if (string.IsNullOrWhiteSpace(conge.Nom)) return BadRequest("Le nom du conge est obligatoire.");

            // --- Validation des dates ---
            // On ne conserve que la partie date : la colonne est de type "date" côté PostgreSQL
            conge.DateDebut = conge.DateDebut.Date;
            conge.DateFin   = conge.DateFin.Date;

            if (conge.DateFin < conge.DateDebut)
                return BadRequest("La date de fin doit etre posterieure ou egale a la date de debut.");

            // Garde-fou : une période de plus d'un an relève forcément d'une saisie erronée
            if ((conge.DateFin - conge.DateDebut).TotalDays > 366)
                return BadRequest("La periode ne peut pas depasser un an.");

            // Force le propriétaire à être l'utilisateur authentifié
            conge.IdUserFk = userId.Value;

            // Un IdCalendrierFk à 0 (valeur par défaut du client) signifie "congé ajouté"
            if (conge.IdCalendrierFk == 0) conge.IdCalendrierFk = null;

            // --- Enregistrement ---
            UserConge? existant = null;

            if (conge.Id > 0)
            {
                // Modification : la ligne doit appartenir à l'utilisateur courant
                existant = await _context.UserConges
                                         .FirstOrDefaultAsync(c => c.Id == conge.Id && c.IdUserFk == userId.Value);
                if (existant == null) return NotFound();
            }
            else if (conge.IdCalendrierFk != null)
            {
                // Nouvelle correction d'un congé officiel déjà corrigé : on écrase l'existante
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

            await _context.SaveChangesAsync();
            return Ok(existant ?? conge);
        }

        // DELETE /api/conges/{id}
        // Supprime une correction : le congé officiel correspondant réapparaît tel quel,
        // et un congé ajouté par l'utilisateur disparaît de son calendrier.
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteConge(int id)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            var conge = await _context.UserConges
                                      .FirstOrDefaultAsync(c => c.Id == id && c.IdUserFk == userId.Value);
            if (conge == null) return NotFound();

            _context.UserConges.Remove(conge);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
