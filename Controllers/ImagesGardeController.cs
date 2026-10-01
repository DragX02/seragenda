using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace seragenda.Controllers
{
    [Route("api/imagesgarde")]
    [ApiController]
    [Authorize]
    public class ImagesGardeController : ControllerBase
    {
        private readonly AgendaContext _context;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        private static readonly List<string> OrdreCategories = new List<string>
        {
            "Nombres", "Écrire", "Lire", "Parler", "Écouter", "Géométrie", "Calculer", "Tracer",
            "Langues", "Traitement des données (problèmes)", "Géographie", "Histoire"
        };

        private const int TailleMax = 1500000;

        public ImagesGardeController(AgendaContext context, IConfiguration config, IWebHostEnvironment env)
        {
            _context = context;
            _config = config;
            _env = env;
        }

        public class ImageDto
        {
            public string Source { get; set; } = "";
            public string Nom { get; set; } = "";
            public string Categorie { get; set; } = "";
            public bool Perso { get; set; }
        }

        public class EnvoiImage
        {
            public string Nom { get; set; } = "";
            public string TypeMime { get; set; } = "";
            public string DonneesBase64 { get; set; } = "";
        }

        private async Task<int?> GetUserId()
        {
            var email = User.FindFirst(ClaimTypes.Name)?.Value;
            if (email == null) return null;
            var user = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == email);
            return user?.IdUser;
        }

        private string DossierReference()
        {
            return Path.Combine(AppContext.BaseDirectory, "ImagesGarde");
        }

        private string DossierPerso(int userId)
        {
            string dossier = _config["ImagesGarde:DossierPerso"] ?? "ImagesPerso";
            if (!Path.IsPathRooted(dossier))
                dossier = Path.Combine(_env.ContentRootPath, dossier);
            return Path.Combine(dossier, userId.ToString());
        }

        private int RangCategorie(string categorie)
        {
            int rang = OrdreCategories.IndexOf(categorie);
            if (rang == -1)
                return 1000;
            return rang;
        }

        private string NomPerso(string fichier)
        {
            string nom = Path.GetFileNameWithoutExtension(fichier);
            int position = nom.LastIndexOf('_');
            if (position > 0)
                nom = nom.Substring(0, position);
            return nom;
        }

        private string NettoyerNom(string nom)
        {
            string resultat = "";
            foreach (char c in nom)
            {
                if (char.IsLetterOrDigit(c) || c == ' ' || c == '-')
                    resultat = resultat + c;
            }
            resultat = resultat.Trim();
            if (resultat == "")
                resultat = "Image";
            if (resultat.Length > 60)
                resultat = resultat.Substring(0, 60);
            return resultat;
        }

        private string? CheminDepuisSource(string source, int userId)
        {
            if (string.IsNullOrEmpty(source) || source.Contains(".."))
                return null;

            string[] morceaux = source.Split('/');

            if (morceaux.Length == 3 && morceaux[0] == "ref")
            {
                string dossier = DossierReference();
                string categorie = morceaux[1];
                string fichier = Path.GetFileName(morceaux[2]);
                if (!Directory.Exists(Path.Combine(dossier, categorie)))
                    return null;
                if (Path.GetFileName(categorie) != categorie)
                    return null;
                return Path.Combine(dossier, categorie, fichier);
            }

            if (morceaux.Length == 2 && morceaux[0] == "perso")
            {
                string fichier = Path.GetFileName(morceaux[1]);
                return Path.Combine(DossierPerso(userId), fichier);
            }

            return null;
        }

        [HttpGet]
        public async Task<IActionResult> GetImages()
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var images = new List<ImageDto>();

                string dossierRef = DossierReference();
                if (Directory.Exists(dossierRef))
                {
                    var categories = Directory.GetDirectories(dossierRef)
                        .Select(d => Path.GetFileName(d))
                        .OrderBy(c => RangCategorie(c))
                        .ThenBy(c => c)
                        .ToList();

                    foreach (var categorie in categories)
                    {
                        var fichiers = Directory.GetFiles(Path.Combine(dossierRef, categorie), "*.png")
                            .Select(f => Path.GetFileName(f))
                            .OrderBy(f => f.Length)
                            .ThenBy(f => f)
                            .ToList();

                        foreach (var fichier in fichiers)
                        {
                            var image = new ImageDto();
                            image.Source = "ref/" + categorie + "/" + fichier;
                            image.Nom = Path.GetFileNameWithoutExtension(fichier);
                            image.Categorie = categorie;
                            image.Perso = false;
                            images.Add(image);
                        }
                    }
                }

                string dossierPerso = DossierPerso(userId.Value);
                if (Directory.Exists(dossierPerso))
                {
                    var fichiers = Directory.GetFiles(dossierPerso)
                        .Where(f => f.EndsWith(".png") || f.EndsWith(".jpg"))
                        .OrderBy(f => System.IO.File.GetCreationTimeUtc(f))
                        .ToList();

                    foreach (var f in fichiers)
                    {
                        var image = new ImageDto();
                        image.Source = "perso/" + Path.GetFileName(f);
                        image.Nom = NomPerso(f);
                        image.Categorie = "Mes images";
                        image.Perso = true;
                        images.Add(image);
                    }
                }

                return Ok(images);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erreur images : " + ex.Message);
            }
        }

        [HttpGet("fichier")]
        public async Task<IActionResult> GetFichier([FromQuery] string source)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            string? chemin = CheminDepuisSource(source, userId.Value);
            if (chemin == null || !System.IO.File.Exists(chemin))
                return NotFound();

            string type = "image/png";
            if (chemin.EndsWith(".jpg"))
                type = "image/jpeg";

            return PhysicalFile(chemin, type);
        }

        [HttpPost("perso")]
        public async Task<IActionResult> EnvoyerPerso([FromBody] EnvoiImage envoi)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            string extension;
            if (envoi.TypeMime == "image/png")
                extension = ".png";
            else if (envoi.TypeMime == "image/jpeg")
                extension = ".jpg";
            else
                return BadRequest(new { message = "Seules les images PNG et JPEG sont acceptées." });

            byte[] octets;
            try
            {
                octets = Convert.FromBase64String(envoi.DonneesBase64);
            }
            catch
            {
                return BadRequest(new { message = "Image invalide." });
            }

            if (octets.Length == 0 || octets.Length > TailleMax)
                return BadRequest(new { message = "L'image est vide ou trop grande (1,5 Mo maximum)." });

            try
            {
                string dossier = DossierPerso(userId.Value);
                Directory.CreateDirectory(dossier);

                string code = Guid.NewGuid().ToString("N").Substring(0, 10);
                string fichier = NettoyerNom(envoi.Nom) + "_" + code + extension;
                await System.IO.File.WriteAllBytesAsync(Path.Combine(dossier, fichier), octets);

                var image = new ImageDto();
                image.Source = "perso/" + fichier;
                image.Nom = NomPerso(fichier);
                image.Categorie = "Mes images";
                image.Perso = true;
                return Ok(image);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erreur images : " + ex.Message);
            }
        }

        [HttpDelete("perso")]
        public async Task<IActionResult> SupprimerPerso([FromQuery] string source)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            if (string.IsNullOrEmpty(source) || !source.StartsWith("perso/"))
                return NotFound();

            string? chemin = CheminDepuisSource(source, userId.Value);
            if (chemin == null || !System.IO.File.Exists(chemin))
                return NotFound();

            try
            {
                System.IO.File.Delete(chemin);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erreur images : " + ex.Message);
            }
        }
    }
}
