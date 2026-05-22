# Changelog

All notable changes to **MultiTenantAuth.AspNetCore** are documented here.  
This project follows [Semantic Versioning](https://semver.org/) and
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

---

## [Unreleased]

---

## [1.1.0] — 2026-05-22

### Security

This release addresses all findings from the comprehensive OWASP security audit performed on v1.0.0.

- **VULN-01/02 (High)** — Tenant context now always cleared via `try/finally` in `MultiTenantAuthMiddleware`, even when validation fails or an unhandled exception is thrown, preventing context leakage across requests.
- **VULN-03 (Medium)** — `SubdomainTenantResolver` now rejects IPv4 and IPv6 addresses via `IPAddress.TryParse`; hosts that are raw IPs no longer produce a spurious tenant slug (e.g., `"192"`).
- **VULN-04 (Medium)** — `DefaultTenantValidator` now sets `WWW-Authenticate: Bearer` on every 401 response, conforming to RFC 7235 §4.1.
- **VULN-05 (Medium)** — Added `MinTenantIdLength` option (default `2`). `TenantFormatValidator` enforces the minimum in addition to the existing maximum, and `MultiTenantAuthOptionsValidator` rejects invalid combinations at startup. `IsValidSlug` now also enforces `MinTenantIdLength` for consistency.
- **VULN-06 (Medium)** — Tenant enumeration via differential response timing/codes is an architectural concern that cannot be eliminated entirely in a library. It is documented in [SECURITY.md](SECURITY.md) with mitigation guidance for consumers.
- **VULN-07 (Medium)** — `MultiTenantAuthOptionsValidator` now verifies that `CustomResolverType` implements `ITenantResolver` and `CustomValidatorType` implements `ITenantValidator`, failing fast at application startup with a descriptive error.
- **VULN-08 (Medium)** — Added `NormalizeTenantIdToLowercase` option (default `true`). Resolved tenant IDs are normalised to lowercase in the middleware before being stored in `TenantContext`, preventing case-confusion attacks on case-sensitive downstream systems.
- **VULN-09 (Low-Medium)** — `MultiTenantAuthMiddleware` now logs `LogWarning` at construction time when `RequireAuthenticatedUser = false`, `RequireResolvedTenant = false`, or `EnableQueryStringResolution = true` are set, alerting operators to insecure configurations in any environment.
- **VULN-10 (Low)** — CD workflow `permissions: contents: write` is now scoped to the `publish` job only. The workflow default is `contents: read`.

### Added

- `MultiTenantAuthOptions.MinTenantIdLength` property (default `2`).
- `MultiTenantAuthOptions.NormalizeTenantIdToLowercase` property (default `true`).
- `TenantConstants.DefaultMinTenantIdLength` constant (`2`).
- `MultiTenantAuthOptionsValidator` validates `TenantQueryStringName` for null/whitespace.
- CD workflow uses inline OIDC shell script for NuGet Trusted Publishing — no long-lived `NUGET_API_KEY` secret stored in GitHub. Third-party actions pinned to commit SHAs for supply-chain safety.

### Changed

- `MultiTenantAuthOptionsValidator` now validates `MinTenantIdLength > 0`, `MinTenantIdLength <= MaxTenantIdLength`, and `TenantQueryStringName` for null/whitespace.
- `TenantFormatValidator` consolidates the default regex pattern constant via `TenantConstants.DefaultAllowedTenantPattern` (single source of truth).
- `IsValidSlug` now also enforces `MinTenantIdLength` — slug and ID validators are consistent.
- `TenantContextAccessor` fully clears the `AsyncLocal` slot on `Current = null`, allowing the `TenantContextHolder` wrapper to be garbage-collected.
- `DefaultTenantValidator.UserBelongsToTenant` removes the redundant `FindFirstValue` pre-check; `FindAll` is sufficient.
- `SubdomainTenantResolver` rejects IP-address hosts instead of extracting the first octet as a tenant slug.
- CD workflow publish job requires `id-token: write` permission for OIDC exchange.
- All third-party GitHub Actions in CD workflow pinned to commit SHAs.

### Fixed

- Single-character tenant IDs are now correctly rejected by default (respects `MinTenantIdLength = 2`).

---

## [1.0.0] — 2026-04-01

### Added

- Initial release of `MultiTenantAuth.AspNetCore`.
- Multi-tenant middleware (`MultiTenantAuthMiddleware`) for ASP.NET Core 8+.
- Five built-in tenant resolvers: `HeaderTenantResolver`, `RouteValueTenantResolver`, `SubdomainTenantResolver`, `ClaimTenantResolver`, `QueryStringTenantResolver`.
- `DefaultTenantValidator` with primary-claim, multi-claim, and comma-separated `allowed_tenants` support.
- `ITenantContextAccessor` backed by `AsyncLocal<T>` for safe per-request tenant context.
- `IValidateOptions<MultiTenantAuthOptions>` startup validation via `MultiTenantAuthOptionsValidator`.
- `TenantFormatValidator` with configurable regex pattern and 50 ms ReDoS protection.
- `AddMultiTenantAuth()` and `UseMultiTenantAuth()` extension methods.
- Full XML documentation and SourceLink support.
- GitHub Actions CI (build / test / vulnerability scan) and CD (pack / publish to NuGet.org).
- CodeQL weekly analysis and Dependabot for both NuGet and Actions.
- Three sample applications: `ApiOnlySample`, `MinimalApiSample`, `SubdomainTenantSample`.

---

[Unreleased]: https://github.com/DNVerma88/multi-tenant-auth-aspnetcore/compare/v1.1.0...HEAD
[1.1.0]: https://github.com/DNVerma88/multi-tenant-auth-aspnetcore/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/DNVerma88/multi-tenant-auth-aspnetcore/releases/tag/v1.0.0
