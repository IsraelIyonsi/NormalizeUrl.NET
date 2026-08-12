# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-08-12

### Added

- `UrlNormalizer` static API: `Normalize(string, NormalizeUrlOptions?)`, `TryNormalize(string?, out string?, NormalizeUrlOptions?)`, and `AreEquivalent(string, string, NormalizeUrlOptions?)`.
- Safe-by-default normalization profile: lowercase scheme and host, default-port removal per scheme, RFC 3986 section 5.2.4 dot-segment resolution (applied only to hierarchical paths, never to opaque non-authority paths such as `mailto:`/`urn:`), RFC 3986 section 6.2.2.2 percent-encoding normalization (decode unreserved octets, uppercase remaining hex), trailing-dot removal on the host, RFC 3986 section 6.2.3 empty-path-to-`/` canonicalization for hierarchical schemes, and dropping an empty query or fragment marker.
- Opt-in transforms via `NormalizeUrlOptions`: `SortQueryParameters`, `StripTrailingSlash`, `StripFragment`, `ForceHttps`, `StripWwwPrefix`, and `QueryParametersToRemove` (with `NormalizeUrlOptions.UtmTrackingParameters` as a ready-made UTM parameter list).
- RFC 3986 Appendix B generic-syntax parsing, including userinfo, IPv6 host literals, and explicit-port handling.
- Zero runtime dependencies; built entirely on in-box BCL types.
- SourceLink (GitHub), deterministic CI builds and `.snupkg` symbol packages.
