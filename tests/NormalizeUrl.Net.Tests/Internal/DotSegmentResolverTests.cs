using NormalizeUrl.Internal;

namespace NormalizeUrl.Tests.Internal;

public sealed class DotSegmentResolverTests
{
    [Theory]
    [MemberData(nameof(Cases))]
    public void RemoveDotSegments_MatchesRfc3986Algorithm(string input, string expected)
    {
        var actual = DotSegmentResolver.RemoveDotSegments(input);

        Assert.Equal(expected, actual);
    }

    public static TheoryData<string, string> Cases() => new()
    {
        // The worked example given directly in RFC 3986 section 5.2.4.
        { "/a/b/c/./../../g", "/a/g" },
        { "mid/content=5/../6", "mid/6" },

        { "", "" },
        { "/", "/" },
        { "/a/b/c", "/a/b/c" },
        { "./a", "a" },
        { "../a", "a" },
        { "/./a", "/a" },
        { "/../a", "/a" },
        { "/.", "/" },
        { "/..", "/" },
        { ".", "" },
        { "..", "" },
        { "/a/..", "/" },
        { "/a/../..", "/" },
        { "/a/b/../..", "/" },
        // remove_dot_segments can introduce a leading slash even when the input had none;
        // it is defined for merging resolved reference paths, not as a generic path cleaner.
        { "a/b/../../c", "/c" },
        { "/a/b/.", "/a/b/" },
        { "/a/b/..", "/a/" },
    };
}
