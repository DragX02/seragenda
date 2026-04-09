// Importation des attributs d'autorisation ASP.NET Core
using Microsoft.AspNetCore.Authorization;
// Importation des types de contrôleur MVC/API de base et des helpers de résultat
using Microsoft.AspNetCore.Mvc;
// Importation d'Entity Framework Core pour les requêtes asynchrones en base de données et les opérations de comptage
using Microsoft.EntityFrameworkCore;
// Importation des modèles du projet (ViseesMaitriser)
using seragenda.Models;

// Déclaration d'espace de noms à portée de fichier (style C# 10+)
namespace seragenda.Controllers;

// Toutes les routes de ce contrôleur sont préfixées par /api/ViseesMaitriser
[Route("api/[controller]")]
// Marque cette classe comme contrôleur API
[ApiController]
// Tous les points de terminaison nécessitent un jeton JWT valide
[Authorize]
// Fournit un accès en lecture paginé à la table de référence ViseesMaitriser (objectifs de maîtrise).
// Les enregistrements ViseesMaitriser représentent des résultats éducatifs de haut niveau ou des objectifs de maîtrise
// définis dans le programme scolaire et référencés tout au long du flux de planification des cours.
public class ViseesMaitriserController : ControllerBase
{
    // Contexte de base de données Entity Framework pour interroger les enregistrements ViseesMaitriser
    private readonly AgendaContext _context;

    // Constructeur — reçoit le contexte de base de données par injection de dépendances.
    // context : le contexte de base de données EF Core
    public ViseesMaitriserController(AgendaContext context)
    {
        _context = context;
    }

    // GET /api/ViseesMaitriser?page=1&limit=50
    // Retourne une liste paginée d'enregistrements d'objectifs de maîtrise
    [HttpGet]
    // Récupère une page paginée d'enregistrements ViseesMaitriser.
    // Retourne le nombre total d'éléments avec les données de la page courante pour que le client
    // puisse afficher les contrôles de pagination sans appel COUNT séparé.
    // page : numéro de page basé sur 1 (par défaut 1)
    // limit : nombre maximum d'enregistrements par page (par défaut 50)
    public async Task<IActionResult> Get(int page = 1, int limit = 50)
    {
        // Comptage du total des enregistrements pour les métadonnées de pagination (s'exécute comme une requête COUNT unique)
        var total = await _context.ViseesMaitrisers.CountAsync();

        // Récupération de la page de données demandée
        var donnees = await _context.ViseesMaitrisers
            // Tri par clé primaire pour garantir un ordre stable et déterministe entre les pages
            .OrderBy(v => v.IdViseesMaitriser)
            // Saut des lignes des pages précédentes
            .Skip((page - 1) * limit)
            // Prise uniquement des lignes de la page courante
            .Take(limit)
            // Projection vers un type anonyme avec uniquement les champs dont le client a besoin
            .Select(v => new
            {
                v.IdViseesMaitriser,
                v.NomViseesMaitriser
            })
            // AsNoTracking() indique à EF de ne pas suivre ces entités dans le tracker de changements,
            // améliorant les performances pour les requêtes en lecture seule
            .AsNoTracking()
            .ToListAsync();

        // Retourne les métadonnées de pagination avec les données de la page
        return Ok(new
        {
            TotalItems  = total,   // Nombre total d'enregistrements sur toutes les pages
            CurrentPage = page,    // La page qui a été demandée
            PageSize    = limit,   // Le nombre maximum d'éléments par page
            Data        = donnees  // Les enregistrements réels pour cette page
        });
    }
}
