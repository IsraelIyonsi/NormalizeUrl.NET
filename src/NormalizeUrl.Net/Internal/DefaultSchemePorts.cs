using static NormalizeUrl.Internal.UrlNormalizationConstants;

namespace NormalizeUrl.Internal;

/// <summary>
/// Maps well-known URI schemes to the port they use by default, so an explicit port matching
/// the scheme default can be dropped without changing where the URL points.
/// </summary>
internal static class DefaultSchemePorts
{
    private const int HttpDefaultPort = 80;
    private const int HttpsDefaultPort = 443;
    private const int FtpDefaultPort = 21;
    private const int WsDefaultPort = 80;
    private const int WssDefaultPort = 443;

    private static readonly IReadOnlyDictionary<string, int> PortsByScheme = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        [HttpScheme] = HttpDefaultPort,
        [HttpsScheme] = HttpsDefaultPort,
        ["ftp"] = FtpDefaultPort,
        ["ws"] = WsDefaultPort,
        ["wss"] = WssDefaultPort,
    };

    internal static bool IsDefaultPort(string scheme, int port) =>
        PortsByScheme.TryGetValue(scheme, out var defaultPort) && defaultPort == port;

    /// <summary>
    /// Determines whether <paramref name="scheme"/> is one of the schemes this library
    /// recognizes as having hierarchical authority-and-path semantics (the same schemes that
    /// define a default port). Per RFC 3986 section 6.2.3, for these schemes an authority
    /// followed by an empty path is equivalent to an authority followed by "/", so this also
    /// drives that canonicalization.
    /// </summary>
    internal static bool IsSpecialScheme(string scheme) => PortsByScheme.ContainsKey(scheme);
}
