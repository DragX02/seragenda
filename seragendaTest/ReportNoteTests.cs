using seragenda.Services;

namespace seragendaTest;

public class ReportNoteTests
{
    private static readonly DateTime Cible = new(2025, 10, 6);

    [Fact]
    public void Marquer_PuisLire_RendLeTexteEtLaDate()
    {
        var marque = ReportNote.Marquer("Lecture suivie chapitre 3", Cible);
        var (texte, cible) = ReportNote.Lire(marque);

        Assert.Equal("Lecture suivie chapitre 3", texte);
        Assert.Equal(Cible, cible);
    }

    [Fact]
    public void Marquer_EcritLaFormeAttendueParLeClient()
    {
        Assert.Equal("↪ Reporté au 06/10/2025", ReportNote.Libelle(Cible));
        Assert.Equal("Dictée\n↪ Reporté au 06/10/2025", ReportNote.Marquer("Dictée", Cible));
    }

    [Fact]
    public void Marquer_NoteSansTexte_NeGardeQueLaMention()
    {
        var marque = ReportNote.Marquer("", Cible);

        Assert.Equal(ReportNote.Libelle(Cible), marque);
        Assert.Equal(Cible, ReportNote.Cible(marque));
        Assert.Equal(string.Empty, ReportNote.Texte(marque));
    }

    [Fact]
    public void Marquer_DeuxFois_NeLaisseQuUneSeuleMention()
    {
        var seconde = new DateTime(2025, 10, 13);

        var marque = ReportNote.Marquer(ReportNote.Marquer("Dictée", Cible), seconde);

        Assert.Equal("Dictée", ReportNote.Texte(marque));
        Assert.Equal(seconde, ReportNote.Cible(marque));
        Assert.Equal(1, marque.Split('\n').Count(l => l.Contains("Report")));
    }

    [Fact]
    public void Marquer_TexteTresLong_LaisseLaPlaceALaMention()
    {
        var marque = ReportNote.Marquer(new string('a', ReportNote.MaxContenu), Cible);

        Assert.True(marque.Length <= ReportNote.MaxContenu);
        Assert.Equal(Cible, ReportNote.Cible(marque));
    }

    [Fact]
    public void Lire_ContenuVide_NeCasseRien()
    {
        Assert.Equal((string.Empty, null), ReportNote.Lire(null));
        Assert.Equal((string.Empty, null), ReportNote.Lire(""));
    }

    [Fact]
    public void Lire_MarqueurSansAccent_EstQuandMemeReconnu()
    {
        var (texte, cible) = ReportNote.Lire("Dictée\n↪ Reporte au 06/10/2025");

        Assert.Equal("Dictée", texte);
        Assert.Equal(Cible, cible);
    }

    [Fact]
    public void Lire_LigneRessemblanteMaisDateIllisible_ResteDuTexte()
    {
        var contenu = "Reporté au prochain cours de gym";
        var (texte, cible) = ReportNote.Lire(contenu);

        Assert.Equal(contenu, texte);
        Assert.Null(cible);
    }
}
