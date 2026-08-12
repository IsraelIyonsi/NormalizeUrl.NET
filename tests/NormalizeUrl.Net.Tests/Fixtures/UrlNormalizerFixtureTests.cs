namespace NormalizeUrl.Tests.Fixtures;

/// <summary>
/// A fixture table of input/expected-output pairs, grouped by named option profile, in the
/// style of the widely used normalize-url npm package's own test suite: many small, concrete
/// before/after examples rather than assertions about internal structure. Every row is asserted
/// for exact string equality, no approximate or "contains" checks.
/// </summary>
public sealed class UrlNormalizerFixtureTests
{
    private static readonly IReadOnlyDictionary<string, NormalizeUrlOptions> Profiles = new Dictionary<string, NormalizeUrlOptions>(StringComparer.Ordinal)
    {
        ["Default"] = NormalizeUrlOptions.Default,
        ["SortQuery"] = new NormalizeUrlOptions { SortQueryParameters = true },
        ["StripTrailingSlash"] = new NormalizeUrlOptions { StripTrailingSlash = true },
        ["StripFragment"] = new NormalizeUrlOptions { StripFragment = true },
        ["ForceHttps"] = new NormalizeUrlOptions { ForceHttps = true },
        ["StripWww"] = new NormalizeUrlOptions { StripWwwPrefix = true },
        ["RemoveTrackingParams"] = new NormalizeUrlOptions { QueryParametersToRemove = NormalizeUrlOptions.UtmTrackingParameters },
        ["Combined"] = new NormalizeUrlOptions
        {
            SortQueryParameters = true,
            StripTrailingSlash = true,
            StripFragment = true,
            ForceHttps = true,
            StripWwwPrefix = true,
            QueryParametersToRemove = NormalizeUrlOptions.UtmTrackingParameters,
        },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Normalize_MatchesFixtureUnderNamedProfile(string profile, string input, string expected)
    {
        var options = Profiles[profile];

        var actual = UrlNormalizer.Normalize(input, options);

        Assert.Equal(expected, actual);
    }

    public static TheoryData<string, string, string> Cases()
    {
        var data = new TheoryData<string, string, string>();

        foreach (var (profile, input, expected) in DefaultProfileCases())
        {
            data.Add(profile, input, expected);
        }

        foreach (var (profile, input, expected) in OptInProfileCases())
        {
            data.Add(profile, input, expected);
        }

        return data;
    }

    private static IEnumerable<(string Profile, string Input, string Expected)> DefaultProfileCases()
    {
        const string profile = "Default";

        yield return (profile, "HTTP://EXAMPLE.COM/", "http://example.com/");
        yield return (profile, "http://EXAMPLE.com/Path", "http://example.com/Path");
        yield return (profile, "http://example.com:80/path", "http://example.com/path");
        yield return (profile, "https://example.com:443/path", "https://example.com/path");
        yield return (profile, "ftp://example.com:21/file", "ftp://example.com/file");
        yield return (profile, "ws://example.com:80/socket", "ws://example.com/socket");
        yield return (profile, "wss://example.com:443/socket", "wss://example.com/socket");
        yield return (profile, "http://example.com:8080/path", "http://example.com:8080/path");
        yield return (profile, "https://example.com:80/path", "https://example.com:80/path");
        yield return (profile, "http://example.com/a/b/../c", "http://example.com/a/c");
        yield return (profile, "https://example.com/../../..", "https://example.com/");
        yield return (profile, "https://example.com/a/./b/./c", "https://example.com/a/b/c");
        yield return (profile, "http://example.com/%7Euser", "http://example.com/~user");
        yield return (profile, "http://example.com/path%2fname", "http://example.com/path%2Fname");
        yield return (profile, "http://example.com/a/%2E%2E/b", "http://example.com/b");
        yield return (profile, "https://example.com/a%20b?c=d%26e", "https://example.com/a%20b?c=d%26e");
        yield return (profile, "http://example.com./path", "http://example.com/path");
        yield return (profile, "http://example.com./", "http://example.com/");
        yield return (profile, "http://example.com/path?", "http://example.com/path");
        yield return (profile, "http://example.com/path#", "http://example.com/path");
        yield return (profile, "http://example.com/path?#", "http://example.com/path");
        yield return (profile, "http://example.com/path?b=2&a=1#top", "http://example.com/path?b=2&a=1#top");
        yield return (profile, "http://www.example.com/", "http://www.example.com/");
        yield return (profile, "http://example.com/", "http://example.com/");
        yield return (profile, "http://example.com/path/", "http://example.com/path/");
        yield return (profile, "http://[2001:DB8::1]/path", "http://[2001:db8::1]/path");
        yield return (profile, "http://user%40name:pa%2Fss@example.com/", "http://user%40name:pa%2Fss@example.com/");
        yield return (profile, "https://example.com:443", "https://example.com/");
        yield return (profile, "MAILTO:User@Example.com", "mailto:User@Example.com");

        // Percent-encoded hosts: unreserved octets decode, everything else keeps uppercase hex
        // (regression coverage for the host lowercasing running before percent normalization).
        yield return (profile, "http://ex%c3%a9mple.com/", "http://ex%C3%A9mple.com/");
        yield return (profile, "http://EX%41MPLE.com/", "http://example.com/");

        // RFC 3986 6.2.3: authority with an empty path is equivalent to authority + "/" for
        // schemes with hierarchical path semantics.
        yield return (profile, "http://example.com", "http://example.com/");
        yield return (profile, "ftp://example.com", "ftp://example.com/");
        yield return (profile, "wss://example.com", "wss://example.com/");

        // Opaque, rootless paths on non-authority URIs must not have dot segments resolved:
        // doing so could fabricate a leading '/' that changes the opaque part's meaning.
        yield return (profile, "urn:example:a/../b", "urn:example:a/../b");
        yield return (profile, "mailto:a/../b@example.com", "mailto:a/../b@example.com");

        // Port 0 is structurally valid per this parser's accepted range and is kept as-is.
        yield return (profile, "http://example.com:0/path", "http://example.com:0/path");
    }

    private static IEnumerable<(string Profile, string Input, string Expected)> OptInProfileCases()
    {
        yield return ("SortQuery", "http://example.com/?b=2&a=1", "http://example.com/?a=1&b=2");
        yield return ("SortQuery", "http://example.com/?a=1&a=0", "http://example.com/?a=0&a=1");
        yield return ("SortQuery", "http://example.com/?z&a=1", "http://example.com/?a=1&z");

        yield return ("StripTrailingSlash", "http://example.com/path/", "http://example.com/path");
        yield return ("StripTrailingSlash", "http://example.com/", "http://example.com/");
        yield return ("StripTrailingSlash", "http://example.com/a/b/", "http://example.com/a/b");

        yield return ("StripFragment", "http://example.com/path#section", "http://example.com/path");
        yield return ("StripFragment", "http://example.com/path#", "http://example.com/path");

        yield return ("ForceHttps", "http://example.com/", "https://example.com/");
        yield return ("ForceHttps", "http://example.com:80/", "https://example.com:80/");
        yield return ("ForceHttps", "https://example.com/", "https://example.com/");
        yield return ("ForceHttps", "ftp://example.com/", "ftp://example.com/");

        // Pins the intended, semantics-changing effect of ForceHttps: the scheme is rewritten
        // to https before the default-port check runs, so an explicit :443 on an http:// URL is
        // absorbed as the new scheme's default port rather than preserved. ForceHttps is opt-in
        // precisely because it can change what the URL addresses; this is that in action.
        yield return ("ForceHttps", "http://example.com:443", "https://example.com/");

        yield return ("StripWww", "http://www.example.com/", "http://example.com/");
        yield return ("StripWww", "http://example.com/", "http://example.com/");
        yield return ("StripWww", "http://www.www.example.com/", "http://www.example.com/");

        yield return ("RemoveTrackingParams", "http://example.com/?utm_source=x&utm_medium=y&id=1", "http://example.com/?id=1");
        yield return ("RemoveTrackingParams", "http://example.com/?utm_source=x", "http://example.com/");
        yield return ("RemoveTrackingParams", "http://example.com/?id=1", "http://example.com/?id=1");

        yield return (
            "Combined",
            "http://WWW.Example.COM:80/path/?utm_source=x&b=2&a=1#section",
            "https://example.com:80/path?a=1&b=2");
    }
}
