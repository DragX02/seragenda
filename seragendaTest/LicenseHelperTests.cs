using seragenda.Services;

namespace seragendaTest;

/// <summary>
/// Tests unitaires pour <see cref="LicenseHelper"/>.
///
/// Couvre :
///   - HashCode : longueur de sortie (64 hex), format minuscules.
///   - Normalisation : casse et espaces — "abc", "ABC" et " abc " produisent le même hachage.
///   - Déterminisme : deux appels identiques retournent le même hachage.
///   - Unicité : deux codes différents produisent des hachages différents.
/// </summary>
public class LicenseHelperTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Longueur et format de sortie
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Le hachage retourné doit toujours contenir exactement 64 caractères (SHA-256 → 32 octets → 64 hex).
    /// </summary>
    [Fact]
    public void HashCode_Returns64CharHexString()
    {
        var hash = LicenseHelper.HashCode("PROF-DUPONT");

        Assert.Equal(64, hash.Length);
    }

    /// <summary>
    /// La sortie doit être en minuscules uniquement (aucune lettre majuscule A–F).
    /// </summary>
    [Fact]
    public void HashCode_OutputIsLowerCase()
    {
        var hash = LicenseHelper.HashCode("SOME-CODE");

        Assert.Equal(hash, hash.ToLower());
    }

    /// <summary>
    /// La sortie ne doit contenir que des caractères hexadécimaux valides (0-9, a-f).
    /// </summary>
    [Fact]
    public void HashCode_OutputContainsOnlyHexChars()
    {
        var hash = LicenseHelper.HashCode("TEST-123");

        Assert.Matches("^[0-9a-f]{64}$", hash);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Déterminisme
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Deux appels avec le même code retournent des hachages identiques.
    /// </summary>
    [Fact]
    public void HashCode_SameInput_ReturnsSameHash()
    {
        var hash1 = LicenseHelper.HashCode("PROF-DUPONT");
        var hash2 = LicenseHelper.HashCode("PROF-DUPONT");

        Assert.Equal(hash1, hash2);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Normalisation (insensibilité à la casse et aux espaces)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Un code en minuscules et le même code en majuscules produisent le même hachage.
    /// </summary>
    [Fact]
    public void HashCode_CaseInsensitive_LowerEqualsUpper()
    {
        var hashLower = LicenseHelper.HashCode("abc123");
        var hashUpper = LicenseHelper.HashCode("ABC123");

        Assert.Equal(hashLower, hashUpper);
    }

    /// <summary>
    /// Un code en casse mixte produit le même hachage que sa version tout-majuscules.
    /// </summary>
    [Fact]
    public void HashCode_CaseInsensitive_MixedEqualsUpper()
    {
        var hashMixed = LicenseHelper.HashCode("Prof-Dupont");
        var hashUpper = LicenseHelper.HashCode("PROF-DUPONT");

        Assert.Equal(hashMixed, hashUpper);
    }

    /// <summary>
    /// Les espaces en début et fin de chaîne sont ignorés (trim).
    /// </summary>
    [Fact]
    public void HashCode_TrimsWhitespace_LeadingAndTrailing()
    {
        var hashTrimmed = LicenseHelper.HashCode("ABC123");
        var hashPadded  = LicenseHelper.HashCode("  ABC123  ");

        Assert.Equal(hashTrimmed, hashPadded);
    }

    /// <summary>
    /// Combinaison trim + casse : " abc123 " et "ABC123" donnent le même hachage.
    /// </summary>
    [Fact]
    public void HashCode_NormalizesLowerCaseWithSpaces()
    {
        var hash1 = LicenseHelper.HashCode(" abc123 ");
        var hash2 = LicenseHelper.HashCode("ABC123");

        Assert.Equal(hash1, hash2);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Unicité
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Deux codes distincts produisent des hachages différents.
    /// </summary>
    [Theory]
    [InlineData("PROF-DUPONT",  "PROF-MARTIN")]
    [InlineData("LICENSE-001",  "LICENSE-002")]
    [InlineData("A",            "B")]
    public void HashCode_DifferentInputs_ProduceDifferentHashes(string code1, string code2)
    {
        var hash1 = LicenseHelper.HashCode(code1);
        var hash2 = LicenseHelper.HashCode(code2);

        Assert.NotEqual(hash1, hash2);
    }

    /// <summary>
    /// Une chaîne vide produit un hachage valide de 64 caractères (SHA-256 de la chaîne vide).
    /// </summary>
    [Fact]
    public void HashCode_EmptyString_Returns64CharHash()
    {
        var hash = LicenseHelper.HashCode("");

        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9a-f]{64}$", hash);
    }

    /// <summary>
    /// Une chaîne composée uniquement d'espaces est rognée à vide, produisant
    /// le même hachage que la chaîne vide.
    /// </summary>
    [Fact]
    public void HashCode_WhitespaceOnly_EqualsEmptyStringHash()
    {
        var hashEmpty  = LicenseHelper.HashCode("");
        var hashSpaces = LicenseHelper.HashCode("   ");

        Assert.Equal(hashEmpty, hashSpaces);
    }
}
