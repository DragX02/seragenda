using seragenda.Validators;

namespace seragendaTest;

/// <summary>
/// Tests unitaires pour <see cref="InputValidator"/>.
///
/// Couvre :
///   - IsValidEmail        : formats valides, invalides, longueur max 100.
///   - ContainsDangerousCharacters : motifs SQL/XSS détectés, entrées sûres ignorées.
///   - IsValidName         : lettres/accents/tirets/apostrophes, longueur max 50.
///   - IsValidLength       : plages inclusives, nulls et espaces.
///   - IsValidPassword     : longueur 6–100, nulls et espaces.
///   - SanitizeInput       : encodage HTML des 5 caractères spéciaux, trim.
/// </summary>
public class InputValidatorTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // IsValidEmail
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Adresses e-mail syntaxiquement correctes → true.
    /// </summary>
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("prof.dupont@school.be")]
    [InlineData("alice+tag@sub.domain.org")]
    [InlineData("x@y.z")]
    public void IsValidEmail_ValidEmails_ReturnsTrue(string email)
    {
        Assert.True(InputValidator.IsValidEmail(email));
    }

    /// <summary>
    /// Adresses e-mail invalides (pas de @, pas de domaine, vide) → false.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("notanemail")]
    [InlineData("missing@dot")]
    [InlineData("@nodomain.com")]
    [InlineData("no-at-sign")]
    public void IsValidEmail_InvalidEmails_ReturnsFalse(string email)
    {
        Assert.False(InputValidator.IsValidEmail(email));
    }

    /// <summary>
    /// Une adresse de plus de 100 caractères est rejetée.
    /// </summary>
    [Fact]
    public void IsValidEmail_TooLong_ReturnsFalse()
    {
        var longEmail = new string('a', 95) + "@b.com"; // 101 chars
        Assert.False(InputValidator.IsValidEmail(longEmail));
    }

    /// <summary>
    /// Une adresse d'exactement 100 caractères est acceptée.
    /// </summary>
    [Fact]
    public void IsValidEmail_Exactly100Chars_ReturnsTrue()
    {
        // Construire user@domain.tld de 100 caractères exactement
        // Format : {local}@{domain}.com  →  local=92, @=1, domain=3, .com=4 → total 100
        var email = new string('a', 92) + "@b.c"; // 96 chars → trop court
        // Plus précis : local(93) + @(1) + x(1) + .(1) + z(4) = 100
        email = new string('a', 93) + "@x.zzzz"; // 100
        Assert.Equal(100, email.Length);
        Assert.True(InputValidator.IsValidEmail(email));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ContainsDangerousCharacters
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Entrées sûres ne contenant aucun motif dangereux → false.
    /// </summary>
    [Theory]
    [InlineData("Jean-Paul")]
    [InlineData("prof@school.be")]
    [InlineData("Bonjour monde")]
    [InlineData("")]
    [InlineData("   ")]
    public void ContainsDangerousCharacters_SafeInput_ReturnsFalse(string input)
    {
        Assert.False(InputValidator.ContainsDangerousCharacters(input));
    }

    /// <summary>
    /// Motifs d'injection SQL/XSS connus → true.
    /// </summary>
    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("javascript:void(0)")]
    [InlineData("a';--")]
    [InlineData("DROP TABLE users")]
    [InlineData("SELECT * FROM users")]
    [InlineData("UNION SELECT 1,2,3")]
    [InlineData("INSERT INTO t VALUES(1)")]
    [InlineData("DELETE FROM accounts")]
    [InlineData("UPDATE users SET pwd='x'")]
    [InlineData("EXEC(xp_cmdshell)")]
    [InlineData("<iframe src='evil.com'/>")]
    [InlineData("-- comment")]
    [InlineData("/* block */")]
    [InlineData("xp_cmdshell")]
    [InlineData("sp_executesql")]
    public void ContainsDangerousCharacters_DangerousInput_ReturnsTrue(string input)
    {
        Assert.True(InputValidator.ContainsDangerousCharacters(input));
    }

    /// <summary>
    /// La détection est insensible à la casse (<SCRIPT>, ScRiPt…).
    /// </summary>
    [Theory]
    [InlineData("<SCRIPT>")]
    [InlineData("<Script>")]
    [InlineData("drop table users")]
    [InlineData("Drop Table Users")]
    public void ContainsDangerousCharacters_CaseInsensitive_ReturnsTrue(string input)
    {
        Assert.True(InputValidator.ContainsDangerousCharacters(input));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // IsValidName
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Noms valides (lettres, accents, tirets, apostrophes, espaces) → true.
    /// </summary>
    [Theory]
    [InlineData("Jean-Paul")]
    [InlineData("O'Brien")]
    [InlineData("Ève")]
    [InlineData("François")]
    [InlineData("Van der Berg")]
    [InlineData("A")]
    public void IsValidName_ValidNames_ReturnsTrue(string name)
    {
        Assert.True(InputValidator.IsValidName(name));
    }

    /// <summary>
    /// Noms contenant des chiffres ou caractères spéciaux non autorisés → false.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Jean123")]
    [InlineData("name@domain")]
    [InlineData("Alice!")]
    public void IsValidName_InvalidNames_ReturnsFalse(string name)
    {
        Assert.False(InputValidator.IsValidName(name));
    }

    /// <summary>
    /// Un nom de plus de 50 caractères est rejeté.
    /// </summary>
    [Fact]
    public void IsValidName_TooLong_ReturnsFalse()
    {
        var longName = new string('A', 51);
        Assert.False(InputValidator.IsValidName(longName));
    }

    /// <summary>
    /// Un nom d'exactement 50 caractères est accepté.
    /// </summary>
    [Fact]
    public void IsValidName_Exactly50Chars_ReturnsTrue()
    {
        var name = new string('A', 50);
        Assert.True(InputValidator.IsValidName(name));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // IsValidLength
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Texte dans la plage [min, max] → true.
    /// </summary>
    [Theory]
    [InlineData("abc", 1, 10)]
    [InlineData("hello", 5, 5)]
    [InlineData("x", 1, 100)]
    public void IsValidLength_WithinRange_ReturnsTrue(string text, int min, int max)
    {
        Assert.True(InputValidator.IsValidLength(text, min, max));
    }

    /// <summary>
    /// Texte hors de la plage → false.
    /// </summary>
    [Theory]
    [InlineData("hi",       5, 10)]   // trop court
    [InlineData("toolong",  1,  5)]   // trop long
    [InlineData("",         1, 10)]   // vide
    [InlineData("   ",      1, 10)]   // espaces uniquement
    public void IsValidLength_OutOfRange_ReturnsFalse(string text, int min, int max)
    {
        Assert.False(InputValidator.IsValidLength(text, min, max));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // IsValidPassword
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Mots de passe de 6 à 100 caractères → true.
    /// </summary>
    [Theory]
    [InlineData("123456")]       // longueur minimale
    [InlineData("password")]
    [InlineData("P@ssw0rd!42")]
    public void IsValidPassword_ValidPasswords_ReturnsTrue(string pwd)
    {
        Assert.True(InputValidator.IsValidPassword(pwd));
    }

    /// <summary>
    /// Mots de passe trop courts, vides ou composés d'espaces → false.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345")]  // 5 caractères — un de moins que le minimum
    public void IsValidPassword_TooShort_ReturnsFalse(string pwd)
    {
        Assert.False(InputValidator.IsValidPassword(pwd));
    }

    /// <summary>
    /// Un mot de passe de plus de 100 caractères est rejeté.
    /// </summary>
    [Fact]
    public void IsValidPassword_TooLong_ReturnsFalse()
    {
        var longPwd = new string('a', 101);
        Assert.False(InputValidator.IsValidPassword(longPwd));
    }

    /// <summary>
    /// Un mot de passe d'exactement 100 caractères est accepté.
    /// </summary>
    [Fact]
    public void IsValidPassword_Exactly100Chars_ReturnsTrue()
    {
        var pwd = new string('a', 100);
        Assert.True(InputValidator.IsValidPassword(pwd));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SanitizeInput
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Les 5 caractères HTML spéciaux sont encodés correctement.
    /// </summary>
    [Fact]
    public void SanitizeInput_EncodesHtmlSpecialChars()
    {
        var result = InputValidator.SanitizeInput("<div>\"hello\" & 'world'/");

        Assert.Contains("&lt;",    result);
        Assert.Contains("&gt;",    result);
        Assert.Contains("&quot;",  result);
        Assert.Contains("&#x27;",  result);
        Assert.Contains("&#x2F;",  result);
    }

    /// <summary>
    /// Les espaces en début et fin sont supprimés.
    /// </summary>
    [Fact]
    public void SanitizeInput_TrimsWhitespace()
    {
        var result = InputValidator.SanitizeInput("  hello  ");

        Assert.Equal("hello", result);
    }

    /// <summary>
    /// Une chaîne nulle ou composée uniquement d'espaces retourne une chaîne vide.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SanitizeInput_EmptyOrWhitespace_ReturnsEmpty(string input)
    {
        Assert.Equal(string.Empty, InputValidator.SanitizeInput(input));
    }

    /// <summary>
    /// Une chaîne sans caractères spéciaux est retournée telle quelle (après trim).
    /// </summary>
    [Fact]
    public void SanitizeInput_SafeInput_ReturnedUnchanged()
    {
        var result = InputValidator.SanitizeInput("Jean-Paul Dupont");

        Assert.Equal("Jean-Paul Dupont", result);
    }

    /// <summary>
    /// Une balise script complète est encodée de sorte qu'elle ne puisse pas être interprétée comme HTML.
    /// </summary>
    [Fact]
    public void SanitizeInput_ScriptTag_IsEncoded()
    {
        var result = InputValidator.SanitizeInput("<script>alert('xss')</script>");

        Assert.DoesNotContain("<script>",   result);
        Assert.DoesNotContain("</script>",  result);
        Assert.Contains("&lt;script&gt;",   result);
    }
}
