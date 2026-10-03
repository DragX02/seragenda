using seragenda.Validators;

namespace seragendaTest;

public class InputValidatorTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("prof.dupont@school.be")]
    [InlineData("alice+tag@sub.domain.org")]
    [InlineData("x@y.z")]
    public void IsValidEmail_ValidEmails_ReturnsTrue(string email)
    {
        Assert.True(InputValidator.IsValidEmail(email));
    }

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

    [Fact]
    public void IsValidEmail_TooLong_ReturnsFalse()
    {
        var longEmail = new string('a', 95) + "@b.com";
        Assert.False(InputValidator.IsValidEmail(longEmail));
    }

    [Fact]
    public void IsValidEmail_Exactly100Chars_ReturnsTrue()
    {
        var email = new string('a', 92) + "@b.c";
        email = new string('a', 93) + "@x.zzzz";
        Assert.Equal(100, email.Length);
        Assert.True(InputValidator.IsValidEmail(email));
    }

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

    [Theory]
    [InlineData("<SCRIPT>")]
    [InlineData("<Script>")]
    [InlineData("drop table users")]
    [InlineData("Drop Table Users")]
    public void ContainsDangerousCharacters_CaseInsensitive_ReturnsTrue(string input)
    {
        Assert.True(InputValidator.ContainsDangerousCharacters(input));
    }

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

    [Fact]
    public void IsValidName_TooLong_ReturnsFalse()
    {
        var longName = new string('A', 51);
        Assert.False(InputValidator.IsValidName(longName));
    }

    [Fact]
    public void IsValidName_Exactly50Chars_ReturnsTrue()
    {
        var name = new string('A', 50);
        Assert.True(InputValidator.IsValidName(name));
    }

    [Theory]
    [InlineData("abc", 1, 10)]
    [InlineData("hello", 5, 5)]
    [InlineData("x", 1, 100)]
    public void IsValidLength_WithinRange_ReturnsTrue(string text, int min, int max)
    {
        Assert.True(InputValidator.IsValidLength(text, min, max));
    }

    [Theory]
    [InlineData("hi",       5, 10)]
    [InlineData("toolong",  1,  5)]
    [InlineData("",         1, 10)]
    [InlineData("   ",      1, 10)]
    public void IsValidLength_OutOfRange_ReturnsFalse(string text, int min, int max)
    {
        Assert.False(InputValidator.IsValidLength(text, min, max));
    }

    [Theory]
    [InlineData("123456")]
    [InlineData("password")]
    [InlineData("P@ssw0rd!42")]
    public void IsValidPassword_ValidPasswords_ReturnsTrue(string pwd)
    {
        Assert.True(InputValidator.IsValidPassword(pwd));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345")]
    public void IsValidPassword_TooShort_ReturnsFalse(string pwd)
    {
        Assert.False(InputValidator.IsValidPassword(pwd));
    }

    [Fact]
    public void IsValidPassword_TooLong_ReturnsFalse()
    {
        var longPwd = new string('a', 101);
        Assert.False(InputValidator.IsValidPassword(longPwd));
    }

    [Fact]
    public void IsValidPassword_Exactly100Chars_ReturnsTrue()
    {
        var pwd = new string('a', 100);
        Assert.True(InputValidator.IsValidPassword(pwd));
    }

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

    [Fact]
    public void SanitizeInput_TrimsWhitespace()
    {
        var result = InputValidator.SanitizeInput("  hello  ");

        Assert.Equal("hello", result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SanitizeInput_EmptyOrWhitespace_ReturnsEmpty(string input)
    {
        Assert.Equal(string.Empty, InputValidator.SanitizeInput(input));
    }

    [Fact]
    public void SanitizeInput_SafeInput_ReturnedUnchanged()
    {
        var result = InputValidator.SanitizeInput("Jean-Paul Dupont");

        Assert.Equal("Jean-Paul Dupont", result);
    }

    [Fact]
    public void SanitizeInput_ScriptTag_IsEncoded()
    {
        var result = InputValidator.SanitizeInput("<script>alert('xss')</script>");

        Assert.DoesNotContain("<script>",   result);
        Assert.DoesNotContain("</script>",  result);
        Assert.Contains("&lt;script&gt;",   result);
    }
}
