using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace seragenda.Controllers
{
    [Route("api/referentiel")]
    [ApiController]
    [Authorize]
    public class ReferentielController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public ReferentielController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet]
        public IActionResult GetList()
        {
            var dossier = Path.Combine(_env.ContentRootPath, "Referentiel");

            if (!Directory.Exists(dossier))
                return Ok(Array.Empty<string>());

            var fichiers = Directory.GetFiles(dossier, "*.pdf")
                .Select(Path.GetFileName)
                .OrderBy(f => f)
                .ToList();

            return Ok(fichiers);
        }

        [HttpGet("{nomFichier}")]
        public IActionResult GetPdf(string nomFichier)
        {
            var nomSur = Path.GetFileName(nomFichier);
            var dossier = Path.Combine(_env.ContentRootPath, "Referentiel");
            var chemin = Path.Combine(dossier, nomSur);

            if (!System.IO.File.Exists(chemin))
                return NotFound();

            var flux = System.IO.File.OpenRead(chemin);
            return File(flux, "application/pdf", nomSur);
        }
    }
}
