namespace NormalizeUrl.Internal;

/// <summary>
/// The generic-syntax components of a URL as decomposed by <see cref="RfcUriParser"/>, before
/// any normalization is applied. "Has" flags distinguish an absent component from one that is
/// present but empty, since the two are not equivalent in the generic URI syntax.
/// </summary>
internal sealed class ParsedUrl
{
    internal required string Scheme { get; init; }

    internal required bool HasAuthority { get; init; }

    internal required bool HasUserInfo { get; init; }

    internal required string UserInfo { get; init; }

    internal required string Host { get; init; }

    internal required bool HasPort { get; init; }

    internal required int Port { get; init; }

    internal required string Path { get; init; }

    internal required bool HasQuery { get; init; }

    internal required string Query { get; init; }

    internal required bool HasFragment { get; init; }

    internal required string Fragment { get; init; }
}
