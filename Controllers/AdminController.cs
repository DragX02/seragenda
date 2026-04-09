// Importation des attributs d'autorisation ASP.NET Core
using Microsoft.AspNetCore.Authorization;
// Importation des types de contrôleur MVC/API de base et des helpers de résultat
using Microsoft.AspNetCore.Mvc;
// Importation d'Entity Framework Core pour les opérations asynchrones en base de données
using Microsoft.EntityFrameworkCore;
// Importation des modèles et services du projet
using seragenda.Models;
using seragenda.Services;
// Importation de la cryptographie pour la génération de codes aléatoires
using System.Security.Cryptography;

namespace seragenda.Controllers
{
    // Marque cette classe comme contrôleur API avec liaison automatique du modèle et validation
    [ApiController]
    // Toutes les routes de ce contrôleur sont préfixées par /api/admin
    [Route("api/[controller]")]
    // Restreint tous les points de terminaison de ce contrôleur aux utilisateurs ayant le rôle ADMIN
    [Authorize(Roles = "ADMIN")]
    // Fournit des points de terminaison d'administration pour la gestion des clés de licence.
    // Accessible uniquement aux utilisateurs ayant le rôle système ADMIN.
    // Supporte la liste, la création, la révocation, la réactivation et la suppression des licences.
    public class AdminController : ControllerBase
    {
        // Contexte de base de données Entity Framework pour interroger et persister les données de licence
        private readonly AgendaContext _context;

        // Constructeur — reçoit le contexte de base de données par injection de dépendances.
        // context : le contexte de base de données EF Core pour la base de données agenda
        public AdminController(AgendaContext context)
        {
            _context = context;
        }

        // GET /api/admin/licenses
        // Retourne toutes les licences avec leur statut courant dérivé de IsActive, ExpiresAt et AssignedUserId
        [HttpGet("licenses")]
        // Récupère tous les enregistrements de licence ordonnés par date de création (les plus récents en premier).
        // Chaque licence inclut une chaîne de statut lisible calculée.
        public async Task<IActionResult> GetLicenses()
        {
            var licenses = await _context.Licenses
                // Chargement eager de la propriété de navigation pour accéder à l'email de l'utilisateur assigné
                .Include(l => l.AssignedUser)
                // Les licences créées le plus récemment apparaissent en premier
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new
                {
                    l.Id,
                    l.Code,    // Il s'agit du hash SHA-256 stocké en base de données ; le code en clair n'est jamais persisté
                    l.Label,
                    l.IsActive,
                    l.CreatedAt,
                    l.ExpiresAt,
                    l.AssignedAt,
                    // Affiche l'email de l'utilisateur qui a activé la licence, ou null si non utilisée
                    AssignedEmail = l.AssignedUser != null ? l.AssignedUser.Email : null,
                    // Dérive une chaîne de statut :
                    // "Révoquée" si désactivée manuellement,
                    // "Expirée" si la date d'expiration est dépassée,
                    // "Utilisée" si assignée à un utilisateur,
                    // "Disponible" si active et pas encore assignée
                    Status = !l.IsActive ? "Révoquée"
                           : l.ExpiresAt.HasValue && l.ExpiresAt.Value < DateTime.UtcNow ? "Expirée"
                           : l.AssignedUserId != null ? "Utilisée"
                           : "Disponible"
                })
                .ToListAsync();

            return Ok(licenses);
        }

        // POST /api/admin/licenses
        // Crée une nouvelle licence avec un code personnalisé optionnel et une date d'expiration optionnelle
        [HttpPost("licenses")]
        // Crée une nouvelle clé de licence.
        // Si aucun code n'est fourni dans le corps de la requête, un code alphanumérique aléatoire de 10 caractères est généré.
        // Le code en clair n'est retourné qu'une seule fois dans cette réponse ; seul son hash SHA-256 est stocké.
        // dto : les paramètres de création de licence (code optionnel, libellé optionnel, expiration optionnelle)
        public async Task<IActionResult> CreateLicense([FromBody] CreateLicenseDto dto)
        {
            try
            {
                string plainCode; // La clé de licence lisible par l'humain (retournée à l'admin, jamais stockée)
                string codeHash;  // Le hash SHA-256 de la clé de licence (stocké en base de données)

                if (string.IsNullOrWhiteSpace(dto.Code))
                {
                    // Aucun code fourni — génération d'un code unique aléatoire
                    // Boucle jusqu'à trouver un code dont le hash n'existe pas déjà en base de données
                    do
                    {
                        plainCode = GenerateCode();
                        // Hachage du code candidat avant la requête — calculé en dehors de LINQ pour éviter les problèmes de traduction EF
                        codeHash = LicenseHelper.HashCode(plainCode);
                    }
                    while (await _context.Licenses.AnyAsync(l => l.Code == codeHash));
                }
                else
                {
                    // Utilisation du code fourni par l'admin, normalisé en majuscules
                    plainCode = dto.Code.Trim().ToUpper();
                    codeHash  = LicenseHelper.HashCode(plainCode);
                    // Rejet si une licence avec le même hash existe déjà
                    if (await _context.Licenses.AnyAsync(l => l.Code == codeHash))
                        return BadRequest(new { message = "Ce code existe déjà" });
                }

                // Construction de la nouvelle entité licence ; seul le hash est persisté, jamais le code en clair
                var license = new License
                {
                    Code      = codeHash,
                    Label     = dto.Label?.Trim(),
                    IsActive  = true,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = dto.ExpiresAt  // Null signifie que la licence n'expire jamais
                };

                _context.Licenses.Add(license);
                await _context.SaveChangesAsync();

                // Retourne le code en clair une seule fois pour que l'admin puisse le transmettre à l'utilisateur final.
                // Après cette réponse, le code en clair est perdu — seul le hash reste en base de données.
                return Ok(new { license.Id, Code = plainCode, license.Label, license.IsActive, license.CreatedAt, license.ExpiresAt });
            }
            catch (Exception)
            {
                // Capture les erreurs inattendues de base de données ou de hachage et retourne un 500 générique
                return StatusCode(500, new { message = "Erreur lors de la création de la licence" });
            }
        }

