namespace NormalizeUrl.Tests.Errors;

public sealed class UrlNormalizerErrorTests
{
    [Fact]
    public void Normalize_ThrowsArgumentNullException_ForNullUrl()
    {
        Assert.Throws<ArgumentNullException>(() => UrlNormalizer.Normalize(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Normalize_ThrowsArgumentException_ForEmptyOrWhitespaceUrl(string url)
    {
        Assert.Throws<ArgumentException>(() => UrlNormalizer.Normalize(url));
    }

    [Theory]
    [InlineData("example.com/path")]
    [InlineData("//example.com/path")]
    [InlineData("/just/a/path")]
    [InlineData("just text, not a url at all")]
    public void Normalize_ThrowsFormatException_ForUrlMissingScheme(string url)
    {
        Assert.Throws<FormatException>(() => UrlNormalizer.Normalize(url));
    }

    [Theory]
    [InlineData("http://example.com/%2")]
    [InlineData("http://example.com/%GG")]
    [InlineData("http://example.com/100%")]
    public void Normalize_ThrowsFormatException_ForInvalidPercentEncoding(string url)
    {
        Assert.Throws<FormatException>(() => UrlNormalizer.Normalize(url));
    }

    [Theory]
    [InlineData("http://example.com:99999/")]
    [InlineData("http://example.com:abc/")]
    [InlineData("http://example.com:-1/")]
    public void Normalize_ThrowsFormatException_ForInvalidPort(string url)
    {
        Assert.Throws<FormatException>(() => UrlNormalizer.Normalize(url));
    }

    [Fact]
    public void Normalize_ThrowsFormatException_ForUnterminatedIPv6Literal()
    {
        Assert.Throws<FormatException>(() => UrlNormalizer.Normalize("http://[2001:db8::1/path"));
    }

    [Theory]
    [InlineData("1http://example.com/")]
    [InlineData("ht tp://example.com/")]
    public void Normalize_ThrowsFormatException_ForInvalidScheme(string url)
    {
        Assert.Throws<FormatException>(() => UrlNormalizer.Normalize(url));
    }

    [Fact]
    public void AreEquivalent_ThrowsArgumentNullException_WhenEitherUrlIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => UrlNormalizer.AreEquivalent(null!, "http://example.com/"));
        Assert.Throws<ArgumentNullException>(() => UrlNormalizer.AreEquivalent("http://example.com/", null!));
    }

    [Fact]
    public void AreEquivalent_ThrowsFormatException_WhenEitherUrlIsMalformed()
    {
        Assert.Throws<FormatException>(() => UrlNormalizer.AreEquivalent("not-a-url", "http://example.com/"));
        Assert.Throws<FormatException>(() => UrlNormalizer.AreEquivalent("http://example.com/", "not-a-url"));
    }
}
