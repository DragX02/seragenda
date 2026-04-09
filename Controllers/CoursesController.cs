// Importation des attributs d'autorisation ASP.NET Core
using Microsoft.AspNetCore.Authorization;
// Importation des types de contrôleur MVC/API de base et des helpers de résultat
using Microsoft.AspNetCore.Mvc;
// Importation d'Entity Framework Core pour les opérations asynchrones en base de données
using Microsoft.EntityFrameworkCore;
// Importation des modèles du projet (UserCourse, Utilisateur, etc.)
using seragenda.Models;
// Importation du support des Claims pour extraire l'email de l'utilisateur depuis le JWT
using System.Security.Claims;

namespace seragenda.Controllers
{
    // Toutes les routes sont préfixées par /api/courses
    [Route("api/[controller]")]
    // Marque cette classe comme contrôleur API
    [ApiController]
    // Tous les points de terminaison nécessitent un jeton JWT valide — l'accès anonyme est refusé
    [Authorize]
    // Gère les entrées de planning de cours récurrents pour l'utilisateur authentifié.
    // Un "cours" représente ici un bloc de classe répétitif avec un créneau horaire, des jours de la semaine,
    // une plage de dates (semestre/année), un nom et une couleur d'affichage.
    // Les "jours de la semaine" sont encodés sous forme de masque de bits (Lundi=1, Mardi=2, Mercredi=4, ...).
    public class CoursesController : ControllerBase
    {
        // Contexte de base de données Entity Framework pour lire et écrire les enregistrements UserCourse
        private readonly AgendaContext _context;

        // Constructeur — reçoit le contexte de base de données par injection de dépendances.
        // context : le contexte de base de données EF Core
        public CoursesController(AgendaContext context)
        {
            _context = context;
        }

        // Résout la clé primaire entière de l'utilisateur actuellement authentifié
        // en recherchant son adresse email (stockée comme claim Name du JWT) en base de données.
        // Retourne null si le claim est absent ou si l'utilisateur n'existe pas.
        // Retourne l'IdUser de l'utilisateur, ou null s'il est introuvable
        private async Task<int?> GetUserId()
        {
            // Le claim Name a été défini sur l'email de l'utilisateur au moment de la connexion
            var email = User.FindFirst(ClaimTypes.Name)?.Value;
            // Si le claim est absent, l'utilisateur ne peut pas être identifié
            if (email == null) return null;
            // Recherche de l'enregistrement utilisateur correspondant à l'email
            var user = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == email);
            // Retourne uniquement la clé primaire entière, ou null si aucune correspondance
            return user?.IdUser;
        }

        // GET /api/courses/date/{date}
        // Retourne tous les cours planifiés à une date calendaire spécifique pour l'utilisateur courant
        [HttpGet("date/{date}")]
        // Récupère toutes les entrées de cours qui ont lieu à une date donnée.
        // Un cours a lieu à une date si :
        // 1. La date tombe dans la plage StartDate–EndDate du cours, ET
        // 2. Le jour de la semaine correspond à l'un des bits définis dans le masque DaysOfWeek.
        // date : la date cible (analysée depuis le segment de route)
        public async Task<IActionResult> GetCoursesForDate(DateTime date)
        {
            // Identification de l'utilisateur demandeur
            var userId = await GetUserId();
            // Retourne 401 si l'identité de l'utilisateur ne peut pas être résolue
            if (userId == null) return Unauthorized();

            // Détermination du bit de jour de la semaine correspondant à la date demandée
            var dayOfWeek = date.DayOfWeek;

            // Correspondance de chaque jour de la semaine avec sa valeur de drapeau de masque de bits
            // Ces valeurs correspondent à la convention utilisée lors de l'enregistrement des cours
            int dayFlag = dayOfWeek switch
            {
                DayOfWeek.Monday    => 1,   // bit 0
                DayOfWeek.Tuesday   => 2,   // bit 1
                DayOfWeek.Wednesday => 4,   // bit 2
                DayOfWeek.Thursday  => 8,   // bit 3
                DayOfWeek.Friday    => 16,  // bit 4
                DayOfWeek.Saturday  => 32,  // bit 5
                DayOfWeek.Sunday    => 64,  // bit 6
                _                   => 0   // Ne devrait jamais se produire (toutes les valeurs d'enum sont couvertes)
            };

            // Récupération de tous les cours de cet utilisateur actifs à la date demandée
            // (c'est-à-dire que la date tombe dans la plage de dates semestrielles du cours)
            var courses = await _context.UserCourses
                .Where(c => c.IdUserFk == userId && c.StartDate <= date && c.EndDate >= date)
                .ToListAsync();

            // Application du filtre de masque de bits du jour de la semaine en mémoire
            // (le ET binaire n'est pas facilement traduit en SQL dans tous les fournisseurs, donc on filtre après récupération)
            var filtered = courses.Where(c => (c.DaysOfWeek & dayFlag) != 0).ToList();

            return Ok(filtered);
        }

