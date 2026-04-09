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
    // Toutes les routes de ce contrôleur sont préfixées par /api/notes
    [Route("api/[controller]")]
    // Marque cette classe comme contrôleur API
    [ApiController]
    // Nécessite un jeton JWT valide sur tous les points de terminaison — les requêtes non authentifiées sont rejetées
    [Authorize]
    // Gère les notes temporisées personnelles (entrées d'agenda) pour l'utilisateur authentifié.
    // Chaque note appartient à un seul jour calendaire et occupe un créneau horaire spécifique (6–22).
    // Les notes supportent un contenu texte brut sans HTML, limité à 2000 caractères.
    public class NotesController : ControllerBase
    {
        // Contexte de base de données Entity Framework pour lire et écrire les enregistrements UserNote
        private readonly AgendaContext _context;

        // Constructeur — reçoit le contexte de base de données par injection de dépendances.
        // context : le contexte de base de données EF Core
        public NotesController(AgendaContext context)
        {
            _context = context;
        }

        // Résout la clé primaire entière de l'utilisateur actuellement authentifié
        // en recherchant son adresse email (stockée comme claim Name du JWT) en base de données.
        // Retourne null si le claim est absent ou si l'enregistrement utilisateur est introuvable.
        // Retourne l'IdUser de l'utilisateur, ou null s'il est introuvable
        private async Task<int?> GetUserId()
        {
            // Le claim Name a été défini sur l'email de l'utilisateur au moment de la connexion
            var email = User.FindFirst(ClaimTypes.Name)?.Value;
            if (email == null) return null;
            var user = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == email);
            return user?.IdUser;
        }

        // GET /api/notes/date/{date}
        // Retourne toutes les notes de l'utilisateur courant à la date calendaire spécifiée
        [HttpGet("date/{date}")]
        // Récupère toutes les notes appartenant à l'utilisateur courant à une date spécifique,
        // ordonnées par heure (la plus ancienne en premier).
        // date : la date cible, analysée depuis le segment de route
        public async Task<IActionResult> GetNotesForDate(DateTime date)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            // Calcul du début et de la fin inclusive du jour calendaire cible
            var dayStart = date.Date;                  // Minuit au début du jour
            var dayEnd   = dayStart.AddDays(1);        // Minuit au début du jour suivant

            // Récupération des notes qui tombent dans cette fenêtre journalière, ordonnées par créneau horaire
            var notes = await _context.UserNotes
                .Where(n => n.IdUserFk == userId && n.Date >= dayStart && n.Date < dayEnd)
                .OrderBy(n => n.Hour)
                .ToListAsync();

            return Ok(notes);
        }

        // GET /api/notes/range?start=...&end=...
        // Retourne toutes les notes de l'utilisateur courant dans une plage de dates (max 62 jours pour prévenir les abus)
        [HttpGet("range")]
        // Récupère toutes les notes appartenant à l'utilisateur courant dans une plage de dates.
        // La plage est limitée à 62 jours pour prévenir des réponses excessivement volumineuses.
        // Les résultats sont triés par date puis par heure dans chaque jour.
        // start : premier jour de la plage (inclus)
        // end : dernier jour de la plage (inclus)
        public async Task<IActionResult> GetNotesForRange([FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            // Rejet des plages de plus d'environ deux mois pour éviter l'épuisement des ressources
            if ((end - start).TotalDays > 62) return BadRequest("Plage trop grande.");

            // Récupération des notes qui tombent dans [start.Date, end.Date] inclus
            var notes = await _context.UserNotes
                .Where(n => n.IdUserFk == userId && n.Date >= start.Date && n.Date <= end.Date)
                .OrderBy(n => n.Date)  // Tri par date en premier (ordre chronologique entre les jours)
                .ThenBy(n => n.Hour)   // Puis par heure dans chaque jour
                .ToListAsync();

            return Ok(notes);
        }

        // POST /api/notes
        // Crée une nouvelle note (Id == 0) ou met à jour une existante (Id non nul)
        [HttpPost]
        // Crée ou met à jour une entrée de note.
        // Applique une assainissement côté serveur au contenu (supprime les balises HTML dangereuses).
        // Impose des plages d'heures valides (6–22 pour le début, 7–23 pour la fin).
        // La date est normalisée à minuit pour prévenir les problèmes de décalage de fuseau horaire.
        // note : les données de note soumises par le client
        public async Task<IActionResult> Save([FromBody] UserNote note)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            // Suppression de la composante heure de la date et marquage comme timezone non spécifiée.
            // Cela empêche le décalage UTC vs. heure locale de déplacer la note vers un autre jour calendaire.
            note.Date = new DateTime(note.Date.Year, note.Date.Month, note.Date.Day, 0, 0, 0, DateTimeKind.Unspecified);

            // --- Assainissement du contenu ---
            // Suppression des espaces de début/fin et garantie que le champ n'est pas null
            note.Content = note.Content?.Trim() ?? string.Empty;

            // Premier passage : suppression du contenu interne des éléments de bloc dangereux
            // (script, style, iframe, object, embed) y compris leurs balises
            note.Content = System.Text.RegularExpressions.Regex.Replace(
                note.Content,
                @"<(script|style|iframe|object|embed)[^>]*>.*?<\/\1>",
                string.Empty,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase |
                System.Text.RegularExpressions.RegexOptions.Singleline);

            // Deuxième passage : suppression de toutes les balises HTML restantes (ex. : <b>, <p>, <a href="...">)
            note.Content = System.Text.RegularExpressions.Regex.Replace(note.Content, "<[^>]*>", string.Empty);

            // Imposition d'un maximum de 2000 caractères pour prévenir les abus de stockage
            if (note.Content.Length > 2000) note.Content = note.Content[..2000];

            // --- Validation des heures ---
            // La grille d'agenda commence à 6 et se termine à 22 ; rejet des heures de début hors de cette plage
            if (note.Hour < 6 || note.Hour > 22) return BadRequest("Heure de début invalide.");

            // EndHour doit être strictement après Hour et dans la limite de la grille (max 23)
            // Si invalide, EndHour est limité à une heure après le début
            if (note.EndHour <= note.Hour || note.EndHour > 23) note.EndHour = note.Hour + 1;

            // Force le propriétaire à être l'utilisateur actuellement authentifié
            note.IdUserFk = userId.Value;

            if (note.Id == 0)
            {
                // Nouvelle note — enregistrement des horodatages de création et de modification
                note.CreatedAt  = DateTime.UtcNow;
                note.ModifiedAt = DateTime.UtcNow;
                _context.UserNotes.Add(note);
            }
            else
            {
                // Note existante — vérification qu'elle appartient à l'utilisateur courant avant la mise à jour
                var existing = await _context.UserNotes
                    .FirstOrDefaultAsync(n => n.Id == note.Id && n.IdUserFk == userId);
                if (existing == null) return NotFound();

                // Mise à jour uniquement des champs de contenu et de timing ; l'horodatage de création est immuable
                existing.Content    = note.Content;
                existing.Hour       = note.Hour;
                existing.EndHour    = note.EndHour;
                existing.ModifiedAt = DateTime.UtcNow;
            }

            // Persistance de l'insertion ou de la mise à jour
            await _context.SaveChangesAsync();
            return Ok(note);
        }

        // DELETE /api/notes/{id}
        // Supprime définitivement une note appartenant à l'utilisateur courant
        [HttpDelete("{id}")]
        // Supprime une note par son ID.
        // Vérifie que la note appartient à l'utilisateur demandeur avant la suppression.
        // id : la clé primaire de la note à supprimer
        public async Task<IActionResult> Delete(int id)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            // Recherche de la note correspondant à la fois à l'ID donné et à l'ID de l'utilisateur courant
            // (empêche les utilisateurs de supprimer les notes d'autres utilisateurs en devinant des ID)
            var note = await _context.UserNotes
                .FirstOrDefaultAsync(n => n.Id == id && n.IdUserFk == userId);
            if (note == null) return NotFound();

            // Suppression de l'entité note et persistance de la suppression
            _context.UserNotes.Remove(note);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
