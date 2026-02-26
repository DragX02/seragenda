using seragenda.Services;

namespace ObriGenieTest;

/// <summary>
/// Tests unitaires pour LicenseHelper (hachage SHA-256 des codes de licence).
/// </summary>
public class LicenseHelperTests
{
    [Fact]
    public void HashCode_MemeCode_RetourneMemeHash()
    {
        var hash1 = LicenseHelper.HashCode("PROMO2025");
        var hash2 = LicenseHelper.HashCode("PROMO2025");

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void HashCode_CaseInsensitive_MinusculeEtMajusculeMemeHash()
    {
        // "promo2025" et "PROMO2025" doivent donner le même hash (normalisation ToUpper)
        var hashMin = LicenseHelper.HashCode("promo2025");
        var hashMaj = LicenseHelper.HashCode("PROMO2025");
        var hashMix = LicenseHelper.HashCode("Promo2025");

        Assert.Equal(hashMaj, hashMin);
        Assert.Equal(hashMaj, hashMix);
    }

    [Fact]
    public void HashCode_CodesDistincts_ProduisentHashsDifferents()
    {
        var hash1 = LicenseHelper.HashCode("CODE_A");
        var hash2 = LicenseHelper.HashCode("CODE_B");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void HashCode_EspacesIgnores_SansEspacesMemeHash()
    {
        // Trim() est appliqué avant le hash
        var hashPropre  = LicenseHelper.HashCode("PROMO2025");
        var hashEspaces = LicenseHelper.HashCode("  PROMO2025  ");

        Assert.Equal(hashPropre, hashEspaces);
    }

    [Fact]
    public void HashCode_FormatHexMinuscule_64Caracteres()
    {
        var hash = LicenseHelper.HashCode("TEST");

        // SHA-256 → 32 bytes → 64 caractères hex
        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9a-f]+$", hash); // hex lowercase uniquement
    }
}
