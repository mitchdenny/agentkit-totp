using AgentKit.Totp;

namespace AgentKit.Totp.Tests;

public class OtpAuthUriParsingTests
{
    [Fact]
    public void ParseOtpAuthUri_WithSha256Algorithm_SetsAlgorithm()
    {
        var uri = "otpauth://totp/Example:alice@example.com?secret=JBSWY3DPEHPK3PXP&algorithm=SHA256";

        var entry = OtpAuthUriHelper.ParseOtpAuthUri(uri);

        Assert.NotNull(entry);
        Assert.Equal("SHA256", entry.Algorithm);
        Assert.Equal("Example", entry.Issuer);
        Assert.Equal("alice@example.com", entry.Account);
    }

    [Fact]
    public void ParseOtpAuthUri_WithoutAlgorithm_DefaultsToSha1()
    {
        var uri = "otpauth://totp/Example:alice@example.com?secret=JBSWY3DPEHPK3PXP";

        var entry = OtpAuthUriHelper.ParseOtpAuthUri(uri);

        Assert.NotNull(entry);
        Assert.Equal("SHA1", entry.Algorithm);
    }

    [Fact]
    public void ParseOtpAuthUri_ColonInAccountName_ParsesCorrectly()
    {
        var uri = "otpauth://totp/Issuer:user%3Aname%40host?secret=JBSWY3DPEHPK3PXP";

        var entry = OtpAuthUriHelper.ParseOtpAuthUri(uri);

        Assert.NotNull(entry);
        Assert.Equal("Issuer", entry.Issuer);
        Assert.Equal("user:name@host", entry.Account);
    }

    [Fact]
    public void ParseOtpAuthUri_InvalidPrefix_ReturnsNull()
    {
        var uri = "otpauth://hotp/Example:alice?secret=JBSWY3DPEHPK3PXP";

        var entry = OtpAuthUriHelper.ParseOtpAuthUri(uri);

        Assert.Null(entry);
    }

    [Fact]
    public void ParseOtpAuthUri_MissingSecret_ReturnsNull()
    {
        var uri = "otpauth://totp/Example:alice@example.com";

        var entry = OtpAuthUriHelper.ParseOtpAuthUri(uri);

        Assert.Null(entry);
    }

    [Fact]
    public void ParseOtpAuthUri_CustomDigitsAndPeriod_ParsesCorrectly()
    {
        var uri = "otpauth://totp/Test?secret=JBSWY3DPEHPK3PXP&digits=8&period=60";

        var entry = OtpAuthUriHelper.ParseOtpAuthUri(uri);

        Assert.NotNull(entry);
        Assert.Equal(8, entry.Digits);
        Assert.Equal(60, entry.Period);
    }
}
