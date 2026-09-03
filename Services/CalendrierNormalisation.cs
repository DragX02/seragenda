// Espace de noms limité au fichier (style C# 10+)
namespace seragenda.Services;

// ─────────────────────────────────────────────────────────────────────────────
// Reconnaissance d'un congé derrière ses variantes de libellé.
//
// Le calendrier officiel décrit régulièrement le même congé deux fois, sous deux
// écritures : "Vacances d'automne (Toussaint)" et "Congé d'automne (Toussaint)",
// "Jour de l'Armistice" et "Commémoration de l'Armistice". Le scraper ne comparait
// que le nom exact et la date de début : les deux écritures entraient donc toutes
// les deux, et chaque client devait ensuite les rapprocher pour ne pas afficher le
// congé à deux endroits.
//
// La normalisation appartient à l'ingestion : c'est là qu'un doublon peut être
// écarté une fois pour toutes, plutôt que dans chaque navigateur à chaque
// chargement de page. Le client garde la même clé comme filet de sécurité, pour
// les doublons déjà en base et pour ceux dont les dates diffèrent légèrement.
// ─────────────────────────────────────────────────────────────────────────────
public static class CalendrierNormalisation
{
    // Mots-clés reconnus, testés dans l'ordre sur le nom sans accent en minuscules.
    // Cette liste est le pendant de HolidayColors.ParMotCle côté client : les deux
    // doivent rester d'accord, sans quoi un doublon écarté ici pourrait continuer
    // d'être rapproché là-bas (ou l'inverse).
    private static readonly string[] MotsCles =
    {
        "rentree",
        "toussaint",
        "automne",
        "noel",
        "hiver",
        "carnaval",
        "detente",
        "paques",
        "printemps",
        "ete",
        "armistice",
        "ferie",
        "fete",
        "pedagogiq",
    };

    // Clé identifiant le congé derrière ses variantes de libellé.
    // Un nom sans mot-clé connu est sa propre clé.
    public static string Cle(string? nom)
    {
        if (string.IsNullOrWhiteSpace(nom)) return string.Empty;

        var normalise = SansAccents(nom).ToLowerInvariant();

        foreach (var motCle in MotsCles)
        {
            if (normalise.Contains(motCle, StringComparison.Ordinal)) return motCle;
        }

        return normalise;
    }

    // Vrai lorsque le nom désigne un marqueur de Rentrée scolaire.
    // La comparaison ignore les accents : selon la source, la base contient aussi
    // bien "Rentree scolaire" que "Rentrée scolaire".
    public static bool EstRentree(string? nom)
        => SansAccents(nom).Contains("Rentree", StringComparison.OrdinalIgnoreCase);

    // Texte débarrassé de ses accents, pour comparer "Congé" et "Conge".
    // Décompose chaque caractère accentué en lettre de base + signe diacritique,
    // puis ne garde que les lettres de base.
    public static string SansAccents(string? texte)
    {
        if (string.IsNullOrEmpty(texte)) return string.Empty;

        var decompose = texte.Normalize(System.Text.NormalizationForm.FormD);
        var sortie = new System.Text.StringBuilder(decompose.Length);

        foreach (var c in decompose)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                sortie.Append(c);
            }
        }

        return sortie.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }
}
