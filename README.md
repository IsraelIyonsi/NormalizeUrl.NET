# NormalizeUrl.NET

URL normalization for .NET: canonicalize URLs for equality checks, deduplication and cache keys, with configurable, safe-by-default transforms. Zero external dependencies.

The same URL shows up spelled a dozen different ways: `HTTP://Example.com:80/a/./b`, `http://example.com/a/b`, `http://example.com/a//b/../b`. If you dedupe crawled links, build a cache key from a request URL, or compare two URLs for equality, string comparison alone gets this wrong constantly. `normalize-url` solved this years ago for Node; .NET never got an equivalent that people actually reach for; most projects end up with a half-finished `Uri` wrapper that mangles percent-encoding or silently drops query strings. NormalizeUrl.NET is a small, dependency-free, RFC 3986-correct implementation you can trust with production traffic.

Everything the default profile does is safe: it can never make two URLs that pointed at different resources collapse into the same string, and it can never change what a URL means. Anything that could change meaning (sorting query parameters, forcing HTTPS, stripping `www.`, dropping tracking parameters) is opt-in.

## Install

```
dotnet add package NormalizeUrl.Net
```

## Usage

### Canonicalize a URL

```csharp
using NormalizeUrl;

string canonical = UrlNormalizer.Normalize("HTTP://Example.com:80/a/./b/../c?");
// "http://example.com/a/c"
```

Scheme and host are lowercased, the default port for `http` is dropped, the dot-segments in the path resolve the same way a browser would resolve them, and the empty trailing `?` disappears. Nothing here can change which resource the URL points to.

### Deduplicate a list of crawled links

```csharp
using NormalizeUrl;

var seen = new HashSet<string>();
var unique = new List<string>();

foreach (var url in crawledUrls)
{
    var key = UrlNormalizer.Normalize(url);
    if (seen.Add(key))
    {
        unique.Add(url);
    }
}
```

### Compare two URLs, ignoring tracking parameters and query order

```csharp
using NormalizeUrl;

var options = new NormalizeUrlOptions
{
    SortQueryParameters = true,
    QueryParametersToRemove = NormalizeUrlOptions.UtmTrackingParameters,
};

bool same = UrlNormalizer.AreEquivalent(
    "https://shop.example.com/item?utm_source=newsletter&id=42",
    "https://shop.example.com/item?id=42&utm_source=email",
    options);
// true
```

### Strip open-ended tracking parameters by pattern

Real tracking keys are open-ended (`utm_source`, `utm_medium`, `fbclid`, `gclid`, `mc_eid`, ...), so a fixed name list cannot cover them. `QueryParameterMatcher` takes a predicate over the parameter name; return `true` to drop it. It is unioned with `QueryParametersToRemove`, and the name is passed verbatim, so the match is case-sensitive unless your predicate folds case.

```csharp
using NormalizeUrl;

var options = new NormalizeUrlOptions
{
    QueryParameterMatcher = name => name.StartsWith("utm_", StringComparison.Ordinal),
};

string canonical = UrlNormalizer.Normalize(
    "https://shop.example.com/item?utm_source=news&utm_medium=email&id=42",
    options);
// "https://shop.example.com/item?id=42"
```

### Build a stable cache key from a request URL

```csharp
using NormalizeUrl;

var options = new NormalizeUrlOptions { SortQueryParameters = true, StripFragment = true };

string cacheKey = UrlNormalizer.Normalize(request.Url, options);
```

## What the default profile does

Applied unconditionally, because none of it can change what a URL points to:

- Lowercase the scheme and host
- Remove the default port for the scheme (`:80` for `http`/`ws`, `:443` for `https`/`wss`, `:21` for `ftp`)
- Resolve `.` and `..` path segments using the RFC 3986 section 5.2.4 algorithm
- Decode percent-encoded octets that represent unreserved characters (`%7E` becomes `~`), and uppercase the hex digits of any percent-encoding left in place (`%2f` becomes `%2F`)
- Remove a single trailing dot from the host (`example.com.` becomes `example.com`)
- Canonicalize an empty path to `/` when an authority is present, for schemes with hierarchical path semantics (`http`, `https`, `ftp`, `ws`, `wss`) — `http://example.com` becomes `http://example.com/`
- Drop a bare trailing `?` or `#` that carries no content

## What is opt-in, and why it is not on by default

These can change what two URLs mean relative to each other, so you turn them on explicitly through `NormalizeUrlOptions`:

| Option | Effect |
|---|---|
| `SortQueryParameters` | Reorders query parameters by key, then value. Off because parameter order occasionally matters to a server. |
| `StripTrailingSlash` | Removes one trailing `/` from the path, unless the path is just `/`. Off because `/a/` and `/a` are not guaranteed to be the same resource. |
| `QueryParametersToRemove` | Strips named query parameters entirely. Off (empty) by default; pass `NormalizeUrlOptions.UtmTrackingParameters` for the common `utm_*` family. |
| `QueryParameterMatcher` | An opt-in predicate that strips query parameters by matching their name, unioned with `QueryParametersToRemove`. Off (`null`) by default. Use it for open-ended tracking families a fixed list cannot enumerate (`utm_*`, `fbclid`, `gclid`, ...). |
| `ForceHttps` | Rewrites `http` to `https`. Off because it changes which endpoint the URL addresses. |
| `StripFragment` | Removes the fragment entirely, regardless of content. Off because single-page apps route on the fragment. |
| `StripWwwPrefix` | Removes one leading `www.` label from the host. Off because `www.example.com` and `example.com` are not guaranteed to be the same host. |

## Zero dependencies, AOT-friendly

No runtime NuGet packages. Parsing runs on a compiled, source-generated `Regex` and plain string operations, so there is no reflection-heavy path to trip up trimming or Native AOT. Targets `net8.0`.

## License

MIT. See [LICENSE](LICENSE).
