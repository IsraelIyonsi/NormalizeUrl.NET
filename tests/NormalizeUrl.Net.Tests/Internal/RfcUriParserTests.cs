using NormalizeUrl.Internal;

namespace NormalizeUrl.Tests.Internal;

public sealed class RfcUriParserTests
{
    [Fact]
    public void Parse_SplitsAllComponents()
    {
        var parsed = RfcUriParser.Parse("https://user:pass@example.com:8080/a/b?x=1#frag");

        Assert.Equal("https", parsed.Scheme);
        Assert.True(parsed.HasAuthority);
        Assert.True(parsed.HasUserInfo);
        Assert.Equal("user:pass", parsed.UserInfo);
        Assert.Equal("example.com", parsed.Host);
        Assert.True(parsed.HasPort);
        Assert.Equal(8080, parsed.Port);
        Assert.Equal("/a/b", parsed.Path);
        Assert.True(parsed.HasQuery);
        Assert.Equal("x=1", parsed.Query);
        Assert.True(parsed.HasFragment);
        Assert.Equal("frag", parsed.Fragment);
    }

    [Fact]
    public void Parse_DistinguishesAbsentFromEmptyQueryAndFragment()
    {
        var withoutEither = RfcUriParser.Parse("https://example.com/path");
        Assert.False(withoutEither.HasQuery);
        Assert.False(withoutEither.HasFragment);

        var withEmptyQuery = RfcUriParser.Parse("https://example.com/path?");
        Assert.True(withEmptyQuery.HasQuery);
        Assert.Equal(string.Empty, withEmptyQuery.Query);

        var withEmptyFragment = RfcUriParser.Parse("https://example.com/path#");
        Assert.True(withEmptyFragment.HasFragment);
        Assert.Equal(string.Empty, withEmptyFragment.Fragment);
    }

    [Fact]
    public void Parse_HandlesIPv6HostLiteralWithPort()
    {
        var parsed = RfcUriParser.Parse("http://[2001:db8::1]:8080/path");

        Assert.Equal("[2001:db8::1]", parsed.Host);
        Assert.True(parsed.HasPort);
        Assert.Equal(8080, parsed.Port);
    }

    [Fact]
    public void Parse_HandlesEmptyHostForFileUrls()
    {
        var parsed = RfcUriParser.Parse("file:///etc/passwd");

        Assert.True(parsed.HasAuthority);
        Assert.Equal(string.Empty, parsed.Host);
        Assert.Equal("/etc/passwd", parsed.Path);
    }

    [Fact]
    public void Parse_SplitsUserInfoOnLastAtSign()
    {
        var parsed = RfcUriParser.Parse("https://a@b@example.com/");

        Assert.Equal("a@b", parsed.UserInfo);
        Assert.Equal("example.com", parsed.Host);
    }

    [Fact]
    public void Parse_TreatsUrlWithoutSchemeAsRelative()
    {
        Assert.Throws<FormatException>(() => RfcUriParser.Parse("/just/a/path"));
    }

    [Theory]
    [InlineData("http://example.com:abc/")]
    [InlineData("http://example.com:99999/")]
    [InlineData("http://example.com:-1/")]
    public void Parse_ThrowsFormatException_ForInvalidPort(string url)
    {
        Assert.Throws<FormatException>(() => RfcUriParser.Parse(url));
    }

    [Theory]
    [InlineData("1http://example.com/")]
    [InlineData("ht tp://example.com/")]
    [InlineData("-http://example.com/")]
    public void Parse_ThrowsFormatException_ForInvalidScheme(string url)
    {
        Assert.Throws<FormatException>(() => RfcUriParser.Parse(url));
    }

    [Fact]
    public void Parse_ThrowsFormatException_ForUnterminatedIPv6Literal()
    {
        Assert.Throws<FormatException>(() => RfcUriParser.Parse("http://[2001:db8::1/path"));
    }

    [Fact]
    public void Parse_ThrowsArgumentNullException_ForNullUrl()
    {
        Assert.Throws<ArgumentNullException>(() => RfcUriParser.Parse(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_ThrowsArgumentException_ForEmptyOrWhitespaceUrl(string url)
    {
        Assert.Throws<ArgumentException>(() => RfcUriParser.Parse(url));
    }
}
