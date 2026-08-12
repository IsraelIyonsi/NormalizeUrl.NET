namespace NormalizeUrl;

/// <summary>
/// Configures which transforms <see cref="UrlNormalizer"/> applies on top of its safe default
/// profile. Every property defaults to off, so a plain <c>new NormalizeUrlOptions()</c> (or
/// <see cref="Default"/>) behaves identically to passing no options at all: the default profile
/// never changes what a URL means, only how it is spelled.
/// </summary>
public sealed class NormalizeUrlOptions
{
    /// <summary>
    /// The default options: every opt-in transform disabled. Equivalent to passing no options
    /// to <see cref="UrlNormalizer.Normalize"/>.
    /// </summary>
    public static NormalizeUrlOptions Default { get; } = new();

    /// <summary>
    /// The classic UTM query parameter names (<c>utm_source</c>, <c>utm_medium</c>,
    /// <c>utm_campaign</c>, <c>utm_term</c>, <c>utm_content</c>), provided as a convenience
    /// value for <see cref="QueryParametersToRemove"/>.
    /// </summary>
    public static IReadOnlyList<string> UtmTrackingParameters { get; } =
    [
        "utm_source",
        "utm_medium",
        "utm_campaign",
        "utm_term",
        "utm_content",
    ];

    /// <summary>
    /// When <see langword="true"/>, query parameters are reordered by key, then by value, using
    /// ordinal comparison. Off by default because query parameter order can be meaningful to
    /// some servers.
    /// </summary>
    public bool SortQueryParameters { get; init; }

    /// <summary>
    /// When <see langword="true"/>, a single trailing slash is removed from the path, unless the
    /// path is exactly "/". Off by default because "/a/" and "/a" are not guaranteed to be the
    /// same resource.
    /// </summary>
    public bool StripTrailingSlash { get; init; }

    /// <summary>
    /// When <see langword="true"/>, the fragment is removed entirely, regardless of its content.
    /// Off by default because the fragment can carry meaning the server never sees but the
    /// client does (for example single-page-app routes).
    /// </summary>
    public bool StripFragment { get; init; }

    /// <summary>
    /// When <see langword="true"/>, the <c>http</c> scheme is rewritten to <c>https</c>. Off by
    /// default because it changes which endpoint the URL addresses.
    /// </summary>
    public bool ForceHttps { get; init; }

    /// <summary>
    /// When <see langword="true"/>, a leading <c>www.</c> label is removed from the host. Off by
    /// default because <c>www.example.com</c> and <c>example.com</c> are not guaranteed to be
    /// the same host.
    /// </summary>
    public bool StripWwwPrefix { get; init; }

    /// <summary>
    /// Query parameter names to remove entirely, compared ordinally and case-sensitively. Empty
    /// by default, which disables the transform. Use <see cref="UtmTrackingParameters"/> to
    /// strip the common UTM tracking family.
    /// </summary>
    public IReadOnlyCollection<string> QueryParametersToRemove { get; init; } = [];
}
