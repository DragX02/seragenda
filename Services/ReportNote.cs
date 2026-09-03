// Espace de noms limité au fichier (style C# 10+)
namespace seragenda.Services;

// ─────────────────────────────────────────────────────────────────────────────
// Mention de report portée par une note.
//
// Reporter une leçon ne la déplace pas : elle est recopiée sur la date choisie,
// et l'originale garde la mention du report pour qu'on voie, sur la semaine
// d'origine, où la leçon est repartie.
//
// Cette mention vit dans le texte de la note, sur une dernière ligne dédiée
// ("↪ Reporté au 06/10/2025"), et non dans une colonne dédiée : le schéma de
// production se modifie à la main.
//
// L'écriture du marqueur appartient au serveur : lui seul connaît le contenu
// réellement stocké, et lui seul peut garantir que la mention survit à une
// modification de la note ou à la troncature des 2000 caractères. Le client
// garde une copie des seules fonctions de LECTURE, dont il a besoin pour
// afficher la note et l'exporter en PDF (Obrigenie/Services/ReportNote.cs).
// ─────────────────────────────────────────────────────────────────────────────
public static class ReportNote
{
    // Début de la ligne de marqueur. La flèche sert de repère visuel dans la
    // base ; la reconnaissance, elle, ne dépend que du mot "Reporté au".
    private const string Fleche = "↪";

    // Mot-clé reconnu, avec et sans accent : selon la saisie, une note relue
    // puis réenregistrée peut avoir perdu ses accents en cours de route.
    private static readonly string[] Cles = { "Reporté au ", "Reporte au " };

    // Format de la date écrite dans le marqueur
    public const string FormatDate = "dd/MM/yyyy";

    // Le marqueur est écrit et relu en culture invariante : la note voyage entre
    // navigateurs, et une date écrite avec le séparateur d'une culture serait
    // illisible par une autre.
    private static readonly System.Globalization.CultureInfo Neutre =
        System.Globalization.CultureInfo.InvariantCulture;

    // Séparateurs acceptés à la relecture, pour rattraper les marqueurs écrits
    // avant cette règle par une culture qui n'utilisait pas la barre oblique.
    private static readonly string[] FormatsLus = { "dd/MM/yyyy", "dd-MM-yyyy", "dd.MM.yyyy" };

    // Longueur maximale du contenu acceptée en base ; au-delà, le contrôleur tronque.
    // Le texte libre cède la place au marqueur plutôt que l'inverse, sans quoi
    // la troncature couperait la mention de report.
    public const int MaxContenu = 2000;

    // Sépare le texte libre de la note de son éventuelle mention de report.
    // Retourne le texte débarrassé du marqueur et la date cible quand il y en a une.
    public static (string Texte, DateTime? Cible) Lire(string? content)
    {
        if (string.IsNullOrEmpty(content)) return (string.Empty, null);

        var lignes = content.Replace("\r\n", "\n").Split('\n');
        var gardees = new List<string>(lignes.Length);
        DateTime? cible = null;

        foreach (var ligne in lignes)
        {
            var date = DateMarqueur(ligne);
            if (date != null)
            {
                // Plusieurs marqueurs ne devraient pas coexister ; si cela arrive,
                // le dernier écrit fait foi et les autres sont écartés du texte.
                cible = date;
                continue;
            }
            gardees.Add(ligne);
        }

        // Le marqueur était précédé d'une ligne vide : elle n'a plus lieu d'être
        return (string.Join("\n", gardees).TrimEnd('\n', ' '), cible);
    }

    // Texte libre seul, sans la mention de report.
    public static string Texte(string? content) => Lire(content).Texte;

    // Date de report portée par la note, ou null quand elle n'a pas été reportée.
    public static DateTime? Cible(string? content) => Lire(content).Cible;

    // Réécrit le contenu avec la mention de report vers `cible`.
    // Une mention déjà présente est remplacée : reporter deux fois une leçon
    // laisse une seule ligne, celle du dernier report.
    public static string Marquer(string? content, DateTime cible)
    {
        var texte = Texte(content);
        var marqueur = Libelle(cible);

        if (string.IsNullOrEmpty(texte)) return marqueur;

        // Le texte libre est rogné si nécessaire pour que le marqueur tienne
        // dans les 2000 caractères acceptés en base.
        int place = MaxContenu - marqueur.Length - 1;
        if (place < 0) return marqueur;
        if (texte.Length > place) texte = texte[..place];

        return $"{texte}\n{marqueur}";
    }

    // Forme exacte du marqueur écrit dans le texte de la note.
    public static string Libelle(DateTime cible)
        => $"{Fleche} Reporté au {cible.ToString(FormatDate, Neutre)}";

    // Reconnaît une ligne de marqueur et en extrait la date, ou null si la ligne
    // est du texte ordinaire.
    private static DateTime? DateMarqueur(string ligne)
    {
        var t = ligne.Trim().TrimStart(Fleche[0], '>', '-', ' ');

        foreach (var cle in Cles)
        {
            if (!t.StartsWith(cle, StringComparison.OrdinalIgnoreCase)) continue;

            var reste = t[cle.Length..].Trim();
            if (DateTime.TryParseExact(reste, FormatsLus, Neutre,
                    System.Globalization.DateTimeStyles.None, out var date))
                return date;

            // Mot-clé reconnu mais date illisible : la ligne reste du texte,
            // mieux vaut l'afficher telle quelle que la faire disparaître.
            return null;
        }

        return null;
    }
}
