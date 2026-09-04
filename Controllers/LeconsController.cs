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
    // Toutes les routes de ce contrôleur sont préfixées par /api/lecons
    [Route("api/[controller]")]
    // Marque cette classe comme contrôleur API
    [ApiController]
    // Nécessite un jeton JWT valide sur tous les points de terminaison
    [Authorize]
    // Gère les préparations de leçon de l'enseignant connecté.
    //
    // Une préparation reprend le formulaire papier « Préparation de leçon type » :
    // un en-tête, les compétences visées, puis le déroulement en phases. Elle
    // appartient à un seul enseignant et n'est jamais visible par un autre : toutes
    // les lectures comme les écritures filtrent sur l'utilisateur du jeton.
    public class LeconsController : ControllerBase
    {
        // Contexte de base de données Entity Framework pour lire et écrire les préparations
        private readonly AgendaContext _context;

        // Constructeur — reçoit le contexte de base de données par injection de dépendances.
        public LeconsController(AgendaContext context)
        {
            _context = context;
        }

        // Résout la clé primaire entière de l'utilisateur actuellement authentifié
        // en recherchant son adresse email (stockée comme claim Name du JWT) en base.
        // Retourne null si le claim est absent ou si l'utilisateur est introuvable.
        private async Task<int?> GetUserId()
        {
            var email = User.FindFirst(ClaimTypes.Name)?.Value;
            if (email == null) return null;
            var user = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == email);
            return user?.IdUser;
        }

        // Nombre maximal de phases acceptées pour une préparation.
        // Le formulaire papier en compte quatre ; la borne laisse largement de quoi
        // détailler une leçon longue sans permettre d'en écrire des milliers.
        private const int MaxPhases = 30;

        // GET /api/lecons
        // Retourne les préparations de l'utilisateur courant, la plus récemment
        // modifiée en tête, avec leurs phases dans l'ordre d'affichage.
        [HttpGet]
        public async Task<IActionResult> GetLecons()
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            try
            {
                // Les phases sont chargées avec leur leçon : la projection les parcourt,
                // et sans Include elles arriveraient vides. La mise en forme se fait
                // ensuite en mémoire — un appel de méthode ne se traduit pas en SQL.
                var lecons = await _context.Lecons
                    .AsNoTracking()
                    .Include(l => l.Phases)
                    .Where(l => l.IdUserFk == userId.Value)
                    .OrderByDescending(l => l.ModifiedAt)
                    .ToListAsync();

                return Ok(lecons.Select(Projeter));
            }
            catch (Exception ex)
            {
                // Cause la plus fréquente : les tables lecon / lecon_phase n'existent pas
                // encore sur cette base, ou le rôle applicatif n'a pas les droits dessus.
                // Sans ce message, le client ne recevrait qu'un 500 au corps vide.
                return StatusCode(500, MessageErreur(ex));
            }
        }

        // GET /api/lecons/{id}
        // Retourne une préparation précise, à condition qu'elle appartienne à
        // l'utilisateur courant.
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetLecon(int id)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var lecon = await _context.Lecons
                    .AsNoTracking()
                    .Include(l => l.Phases)
                    .FirstOrDefaultAsync(l => l.Id == id && l.IdUserFk == userId.Value);

                if (lecon == null) return NotFound();

                return Ok(Projeter(lecon));
            }
            catch (Exception ex)
            {
                return StatusCode(500, MessageErreur(ex));
            }
        }

        // POST /api/lecons
        // Crée une préparation (Id == 0) ou met à jour une existante.
        //
        // Les phases sont réécrites en bloc : l'ancien déroulement est supprimé et
        // remplacé par celui reçu. C'est ce que l'écran manipule — on ajoute, retire
        // et réordonne des phases avant d'enregistrer — et cela évite de distinguer
        // ajout, modification et suppression ligne par ligne. L'ensemble tient dans
        // une transaction : une préparation ne peut pas se retrouver avec la moitié
        // de son ancien déroulement et la moitié du nouveau.
        [HttpPost]
        public async Task<IActionResult> Save([FromBody] Lecon lecon)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            var refus = Assainir(lecon, userId.Value);
            if (refus != null) return BadRequest(new { message = refus });

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                Lecon cible;

                if (lecon.Id == 0)
                {
                    cible = new Lecon
                    {
                        IdUserFk  = userId.Value,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Lecons.Add(cible);
                }
                else
                {
                    // La préparation doit appartenir à l'utilisateur courant : ce filtre
                    // est ce qui empêche de modifier celle d'un collègue en devinant un id.
                    var existante = await _context.Lecons
                        .Include(l => l.Phases)
                        .FirstOrDefaultAsync(l => l.Id == lecon.Id && l.IdUserFk == userId.Value);

                    if (existante == null) return NotFound();

                    // Le déroulement reçu remplace l'ancien en entier
                    _context.LeconPhases.RemoveRange(existante.Phases);
                    cible = existante;
                }

                cible.Titre         = lecon.Titre;
                cible.Enseignant    = lecon.Enseignant;
                cible.Duree         = lecon.Duree;
                cible.NombreSeances = lecon.NombreSeances;
                cible.Niveaux       = lecon.Niveaux;
                cible.Competences   = lecon.Competences;
                cible.IdViseeFk     = lecon.IdViseeFk;
                cible.ModifiedAt    = DateTime.UtcNow;

                // Les identifiants de phase reçus du client sont ignorés : les lignes
                // sont recréées, et l'ordre est renuméroté à partir de 1 pour qu'aucun
                // trou ne subsiste après la suppression d'une phase du milieu.
                int ordre = 1;
                foreach (var phase in lecon.Phases)
                {
                    cible.Phases.Add(new LeconPhase
                    {
                        Ordre    = ordre++,
                        Intitule = phase.Intitule,
                        Temps    = phase.Temps
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    cible.Id,
                    cible.Titre,
                    cible.Enseignant,
                    cible.Duree,
                    cible.NombreSeances,
                    cible.Niveaux,
                    cible.Competences,
                    cible.IdViseeFk,
                    cible.CreatedAt,
                    cible.ModifiedAt,
                    Phases = cible.Phases
                        .OrderBy(p => p.Ordre)
                        .Select(p => new { p.Id, p.Ordre, p.Intitule, p.Temps })
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = MessageErreur(ex) });
            }
        }

        // DELETE /api/lecons/{id}
        // Supprime définitivement une préparation de l'utilisateur courant.
        // Ses phases partent avec elle (ON DELETE CASCADE).
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var lecon = await _context.Lecons
                    .FirstOrDefaultAsync(l => l.Id == id && l.IdUserFk == userId.Value);

                if (lecon == null) return NotFound();

                _context.Lecons.Remove(lecon);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, MessageErreur(ex));
            }
        }

        // Projection commune aux lectures : la préparation et son déroulement,
        // sans la propriété de navigation vers l'utilisateur.
        private static object Projeter(Lecon l) => new
        {
            l.Id,
            l.Titre,
            l.Enseignant,
            l.Duree,
            l.NombreSeances,
            l.Niveaux,
            l.Competences,
            l.IdViseeFk,
            l.CreatedAt,
            l.ModifiedAt,
            Phases = l.Phases
                .OrderBy(p => p.Ordre)
                .Select(p => new { p.Id, p.Ordre, p.Intitule, p.Temps })
        };

        // Normalise et assainit une préparation reçue du client.
        // Modifie l'objet sur place. Retourne null si elle est acceptable, sinon le
        // motif de refus à renvoyer au client.
        private static string? Assainir(Lecon lecon, int userId)
        {
            lecon.IdUserFk = userId;

            // Le titre identifie la préparation dans la liste : sans lui, elle serait
            // impossible à retrouver.
            lecon.Titre = Texte(lecon.Titre, 200);
            if (lecon.Titre.Length == 0) return "Le titre de la leçon est obligatoire.";

            lecon.Enseignant  = Texte(lecon.Enseignant, 150);
            lecon.Duree       = Texte(lecon.Duree, 100);
            lecon.Niveaux     = Texte(lecon.Niveaux, 200);
            lecon.Competences = Texte(lecon.Competences, 4000, multiligne: true);

            // Normalise l'absence de visée : 0 (valeur par défaut du client) → null
            if (lecon.IdViseeFk == 0) lecon.IdViseeFk = null;

            // Une préparation couvre au moins une séance ; la borne haute écarte
            // seulement les saisies aberrantes.
            if (lecon.NombreSeances < 1)   lecon.NombreSeances = 1;
            if (lecon.NombreSeances > 100) lecon.NombreSeances = 100;

            lecon.Phases ??= new List<LeconPhase>();

            if (lecon.Phases.Count > MaxPhases)
                return $"Une leçon ne peut pas dépasser {MaxPhases} phases.";

            foreach (var phase in lecon.Phases)
            {
                phase.Intitule = Texte(phase.Intitule, 1000, multiligne: true);
                phase.Temps    = Texte(phase.Temps, 50);
            }

            return null;
        }

        // Nettoie un champ de saisie : supprime les balises HTML pour qu'aucun contenu
        // ne puisse être injecté à l'affichage, puis borne la longueur.
        // Les champs multilignes gardent leurs retours à la ligne, les autres non.
        private static string Texte(string? valeur, int maximum, bool multiligne = false)
        {
            var t = (valeur ?? string.Empty).Trim();

            // Premier passage : le contenu interne des éléments dangereux part avec eux
            t = System.Text.RegularExpressions.Regex.Replace(
                t,
                @"<(script|style|iframe|object|embed)[^>]*>.*?<\/\1>",
                string.Empty,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase |
                System.Text.RegularExpressions.RegexOptions.Singleline);

            // Deuxième passage : les balises restantes (<b>, <p>, <a href="…">)
            t = System.Text.RegularExpressions.Regex.Replace(t, "<[^>]*>", string.Empty);

            // Un champ d'une seule ligne ne doit pas pouvoir en cacher plusieurs
            if (!multiligne) t = t.Replace("\r", " ").Replace("\n", " ").Trim();
            else             t = t.Replace("\r\n", "\n").Replace("\r", "\n");

            return t.Length > maximum ? t[..maximum] : t;
        }

        // Compose un message d'erreur lisible à partir d'une exception de base de données.
        // Npgsql place la cause réelle (table absente, droits insuffisants, contrainte
        // violée) dans l'exception interne : sans elle, le client ne voit qu'un 500 vide.
        private static string MessageErreur(Exception ex)
        {
            var detail = ex.InnerException?.Message ?? ex.Message;
            return $"Erreur base de donnees : {detail}";
        }
    }
}
