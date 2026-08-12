using System.Text;
using static NormalizeUrl.Internal.UrlNormalizationConstants;

namespace NormalizeUrl.Internal;

/// <summary>
/// Implements RFC 3986 section 6.2.2.2 percent-encoding normalization: a percent-encoded
/// octet that maps to an unreserved character is decoded to that literal character, and any
/// percent-encoded octet that is kept escaped has its hex digits uppercased.
/// </summary>
internal static class PercentEncodingNormalizer
{
    internal static string Normalize(string component)
    {
        if (component.Length == 0)
        {
            return component;
        }

        StringBuilder? builder = null;

        for (var i = 0; i < component.Length; i++)
        {
            var current = component[i];

            if (current != PercentEncodingPrefix)
            {
                builder?.Append(current);
                continue;
            }

            if (i + 2 >= component.Length || !IsHexDigit(component[i + 1]) || !IsHexDigit(component[i + 2]))
            {
                throw new FormatException(
                    $"Invalid percent-encoding at position {i} in \"{component}\": a '%' must be followed by two hexadecimal digits.");
            }

            var decodedValue = (HexValue(component[i + 1]) << 4) | HexValue(component[i + 2]);

            builder ??= new StringBuilder(component, 0, i, component.Length);

            if (IsUnreserved((char)decodedValue))
            {
                builder.Append((char)decodedValue);
            }
            else
            {
                builder.Append(PercentEncodingPrefix);
                builder.Append(char.ToUpperInvariant(component[i + 1]));
                builder.Append(char.ToUpperInvariant(component[i + 2]));
            }

            i += 2;
        }

        return builder?.ToString() ?? component;
    }

    private static bool IsUnreserved(char value) =>
        (value >= 'A' && value <= 'Z') ||
        (value >= 'a' && value <= 'z') ||
        (value >= '0' && value <= '9') ||
        value is '-' or '.' or '_' or '~';

    private static bool IsHexDigit(char value) =>
        (value >= '0' && value <= '9') ||
        (value >= 'a' && value <= 'f') ||
        (value >= 'A' && value <= 'F');

    private static int HexValue(char value) => value switch
    {
        >= '0' and <= '9' => value - '0',
        >= 'a' and <= 'f' => value - 'a' + 10,
        >= 'A' and <= 'F' => value - 'A' + 10,
        _ => throw new FormatException($"'{value}' is not a hexadecimal digit."),
    };
}
