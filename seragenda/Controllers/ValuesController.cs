// Importation des attributs d'autorisation ASP.NET Core
using Microsoft.AspNetCore.Authorization;
// Importation des types de contrôleur MVC/API de base et des helpers de résultat
using Microsoft.AspNetCore.Mvc;
// Importation d'Entity Framework Core pour les requêtes asynchrones en base de données
using Microsoft.EntityFrameworkCore;
// Importation des modèles du projet (CalendrierScolaire)
using seragenda.Models;

// Déclaration d'espace de noms à portée de fichier (style C# 10+)
namespace seragenda.Controllers;

// Toutes les routes de ce contrôleur sont préfixées par /api/values
[Route("api/[controller]")]
// Marque cette classe comme contrôleur API
[ApiController]
// Tous les points de terminaison nécessitent un jeton JWT valide
[Authorize]
// Expose le calendrier scolaire (événements de l'année académique et jours fériés) aux utilisateurs authentifiés.
// Les données proviennent de la table CalendrierScolaire, qui est alimentée
// par le service en arrière-plan ScolaireScraper.
public class ValuesController : ControllerBase
{
    // Contexte de base de données Entity Framework pour interroger la table du calendrier scolaire
    private readonly AgendaContext _context;

    // Constructeur — reçoit le contexte de base de données par injection de dépendances.
    // context : le contexte de base de données EF Core
    public ValuesController(AgendaContext context)
    {
        _context = context;
    }

    // GET /api/values
    // Retourne toutes les entrées du calendrier scolaire ordonnées par date de début (croissant)
    [HttpGet]
    // Récupère toutes les entrées du calendrier scolaire (vacances, rentrées, jours fériés, etc.),
    // ordonnées chronologiquement par leur date de début.
    public async Task<IActionResult> Get()
    {
        // Récupération de toutes les entrées du calendrier triées pour que le client les reçoive dans l'ordre chronologique
        var donnees = await _context.CalendrierScolaires
                                    .OrderBy(d => d.DateDebut)
                                    .ToListAsync();
        return Ok(donnees);
    }
}
