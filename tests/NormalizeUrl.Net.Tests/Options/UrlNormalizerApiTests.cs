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
        Assert.Null(options.QueryParameterMatcher);
    }

    [Fact]
    public void UtmTrackingParameters_ContainsTheClassicUtmFamily()
    {
        Assert.Equal(
            new[] { "utm_source", "utm_medium", "utm_campaign", "utm_term", "utm_content" },
            NormalizeUrlOptions.UtmTrackingParameters);
    }

    [Fact]
    public void Normalize_WithQueryParameterMatcher_StripsMatchingParametersOnly()
    {
        var options = new NormalizeUrlOptions
        {
            QueryParameterMatcher = name => name.StartsWith("utm_", StringComparison.Ordinal),
        };

        var actual = UrlNormalizer.Normalize(
            "http://example.com/path?utm_source=news&utm_medium=email&utm_campaign=spring&id=42",
            options);

        Assert.Equal("http://example.com/path?id=42", actual);
    }

    [Fact]
    public void Normalize_WithMatcherAndExactNames_AppliesTheUnion()
    {
        var options = new NormalizeUrlOptions
        {
            QueryParametersToRemove = ["fbclid"],
            QueryParameterMatcher = name => name.StartsWith("utm_", StringComparison.Ordinal),
        };

        var actual = UrlNormalizer.Normalize(
            "http://example.com/path?fbclid=abc&utm_source=news&keep=1",
            options);

        Assert.Equal("http://example.com/path?keep=1", actual);
    }

    [Fact]
    public void Normalize_WithNullMatcher_MatchesDefaultProfile()
    {
        const string url = "http://example.com/path?utm_source=news&id=42";

        var withNullMatcher = UrlNormalizer.Normalize(url, new NormalizeUrlOptions { QueryParameterMatcher = null });
        var withDefault = UrlNormalizer.Normalize(url);

        Assert.Equal(withDefault, withNullMatcher);
        Assert.Equal("http://example.com/path?utm_source=news&id=42", withNullMatcher);
    }

    [Fact]
    public void Normalize_WithMatcherThatRemovesNothing_LeavesQueryUnchanged()
    {
        var options = new NormalizeUrlOptions { QueryParameterMatcher = _ => false };

        var actual = UrlNormalizer.Normalize("http://example.com/path?a=1&b=2", options);

        Assert.Equal("http://example.com/path?a=1&b=2", actual);
    }

    [Fact]
    public void Normalize_WithMatcherAndSort_IsDeterministic()
    {
        var options = new NormalizeUrlOptions
        {
            SortQueryParameters = true,
            QueryParameterMatcher = name => name.StartsWith("utm_", StringComparison.Ordinal),
        };

        var actual = UrlNormalizer.Normalize(
            "http://example.com/path?b=2&utm_source=news&a=1",
            options);

        Assert.Equal("http://example.com/path?a=1&b=2", actual);
    }

    [Fact]
    public void Normalize_MatcherCaseSensitivity_MatchesExactNameRemoval()
    {
        var matcherOptions = new NormalizeUrlOptions
        {
            QueryParameterMatcher = name => name.Equals("utm_source", StringComparison.Ordinal),
        };
        var exactOptions = new NormalizeUrlOptions { QueryParametersToRemove = ["utm_source"] };

        const string url = "http://example.com/path?UTM_SOURCE=x&utm_source=y&b=2";

        var viaMatcher = UrlNormalizer.Normalize(url, matcherOptions);
        var viaExact = UrlNormalizer.Normalize(url, exactOptions);

        Assert.Equal("http://example.com/path?UTM_SOURCE=x&b=2", viaMatcher);
        Assert.Equal(viaExact, viaMatcher);
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
