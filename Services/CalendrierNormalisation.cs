namespace seragenda.Services;

public static class CalendrierNormalisation
{
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

    public static bool EstRentree(string? nom)
        => SansAccents(nom).Contains("Rentree", StringComparison.OrdinalIgnoreCase);

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
