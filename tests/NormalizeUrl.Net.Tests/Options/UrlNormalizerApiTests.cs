namespace NormalizeUrl.Tests.Options;

public sealed class UrlNormalizerApiTests
{
    [Theory]
    [InlineData("HTTP://Example.com:80/a/./b", "http://example.com/a/b", true)]
    [InlineData("http://example.com/a", "http://example.com/b", false)]
    [InlineData("http://example.com/path?a=1&b=2", "http://example.com/path?b=2&a=1", false)]
    public void AreEquivalent_UsesDefaultProfile_WhenNoOptionsGiven(string first, string second, bool expected)
    {
        var actual = UrlNormalizer.AreEquivalent(first, second);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AreEquivalent_UsesGivenOptions()
    {
        var options = new NormalizeUrlOptions { StripWwwPrefix = true };

        Assert.False(UrlNormalizer.AreEquivalent("http://www.example.com/", "http://example.com/"));
        Assert.True(UrlNormalizer.AreEquivalent("http://www.example.com/", "http://example.com/", options));
    }

    [Fact]
    public void AreEquivalent_WithSortQueryParameters_IgnoresQueryOrder()
    {
        var options = new NormalizeUrlOptions { SortQueryParameters = true };

        Assert.True(UrlNormalizer.AreEquivalent(
            "http://example.com/path?a=1&b=2",
            "http://example.com/path?b=2&a=1",
            options));
    }

    [Fact]
    public void TryNormalize_ReturnsTrueAndNormalizedValue_ForValidUrl()
    {
        var succeeded = UrlNormalizer.TryNormalize("HTTP://Example.com:80/path", out var normalized);

        Assert.True(succeeded);
        Assert.Equal("http://example.com/path", normalized);
    }

    [Fact]
    public void TryNormalize_ReturnsFalseAndNull_ForNullUrl()
    {
        var succeeded = UrlNormalizer.TryNormalize(null, out var normalized);

        Assert.False(succeeded);
        Assert.Null(normalized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-url-without-a-scheme")]
    [InlineData("http://example.com/%2")]
    [InlineData("http://example.com:99999/")]
    public void TryNormalize_ReturnsFalseAndNull_ForInvalidUrl(string url)
    {
        var succeeded = UrlNormalizer.TryNormalize(url, out var normalized);

        Assert.False(succeeded);
        Assert.Null(normalized);
    }

    [Fact]
    public void TryNormalize_HonorsOptions()
    {
        var options = new NormalizeUrlOptions { ForceHttps = true };

        var succeeded = UrlNormalizer.TryNormalize("http://example.com/", out var normalized, options);

        Assert.True(succeeded);
        Assert.Equal("https://example.com/", normalized);
    }

    [Fact]
    public void Default_HasEveryOptInTransformDisabled()
    {
        var options = NormalizeUrlOptions.Default;

        Assert.False(options.SortQueryParameters);
        Assert.False(options.StripTrailingSlash);
        Assert.False(options.StripFragment);
        Assert.False(options.ForceHttps);
        Assert.False(options.StripWwwPrefix);
        Assert.Empty(options.QueryParametersToRemove);
    }

    [Fact]
    public void UtmTrackingParameters_ContainsTheClassicUtmFamily()
    {
        Assert.Equal(
            new[] { "utm_source", "utm_medium", "utm_campaign", "utm_term", "utm_content" },
            NormalizeUrlOptions.UtmTrackingParameters);
    }

    [Fact]
    public void Normalize_WithNullOptions_BehavesLikeDefaultProfile()
    {
        var withNull = UrlNormalizer.Normalize("HTTP://Example.com:80/path/", null);
        var withDefault = UrlNormalizer.Normalize("HTTP://Example.com:80/path/", NormalizeUrlOptions.Default);
        var withOmitted = UrlNormalizer.Normalize("HTTP://Example.com:80/path/");

        Assert.Equal(withDefault, withNull);
        Assert.Equal(withDefault, withOmitted);
    }
}
