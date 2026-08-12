using System.Text;
using static NormalizeUrl.Internal.UrlNormalizationConstants;

namespace NormalizeUrl.Internal;

/// <summary>
/// Implements the remove_dot_segments algorithm from RFC 3986 section 5.2.4, used to collapse
/// "." and ".." path segments into their canonical form.
/// </summary>
internal static class DotSegmentResolver
{
    private const string SingleDotSegment = ".";
    private const string DoubleDotSegment = "..";
    private const string SingleDotSegmentWithTrailingSlash = "./";
    private const string DoubleDotSegmentWithTrailingSlash = "../";
    private const string SlashSingleDotSegment = "/.";
    private const string SlashSingleDotSegmentWithTrailingSlash = "/./";
    private const string SlashDoubleDotSegment = "/..";
    private const string SlashDoubleDotSegmentWithTrailingSlash = "/../";

    internal static string RemoveDotSegments(string path)
    {
        if (path.Length == 0)
        {
            return path;
        }

        var input = path;
        var output = new StringBuilder(path.Length);

        while (input.Length > 0)
        {
            if (input.StartsWith(DoubleDotSegmentWithTrailingSlash, StringComparison.Ordinal))
            {
                input = input[DoubleDotSegmentWithTrailingSlash.Length..];
            }
            else if (input.StartsWith(SingleDotSegmentWithTrailingSlash, StringComparison.Ordinal))
            {
                input = input[SingleDotSegmentWithTrailingSlash.Length..];
            }
            else if (input.StartsWith(SlashSingleDotSegmentWithTrailingSlash, StringComparison.Ordinal))
            {
                input = PathSegmentDelimiter + input[SlashSingleDotSegmentWithTrailingSlash.Length..];
            }
            else if (input == SlashSingleDotSegment)
            {
                input = PathSegmentDelimiter.ToString();
            }
            else if (input.StartsWith(SlashDoubleDotSegmentWithTrailingSlash, StringComparison.Ordinal))
            {
                input = PathSegmentDelimiter + input[SlashDoubleDotSegmentWithTrailingSlash.Length..];
                RemoveLastSegment(output);
            }
            else if (input == SlashDoubleDotSegment)
            {
                input = PathSegmentDelimiter.ToString();
                RemoveLastSegment(output);
            }
            else if (input is SingleDotSegment or DoubleDotSegment)
            {
                input = string.Empty;
            }
            else
            {
                var segmentStart = input[0] == PathSegmentDelimiter ? 1 : 0;
                var nextSlash = input.IndexOf(PathSegmentDelimiter, segmentStart);

                if (nextSlash == -1)
                {
                    output.Append(input);
                    input = string.Empty;
                }
                else
                {
                    output.Append(input, 0, nextSlash);
                    input = input[nextSlash..];
                }
            }
        }

        return output.ToString();
    }

    private static void RemoveLastSegment(StringBuilder output)
    {
        var lastSlash = -1;

        for (var i = output.Length - 1; i >= 0; i--)
        {
            if (output[i] == PathSegmentDelimiter)
            {
                lastSlash = i;
                break;
            }
        }

        output.Length = lastSlash >= 0 ? lastSlash : 0;
    }
}
