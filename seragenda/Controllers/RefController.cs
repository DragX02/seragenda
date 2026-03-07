// Importation des attributs d'autorisation ASP.NET Core
using Microsoft.AspNetCore.Authorization;
// Importation des types de base pour les contrôleurs MVC/API et les helpers de résultat
using Microsoft.AspNetCore.Mvc;
// Importation d'Entity Framework Core pour les requêtes asynchrones en base de données
using Microsoft.EntityFrameworkCore;

namespace seragenda.Controllers
{
    // Les routes de ce contrôleur sont préfixées par /api/ref (chemin explicite, sans le jeton [controller])
    [Route("api/ref")]
    // Indique que cette classe est un contrôleur d'API
    [ApiController]
    // Tous les points de terminaison nécessitent un token JWT valide
    [Authorize]
    // Fournit des données de référence en lecture seule utilisées pour alimenter les listes de sélection en cascade côté client.
    // Expose quatre lookups liés : catégories, cours (filtrés par catégorie), niveaux et domaines.
    // Utilisation prévue :
    //   1. Charger toutes les catégories → l'utilisateur en choisit une.
    //   2. Charger les cours de la catégorie sélectionnée → l'utilisateur en choisit un.
    //   3. Charger les niveaux du cours sélectionné → l'utilisateur en choisit un.
    //   4. Charger les domaines du cours + niveau sélectionnés → l'utilisateur en choisit un.
    public class RefController : ControllerBase
    {
        // Contexte de base de données Entity Framework pour interroger les tables de référence
        private readonly AgendaContext _context;

        // Constructeur — reçoit le contexte de base de données par injection de dépendances.
        // Paramètre context : le contexte de base de données EF Core
        public RefController(AgendaContext context)
        {
            _context = context;
        }

        // GET /api/ref/categories
        // Retourne toutes les catégories de matières, triées par leur ordre d'affichage
        [HttpGet("categories")]
        // Récupère tous les enregistrements de catégories depuis la table categorie_cours.
        // Retourne l'identifiant, le nom et l'ordre de tri pour chaque catégorie.
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.CategorieCours
                // Trier par la colonne d'ordre explicite pour que la liste déroulante corresponde à la séquence prévue
                .OrderBy(c => c.Ordre)
                .Select(c => new { c.IdCat, c.NomCat, c.Ordre })
                .ToListAsync();

            return Ok(categories);
        }

        // GET /api/ref/cours/{idCat}
        // Retourne tous les cours appartenant à une catégorie spécifique, triés alphabétiquement
        [HttpGet("cours/{idCat:int}")]
        // Récupère tous les enregistrements de cours (matières) appartenant à une catégorie donnée.
        // Retourne uniquement les champs nécessaires à l'affichage et aux lookups suivants :
        // le code unique, le nom d'affichage et la couleur de l'agenda.
        // Paramètre idCat : la clé primaire de la catégorie à filtrer
        public async Task<IActionResult> GetCours(int idCat)
        {
            var cours = await _context.Cours
                // Filtrer uniquement les matières appartenant à la catégorie demandée
                .Where(c => c.IdCatFk == idCat)
                // Trier alphabétiquement pour que la liste déroulante soit ordonnée
                .OrderBy(c => c.NomCours)
                // Projeter uniquement les colonnes dont le client a besoin ; évite la sur-récupération
                .Select(c => new { c.CodeCours, c.NomCours, c.CouleurAgenda })
                .ToListAsync();

            return Ok(cours);
        }

        // GET /api/ref/niveaux/{codeCours}
        // Retourne les niveaux d'enseignement distincts disponibles pour un code de cours donné
        [HttpGet("niveaux/{codeCours}")]
        // Récupère tous les niveaux scolaires (années) associés à un cours spécifique.
        // La liste des niveaux est dérivée de la table de liaison many-to-many CoursNiveau.
        // Les doublons sont supprimés avec Distinct() au cas où plusieurs professeurs enseignent au même niveau.
        // Paramètre codeCours : le code unique du cours (ex. : "MATH", "FR")
        public async Task<IActionResult> GetNiveaux(string codeCours)
        {
            var niveaux = await _context.CoursNiveaus
                // Filtrer les enregistrements de liaison où le cours associé correspond au code demandé
                .Where(cn => cn.IdCoursFkNavigation.CodeCours == codeCours)
                // Naviguer à travers la table de liaison vers l'entité Niveau
                .Select(cn => cn.IdNiveauFkNavigation)
                // Supprimer les doublons générés par plusieurs professeurs enseignant au même niveau
                .Distinct()
                // Trier par le code de niveau pour un ordre cohérent
                .OrderBy(n => n.CodeNiveau)
                // Projeter uniquement les champs nécessaires à l'affichage et aux lookups ultérieurs
                .Select(n => new { n.CodeNiveau, n.NomNiveau })
                .ToListAsync();

            return Ok(niveaux);
        }

        // GET /api/ref/domaines/{codeCours}/{codeNiveau}
        // Retourne les domaines pour une combinaison cours + niveau donnée
        [HttpGet("domaines/{codeCours}/{codeNiveau}")]
        // Récupère tous les domaines pédagogiques pour une combinaison cours et niveau spécifique.
        // Les domaines sont des zones spécifiques du programme au sein d'un cours pour un niveau donné
        // (ex. : "Algèbre" dans "Mathématiques" en "3ème année").
        // Paramètre codeCours : le code unique du cours
        // Paramètre codeNiveau : le code unique du niveau scolaire
        public async Task<IActionResult> GetDomaines(string codeCours, string codeNiveau)
        {
            var domaines = await _context.CoursNiveaus
                // Trouver les enregistrements de liaison correspondant à la fois au cours et au niveau
                .Where(cn =>
                    cn.IdCoursFkNavigation.CodeCours   == codeCours &&
                    cn.IdNiveauFkNavigation.CodeNiveau == codeNiveau)
                // Aplatir la collection d'enfants Domaine de chaque enregistrement CoursNiveau correspondant
                .SelectMany(cn => cn.Domaines)
                // Trier alphabétiquement pour la liste déroulante
                .OrderBy(d => d.Nom)
                // Projeter uniquement l'identifiant et le nom d'affichage
                .Select(d => new { d.IdDom, d.Nom })
                .ToListAsync();

            return Ok(domaines);
        }
    }
}
