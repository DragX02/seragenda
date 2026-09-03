using seragenda.Services;

namespace seragendaTest;

/// <summary>
/// Tests for <see cref="ReportNote"/>.
///
/// Reporter une leçon la recopie sur une autre date et laisse sur l'originale la
/// mention « Reporté au … ». Faute de colonne dédiée en base, cette mention vit dans
/// le texte de la note : elle doit donc se réécrire sans jamais s'empiler, survivre
/// à la troncature des 2000 caractères, et se relire à l'identique.
///
/// L'écriture appartient au serveur : le client la produisait de mémoire et la
/// perdait dès que sa copie de la note était en retard sur la base. Il n'en garde
/// que la lecture, couverte par ObrigenieTest/ReportNoteTests.
/// </summary>
public class ReportNoteTests
{
    private static readonly DateTime Cible = new(2025, 10, 6);

    [Fact]
    public void Marquer_PuisLire_RendLeTexteEtLaDate()
    {
        var marque = ReportNote.Marquer("Lecture suivie chapitre 3", Cible);
        var (texte, cible) = ReportNote.Lire(marque);

        // Le texte revient tel qu'il a été saisi : c'est lui que le client réaffiche.
        Assert.Equal("Lecture suivie chapitre 3", texte);
        Assert.Equal(Cible, cible);
    }

    [Fact]
    public void Marquer_EcritLaFormeAttendueParLeClient()
    {
        // Le client reconnaît le marqueur à cette forme exacte : les deux côtés doivent
        // rester d'accord, sinon la mention s'afficherait telle quelle dans la leçon.
        Assert.Equal("↪ Reporté au 06/10/2025", ReportNote.Libelle(Cible));
        Assert.Equal("Dictée\n↪ Reporté au 06/10/2025", ReportNote.Marquer("Dictée", Cible));
    }

    [Fact]
    public void Marquer_NoteSansTexte_NeGardeQueLaMention()
    {
        var marque = ReportNote.Marquer("", Cible);

        // Une leçon peut n'avoir qu'une visée : la mention ne doit pas traîner de ligne vide.
        Assert.Equal(ReportNote.Libelle(Cible), marque);
        Assert.Equal(Cible, ReportNote.Cible(marque));
        Assert.Equal(string.Empty, ReportNote.Texte(marque));
    }

    [Fact]
    public void Marquer_DeuxFois_NeLaisseQuUneSeuleMention()
    {
        var seconde = new DateTime(2025, 10, 13);

        var marque = ReportNote.Marquer(ReportNote.Marquer("Dictée", Cible), seconde);

        // Reporter une leçon déjà reportée écrase la mention précédente au lieu de l'empiler.
        Assert.Equal("Dictée", ReportNote.Texte(marque));
        Assert.Equal(seconde, ReportNote.Cible(marque));
        Assert.Equal(1, marque.Split('\n').Count(l => l.Contains("Report")));
    }

    [Fact]
    public void Marquer_TexteTresLong_LaisseLaPlaceALaMention()
    {
        // La colonne s'arrête à 2000 caractères : c'est le texte qui cède, pas la mention,
        // sinon la trace du report serait coupée en base.
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
        // Selon le trajet du texte, les accents peuvent se perdre : la relecture doit tenir,
        // sans quoi une note réenregistrée gagnerait une seconde mention.
        var (texte, cible) = ReportNote.Lire("Dictée\n↪ Reporte au 06/10/2025");

        Assert.Equal("Dictée", texte);
        Assert.Equal(Cible, cible);
    }

    [Fact]
    public void Lire_LigneRessemblanteMaisDateIllisible_ResteDuTexte()
    {
        // Une phrase de l'utilisateur ne doit pas disparaître parce qu'elle commence pareil.
        var contenu = "Reporté au prochain cours de gym";
        var (texte, cible) = ReportNote.Lire(contenu);

        Assert.Equal(contenu, texte);
        Assert.Null(cible);
    }
}
