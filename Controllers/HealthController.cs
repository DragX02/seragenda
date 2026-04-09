// Importation des attributs d'autorisation ASP.NET Core
using Microsoft.AspNetCore.Authorization;
// Importation des types de contrôleur MVC/API de base et des helpers de résultat
using Microsoft.AspNetCore.Mvc;

namespace seragenda.Controllers
{
    // Marque cette classe comme contrôleur API
    [ApiController]
    // Les routes de ce contrôleur sont préfixées par /api/health
    [Route("api/[controller]")]
    // Le contrôle de santé doit être accessible publiquement — aucun JWT requis (utilisé par les moniteurs de disponibilité, les équilibreurs de charge, etc.)
    [AllowAnonymous]
    // Fournit un point de terminaison de contrôle de santé léger pour la surveillance des infrastructures.
    // Les moniteurs de disponibilité et les équilibreurs de charge peuvent interroger GET /api/health pour vérifier
    // que le processus API est en cours d'exécution et accessible.
    public class HealthController : ControllerBase
    {
        // GET /api/health
        // Retourne un payload JSON simple confirmant que l'API est en ligne
        [HttpGet]
        // Retourne un objet JSON avec le statut courant du serveur, l'horodatage UTC,
        // le nom du serveur et le numéro de version de l'API.
        // Une réponse 200 OK indique que le service est en bonne santé.
        public IActionResult Get()
        {
            return Ok(new
            {
                // Indicateur de statut lisible — toujours "online" quand ce code s'exécute
                status    = "online",
                // Heure UTC actuelle pour que l'appelant puisse vérifier que l'horloge du serveur est raisonnable
                timestamp = DateTime.UtcNow,
                // Identifie quelle application répond (utile dans les déploiements multi-services)
                server    = "AgendaProf API",
                // Version sémantique de ce déploiement de l'API
                version   = "1.0.0"
            });
        }
    }
}
