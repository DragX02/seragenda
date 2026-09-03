using seragenda.Services;

namespace seragendaTest;

/// <summary>
/// Tests for <see cref="CalendrierNormalisation"/>.
///
/// La page officielle décrit régulièrement le même congé sous deux écritures. Le
/// scraper ne comparait que le nom exact : les deux entraient, et chaque client
/// devait ensuite les rapprocher pour ne pas afficher le congé à deux endroits.
/// La clé sert à les reconnaître à l'ingestion — mais elle ne doit pas confondre
/// deux congés réellement distincts qui partagent un mot.
/// </summary>
public class CalendrierNormalisationTests
{
    [Theory]
    // Les deux écritures du congé d'automne, telles qu'elles arrivent de la source
    [InlineData("Vacances d'automne (Toussaint)", "Congé d'automne (Toussaint)")]
    // Accents perdus en cours de route : la clé ne doit pas s'en apercevoir
    [InlineData("Congé d'automne (Toussaint)", "Conge d'automne (Toussaint)")]
    // Le même jour férié sous deux libellés
    [InlineData("Jour de l'Armistice", "Commémoration de l'Armistice")]
    // Casse indifférente
    [InlineData("VACANCES D'HIVER (NOEL)", "Vacances d'hiver (Noël)")]
    public void Cle_MemesCongesSousDesLibellesDifferents_SeRejoignent(string a, string b)
    {
        Assert.Equal(CalendrierNormalisation.Cle(a), CalendrierNormalisation.Cle(b));
    }

    [Theory]
    // Deux congés bien distincts : les confondre ferait disparaître l'un des deux
    [InlineData("Vacances d'hiver (Noël)", "Congé de détente (Carnaval)")]
    [InlineData("Rentrée scolaire", "Vacances d'été")]
    // Un nom sans mot-clé connu est sa propre clé : deux journées libres restent deux
    [InlineData("Excursion à Namur", "Excursion à Liège")]
    public void Cle_CongesDistincts_NeSeRejoignentPas(string a, string b)
    {
        Assert.NotEqual(CalendrierNormalisation.Cle(a), CalendrierNormalisation.Cle(b));
    }

    [Fact]
    public void Cle_ContenuVide_NeCassePas()
    {
        Assert.Equal(string.Empty, CalendrierNormalisation.Cle(null));
        Assert.Equal(string.Empty, CalendrierNormalisation.Cle("   "));
    }

    [Theory]
    [InlineData("Rentrée scolaire")]
    [InlineData("Rentree scolaire")]
    [InlineData("RENTREE DES CLASSES")]
    public void EstRentree_ReconnaitLesDeuxEcritures(string nom)
    {
        // La rentrée ancre le calcul des semaines scolaires : la manquer décalerait
        // toute la numérotation.
        Assert.True(CalendrierNormalisation.EstRentree(nom));
    }

    [Fact]
    public void EstRentree_AutreConge_EstFaux()
    {
        Assert.False(CalendrierNormalisation.EstRentree("Vacances d'été"));
    }

    [Fact]
    public void SansAccents_GardeLeTexteLisible()
    {
        Assert.Equal("Conge d'ete a Liege", CalendrierNormalisation.SansAccents("Congé d'été à Liège"));
    }
}
