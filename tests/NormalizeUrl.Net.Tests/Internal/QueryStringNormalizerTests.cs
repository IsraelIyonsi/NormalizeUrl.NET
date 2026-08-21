using NormalizeUrl.Internal;

namespace NormalizeUrl.Tests.Internal;

public sealed class QueryStringNormalizerTests
{
    [Theory]
    [InlineData("b=2&a=1", "a=1&b=2")]
    [InlineData("a=1&a=0", "a=0&a=1")]
    [InlineData("z&a", "a&z")]
    [InlineData("a=1", "a=1")]
    [InlineData("", "")]
    [InlineData("b=2&a=2&a=1", "a=1&a=2&b=2")]
    public void Sort_OrdersByKeyThenValue(string input, string expected)
    {
        var actual = QueryStringNormalizer.Sort(input);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("utm_source=x&a=1", new[] { "utm_source" }, "a=1")]
    [InlineData("a=1&utm_source=x&utm_medium=y&b=2", new[] { "utm_source", "utm_medium" }, "a=1&b=2")]
    [InlineData("utm_source=x", new[] { "utm_source" }, "")]
    [InlineData("a=1&b=2", new[] { "utm_source" }, "a=1&b=2")]
    [InlineData("", new[] { "utm_source" }, "")]
    public void RemoveParameters_DropsNamedParametersOnly(string input, string[] namesToRemove, string expected)
    {
        var actual = QueryStringNormalizer.RemoveParameters(input, namesToRemove, null);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RemoveParameters_IsCaseSensitiveAndPreservesBareFlags()
    {
        var actual = QueryStringNormalizer.RemoveParameters("UTM_SOURCE=x&flag&b=2", ["utm_source"], null);

        Assert.Equal("UTM_SOURCE=x&flag&b=2", actual);
    }

    [Fact]
    public void RemoveParameters_WithMatcher_DropsEveryMatchingName()
    {
        var actual = QueryStringNormalizer.RemoveParameters(
            "utm_source=x&utm_medium=y&id=42",
            [],
            name => name.StartsWith("utm_", StringComparison.Ordinal));

        Assert.Equal("id=42", actual);
    }

    [Fact]
    public void RemoveParameters_UnionsExactNamesAndMatcher()
    {
        var actual = QueryStringNormalizer.RemoveParameters(
            "fbclid=z&utm_source=x&id=42",
            ["fbclid"],
            name => name.StartsWith("utm_", StringComparison.Ordinal));

        Assert.Equal("id=42", actual);
    }

    [Fact]
    public void RemoveParameters_MatcherAppliesToNameNotValue()
    {
        var actual = QueryStringNormalizer.RemoveParameters(
            "keep=utm_source&utm_source=drop",
            [],
            name => name.StartsWith("utm_", StringComparison.Ordinal));

        Assert.Equal("keep=utm_source", actual);
    }

    [Fact]
    public void RemoveParameters_MatcherIsCaseSensitiveWhenPredicateIsOrdinal()
    {
        var actual = QueryStringNormalizer.RemoveParameters(
            "UTM_SOURCE=x&utm_medium=y",
            [],
            name => name.StartsWith("utm_", StringComparison.Ordinal));

        Assert.Equal("UTM_SOURCE=x", actual);
    }
}
