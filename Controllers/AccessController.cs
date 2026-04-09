// Importation des attributs d'autorisation ASP.NET Core
using Microsoft.AspNetCore.Authorization;
// Importation des types de contrôleur MVC/API de base et des helpers de résultat
using Microsoft.AspNetCore.Mvc;
// Importation d'Entity Framework Core pour les requêtes asynchrones en base de données
using Microsoft.EntityFrameworkCore;
// Importation des modèles du projet et du helper de hachage de licence
using seragenda.Models;
using seragenda.Services;
// Importation du support des Claims pour extraire l'identité de l'utilisateur courant depuis le JWT
using System.Security.Claims;

namespace seragenda.Controllers
{
    // Marque cette classe comme contrôleur API (active la liaison automatique du modèle, les attributs de validation, etc.)
    [ApiController]
    // Toutes les routes de ce contrôleur sont préfixées par /api/access
    [Route("api/[controller]")]
    // Gère la validation des clés de licence et la vérification du statut pour les utilisateurs finaux.
    // Les utilisateurs authentifiés soumettent leur code de licence ici pour obtenir ou vérifier l'accès à l'application.
    public class AccessController : ControllerBase
    {
        // Contexte de base de données Entity Framework pour interroger les enregistrements de licence et d'utilisateur
        private readonly AgendaContext _context;

        // Constructeur — reçoit le contexte de base de données par injection de dépendances.
        // context : le contexte de base de données EF Core pour la base de données agenda
        public AccessController(AgendaContext context)
        {
            _context = context;
        }

        // POST /api/access/validate
        // Un utilisateur authentifié soumet son code de licence pour activer l'accès
        [HttpPost("validate")]
        // Nécessite un jeton JWT valide — seuls les utilisateurs connectés peuvent valider une licence
        [Authorize]
        // Valide un code de licence soumis par l'utilisateur actuellement authentifié.
        // Si la licence est valide et non assignée, elle est liée à l'utilisateur demandeur.
        // Retourne le code en clair normalisé en cas de succès pour que le client puisse le mettre en cache localement.
        // dto : DTO contenant le code de licence saisi par l'utilisateur
        public async Task<IActionResult> Validate([FromBody] AccessCodeDto dto)
        {
            // Rejet immédiat des codes vides ou ne contenant que des espaces
            if (string.IsNullOrWhiteSpace(dto.Code))
                return BadRequest(new { message = "Code requis" });

            // Hachage du code soumis pour le comparer à la valeur hachée stockée en base de données.
            // Le code en clair n'est jamais stocké — seul son hash SHA-256 est persisté.
            var codeHash = LicenseHelper.HashCode(dto.Code);

            // Recherche de la licence par son code haché
            var license = await _context.Licenses
                .FirstOrDefaultAsync(l => l.Code == codeHash);

            // Rejet si aucune licence correspondante n'existe ou si elle a été révoquée manuellement
            if (license == null || !license.IsActive)
                return BadRequest(new { message = "Code invalide ou révoqué" });

            // Rejet si la licence a dépassé sa date d'expiration optionnelle
            if (license.ExpiresAt.HasValue && license.ExpiresAt.Value < DateTime.UtcNow)
                return BadRequest(new { message = "Licence expirée" });

            // Si la licence n'a pas encore été assignée à quelqu'un, la lier à l'utilisateur courant
            if (license.AssignedUserId == null)
            {
                // Lecture de l'email de l'utilisateur courant depuis le claim Name du JWT
                var email = User.FindFirst(ClaimTypes.Name)?.Value;
                // Chargement de l'entité utilisateur complète pour obtenir la clé primaire entière
                var user  = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == email);
                if (user != null)
                {
                    // Assignation de la licence à cet utilisateur et enregistrement du moment de l'assignation
                    license.AssignedUserId = user.IdUser;
                    license.AssignedAt     = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }
            // Si la licence est déjà assignée (éventuellement au même utilisateur), on retourne quand même un succès.
            // Le client doit vérifier /api/access/check pour confirmer la validité continue.

            // Retourne le code en clair normalisé (mis en majuscules et épuré) — pas le hash stocké
            return Ok(new { valid = true, code = dto.Code.Trim().ToUpper() });
        }

        // GET /api/access/check?code=...
        // Permet au client de vérifier qu'une licence précédemment validée est toujours active
        [HttpGet("check")]
        // Nécessite un jeton JWT valide — seuls les utilisateurs connectés peuvent vérifier une licence
        [Authorize]
        // Vérifie si un code de licence donné est toujours valide (actif et non expiré).
        // Utilisé par le client pour revérifier périodiquement l'accès sans soumettre à nouveau le flux de validation complet.
        // code : le code de licence en clair à vérifier (passé en paramètre de chaîne de requête)
        public async Task<IActionResult> Check([FromQuery] string code)
        {
            // Rejet si aucun code n'a été fourni dans la chaîne de requête
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest(new { valid = false, message = "Code manquant" });

            // Hachage du code soumis pour le comparer au hash stocké
            var codeHash = LicenseHelper.HashCode(code);

            // Recherche de l'enregistrement de licence par hash
            var license = await _context.Licenses
                .FirstOrDefaultAsync(l => l.Code == codeHash);

            // Retourne valid=false si la licence a été révoquée ou n'existe pas
            if (license == null || !license.IsActive)
                return Ok(new { valid = false, message = "Licence révoquée" });

            // Retourne valid=false si la licence a expiré
            if (license.ExpiresAt.HasValue && license.ExpiresAt.Value < DateTime.UtcNow)
                return Ok(new { valid = false, message = "Licence expirée" });

            // La licence est active et dans sa fenêtre de validité
            return Ok(new { valid = true });
        }
    }

    // Objet de transfert de données pour le point de terminaison POST /api/access/validate.
    // Transporte le code de licence en clair saisi par l'utilisateur final.
    public class AccessCodeDto
    {
        // Le code de licence tel que saisi par l'utilisateur (avant normalisation ou hachage)
        public string Code { get; set; } = string.Empty;
    }
}
