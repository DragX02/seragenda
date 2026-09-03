// Importation des attributs d'autorisation ASP.NET Core
using Microsoft.AspNetCore.Authorization;
// Importation des types de contrôleur MVC/API de base et des helpers de résultat
using Microsoft.AspNetCore.Mvc;
// Importation d'Entity Framework Core pour les opérations asynchrones en base de données
using Microsoft.EntityFrameworkCore;
// Importation des modèles du projet
using seragenda.Models;
// Importation du helper de mention de report (écriture et lecture du marqueur)
using seragenda.Services;
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

        // Plage maximale acceptée par les lectures groupées (notes et cours).
        //
        // La borne valait deux mois, ce qui suffisait aux vues semaine et mois mais pas
        // à la vue Trimestre : une période comme "Pâques → fin d'année" dépasse 70 jours,
        // la requête était refusée et le client, qui avale l'erreur, affichait un
        // trimestre vide. Un semestre de notes pour un seul enseignant reste une
        // réponse modeste, la borne sert seulement de garde-fou.
        public const int MaxJoursPlage = 200;

        // GET /api/notes/range?start=...&end=...
        // Retourne toutes les notes de l'utilisateur courant dans une plage de dates
        [HttpGet("range")]
        // Récupère toutes les notes appartenant à l'utilisateur courant dans une plage de dates.
        // La plage est limitée à MaxJoursPlage pour prévenir des réponses excessivement volumineuses.
        // Les résultats sont triés par date puis par heure dans chaque jour.
        // start : premier jour de la plage (inclus)
        // end : dernier jour de la plage (inclus)
        public async Task<IActionResult> GetNotesForRange([FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            // Rejet des plages déraisonnables pour éviter l'épuisement des ressources
            if ((end - start).TotalDays > MaxJoursPlage) return BadRequest("Plage trop grande.");

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

            // Normalisation et assainissement communs à toutes les écritures de note
            var refus = Assainir(note, userId.Value);
            if (refus != null) return BadRequest(refus);

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

                // La mention de report survit à la modification de la note. Le client
                // envoie le texte tel qu'il est affiché, c'est-à-dire sans le marqueur :
                // sans cette reprise, retoucher une leçon déjà reportée effacerait la
                // trace du report. C'est ici, et non côté client, que la règle vit :
                // le serveur est le seul à connaître le contenu réellement stocké.
                if (ReportNote.Cible(note.Content) == null &&
                    ReportNote.Cible(existing.Content) is DateTime dejaReportee)
                {
                    note.Content = ReportNote.Marquer(note.Content, dejaReportee);
                }

                // Mise à jour uniquement des champs de contenu et de timing ; l'horodatage de création est immuable
                existing.Content       = note.Content;
                existing.Hour          = note.Hour;
                existing.EndHour       = note.EndHour;
                existing.Minute        = note.Minute;
                existing.EndMinute     = note.EndMinute;
                existing.IdViseeFk     = note.IdViseeFk;
                existing.ViseeContexte = note.ViseeContexte;
                existing.ModifiedAt    = DateTime.UtcNow;
            }

            // Persistance de l'insertion ou de la mise à jour
            await _context.SaveChangesAsync();
            return Ok(note);
        }

        // Normalise et assainit une note reçue du client, avant insertion ou mise à jour.
        // Regroupe ce que toute écriture doit subir, quelle que soit la porte d'entrée :
        // POST /api/notes pour une note isolée, POST /api/notes/copier pour un modèle
        // recopié sur plusieurs dates. Le client ne peut donc pas contourner ces règles
        // en passant par l'un plutôt que l'autre.
        //
        // Modifie la note sur place. Retourne null si elle est acceptable, sinon le
        // motif de refus à renvoyer au client.
        private static string? Assainir(UserNote note, int userId)
        {
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
            if (note.Content.Length > ReportNote.MaxContenu)
                note.Content = note.Content[..ReportNote.MaxContenu];

            // --- Validation des heures ---
            // La grille d'agenda commence à 8 et se termine à 18 ; rejet des heures de début hors de cette plage
            if (note.Hour < 8 || note.Hour > 17) return "Heure de début invalide.";

            // Normalise les minutes sur un pas de 5, bornées à [0, 55]
            note.Minute    = NormalizeMinute(note.Minute);
            note.EndMinute = NormalizeMinute(note.EndMinute);

            // La fin (EndHour:EndMinute) doit être strictement après le début (Hour:Minute)
            // et rester dans la limite de la grille (max 18h). Sinon, la fin est fixée
            // à une heure après le début, en conservant la même minute.
            int startTotal = note.Hour * 60 + note.Minute;
            int endTotal   = note.EndHour * 60 + note.EndMinute;
            if (note.EndHour < 9 || note.EndHour > 18 || endTotal <= startTotal)
            {
                note.EndHour   = note.Hour + 1;
                note.EndMinute = note.Minute;
            }

            // 18h00 est la borne de fin de la grille : aucune minute au-dela n'est admise
            if (note.EndHour == 18) note.EndMinute = 0;

            // Force le propriétaire à être l'utilisateur actuellement authentifié
            note.IdUserFk = userId;

            // Normalise l'absence de visée : 0 (valeur par défaut du client) → null
            if (note.IdViseeFk == 0) note.IdViseeFk = null;

            // Assainit le contexte de cascade (texte composé côté client) : supprime les balises
            // HTML et limite la longueur ; chaîne vide → null.
            if (!string.IsNullOrWhiteSpace(note.ViseeContexte))
            {
                var ctx = System.Text.RegularExpressions.Regex.Replace(note.ViseeContexte, "<[^>]*>", string.Empty).Trim();
                note.ViseeContexte = ctx.Length > 2000 ? ctx[..2000] : (ctx.Length == 0 ? null : ctx);
            }
            else
            {
                note.ViseeContexte = null;
            }

            return null;
        }

        // Ramène une minute dans l'intervalle [0, 55] en la tronquant au multiple de 5 inférieur.
        // Les flèches de l'interface avancent déjà par pas de 5 ; ce filet garantit une valeur propre
        // même si un client envoie une minute arbitraire.
        private static int NormalizeMinute(int minute)
        {
            if (minute < 0) minute = 0;
            if (minute > 59) minute = 59;
            return (minute / 5) * 5;
        }

        // Nombre maximal de notes créées par un seul appel à /api/notes/copier.
        // Copier une semaine chargée vers une dizaine de semaines reste très en dessous ;
        // la borne empêche seulement qu'une requête malformée écrive des milliers de lignes.
        private const int MaxCopies = 500;

        // POST /api/notes/copier
        // Recopie des leçons existantes sur d'autres dates, en une seule requête et
        // dans une seule transaction.
        //
        // Le client faisait ce travail note par note : un POST par copie, plus un POST
        // par originale à marquer. Reporter une semaine de dix leçons partait ainsi en
        // vingt allers-retours, et une coupure au milieu laissait la moitié des leçons
        // recopiées et l'autre non — état dont il n'existait aucun moyen de revenir.
        // Ici tout est écrit ou rien ne l'est.
        //
        // Les deux usages de l'écran partagent ce point d'entrée, car ils partagent leur
        // calcul : chaque destination est un décalage en jours appliqué à la date de la
        // leçon source.
        //   - Reporter : un seul décalage, et l'originale reçoit la mention "Reporté au …"
        //   - Copier   : un décalage par destination, et rien n'est marqué (ce sont de
        //                nouvelles occurrences, pas la trace d'un déplacement)
        //
        // Une source est soit une leçon enregistrée, désignée par son identifiant, soit un
        // modèle envoyé tel quel. Le second cas est celui de la copie d'une seule leçon
        // depuis sa fenêtre d'édition : les retouches en cours partent dans les copies
        // sans que l'originale soit modifiée, et le modèle n'existe donc nulle part en base.
        [HttpPost("copier")]
        public async Task<IActionResult> Copier([FromBody] CopierNotesDto dto)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            // Les identifiants en double ne doivent pas produire deux copies de la même leçon
            var ids       = (dto.IdsNotes ?? new()).Distinct().ToList();
            var modeles   = dto.Modeles ?? new();
            var decalages = (dto.Decalages ?? new()).Distinct().ToList();

            if (ids.Count == 0 && modeles.Count == 0)
                return BadRequest(new { message = "Aucune leçon à copier." });
            if (decalages.Count == 0)
                return BadRequest(new { message = "Aucune date de destination." });

            // Marquer l'originale n'a de sens que pour un report, qui vise une seule date :
            // avec plusieurs destinations, la mention ne pourrait en désigner qu'une.
            if (dto.Marquer && decalages.Count != 1)
                return BadRequest(new { message = "Un report ne vise qu'une seule date." });

            // Un modèle n'est enregistré nulle part : il n'y a rien à marquer.
            if (dto.Marquer && modeles.Count > 0)
                return BadRequest(new { message = "Un report part d'une leçon enregistrée." });

            if ((ids.Count + modeles.Count) * decalages.Count > MaxCopies)
                return BadRequest(new { message = $"Trop de copies demandées (maximum {MaxCopies})." });

            // Les leçons sources doivent appartenir à l'utilisateur : le filtre sur IdUserFk
            // est ce qui empêche de recopier — donc de lire — la leçon d'un collègue.
            var sources = ids.Count == 0
                ? new List<UserNote>()
                : await _context.UserNotes
                    .Where(n => n.IdUserFk == userId.Value && ids.Contains(n.Id))
                    .ToListAsync();

            if (ids.Count > 0 && sources.Count == 0)
                return NotFound(new { message = "Aucune leçon trouvée." });

            // Un modèle vient du client : il passe par les mêmes règles qu'une note
            // enregistrée normalement, sinon cette porte d'entrée les contournerait.
            foreach (var modele in modeles)
            {
                var refus = Assainir(modele, userId.Value);
                if (refus != null) return BadRequest(new { message = refus });
            }

            // Une seule transaction : la copie d'une semaine réussit en entier ou pas du tout
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                int copiees = 0;

                // Écrit une copie de `source` à sa date décalée de `decalage` jours.
                void Copie(UserNote source, string contenu, int decalage)
                {
                    var cible = source.Date.Date.AddDays(decalage);

                    _context.UserNotes.Add(new UserNote
                    {
                        IdUserFk      = userId.Value,
                        // Date nue, comme dans Assainir : aucune composante horaire ne doit
                        // pouvoir faire glisser la copie sur le jour précédent.
                        Date          = new DateTime(cible.Year, cible.Month, cible.Day,
                                                     0, 0, 0, DateTimeKind.Unspecified),
                        Hour          = source.Hour,
                        Minute        = source.Minute,
                        EndHour       = source.EndHour,
                        EndMinute     = source.EndMinute,
                        Content       = contenu,
                        IdViseeFk     = source.IdViseeFk,
                        ViseeContexte = source.ViseeContexte,
                        CreatedAt     = DateTime.UtcNow,
                        ModifiedAt    = DateTime.UtcNow
                    });

                    copiees++;
                }

                foreach (var source in sources)
                {
                    // Le texte de la copie repart sans mention de report : c'est elle,
                    // la leçon reportée, pas la trace d'un départ.
                    var contenu = ReportNote.Texte(source.Content);

                    foreach (var decalage in decalages) Copie(source, contenu, decalage);

                    // Report : l'originale porte désormais la date où la leçon est repartie
                    if (dto.Marquer)
                    {
                        source.Content    = ReportNote.Marquer(source.Content,
                                                               source.Date.Date.AddDays(decalages[0]));
                        source.ModifiedAt = DateTime.UtcNow;
                    }
                }

                foreach (var modele in modeles)
                {
                    var contenu = ReportNote.Texte(modele.Content);
                    foreach (var decalage in decalages) Copie(modele, contenu, decalage);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Le client annonce le résultat ; il n'a plus à compter les succès partiels
                return Ok(new { Copiees = copiees, Sources = sources.Count + modeles.Count });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var detail = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = $"La copie a échoué, rien n'a été enregistré : {detail}" });
            }
        }

        // Corps attendu par POST /api/notes/copier
        public class CopierNotesDto
        {
            // Identifiants des leçons enregistrées à recopier (celles de l'utilisateur courant)
            public List<int> IdsNotes { get; set; } = new();

            // Leçons sources qui n'existent pas en base : la copie d'une leçon depuis sa
            // fenêtre d'édition part des valeurs affichées, sans toucher à l'originale.
            // Leur date sert de référence aux décalages, exactement comme celle d'une
            // leçon enregistrée ; elles ne sont pas insérées telles quelles.
            public List<UserNote> Modeles { get; set; } = new();

            // Destinations, exprimées en jours par rapport à la date de chaque leçon source.
            // Copier une semaine entière donne des multiples de 7, ce qui conserve le jour
            // de la semaine de chaque leçon.
            public List<int> Decalages { get; set; } = new();

            // Vrai pour un report : l'originale reçoit la mention "Reporté au …".
            // Faux pour une copie : rien n'est écrit sur les leçons sources.
            public bool Marquer { get; set; }
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
