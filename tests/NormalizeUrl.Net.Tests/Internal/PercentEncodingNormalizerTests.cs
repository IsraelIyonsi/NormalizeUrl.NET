using NormalizeUrl.Internal;

namespace NormalizeUrl.Tests.Internal;

public sealed class PercentEncodingNormalizerTests
{
    [Theory]
    [MemberData(nameof(Cases))]
    public void Normalize_DecodesUnreservedAndUppercasesRemainingHex(string input, string expected)
    {
        var actual = PercentEncodingNormalizer.Normalize(input);

        Assert.Equal(expected, actual);
    }

    public static TheoryData<string, string> Cases() => new()
    {
        { "", "" },
        { "abc", "abc" },

        // Unreserved: ALPHA / DIGIT / '-' / '.' / '_' / '~' get decoded.
        { "%41", "A" },
        { "%61", "a" },
        { "%30", "0" },
        { "%2D", "-" },
        { "%2E", "." },
        { "%5F", "_" },
        { "%7E", "~" },
        { "%2d%2e%5f%7e", "-._~" },

        // Reserved / other octets stay percent-encoded, hex uppercased.
        { "%2f", "%2F" },
        { "%2F", "%2F" },
        { "%3f", "%3F" },
        { "%23", "%23" },
        { "%20", "%20" },
        { "%c3%a9", "%C3%A9" },

        // Mixed unreserved and reserved in one string.
        { "a%2Fb%2ec", "a%2Fb.c" },
        { "%68%65%6C%6C%6F%2F%77%6F%72%6C%64", "hello%2Fworld" },
    };

    [Theory]
    [InlineData("%")]
    [InlineData("%2")]
    [InlineData("%2G")]
    [InlineData("%G2")]
    [InlineData("100%")]
    [InlineData("a%")]
    public void Normalize_ThrowsFormatException_ForInvalidPercentSequence(string input)
    {
        Assert.Throws<FormatException>(() => PercentEncodingNormalizer.Normalize(input));
    }
}
