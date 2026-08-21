using System.Text;
using NormalizeUrl.Internal;
using static NormalizeUrl.Internal.UrlNormalizationConstants;

namespace NormalizeUrl;

/// <summary>
/// Normalizes URLs into a canonical string form suitable for equality checks, deduplication and
/// cache keys. The default profile only applies transforms that can never change what a URL
/// addresses: scheme and host case-folding, default-port removal, dot-segment resolution,
/// percent-encoding normalization, trailing-dot removal on the host, canonicalizing an empty
/// path to "/" when an authority is present, and dropping an empty query or fragment marker.
/// Every other transform is opt-in through <see cref="NormalizeUrlOptions"/>.
/// </summary>
public static class UrlNormalizer
{
    /// <summary>
    /// Normalizes <paramref name="url"/> into its canonical string form.
    /// </summary>
    /// <param name="url">An absolute URL, including its scheme (for example <c>https://Example.com/</c>).</param>
    /// <param name="options">
    /// The opt-in transforms to apply on top of the default profile. Pass <see langword="null"/>
    /// or omit to use <see cref="NormalizeUrlOptions.Default"/>.
    /// </param>
    /// <returns>The canonical form of <paramref name="url"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="url"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="url"/> is empty or whitespace.</exception>
    /// <exception cref="FormatException">
    /// <paramref name="url"/> is not a structurally valid, absolute URL: it is missing a scheme,
    /// has an invalid scheme, an invalid port, an unterminated IPv6 host literal, or an invalid
    /// percent-encoded sequence.
    /// </exception>
    public static string Normalize(string url, NormalizeUrlOptions? options = null)
    {
        var effectiveOptions = options ?? NormalizeUrlOptions.Default;
        var parsed = RfcUriParser.Parse(url);

        var scheme = parsed.Scheme.ToLowerInvariant();
        if (effectiveOptions.ForceHttps && scheme == HttpScheme)
        {
            scheme = HttpsScheme;
        }

        var userInfo = PercentEncodingNormalizer.Normalize(parsed.UserInfo);

        var host = LowercaseHostPreservingPercentEncoding(PercentEncodingNormalizer.Normalize(parsed.Host));
        host = TrimTrailingHostDot(host);
        if (effectiveOptions.StripWwwPrefix)
        {
            host = StripWwwPrefix(host);
        }

        var hasPort = parsed.HasPort && !DefaultSchemePorts.IsDefaultPort(scheme, parsed.Port);

        var path = PercentEncodingNormalizer.Normalize(parsed.Path);
        if (parsed.HasAuthority || (path.Length > 0 && path[0] == PathSegmentDelimiter))
        {
            // remove_dot_segments (RFC 3986 5.2.4) is defined for merging resolved reference
            // paths against a hierarchical base. Applying it to a rootless path with no
            // authority (an opaque mailto:/urn: path, for example) can fabricate a leading '/'
            // that was never there, changing what the URI identifies. Only run it where the
            // path is unambiguously hierarchical.
            path = DotSegmentResolver.RemoveDotSegments(path);
        }

        if (parsed.HasAuthority && path.Length == 0 && DefaultSchemePorts.IsSpecialScheme(scheme))
        {
            // RFC 3986 6.2.3: for schemes with hierarchical path semantics, an authority
            // followed by an empty path is equivalent to an authority followed by "/".
            path = PathSegmentDelimiter.ToString();
        }

        if (effectiveOptions.StripTrailingSlash && path.Length > 1 && path[^1] == PathSegmentDelimiter)
        {
            path = path[..^1];
        }

        var hasQuery = parsed.HasQuery;
        var query = PercentEncodingNormalizer.Normalize(parsed.Query);
        var removesQueryParameters =
            effectiveOptions.QueryParametersToRemove.Count > 0 || effectiveOptions.QueryParameterMatcher is not null;
        if (hasQuery && removesQueryParameters)
        {
            query = QueryStringNormalizer.RemoveParameters(
                query,
                effectiveOptions.QueryParametersToRemove,
                effectiveOptions.QueryParameterMatcher);
        }

        if (hasQuery && effectiveOptions.SortQueryParameters)
        {
            query = QueryStringNormalizer.Sort(query);
        }

        if (hasQuery && query.Length == 0)
        {
            hasQuery = false;
        }

        var hasFragment = parsed.HasFragment && !effectiveOptions.StripFragment;
        var fragment = hasFragment ? PercentEncodingNormalizer.Normalize(parsed.Fragment) : string.Empty;
        if (hasFragment && fragment.Length == 0)
        {
            hasFragment = false;
        }

        return Compose(
            scheme,
            parsed.HasAuthority,
            parsed.HasUserInfo,
            userInfo,
            host,
            hasPort,
            parsed.Port,
            path,
            hasQuery,
            query,
            hasFragment,
            fragment);
    }

