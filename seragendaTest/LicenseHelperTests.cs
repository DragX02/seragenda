using seragenda.Services;

namespace seragendaTest;

public class LicenseHelperTests
{
    [Fact]
    public void HashCode_Returns64CharHexString()
    {
        var hash = LicenseHelper.HashCode("PROF-DUPONT");

        Assert.Equal(64, hash.Length);
    }

    [Fact]
    public void HashCode_OutputIsLowerCase()
    {
        var hash = LicenseHelper.HashCode("SOME-CODE");

        Assert.Equal(hash, hash.ToLower());
    }

    [Fact]
    public void HashCode_OutputContainsOnlyHexChars()
    {
        var hash = LicenseHelper.HashCode("TEST-123");

        Assert.Matches("^[0-9a-f]{64}$", hash);
    }

    [Fact]
    public void HashCode_SameInput_ReturnsSameHash()
    {
        var hash1 = LicenseHelper.HashCode("PROF-DUPONT");
        var hash2 = LicenseHelper.HashCode("PROF-DUPONT");

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void HashCode_CaseInsensitive_LowerEqualsUpper()
    {
        var hashLower = LicenseHelper.HashCode("abc123");
        var hashUpper = LicenseHelper.HashCode("ABC123");

        Assert.Equal(hashLower, hashUpper);
    }

    [Fact]
    public void HashCode_CaseInsensitive_MixedEqualsUpper()
    {
        var hashMixed = LicenseHelper.HashCode("Prof-Dupont");
        var hashUpper = LicenseHelper.HashCode("PROF-DUPONT");

        Assert.Equal(hashMixed, hashUpper);
    }

    [Fact]
    public void HashCode_TrimsWhitespace_LeadingAndTrailing()
    {
        var hashTrimmed = LicenseHelper.HashCode("ABC123");
        var hashPadded  = LicenseHelper.HashCode("  ABC123  ");

        Assert.Equal(hashTrimmed, hashPadded);
    }

    [Fact]
    public void HashCode_NormalizesLowerCaseWithSpaces()
    {
        var hash1 = LicenseHelper.HashCode(" abc123 ");
        var hash2 = LicenseHelper.HashCode("ABC123");

        Assert.Equal(hash1, hash2);
    }

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

    [Fact]
    public void HashCode_EmptyString_Returns64CharHash()
    {
        var hash = LicenseHelper.HashCode("");

        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9a-f]{64}$", hash);
    }

    [Fact]
    public void HashCode_WhitespaceOnly_EqualsEmptyStringHash()
    {
        var hashEmpty  = LicenseHelper.HashCode("");
        var hashSpaces = LicenseHelper.HashCode("   ");

        Assert.Equal(hashEmpty, hashSpaces);
    }
}
