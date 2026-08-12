using System.Globalization;
using System.Text.RegularExpressions;
using static NormalizeUrl.Internal.UrlNormalizationConstants;

namespace NormalizeUrl.Internal;

/// <summary>
/// Splits a URL into its generic-syntax components using the reference regular expression
/// given in RFC 3986 Appendix B, then further splits the authority into userinfo, host and
/// port. Performs only structural parsing; no normalization happens here.
/// </summary>
/// <remarks>
/// Userinfo is split from the rest of the authority on the last '@', matching RFC 3986's
/// generic-syntax grammar. A raw, unencoded '@' inside userinfo is not itself rejected as
/// malformed: it is accepted and treated as part of the userinfo, so parsing stays lenient and
/// round-trip-stable rather than throwing on structurally unusual but unambiguous input. See
/// <c>Parse_SplitsUserInfoOnLastAtSign</c> for the pinned behavior.
/// </remarks>
internal static partial class RfcUriParser
{
    [GeneratedRegex("^(([^:/?#]+):)?(//([^/?#]*))?([^?#]*)(\\?([^#]*))?(#(.*))?$", RegexOptions.Compiled)]
    private static partial Regex GenericSyntaxRegex();

    internal static ParsedUrl Parse(string url)
    {
        ArgumentNullException.ThrowIfNull(url);

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL must not be empty or consist only of whitespace.", nameof(url));
        }

        var match = GenericSyntaxRegex().Match(url);

        var schemeGroup = match.Groups[2];
        if (!schemeGroup.Success || schemeGroup.Value.Length == 0)
        {
            throw new FormatException($"URL \"{url}\" is not absolute: it must start with a scheme such as \"https:\".");
        }

        var scheme = schemeGroup.Value;
        ValidateScheme(url, scheme);

        var hasAuthority = match.Groups[3].Success;
        var authority = match.Groups[4].Value;
        var path = match.Groups[5].Value;
        var hasQuery = match.Groups[6].Success;
        var query = match.Groups[7].Value;
        var hasFragment = match.Groups[8].Success;
        var fragment = match.Groups[9].Value;

        var hasUserInfo = false;
        var userInfo = string.Empty;
        var host = string.Empty;
        var hasPort = false;
        var port = 0;

        if (hasAuthority)
        {
            var remainder = authority;

            var userInfoDelimiterIndex = remainder.LastIndexOf(AuthorityUserInfoDelimiter);
            if (userInfoDelimiterIndex >= 0)
            {
                hasUserInfo = true;
                userInfo = remainder[..userInfoDelimiterIndex];
                remainder = remainder[(userInfoDelimiterIndex + 1)..];
            }

            (host, hasPort, port) = ParseHostAndPort(url, remainder);
        }

        return new ParsedUrl
        {
            Scheme = scheme,
            HasAuthority = hasAuthority,
            HasUserInfo = hasUserInfo,
            UserInfo = userInfo,
            Host = host,
            HasPort = hasPort,
            Port = port,
            Path = path,
            HasQuery = hasQuery,
            Query = query,
            HasFragment = hasFragment,
            Fragment = fragment,
        };
    }

    private static (string Host, bool HasPort, int Port) ParseHostAndPort(string url, string authorityRemainder)
    {
        if (authorityRemainder.Length > 0 && authorityRemainder[0] == IPv6HostOpenBracket)
        {
            var closingBracketIndex = authorityRemainder.IndexOf(IPv6HostCloseBracket);
            if (closingBracketIndex < 0)
            {
                throw new FormatException($"URL \"{url}\" has an unterminated IPv6 literal host.");
            }

            var host = authorityRemainder[..(closingBracketIndex + 1)];
            var afterHost = authorityRemainder[(closingBracketIndex + 1)..];

            if (afterHost.Length == 0)
            {
                return (host, false, 0);
            }

            if (afterHost[0] != AuthorityPortDelimiter)
            {
                throw new FormatException($"URL \"{url}\" has an invalid authority component after its IPv6 host.");
            }

            var (hasPort, port) = ParsePort(url, afterHost[1..]);
            return (host, hasPort, port);
        }

        var portDelimiterIndex = authorityRemainder.IndexOf(AuthorityPortDelimiter);
        if (portDelimiterIndex < 0)
        {
            return (authorityRemainder, false, 0);
        }

        var (hostHasPort, hostPort) = ParsePort(url, authorityRemainder[(portDelimiterIndex + 1)..]);
        return (authorityRemainder[..portDelimiterIndex], hostHasPort, hostPort);
    }

    private static (bool HasPort, int Port) ParsePort(string url, string portText)
    {
        if (portText.Length == 0)
        {
            return (false, 0);
        }

        if (!int.TryParse(portText, NumberStyles.None, CultureInfo.InvariantCulture, out var port) ||
            port < MinPortNumber || port > MaxPortNumber)
        {
            throw new FormatException($"URL \"{url}\" has an invalid port \"{portText}\".");
        }

        return (true, port);
    }

    private static void ValidateScheme(string url, string scheme)
    {
        var first = scheme[0];
        var isValid = first is >= 'A' and <= 'Z' or >= 'a' and <= 'z';

        for (var i = 1; isValid && i < scheme.Length; i++)
        {
            var c = scheme[i];
            isValid = c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '+' or '-' or '.';
        }

        if (!isValid)
        {
            throw new FormatException(
                $"URL \"{url}\" has an invalid scheme \"{scheme}\": a scheme must start with a letter and contain only letters, digits, '+', '-' or '.'.");
        }
    }
}