        // GET /api/courses
        // Retourne toutes les entrées de cours créées par l'utilisateur courant (pour la configuration/gestion du calendrier)
        [HttpGet]
        // Récupère toutes les entrées de planning de cours appartenant à l'utilisateur courant.
        // Utilisé par la vue de paramètres/gestion pour lister et modifier les cours récurrents.
        public async Task<IActionResult> GetAll()
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            // Retourne tous les cours appartenant à cet utilisateur, dans l'ordre de la base de données
            var courses = await _context.UserCourses
                .Where(c => c.IdUserFk == userId)
                .ToListAsync();

            return Ok(courses);
        }

        // POST /api/courses
        // Crée une nouvelle entrée de cours ou met à jour une existante (pattern upsert basé sur Id == 0)
        [HttpPost]
        // Crée une nouvelle entrée de cours si l'Id soumis est 0,
        // ou met à jour une entrée existante si un Id non nul est fourni.
        // L'IdUserFk est toujours écrasé avec l'ID de l'utilisateur courant pour empêcher
        // un utilisateur de modifier les cours d'un autre utilisateur.
        // course : les données de cours à sauvegarder
        public async Task<IActionResult> Save([FromBody] UserCourse course)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            // Force le propriétaire à être l'utilisateur actuellement authentifié, indépendamment de ce que le client a envoyé
            course.IdUserFk = userId.Value;

            if (course.Id == 0)
            {
                // Id == 0 signifie que c'est un nouvel enregistrement — l'ajouter au contexte
                _context.UserCourses.Add(course);
            }
            else
            {
                // Id non nul — recherche de l'enregistrement existant et vérification qu'il appartient à cet utilisateur
                var existing = await _context.UserCourses
                    .FirstOrDefaultAsync(c => c.Id == course.Id && c.IdUserFk == userId);
                // Retourne 404 si l'enregistrement n'existe pas ou appartient à quelqu'un d'autre
                if (existing == null) return NotFound();

                // Mise à jour uniquement des champs modifiables ; l'Id et l'IdUserFk ne sont intentionnellement pas modifiés
                existing.Name       = course.Name;
                existing.Color      = course.Color;
                existing.StartDate  = course.StartDate;
                existing.EndDate    = course.EndDate;
                existing.StartTime  = course.StartTime;
                existing.EndTime    = course.EndTime;
                existing.DaysOfWeek = course.DaysOfWeek;
            }

            // Persistance de l'insertion ou de la mise à jour en base de données
            await _context.SaveChangesAsync();
            // Retourne le cours sauvegardé (avec son nouvel Id s'il s'agissait d'une création)
            return Ok(course);
        }

        // DELETE /api/courses/{id}
        // Supprime définitivement une entrée de cours appartenant à l'utilisateur courant
        [HttpDelete("{id}")]
        // Supprime une entrée de cours par son ID.
        // Vérifie que l'entrée appartient à l'utilisateur demandeur avant la suppression.
        // id : la clé primaire du cours à supprimer
        public async Task<IActionResult> Delete(int id)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            // Recherche du cours correspondant à la fois à l'ID donné et à l'ID de l'utilisateur courant
            // Cela empêche un utilisateur de supprimer le cours d'un autre utilisateur en devinant un ID
            var course = await _context.UserCourses
                .FirstOrDefaultAsync(c => c.Id == id && c.IdUserFk == userId);
            if (course == null) return NotFound();

            // Suppression de l'entité et persistance de la suppression
            _context.UserCourses.Remove(course);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