    /// <summary>
    /// Attempts to normalize <paramref name="url"/> without throwing when it is missing,
    /// malformed or not absolute.
    /// </summary>
    /// <param name="url">The URL to normalize, or <see langword="null"/>.</param>
    /// <param name="normalized">
    /// When this method returns <see langword="true"/>, the canonical form of
    /// <paramref name="url"/>; otherwise <see langword="null"/>.
    /// </param>
    /// <param name="options">
    /// The opt-in transforms to apply on top of the default profile. Pass <see langword="null"/>
    /// or omit to use <see cref="NormalizeUrlOptions.Default"/>.
    /// </param>
    /// <returns><see langword="true"/> if <paramref name="url"/> normalized successfully.</returns>
    public static bool TryNormalize(string? url, out string? normalized, NormalizeUrlOptions? options = null)
    {
        if (url is null)
        {
            normalized = null;
            return false;
        }

        try
        {
            normalized = Normalize(url, options);
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException)
        {
            normalized = null;
            return false;
        }
    }

    /// <summary>
    /// Determines whether <paramref name="first"/> and <paramref name="second"/> normalize to
    /// the same canonical form.
    /// </summary>
    /// <param name="first">The first URL to compare.</param>
    /// <param name="second">The second URL to compare.</param>
    /// <param name="options">
    /// The opt-in transforms to apply on top of the default profile. Pass <see langword="null"/>
    /// or omit to use <see cref="NormalizeUrlOptions.Default"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="first"/> and <paramref name="second"/> normalize
    /// to the same canonical form.
    /// </returns>
    /// <exception cref="ArgumentNullException">Either argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Either argument is empty or whitespace.</exception>
    /// <exception cref="FormatException">Either argument is not a structurally valid, absolute URL.</exception>
    public static bool AreEquivalent(string first, string second, NormalizeUrlOptions? options = null) =>
        string.Equals(Normalize(first, options), Normalize(second, options), StringComparison.Ordinal);

    private static string LowercaseHostPreservingPercentEncoding(string host)
    {
        if (host.Length == 0)
        {
            return host;
        }

        var builder = new StringBuilder(host.Length);

        for (var i = 0; i < host.Length; i++)
        {
            var current = host[i];

            // PercentEncodingNormalizer.Normalize has already run and guarantees any '%' here
            // is followed by two valid, already-uppercased hex digits; skip them so a case-fold
            // of the host doesn't undo that uppercasing.
            if (current == PercentEncodingPrefix && i + 2 < host.Length)
            {
                builder.Append(current).Append(host[i + 1]).Append(host[i + 2]);
                i += 2;
            }
            else
            {
                builder.Append(char.ToLowerInvariant(current));
            }
        }

        return builder.ToString();
    }

    private static string TrimTrailingHostDot(string host) =>
        host.Length > 0 && host[^1] == HostLabelSeparator ? host[..^1] : host;

    private static string StripWwwPrefix(string host) =>
        host.StartsWith(WwwHostPrefix, StringComparison.Ordinal) ? host[WwwHostPrefix.Length..] : host;

    private static string Compose(
        string scheme,
        bool hasAuthority,
        bool hasUserInfo,
        string userInfo,
        string host,
        bool hasPort,
        int port,
        string path,
        bool hasQuery,
        string query,
        bool hasFragment,
        string fragment)
    {
        var builder = new StringBuilder();
        builder.Append(scheme).Append(SchemeDelimiter);

        if (hasAuthority)
        {
            builder.Append(PathSegmentDelimiter).Append(PathSegmentDelimiter);

            if (hasUserInfo)
            {
                builder.Append(userInfo).Append(AuthorityUserInfoDelimiter);
            }

            builder.Append(host);

            if (hasPort)
            {
                builder.Append(AuthorityPortDelimiter).Append(port);
            }
        }

        builder.Append(path);

        if (hasQuery)
        {
            builder.Append(QueryDelimiter).Append(query);
        }

        if (hasFragment)
        {
            builder.Append(FragmentDelimiter).Append(fragment);
        }

        return builder.ToString();
    }
}
