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
        var actual = QueryStringNormalizer.RemoveParameters(input, namesToRemove);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RemoveParameters_IsCaseSensitiveAndPreservesBareFlags()
    {
        var actual = QueryStringNormalizer.RemoveParameters("UTM_SOURCE=x&flag&b=2", ["utm_source"]);

        Assert.Equal("UTM_SOURCE=x&flag&b=2", actual);
    }
}
