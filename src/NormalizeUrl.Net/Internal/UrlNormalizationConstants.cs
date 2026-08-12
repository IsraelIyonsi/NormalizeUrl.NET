namespace NormalizeUrl.Internal;

/// <summary>
/// Named constants shared across the normalization pipeline. Kept internal so the public
/// surface stays small; values are referenced by name everywhere instead of being repeated
/// as literals.
/// </summary>
internal static class UrlNormalizationConstants
{
    internal const string HttpScheme = "http";

    internal const string HttpsScheme = "https";

    internal const string WwwHostPrefix = "www.";

    internal const char HostLabelSeparator = '.';

    internal const char SchemeDelimiter = ':';

    internal const char AuthorityUserInfoDelimiter = '@';

    internal const char AuthorityPortDelimiter = ':';

    internal const char PathSegmentDelimiter = '/';

    internal const char QueryDelimiter = '?';

    internal const char FragmentDelimiter = '#';

    internal const char PercentEncodingPrefix = '%';

    internal const char QueryPairSeparator = '&';

    internal const char QueryKeyValueSeparator = '=';

    internal const char IPv6HostOpenBracket = '[';

    internal const char IPv6HostCloseBracket = ']';

    internal const int MinPortNumber = 0;

    internal const int MaxPortNumber = 65535;
}
