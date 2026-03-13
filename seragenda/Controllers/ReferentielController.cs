// Importation des attributs d'autorisation ASP.NET Core
using Microsoft.AspNetCore.Authorization;
// Importation des types de base pour les contrôleurs et les résultats HTTP
using Microsoft.AspNetCore.Mvc;

namespace seragenda.Controllers
{
    // Route de base pour tous les endpoints de ce contrôleur
    [Route("api/referentiel")]
    // Indique qu'il s'agit d'un contrôleur d'API REST
    [ApiController]
    // Tous les endpoints nécessitent un token JWT valide
    [Authorize]
    // Expose les fichiers PDF du dossier Referentiel pour le lecteur intégré du frontend.
    // Deux opérations : lister les fichiers disponibles et en télécharger un spécifique.
    public class ReferentielController : ControllerBase
    {
        // Environnement d'hébergement pour résoudre le chemin physique du dossier Referentiel
        private readonly IWebHostEnvironment _env;

        // Constructeur — reçoit l'environnement d'hébergement par injection de dépendances
        public ReferentielController(IWebHostEnvironment env)
        {
            _env = env;
        }

        // GET api/referentiel
        // Retourne la liste triée des noms de fichiers PDF présents dans le dossier Referentiel
        [HttpGet]
        public IActionResult GetList()
        {
            // Chemin absolu vers le dossier Referentiel à partir de la racine du projet
            var dossier = Path.Combine(_env.ContentRootPath, "Referentiel");

            // Si le dossier n'existe pas encore, retourne une liste vide plutôt qu'une erreur
            if (!Directory.Exists(dossier))
                return Ok(Array.Empty<string>());

            // Récupère uniquement les fichiers .pdf et les trie alphabétiquement
            var fichiers = Directory.GetFiles(dossier, "*.pdf")
                .Select(Path.GetFileName)
                .OrderBy(f => f)
                .ToList();

            return Ok(fichiers);
        }

        // GET api/referentiel/{nomFichier}
        // Sert le fichier PDF demandé avec le type MIME application/pdf
        [HttpGet("{nomFichier}")]
        public IActionResult GetPdf(string nomFichier)
        {
            // Sécurisation : extraction du nom seul pour bloquer les attaques de traversée de dossier
            // ex. : "../../secret.config" devient "secret.config", puis introuvable dans Referentiel
            var nomSur = Path.GetFileName(nomFichier);
            var dossier = Path.Combine(_env.ContentRootPath, "Referentiel");
            var chemin = Path.Combine(dossier, nomSur);

            // Retourne 404 si le fichier demandé n'existe pas dans le dossier autorisé
            if (!System.IO.File.Exists(chemin))
                return NotFound();

            // Ouvre un flux en lecture et le retourne avec le type MIME correct
            var flux = System.IO.File.OpenRead(chemin);
            return File(flux, "application/pdf", nomSur);
        }
    }
}
