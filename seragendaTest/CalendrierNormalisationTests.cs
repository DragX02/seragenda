using seragenda.Services;

namespace seragendaTest;

public class CalendrierNormalisationTests
{
    [Theory]
    [InlineData("Vacances d'automne (Toussaint)", "Congé d'automne (Toussaint)")]
    [InlineData("Congé d'automne (Toussaint)", "Conge d'automne (Toussaint)")]
    [InlineData("Jour de l'Armistice", "Commémoration de l'Armistice")]
    [InlineData("VACANCES D'HIVER (NOEL)", "Vacances d'hiver (Noël)")]
    public void Cle_MemesCongesSousDesLibellesDifferents_SeRejoignent(string a, string b)
    {
        Assert.Equal(CalendrierNormalisation.Cle(a), CalendrierNormalisation.Cle(b));
    }

    [Theory]
    [InlineData("Vacances d'hiver (Noël)", "Congé de détente (Carnaval)")]
    [InlineData("Rentrée scolaire", "Vacances d'été")]
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
