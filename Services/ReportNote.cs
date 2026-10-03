namespace seragenda.Services;

public static class ReportNote
{
    private const string Fleche = "↪";

    private static readonly string[] Cles = { "Reporté au ", "Reporte au " };

    public const string FormatDate = "dd/MM/yyyy";

    private static readonly System.Globalization.CultureInfo Neutre =
        System.Globalization.CultureInfo.InvariantCulture;

    private static readonly string[] FormatsLus = { "dd/MM/yyyy", "dd-MM-yyyy", "dd.MM.yyyy" };

    public const int MaxContenu = 2000;

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
                cible = date;
                continue;
            }
            gardees.Add(ligne);
        }

        return (string.Join("\n", gardees).TrimEnd('\n', ' '), cible);
    }

    public static string Texte(string? content) => Lire(content).Texte;

    public static DateTime? Cible(string? content) => Lire(content).Cible;

    public static string Marquer(string? content, DateTime cible)
    {
        var texte = Texte(content);
        var marqueur = Libelle(cible);

        if (string.IsNullOrEmpty(texte)) return marqueur;

        int place = MaxContenu - marqueur.Length - 1;
        if (place < 0) return marqueur;
        if (texte.Length > place) texte = texte[..place];

        return $"{texte}\n{marqueur}";
    }

    public static string Libelle(DateTime cible)
        => $"{Fleche} Reporté au {cible.ToString(FormatDate, Neutre)}";

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

            return null;
        }

        return null;
    }
}