        // PUT /api/admin/licenses/{id}/revoke
        // Désactive une licence sans la supprimer — préserve l'historique d'audit
        [HttpPut("licenses/{id}/revoke")]
        // Révoque une licence en mettant son indicateur IsActive à false.
        // Les licences révoquées sont rejetées par le point de terminaison de validation d'accès.
        // id : la clé primaire de la licence à révoquer
        public async Task<IActionResult> Revoke(int id)
        {
            // Recherche de la licence par sa clé primaire
            var license = await _context.Licenses.FindAsync(id);
            // Retourne 404 si aucune licence avec cet ID n'existe
            if (license == null) return NotFound();

            // Marquage de la licence comme inactive
            license.IsActive = false;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Licence révoquée" });
        }

        // PUT /api/admin/licenses/{id}/reactivate
        // Réactive une licence précédemment révoquée
        [HttpPut("licenses/{id}/reactivate")]
        // Réactive une licence précédemment révoquée en remettant son indicateur IsActive à true.
        // id : la clé primaire de la licence à réactiver
        public async Task<IActionResult> Reactivate(int id)
        {
            var license = await _context.Licenses.FindAsync(id);
            if (license == null) return NotFound();

            // Restauration du statut actif
            license.IsActive = true;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Licence réactivée" });
        }

        // DELETE /api/admin/licenses/{id}
        // Supprime définitivement l'enregistrement de licence de la base de données
        [HttpDelete("licenses/{id}")]
        // Supprime définitivement un enregistrement de licence.
        // Cette action est irréversible ; envisager d'utiliser la révocation si l'historique d'audit est important.
        // id : la clé primaire de la licence à supprimer
        public async Task<IActionResult> Delete(int id)
        {
            var license = await _context.Licenses.FindAsync(id);
            if (license == null) return NotFound();

            // Suppression de l'entité du contexte et persistance de la suppression
            _context.Licenses.Remove(license);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Licence supprimée" });
        }

        // Génère un code de licence alphanumérique majuscule aléatoire de 10 caractères.
        // Utilise un jeu de caractères visuellement non ambigu (sans 0/O, 1/I, etc.) pour prévenir
        // les erreurs de transcription lorsque les codes sont partagés oralement ou imprimés.
        // Retourne une chaîne aléatoire de 10 caractères tirés du jeu de caractères sûr
        private static string GenerateCode()
        {
            // Jeu de caractères sûr : lettres majuscules et chiffres, excluant les caractères visuellement ambigus
            // (sans 0/O, sans 1/I) pour rendre les codes faciles à lire et à saisir
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            // Génération de 10 octets aléatoires cryptographiquement sécurisés via le RNG du système d'exploitation
            var bytes = RandomNumberGenerator.GetBytes(10);
            // Mappage de chaque octet aléatoire vers un caractère de l'alphabet par modulo
            return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
        }
    }

    // Objet de transfert de données pour le point de terminaison POST /api/admin/licenses.
    // Tous les champs sont optionnels — des valeurs par défaut sensées sont appliquées lorsqu'ils sont omis.
    public class CreateLicenseDto
    {
        // Optionnel : si null ou vide, un code aléatoire est généré automatiquement
        public string? Code { get; set; }
        // Libellé optionnel lisible pour aider l'admin à identifier cette licence (ex. : "PROF-DUPONT")
        public string? Label { get; set; }
        // Date/heure d'expiration optionnelle en UTC ; null signifie que la licence n'expire jamais
        public DateTime? ExpiresAt { get; set; }
    }
}
