using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace seragenda.Controllers
{
    [Route("api/emojis")]
    [ApiController]
    [Authorize]
    public class EmojisController : ControllerBase
    {
        private static List<EmojiDto>? liste;

        [HttpGet]
        public IActionResult GetEmojis()
        {
            if (liste == null)
            {
                string chemin = Path.Combine(AppContext.BaseDirectory, "Emojis", "fr-compact.json");
                if (!System.IO.File.Exists(chemin))
                    return NotFound("Fichier emojis introuvable");

                string json = System.IO.File.ReadAllText(chemin);
                var tous = JsonSerializer.Deserialize<List<EmojibaseEmoji>>(json) ?? new List<EmojibaseEmoji>();

                var resultat = new List<EmojiDto>();
                foreach (var e in tous)
                {
                    if (e.group == null || e.group == 2)
                        continue;

                    var dto = new EmojiDto();
                    dto.Emoji = e.unicode ?? "";
                    dto.Nom = e.label ?? "";
                    dto.Groupe = e.group.Value;
                    dto.Ordre = e.order ?? 0;
                    resultat.Add(dto);
                }

                liste = resultat.OrderBy(x => x.Ordre).ToList();
            }

            return Ok(liste);
        }

        public class EmojibaseEmoji
        {
            public string? unicode { get; set; }
            public string? label { get; set; }
            public int? group { get; set; }
            public int? order { get; set; }
        }

        public class EmojiDto
        {
            public string Emoji { get; set; } = "";
            public string Nom { get; set; } = "";
            public int Groupe { get; set; }
            public int Ordre { get; set; }
        }
    }
}
