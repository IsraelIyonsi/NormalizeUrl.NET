using static NormalizeUrl.Internal.UrlNormalizationConstants;

namespace NormalizeUrl.Internal;

/// <summary>
/// Structural transforms over a query string: removing named parameters and sorting the
/// remaining parameters into a stable, comparable order. Each transform preserves whether a
/// parameter carried an explicit value (<c>key=</c>) versus none (<c>key</c>).
/// </summary>
internal static class QueryStringNormalizer
{
    internal static string RemoveParameters(
        string query,
        IReadOnlyCollection<string> namesToRemove,
        Func<string, bool>? nameMatcher)
    {
        if (query.Length == 0 || (namesToRemove.Count == 0 && nameMatcher is null))
        {
            return query;
        }

        var kept = SplitPairs(query).Where(pair => !ShouldRemove(pair.Key, namesToRemove, nameMatcher));
        return Join(kept);
    }

    private static bool ShouldRemove(
        string name,
        IReadOnlyCollection<string> namesToRemove,
        Func<string, bool>? nameMatcher) =>
        namesToRemove.Contains(name, StringComparer.Ordinal) || (nameMatcher is not null && nameMatcher(name));

    internal static string Sort(string query)
    {
        if (query.Length == 0)
        {
            return query;
        }

        var pairs = SplitPairs(query).ToList();
        pairs.Sort((left, right) =>
        {
            var keyComparison = string.CompareOrdinal(left.Key, right.Key);
            return keyComparison != 0 ? keyComparison : string.CompareOrdinal(left.Value, right.Value);
        });

        return Join(pairs);
    }

    private static IEnumerable<QueryParameter> SplitPairs(string query)
    {
        foreach (var rawPair in query.Split(QueryPairSeparator))
        {
            var separatorIndex = rawPair.IndexOf(QueryKeyValueSeparator);

            yield return separatorIndex < 0
                ? new QueryParameter(rawPair, false, string.Empty)
                : new QueryParameter(rawPair[..separatorIndex], true, rawPair[(separatorIndex + 1)..]);
        }
    }

    private static string Join(IEnumerable<QueryParameter> pairs) =>
        string.Join(QueryPairSeparator, pairs.Select(pair => pair.ToString()));

    private readonly record struct QueryParameter(string Key, bool HasValue, string Value)
    {
        public override string ToString() => HasValue ? $"{Key}={Value}" : Key;
    }
}
